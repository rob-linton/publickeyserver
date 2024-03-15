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

	[Option('a', "alias", HelpText = "Alias to use")]
    public string? Alias { get; set; }

	[Option('f', "force", Default = false, HelpText = "Download all deadpacks without prompting")]
	public required bool Force { get; set; } = false;

	[Option('s', "seconds", Default = 0, HelpText = "Check for deadpacks every x seconds")]
	public required int Interval { get; set; } = 0;

}

class Receive 
{
	public static async Task<int> Execute(ReceiveOptions opts, IProgress<StatusUpdate> progress = null)
	{
		Misc.LogHeader();
		Misc.LogLine($"Receiving packages...");
		Misc.LogLine($"Alias: {opts.Alias}");
		Misc.LogLine($"");

		if (String.IsNullOrEmpty(Globals.Password))
		Globals.Password = Misc.GetPassword();
		
			
		//Misc.LogLine($"Recipient Alias: {alias}");

		if (!String.IsNullOrEmpty(opts.Alias))
		{
			return await ExecuteInternal(opts, opts.Alias, progress);
		}
		else
		{
			// loop through all of the aliases
			foreach (var a in Storage.GetAliases())
			{
				string alias = a.Name;
				Misc.LogLine($"\nChecking alias {alias}...");
				int result = await ExecuteInternal(opts, alias, progress);
			}
			return 0;
		}
	}
	public static async Task<int> ExecuteInternal(ReceiveOptions opts, string alias, IProgress<StatusUpdate> progress = null)
	{
		// get a tmp output directory
		string tmpOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
		Directory.CreateDirectory(tmpOutputDirectory);
		

		try
		{

			// replace emails with actual aliases
			CertResult cert = await EmailHelper.GetAliasOrEmailFromServer(alias, false);

			alias = cert.Alias;

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{alias}.root", Globals.Password);
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// verify the alias
			string toDomain = Misc.GetDomain(alias);
			(bool fromValid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, alias, "");
			
			// verify the fingerprint
			if (fromFingerprint.SequenceEqual(rootFingerprintFromFile))
				Misc.LogCheckMark($"Root fingerprint matches");
			else
				Misc.LogLine($"Invalid: Root fingerprint does not match");

			if (!fromValid)
			{
				Misc.LogError("Invalid alias", alias);
				return 1;
			}

			// Receive a unix timestamp in seconds UTC
			long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			// get the private key
			AsymmetricCipherKeyPair privateKey;
			try
			{
				string privateKeyPem = Storage.GetPrivateKey($"{alias}.rsa", Globals.Password);
				privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
			}
			catch (Exception ex)
			{
				Misc.LogError($"Error: could not read private key for {alias}", ex.Message);

				// application exit
				return 1;
			}

			// get the kyber private key
			AsymmetricKeyParameter kyberPrivateKey;
			try
			{
				string privateKeyKyber = Storage.GetPrivateKey($"{alias}.kyber", Globals.Password);
				byte[] privateKeyKyberBytes = Convert.FromBase64String(privateKeyKyber);
				kyberPrivateKey = BouncyCastleQuantumHelper.WriteKyberPrivateKey(privateKeyKyberBytes);
			}
			catch (Exception ex)
			{
				Misc.LogError($"Error: could not read kyber private key for {alias}", ex.Message);

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
				string domain = Misc.GetDomainFromAlias(alias);
				byte[] data = $"{alias}{unixTimestamp.ToString()}{domain}".ToBytes();
				byte[] signature = BouncyCastleHelper.SignData(data, privateKey.Private);
				string base64Signature = Convert.ToBase64String(signature);


				// get a list of deadpacks from the server
				string result = await HttpHelper.Get($"https://{toDomain}/list/{alias}?timestamp={unixTimestamp}&signature={base64Signature}");

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
					string date = modified.ToString("dd-MMM-yyyy hh.mmtt");

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
							Misc.LogError("Invalid number", input);
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
					await HttpHelper.GetFile($"https://{toDomain}/package/{alias}/{key}?timestamp={unixTimestamp}&signature={base64Signature}", opts, tmpOutputName);


					// get the envelope from the file
					Envelope envelope = Envelope.LoadFromFile(tmpOutputName);
					Recipient recipient = envelope.To.FirstOrDefault(r => r.Alias == alias) ?? throw new Exception($"Could not find recipient {alias} in package");
					if (recipient == null)
					{
						Misc.LogError($"Could not find recipient {alias} in package");
						return 1;
					}
					string kyberKeyString = recipient.KyberKey;
					byte[] kyberKey = Convert.FromBase64String(kyberKeyString);

					// get the manifest from the file
					//Manifest manifest = Manifest.LoadFromFile(tmpOutputName, privateKey, alias, Globals.Password);
					
					// get a compact string showing the date and time
					long timestamp = envelope.Created;
					string filename = $"{timestamp}-{Guid.NewGuid()}.deadpack";
					
					string destFilename = Storage.GetDeadPackDirectoryInbox(alias, filename);

					// now move the deadpack to the destination filename
					File.Move(tmpOutputName, destFilename);

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
			try
			{
				progress?.Report(new StatusUpdate { Status = ex.Message });
				await System.Threading.Tasks.Task.Delay(100); // DO NOT REMOVE-REQUIRED FOR UX
			}
			catch { }

			Misc.LogError("Error receiving package", ex.Message);
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
