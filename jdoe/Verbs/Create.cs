#pragma warning disable 1998
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace jdoe.Verbs;

[Verb("create", HelpText = "Create an alias.")]
public class CreateOptions : Options
{
  	
}
class Create 
{
	public static async Task<int> Execute(CreateOptions opts)
	{
		Console.WriteLine("Create");

		//
		// create the public/private key pair using bouncy castle
		//
		AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.GenerateKeyPair(2048);

		//
		// save your public and private key pair
		//
		var data = new Dictionary<string, string>();
		string alias = String.Empty;
		string publicKey = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Public);

		if (publicKey == null)
		{
			Console.WriteLine("Error: could not read public key");
			return 1;
		}
		
		// save it as PEM format
		data["key"] = publicKey;
		string json = JsonSerializer.Serialize(data);
		var result = await HttpHelper.Post($"https://{opts.Domain}/simpleenroll", json);	

		var j = JsonSerializer.Deserialize<SimpleEnrollResult>(result);
		alias = j?.alias ?? string.Empty;
		string certificate = j?.certificate ?? string.Empty;

		File.WriteAllText($"{alias}.pem", certificate);
		

		string privateKeyText = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);
		if (privateKeyText == null)
		{
			Console.WriteLine("Error: could not read private key");
			return 1;
		}

		// encrypt the private key
		byte[] msg = privateKeyText.ToBytes();
		byte[] key = opts.Password.ToBytes();
		byte[] nonce = opts.Domain.ToBytes();

		byte[] cipherText = BouncyCastleHelper.EncryptWithKey(msg, key, nonce);

		// save it as PEM format
		File.WriteAllBytes($"{alias}.privatekey.pem", cipherText);

		return 0;
	}
}