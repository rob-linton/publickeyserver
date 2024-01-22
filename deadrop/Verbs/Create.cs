#pragma warning disable 1998
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.Security;

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
			Misc.LogCheckMark("Generated RSA key pair");
			AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.GenerateKeyPair(2048);

			//
			// generate a Kyber keypair using bouncy castle
			//
			Misc.LogCheckMark("Generated Quantum Kyber key pair");
			AsymmetricCipherKeyPair KyberKeyPair = BouncyCastleQuantumHelper.GenerateKyberKeyPair();
			(byte[] KyberPublicKey, byte[] KyberPrivatyeKey) = BouncyCastleQuantumHelper.ReadKyberKeyPair(KyberKeyPair);

			//
			// generate a Dilithium keypair using bouncy castle
			//
			Misc.LogCheckMark("Generated Quantum Dilithium key pair");
			AsymmetricCipherKeyPair DilithiumKeyPair = BouncyCastleQuantumHelper.GenerateDilithiumKeyPair();
			(byte[] DilithiumPublicKey, byte[] DilithiumPrivateKey) = BouncyCastleQuantumHelper.ReadDilithiumKeyPair(DilithiumKeyPair);

			//
			// now create the data payload to send to the server
			//
			var data = new Dictionary<string, string>();
			
			data["kyber_key"] = Convert.ToBase64String(KyberPublicKey);
			data["dilithium_key"] = Convert.ToBase64String(DilithiumPublicKey);

			string jsondata = JsonSerializer.Serialize(data);
			byte[] jsondataBytes = Encoding.UTF8.GetBytes(jsondata);
			string jsondataBase64 = Convert.ToBase64String(jsondataBytes);

			//
			// save your public and private RSA key pair
			//
			var payload = new Dictionary<string, string>();
			string alias = String.Empty;
			string publicKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Public);

			if (publicKeyPem == null)
			{
				Misc.LogError(opts, "Could not read public key from new key pair");
				return 1;
			}

			// save it as PEM format
			payload["key"] = publicKeyPem;
			payload["data"] = jsondataBase64;
			string json = JsonSerializer.Serialize(payload);

			Misc.LogLine(opts, "- Sending public key...");

			var result = await HttpHelper.Post($"https://{domain}/simpleenroll", json, opts);

			var j = JsonSerializer.Deserialize<SimpleEnrollResult>(result);
			alias = j?.Alias ?? string.Empty;
			if (String.IsNullOrEmpty(j?.Certificate))
			{
				Misc.LogError(opts, "No certificate returned from simpleenroll");
				return 1;
			}
		
			
			// store RSA private key
			string privateKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);
			if (privateKeyPem == null)
			{
				Misc.LogError(opts, "Could not read private key");
				return 1;
			}
			Storage.StorePrivateKey($"{alias}.rsa", privateKeyPem, opts.Password);

			// store kyber private key
			string kyberPrivateKeyPem = Convert.ToBase64String(KyberPrivatyeKey);
			Storage.StorePrivateKey($"{alias}.kyber", kyberPrivateKeyPem, opts.Password);

			// store dilithium private key
			string dilithiumPrivateKeyPem = Convert.ToBase64String(DilithiumPrivateKey);
			Storage.StorePrivateKey($"{alias}.dilithium", dilithiumPrivateKeyPem, opts.Password);

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