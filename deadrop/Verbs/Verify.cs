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
			Console.WriteLine($"\nVerifying  {opts.Alias}\n");

			string domain = Misc.GetDomain(opts, opts.Alias);

			// first get the CA
			if (opts.Verbose > 0)
				Console.WriteLine($"GET: https://{domain}/cacerts");

			var result = await HttpHelper.Get($"https://{domain}/cacerts");
			var ca = JsonSerializer.Deserialize<CaCertsResult>(result);
			var cacerts = ca?.CaCerts;

			// now get the alias	
			if (opts.Verbose > 0)
				Console.WriteLine($"GET: https://{domain}/cert/{Misc.GetAliasFromAlias(opts.Alias)}");

			result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(opts.Alias)}");

			var c = JsonSerializer.Deserialize<CertResult>(result);
			var certificate = c?.Certificate;

			// now validate the certificate chain
			bool valid = false;
			if (certificate != null && cacerts != null) // Add null check for cacerts
			{
				valid = BouncyCastleHelper.ValidateCertificateChain(certificate, cacerts, domain);
			}

			//Console.WriteLine("Please validate the CA certificate fingerprint at");
			//Console.WriteLine($"https://{domain}");

			if (valid)
			{
				Console.WriteLine("\nAlias is valid\n");
			}
			else
			{
				Console.WriteLine("\nAlias is *NOT* valid\n");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nUnable to validate alias\n");
			if (opts.Verbose > 0)
				Console.WriteLine(ex.Message);
			return 1;
		}
		
		return 0;
	}
}