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

[Verb("list", HelpText = "List all of your aliases")]
public class ListOptions : Options
{
  
}
class List 
{
	public static async Task<int> Execute(ListOptions opts)
	{
		Console.WriteLine($"\nListing your aliases");
		Console.WriteLine("================================================\n");

		List<string> aliases = Storage.GetAliases();

		foreach (string alias in aliases)
		{
			Console.WriteLine($"{alias}\n");

			try
			{
				string domain = Misc.GetDomain(opts, alias);

				bool valid = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, opts.Verbose);

				if (valid)
					Console.WriteLine($"Alias {alias} is valid\n");
				else
					Console.WriteLine($"Alias {alias} is *NOT* valid\n");

				Console.WriteLine("---\n");
			}
			catch (Exception ex)
			{
				Console.WriteLine("\nError: Unable to validate alias\n");
				if (opts.Verbose > 0)
					Console.WriteLine(ex.Message);
			}
		}


		
		
		return 0;
	}
}