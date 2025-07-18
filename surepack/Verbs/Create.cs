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

namespace suredrop.Verbs;

[Verb("create", HelpText = "Create a new alias (digital identity) for sending and receiving encrypted files")]
public class CreateOptions : Options
{
  	[Option('e', "email", HelpText = "Optional email address to associate with alias")]
    public string? Email { get; set; }
	
	[Option('t', "token", HelpText = "Email validation token")]
    public string? Token { get; set; }
}
class Create 
{
	public static async Task<int> Execute(CreateOptions opts, IProgress<StatusUpdate>? progress = null)
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
				Identity = opts.Email ?? string.Empty,
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
				throw new Exception("Could not read public key from new key pair");
				//return 1;
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
				throw new Exception("No certificate returned from simpleenroll");
				//return 1;
			}

			// store the certificate
			if (j?.Certificate != null)
			{
				Storage.StoreCert(alias, j.Certificate);
			}
			else
			{
				Misc.LogError("No certificate returned from simpleenroll");
				throw new Exception("No certificate returned from simpleenroll");
				//return 1;
			}
			// store RSA private key
			string privateKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);
			if (privateKeyPem == null)
			{
				Misc.LogError("Could not read private key");
				throw new Exception("Could not read private key");
				//return 1;
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
				throw new Exception("Unable to verify alias");
				//return 1;
			}

			// now save the root fingerprint to a file
			string rootFingerprintHex = Convert.ToBase64String(rootFingerprint);
			Storage.StorePrivateKey($"{alias}.root", rootFingerprintHex, Globals.Password);

			Misc.LogCheckMark("Root certificate fingerprint saved");

			Misc.WriteLine($"\nAlias {alias} created\n");

			// update the progress bar if it is not null
			StatusUpdate statusUpdate = new StatusUpdate
			{
				Status = $"Alias {alias} created"
			};

			progress?.Report(statusUpdate);
			await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR PROGRESS BAR
		}
		catch (Exception ex)
		{
			try
			{
				StatusUpdate statusUpdate = new StatusUpdate
				{
					Status = ex.Message
				};
				progress?.Report(statusUpdate);
				await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR PROGRESS BAR
			}
			catch { }

			Misc.LogCriticalError("Unable to create alias", ex.Message);
			return 1;
		}

		return 0;
	}
}