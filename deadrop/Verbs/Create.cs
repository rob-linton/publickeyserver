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

[Verb("create", HelpText = "Create an alias.")]
public class CreateOptions : Options
{
  	
}
class Create 
{
	public static async Task<int> Execute(CreateOptions opts)
	{
		try
		{
			Misc.LogHeader();
			Misc.LogLine($"Creating...");

			// get the domain for requests
			string domain = Misc.GetDomain(opts, "");

			Misc.LogLine($"Domain: {domain}");
			Misc.LogLine($"");
			Misc.LogLine(opts, $"- Requesting alias from: {domain}\n");

			//
			// create the public/private key pair using bouncy castle
			//
			Misc.LogLine(opts, "- Generating public/private key pair...");
			AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.GenerateKeyPair(2048);

			//
			// save your public and private key pair
			//
			var data = new Dictionary<string, string>();
			string alias = String.Empty;
			string publicKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Public);

			if (publicKeyPem == null)
			{
				Misc.LogError(opts, "Could not read public key from new key pair");
				return 1;
			}

			// save it as PEM format
			data["key"] = publicKeyPem;
			string json = JsonSerializer.Serialize(data);

			Misc.LogLine(opts, "- Sending public key...");

			var result = await HttpHelper.Post($"https://{domain}/simpleenroll", json, opts);

			var j = JsonSerializer.Deserialize<SimpleEnrollResult>(result);
			alias = j?.Alias ?? string.Empty;
			if (String.IsNullOrEmpty(j?.Certificate))
			{
				Misc.LogError(opts, "No certificate returned from simpleenroll");
				return 1;
			}
		
			//Storage.StoreCert(alias, certificate);

			string privateKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);
			if (privateKeyPem == null)
			{
				Misc.LogError(opts, "Could not read private key");
				return 1;
			}

			Storage.StorePrivateKey(alias, privateKeyPem, opts.Password);

			(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, opts);

			Misc.LogLine($"\nAlias {alias} created\n");
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Unable to create alias", ex.Message);
			return 1;
		}

		return 0;
	}
}