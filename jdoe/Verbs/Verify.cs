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

[Verb("verify", HelpText = "Verify an alias.")]
public class VerifyOptions : Options
{
  [Option('a', "alias", Required = true, HelpText = "Alias to be verified")]
    public string Alias { get; set; } = "";	
}
class Verify 
{
	public static async Task<int> Execute(VerifyOptions opts)
	{
		Console.WriteLine($"Verifying  {opts.Alias}\n");

		// first get the CA
#if DEBUG
		var result = await HttpHelper.Get($"http://{opts.Domain}/cacerts");
#else
		var result = await HttpHelper.Get($"https://{opts.Domain}/cacerts");	
#endif
		var ca = JsonSerializer.Deserialize<CaCertsResult>(result);
		var cacerts = ca?.cacerts;

		// now get the alias
#if DEBUG
		result = await HttpHelper.Get($"http://{opts.Domain}/cert/{Misc.getAliasFromAliasAndDomain(opts.Alias)}");
		#else	
		result = await HttpHelper.Get($"https://{opts.Domain}/cert/{Misc.getAliasFromAliasAndDomain(opts.Alias)}");
		#endif
		var c = JsonSerializer.Deserialize<CertResult>(result);
		var certificate = c?.certificate;


		// now validate the certificate chain
		bool valid = BouncyCastleHelper.ValidateCertificateChain(certificate, cacerts);
		
		Console.WriteLine("Please validate the CA certificate fingerprint at");
		Console.WriteLine($"https://{opts.Domain}");

		if (valid)
		{
			Console.WriteLine("\nAlias is valid");
		}
		else
		{
			Console.WriteLine("\nAlias is *NOT* valid");
		}

		return 0;
	}
}