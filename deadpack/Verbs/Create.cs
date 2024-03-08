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
  	[Option('e', "email", HelpText = "Optional email address to associate with alias")]
    public string? Email { get; set; }
	
	[Option('t', "token", HelpText = "Email validation token")]
    public string? Token { get; set; }
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
			string domain = Misc.GetDomain("");

			Misc.LogLine($"Domain: {domain}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(Globals.Password))
			Globals.Password = Misc.GetPassword();
			
			Misc.LogLine($"- Requesting alias from: {domain}\n");

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
			var data = new CustomExtensionData{
				KyberKey = Convert.ToBase64String(KyberPublicKey),
				DilithiumKey = Convert.ToBase64String(DilithiumPublicKey),
				Email = opts.Email ?? string.Empty,
				Token = opts.Token ?? string.Empty
			};
			
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
				Misc.LogError("Could not read public key from new key pair");
				return 1;
			}

			// save it as PEM format
			payload["key"] = publicKeyPem;
			payload["data"] = jsondataBase64;
			string json = JsonSerializer.Serialize(payload);

			Misc.LogLine("- Sending public key...");

			var result = await HttpHelper.Post($"https://{domain}/simpleenroll", json);

			var j = JsonSerializer.Deserialize<SimpleEnrollResult>(result);
			alias = j?.Alias ?? string.Empty;
			if (String.IsNullOrEmpty(j?.Certificate))
			{
				Misc.LogError("No certificate returned from simpleenroll");
				return 1;
			}
		
			
			// store RSA private key
			string privateKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);
			if (privateKeyPem == null)
			{
				Misc.LogError("Could not read private key");
				return 1;
			}
			Storage.StorePrivateKey($"{alias}.rsa", privateKeyPem, Globals.Password);

			// store kyber private key
			string kyberPrivateKeyPem = Convert.ToBase64String(KyberPrivatyeKey);
			Storage.StorePrivateKey($"{alias}.kyber", kyberPrivateKeyPem, Globals.Password);

			// store dilithium private key
			string dilithiumPrivateKeyPem = Convert.ToBase64String(DilithiumPrivateKey);
			Storage.StorePrivateKey($"{alias}.dilithium", dilithiumPrivateKeyPem, Globals.Password);

			(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, "");

			if (!valid)
			{
				Misc.LogError("Unable to verify alias");
				return 1;
			}

			// now save the root fingerprint to a file
			string rootFingerprintHex = Convert.ToBase64String(rootFingerprint);
			Storage.StorePrivateKey($"{alias}.root", rootFingerprintHex, Globals.Password);

			Misc.LogCheckMark("Root certificate fingerprint saved");

			Misc.LogLine($"\nAlias {alias} created\n");
		}
		catch (Exception ex)
		{
			Misc.LogError("Unable to create alias", ex.Message);
			return 1;
		}

		return 0;
	}
}