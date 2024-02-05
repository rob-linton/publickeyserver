#pragma warning disable 1998
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.Utilities;

namespace deadrop.Verbs;

[Verb("receive", HelpText = "Receive a package")]
public class ReceiveOptions : Options
{
	//[Option('i', "input", Required = true, HelpText = "Package key to Receive")]
    //public required string Key { get; set; }

	[Option('a', "alias", Required = true, HelpText = "Alias to use")]
    public required string Alias { get; set; }

	[Option('f', "force", Default = false, HelpText = "Download all deadpacks without prompting")]
	public required bool Force { get; set; } = false;

	[Option('s', "seconds", Default = 0, HelpText = "Check for deadpacks every x seconds")]
	public required int Interval { get; set; } = 0;

}

class Receive 
{
	public static async Task<int> Execute(ReceiveOptions opts)
	{
		// get a tmp output directory
		string tmpOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tmpOutputDirectory);
		

		try
		{
			Misc.LogHeader();
			Misc.LogLine($"Receiving packages...");
			Misc.LogLine($"Alias: {opts.Alias}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(opts.Password))
			opts.Password = Misc.GetPassword();

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{opts.Alias}.root", opts.Password);
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// verify the alias
			string toDomain = Misc.GetDomain(opts, opts.Alias);
			(bool fromValid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, opts.Alias, "", opts);
			
			// verify the fingerprint
			if (fromFingerprint.SequenceEqual(rootFingerprintFromFile))
				Misc.LogCheckMark($"Root fingerprint matches", opts);
			else
				Misc.LogLine($"Invalid: Root fingerprint does not match");

			if (!fromValid)
			{
				Misc.LogError(opts, "Invalid alias", opts.Alias);
				return 1;
			}

			// Receive a unix timestamp in seconds UTC
			long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			// get the private key
			AsymmetricCipherKeyPair privateKey;
			try
			{
				string privateKeyPem = Storage.GetPrivateKey($"{opts.Alias}.rsa", opts.Password);
				privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
			}
			catch (Exception ex)
			{
				Misc.LogError(opts, $"Error: could not read private key for {opts.Alias}", ex.Message);

				// application exit
				return 1;
			}

			// get the kyber private key
			AsymmetricKeyParameter kyberPrivateKey;
			try
			{
				string privateKeyKyber = Storage.GetPrivateKey($"{opts.Alias}.kyber", opts.Password);
				byte[] privateKeyKyberBytes = Convert.FromBase64String(privateKeyKyber);
				kyberPrivateKey = BouncyCastleQuantumHelper.WriteKyberPrivateKey(privateKeyKyberBytes);
			}
			catch (Exception ex)
			{
				Misc.LogError(opts, $"Error: could not read kyber private key for {opts.Alias}", ex.Message);

				// application exit
				return 1;
			}
			
			// if we are checking on an interval force downloading
			if (opts.Interval > 0)
			{
				opts.Force = true;
			}

			bool first = true;
			while (true)
			{
				// sign it with the sender
				string domain = Misc.GetDomainFromAlias(opts.Alias);
				byte[] data = $"{opts.Alias}{unixTimestamp.ToString()}{domain}".ToBytes();
				byte[] signature = BouncyCastleHelper.SignData(data, privateKey.Private);
				string base64Signature = Convert.ToBase64String(signature);


				// get a list of deadpacks from the server
				string result = await HttpHelper.Get($"https://{toDomain}/list/{opts.Alias}?timestamp={unixTimestamp}&signature={base64Signature}", opts);

				// parse the json
				ListResult files = JsonSerializer.Deserialize<ListResult>(result) ?? throw new Exception($"Could not parse json: {result}");

				// get the number of files
				int receiveCount = files.Count;
				long receiveSize = files.Size;


				// sound the bell if there are deadpacks waiting
				if (opts.Interval == 0 || receiveCount > 0)
					Misc.LogLine($"\n{receiveCount} deadpack(s) waiting of {Misc.FormatBytes(receiveSize)} total size\n");
				
				if (opts.Interval > 0 && receiveCount > 0)
				{
					if (receiveCount == 1)
					{
						// ring 1 bell
						Console.Beep();
					}
					else if (receiveCount == 2)
					{
						// ring 2 bells
						Console.Beep();
						Console.Beep();
					}
					else if (receiveCount > 2)
					{
						// ring 3 bells
						Console.Beep();
						Console.Beep();
						Console.Beep();
					}
				}

				if (opts.Force)
				{
					if (opts.Interval == 0)
						Misc.LogLine($"Downloading all deadpacks without prompting");
				}
				else
				{
					Misc.LogLine($"Downloading deadpacks...");
				}

				// loop through them app and print out the size, date and nuber to the console
				int i = 1;
				foreach (ListFile file in files.Files)
				{
					string fullKey = file.Key;
					long size = file.Size;
					DateTime modified = file.LastModified;
					string date = modified.ToString("yyyy-MM-dd.HH-mm-ss");

					// get the last bit of a/a/a
					string[] parts = fullKey.Split("/");
					string key = parts[parts.Length - 1];
					Misc.LogList(i.ToString(), key, Misc.FormatBytes(size), date);
					//Misc.LogLine($"{i}. {key} ({Misc.FormatBytes(size)}) {date}");
					i++;
				}

				// if no deadpacks then exit
				if (receiveCount == 0 && opts.Interval == 0)
				{
					Misc.LogLine($"No deadpacks waiting\n");
					return 0;
				}

				// if force then skip asking
				ListResult selectedFiles;
				if (opts.Force)
				{
					selectedFiles = files;
				}
				else
				{
					// ask for the number to download
					Misc.LogLine($"\nWhich deadpack do you want to receive? ([1-{receiveCount}] or [a] for all, or [q] to quit)");
					string input = Console.ReadLine() ?? throw new Exception("Could not read input");

					// if a then download all
					if (input.ToLower() == "a")
					{
						selectedFiles = files;
					}
					else if (input.ToLower() == "q")
					{
						return 0;
					}
					else
					{
						// get the number
						int number = Convert.ToInt32(input);

						// check the number is valid
						if (number < 1 || number > receiveCount)
						{
							Misc.LogError(opts, "Invalid number", input);
							return 1;
						}

						// get the file
						ListFile file = files.Files[number - 1];

						// create a new listresult with just the one file
						selectedFiles = new ListResult()
						{
							Files = new List<ListFile>() { file },
							Count = 1,
							Size = file.Size
						};
					}
				}

				// loop through each package and receive it
				foreach (ListFile file in selectedFiles.Files)
				{
					string fullKey = file.Key;
					long size = file.Size;
					DateTime modified = file.LastModified;

					// get the last bit of a/a/a
					string[] parts = fullKey.Split("/");
					string key = parts[parts.Length - 1];

					string tmpOutputName = Path.Combine(tmpOutputDirectory, key);

					Misc.LogLine($"\nGetting deadpack {key}...");
					await HttpHelper.GetFile($"https://{toDomain}/package/{opts.Alias}/{key}?timestamp={unixTimestamp}&signature={base64Signature}", opts, tmpOutputName);


					// get the envelope from the file
					Envelope envelope = Envelope.LoadFromFile(tmpOutputName);
					Recipient recipient = envelope.To.FirstOrDefault(r => r.Alias == opts.Alias) ?? throw new Exception($"Could not find recipient {opts.Alias} in package");
					if (recipient == null)
					{
						Misc.LogError(opts, $"Could not find recipient {opts.Alias} in package");
						return 1;
					}
					string kyberKeyString = recipient.KyberKey;
					byte[] kyberKey = Convert.FromBase64String(kyberKeyString);

					// get the manifest from the file
// TODO					// *** TO DO Manifest manifest = Manifest.LoadFromFile(tmpOutputName, privateKey, opts.Alias, kyberKey);
					
					// get a compact string showing the date and time
					long timestamp = envelope.Created;

					// convert the unix timestamp to a readable datetime
					DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
					dt = dt.AddSeconds(Convert.ToDouble(timestamp));

					// convert utc date to localtime date
					dt = dt.ToLocalTime();

					string date = dt.ToString("yyyy-MM-dd HH-mm-ss");
// TODO					string destFilename = $"{date} {manifest.Name}";
					string destFilename = $"{date} {key}";  // <<-- TDO REMOVE

					// check if the file already exists, and if it does then add a (1) to the end, loop until the file does not exist
					int ii = 1;
					while (File.Exists(destFilename))
					{
						//destFilename = $"{date} {manifest.Name} ({ii})";
						destFilename = $"{date} {key} ({ii})";  // <<-- TDO REMOVE

						ii++;
					}

					// now move the deadpack to the destination filename
					File.Move(tmpOutputName, destFilename);

					// show result ok
					Misc.LogLine($"\n{destFilename} retrieved OK\n");

				}


				if (opts.Interval == 0)
				{
					return 0;
				}
				else
				{	if (first)
					{
						Misc.LogLine($"\nChecking every {opts.Interval} seconds...<crtl-c> to quit");
						first = false;
					}
					await Task.Delay(opts.Interval * 1000);
				}
			}
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Error receiving package", ex.Message);
			return 1;
		}
		finally
		{

			// clean up
			if (Directory.Exists(tmpOutputDirectory))
				Directory.Delete(tmpOutputDirectory, true);

		}
		
	}
}
