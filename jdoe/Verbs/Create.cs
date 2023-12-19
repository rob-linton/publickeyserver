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

		// save your public and private key pair
		

		var data = new Dictionary<string, string>();
		string alias = String.Empty;
		
		//using (TextWriter publicKeyTextWriter = new StringWriter())
		//{
			//PemWriter pemWriter = new PemWriter(publicKeyTextWriter);
			//pemWriter.WriteObject(keyPair.Public);
			//pemWriter.Writer.Flush();
			//string? publicKey = publicKeyTextWriter.ToString();

			string publicKey = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Public);

			if (publicKey != null)
			{
				// save it as PEM forma

				data["key"] = publicKey;
				string json = JsonSerializer.Serialize(data);
#if DEBUG
				var result = await HttpHelper.Post($"http://{opts.Domain}/simpleenroll", json);
#else
				var result = await HttpHelper.Post($"https://{opts.Domain}/simpleenroll", json);	
#endif
				var j = JsonSerializer.Deserialize<SimpleEnrollResult>(result);
				alias = j?.alias ?? string.Empty;
				string certificate = j?.certificate ?? string.Empty;


				File.WriteAllText($"{alias}.pem", certificate);

			}
		//}
		
		// encrypt the private key with a password
		//using (TextWriter privateKeyTextWriter = new StringWriter())
		//{
			//PemWriter pemWriter = new PemWriter(privateKeyTextWriter);
			//pemWriter.WriteObject(keyPair.Private);
			//pemWriter.Writer.Flush();
			//string privateKeyText = privateKeyTextWriter?.ToString() ?? string.Empty;

			string privateKeyText = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);

			// encrypt the private key
			byte[] msg = privateKeyText.ToBytes();
			byte[] key = opts.Password.ToBytes();
			byte[] nonce = opts.Domain.ToBytes();

			byte[] cipherText = BouncyCastleHelper.EncryptWithKey(msg, key, nonce);

			// save it as PEM format
			File.WriteAllBytes($"{alias}.privatekey.pem", cipherText);
		//}
		

		return 0;
	}
}