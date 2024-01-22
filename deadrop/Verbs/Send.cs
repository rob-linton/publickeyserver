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
	[Option('i', "input", Required = true, HelpText = "Package file to send")]
    public required string File { get; set; }
}

class Send 
{
	public static async Task<int> Execute(SendOptions opts)
	{
		Misc.LogHeader();
		Misc.LogLine($"Sending...");
		Misc.LogLine($"Input: {opts.File}");
		Misc.LogLine($"");
		
		// get the envelop from the package file
		Envelope envelope = Envelope.LoadFromFile(opts.File);

		// now get the "from" alias
		string fromAlias = envelope.From;
		Misc.LogLine($"Sending from {fromAlias}...\n");

		// validate the from alias
		string fromDomain = Misc.GetDomain(opts, fromAlias);
		(bool fromValid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, fromAlias, opts);
		if (!fromValid)
		{
			Misc.LogError(opts, "Invalid from alias", fromAlias);
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
				string toDomain = Misc.GetDomain(opts, toAlias);
				(bool toValid, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, toAlias, opts);
				if (!toValid)
				{
					Misc.LogError(opts, "Invalid to alias", toAlias);
					return 1;
				}

				if (!fromFingerprint.SequenceEqual(toFingerprint))
				{
					Misc.LogLine(opts, $"Aliases do not share the same root certificate {fromAlias} -> {toAlias}");
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
					Misc.LogError(opts, $"Error: could not read private key for {fromAlias}", ex.Message);

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
				var result = await HttpHelper.PostFile($"https://{toDomain}/package/{toAlias}?sender={fromAlias}&timestamp={unixTimestamp}&signature={base64Signature}", opts, opts.File);

				// show result ok
				Misc.LogLine($"\n{result}\n");		
			}
			catch (Exception ex)
			{
				Misc.LogError(opts, "Unable to send package", ex.Message);
			}
		}

		return 0;
	}
}
