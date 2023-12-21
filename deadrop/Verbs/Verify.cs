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

[Verb("verify", HelpText = "Verify an alias.")]
public class VerifyOptions : Options
{
  [Option('a', "alias", Required = true, HelpText = "Alias to be verified")]
    public required string Alias { get; set; }	
}
class Verify 
{
	public static async Task<int> Execute(VerifyOptions opts)
	{
		try
		{
			Console.WriteLine($"\nVerifying alias");
			Console.WriteLine("================================================\n");

			Console.WriteLine($"\nVerifying  {opts.Alias}");

			string domain = Misc.GetDomain(opts, opts.Alias);

			bool valid = await BouncyCastleHelper.VerifyAliasAsync(domain, opts.Alias, opts.Verbose);

			if (valid)
				Console.WriteLine($"\nAlias {opts.Alias} is valid\n");
			else
				Console.WriteLine($"\nAlias {opts.Alias} is *NOT* valid\n");
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nError: Unable to validate alias\n");
			if (opts.Verbose > 0)
				Console.WriteLine(ex.Message);
			return 1;
		}
		
		return 0;
	}
}