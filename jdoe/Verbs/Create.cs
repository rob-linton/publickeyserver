#pragma warning disable 1998
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;

namespace jdoe.Verbs;

[Verb("create", HelpText = "Create an alias.")]
public class CreateOptions : Options
{
  
}
class Create 
{
	public static async Task<int> Execute(Options opts)
	{
		//await Task.Delay(1000);
		Console.WriteLine("Create");

		//
		// create the public/private key pair using bouncy castle
		//
		AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.GenerateKeyPair(2048);

		//
		// create the json string
		//
		var data = new Dictionary<string, object>
        {
            { "Key1", "Value1" },
            { "Key2", 123 },
            { "Key3", true },
            { "Key4", new DateTime(2023, 1, 1) }
        };
		string json = JsonSerializer.Serialize(data);

		//
		// now call the rest api to create the alias
		//


		return 0;
	}
}