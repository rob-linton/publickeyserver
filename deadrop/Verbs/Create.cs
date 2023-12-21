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
			Console.WriteLine("\nCreating alias...");

			// get the domain for requests
			string domain = Misc.GetDomain(opts, "");

			//
			// create the public/private key pair using bouncy castle
			//
			AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.GenerateKeyPair(2048);

			//
			// save your public and private key pair
			//
			var data = new Dictionary<string, string>();
			string alias = String.Empty;
			string publicKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Public);

			if (publicKeyPem == null)
			{
				Console.WriteLine("Error: could not read public key");
				return 1;
			}

			// save it as PEM format
			data["key"] = publicKeyPem;
			string json = JsonSerializer.Serialize(data);

			if (opts.Verbose > 0)
				Console.WriteLine($"POST: https://{domain}/simpleenroll");

			var result = await HttpHelper.Post($"https://{domain}/simpleenroll", json);

			var j = JsonSerializer.Deserialize<SimpleEnrollResult>(result);
			alias = j?.Alias ?? string.Empty;
			string certificate = j?.Certificate ?? string.Empty;

			Storage.StoreCert(alias, certificate);

			string privateKeyPem = BouncyCastleHelper.ReadPemStringFromKey(keyPair.Private);
			if (privateKeyPem == null)
			{
				Console.WriteLine("Error: could not read private key");
				return 1;
			}

			Storage.StorePrivateKey(alias, privateKeyPem, opts.Password);

			Console.WriteLine($"\nAlias {alias} created\n");
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nUnable to create alias\n");
			if (opts.Verbose > 0)
				Console.WriteLine(ex.Message);
			return 1;
		}

		return 0;
	}
}