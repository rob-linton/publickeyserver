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

[Verb("receive", HelpText = "Receive a package")]
public class ReceiveOptions : Options
{
	[Option('i', "input", Required = true, HelpText = "Package key to Receive")]
    public required string Key { get; set; }

	[Option('a', "alias", Required = true, HelpText = "Alias to use")]
    public required string Alias { get; set; }

	[Option('o', "output", Default = "package", HelpText = "Output package file")]
    public string Output { get; set; } = "My.deadpack";	
}

class Receive 
{
	public static async Task<int> Execute(ReceiveOptions opts)
	{
		try
		{
			Misc.LogHeader();
			Misc.LogLine($"Receiveting package...");
			Misc.LogLine($"Input: {opts.Key}");
			Misc.LogLine($"Alias: {opts.Alias}");
			Misc.LogLine($"");

			// verify the alias
			string toDomain = Misc.GetDomain(opts, opts.Alias);
			(bool fromValid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, opts.Alias, opts);
			if (!fromValid)
			{
				Misc.LogError(opts, "Invalid alias", opts.Alias);
				return 1;
			}

			// Receive a unix timestamp in seconds UTC
			long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

			// Receive the from private key
			AsymmetricCipherKeyPair privateKey;
			try
			{
				string privateKeyPem = Storage.GetPrivateKey(opts.Alias, opts.Password);
				privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
			}
			catch (Exception ex)
			{
				Misc.LogError(opts, $"Error: could not read private key for {opts.Alias}", ex.Message);

				// application exit
				return 1;
			}


			// signiture = sender + recipient + timestamp + origin

			// sign it with the sender
			string domain = Misc.GetDomainFromAlias(opts.Alias);
			byte[] data = $"{opts.Alias}{unixTimestamp.ToString()}{domain}".ToBytes();
			byte[] signature = BouncyCastleHelper.SignData(data, privateKey.Private);
			string base64Signature = Convert.ToBase64String(signature);

			Misc.LogLine(opts, $"Getting deadpack from {opts.Alias}...");
			await HttpHelper.GetFile($"https://{toDomain}/package/{opts.Alias}/{opts.Key}?timestamp={unixTimestamp}&signature={base64Signature}", opts, opts.Output);

			// show result ok
			Misc.LogLine(opts, $"\n{opts.Output} retrieved OK\n");

			return 0;
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Error receiving package", ex.Message);
			return 1;
		}
	}
}
