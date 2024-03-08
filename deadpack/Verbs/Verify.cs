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
  [Option('e', "email", Required = true, HelpText = "Email to be verified")]
    public required string Email { get; set; }	
}
class Verify 
{
	public static async Task<int> Execute(VerifyOptions opts)
	{
		try
		{
			Misc.LogHeader();
			Misc.LogLine($"Verifying...");

			// get the domain for requests
			string domain = Misc.GetDomain("");

			Misc.LogLine($"Domain: {domain}");
			Misc.LogLine($"");

			Misc.LogLine("- verifying email...");

			var result = await HttpHelper.Post($"https://{domain}/verify/{opts.Email}", "");

			Misc.LogLine($"\n{result}\n");
		}
		catch (Exception ex)
		{
			Misc.LogError("Unable to verify email", ex.Message);
			return 1;
		}
		
		return 0;
	}
}