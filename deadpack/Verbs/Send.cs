#pragma warning disable 1998
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace deadrop.Verbs;

[Verb("send", HelpText = "Send your package to another alias")]
public class SendOptions : Options
{
	[Option('i', "input", HelpText = "Package file to send")]
    public string? File { get; set; }
}

class Send 
{
	public static async Task<int> Execute(SendOptions opts)
	{
		Misc.LogHeader();
		Misc.LogLine($"Sending...");
		if (!String.IsNullOrEmpty(opts.File))
			Misc.LogLine($"Input: {opts.File}");
		Misc.LogLine($"");

		if (!String.IsNullOrEmpty(opts.File))
		{
			return await ExecuteInternal(opts, opts.File);
		}
		else
		{
			var deadpacks = Storage.ListDeadPacks("","outbox", Globals.Password);
			foreach (var file in deadpacks)
			{
				await ExecuteInternal(opts, file.Filename);
			}
			return 0;
		}

	}

	public static async Task<int> ExecuteInternal(SendOptions opts, string file)
	{

		if (String.IsNullOrEmpty(opts.Password))
			opts.Password = Misc.GetPassword();

		// get the envelop from the package file
		Envelope envelope = Envelope.LoadFromFile(file);

		// now get the "from" alias
		string fromAlias = envelope.From;
		Misc.LogLine($"Sending from {fromAlias}...\n");

		// now load the root fingerprint from a file
		string rootFingerprintFromFileString = Storage.GetPrivateKey($"{fromAlias}.root", opts.Password);
		byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

		// validate the from alias
		string fromDomain = Misc.GetDomain(fromAlias);
		(bool fromValid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, fromAlias, "");
		
		// verify the fingerprint
		if (fromFingerprint.SequenceEqual(rootFingerprintFromFile))
			Misc.LogCheckMark($"Root fingerprint matches");
		else
			Misc.LogLine($"Invalid: Root fingerprint does not match");
		
		if (!fromValid)
		{
			Misc.LogError("Invalid from alias", fromAlias);
			return 1;
		}
		

		// get the to aliases
		List<string> toAliases = envelope.To.Select(r => r.Alias).ToList();

		// loop through each toAlias
		foreach (string toAlias in toAliases)
		{
			try
			{
				Misc.LogLine($"\nProcessing {toAlias}...\n");

				// valiadate the to alias
				string toDomain = Misc.GetDomain(toAlias);
				(bool toValid, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, toAlias, "");
				
				// verify the fingerprint
				if (toFingerprint.SequenceEqual(rootFingerprintFromFile))
					Misc.LogCheckMark($"Root fingerprint matches");
				else
					Misc.LogLine($"Invalid: Root fingerprint does not match");

				if (!toValid)
				{
					Misc.LogError("Invalid to alias", toAlias);
					return 1;
				}

				if (!fromFingerprint.SequenceEqual(toFingerprint))
				{
					Misc.LogLine($"Aliases do not share the same root certificate {fromAlias} -> {toAlias}");
					return 1;
				}
				Misc.LogCheckMark($"Shared root certificate {fromAlias} -> {toAlias}");

				// send the file to the server
				// UploadPackage(string sender, string timestamp, string signature)

				// get a unix timestamp in seconds UTC
				long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

				// get the from private key
				AsymmetricCipherKeyPair privateKey;
				try
				{
					string privateKeyPem = Storage.GetPrivateKey($"{fromAlias}.rsa", opts.Password);
					privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
				}
				catch (Exception ex)
				{
					Misc.LogError($"Error: could not read private key for {fromAlias}", ex.Message);

					// application exit
					return 1;
				}

				
				// signiture = sender + recipient + timestamp + origin

				// sign it with the sender
				string domain = Misc.GetDomainFromAlias(fromAlias);
				byte[] data = $"{fromAlias}{toAlias}{unixTimestamp.ToString()}{domain}".ToBytes();
				byte[] signature = BouncyCastleHelper.SignData(data, privateKey.Private);
				string base64Signature = Convert.ToBase64String(signature);

				Misc.LogLine($"\nSending deadpack to {toAlias}...\n");
				// UploadPackage(string sender, string recipient, string timestamp, string signature)
				var result = await HttpHelper.PostFile($"https://{toDomain}/package/{toAlias}?sender={fromAlias}&timestamp={unixTimestamp}&signature={base64Signature}", file);

				// show result ok
				Misc.LogLine($"\n{result}\n");	

				// now move it to the sent folder
				string sentFolder = Storage.GetDeadPackDirectorySent(fromAlias, "");
				// get the filename from opt.file
				string filename = Path.GetFileName(file);

				File.Move(file, Path.Combine(sentFolder, filename));	
			}
			catch (Exception ex)
			{
				Misc.LogError("Unable to send package", ex.Message);
			}
		}

		return 0;
	}
}
