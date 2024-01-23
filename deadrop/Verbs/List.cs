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
		Misc.LogHeader();
		Misc.LogLine($"Listing...");
		Misc.LogLine($"");
		
		List<string> aliases = Storage.GetAliases();

		foreach (string alias in aliases)
		{
			try
			{
				// now load the root fingerprint from a file
				string rootFingerprintFromFileString = Storage.GetPrivateKey($"{alias}.root", opts.Password);
				byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);


				string domain = Misc.GetDomain(opts, alias);

				(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, opts);

				// validate the fingerprint
				if (rootFingerprint.SequenceEqual(rootFingerprintFromFile))
					Misc.LogCheckMark($"Root fingerprint matches");
				else
					Misc.LogLine($"Invalid: Root fingerprint does not match");

				if (valid)
				{
					Misc.LogLine("");
					Misc.LogCheckMark($"*** {alias}");
					Misc.LogLine("");
				}
				else
					Misc.LogLine($"*** Invalid: {alias}");

			}
			catch (Exception ex)
			{
				Misc.LogError(opts, "Unable to validate alias", ex.Message);
			}
		}

		return 0;
	}
}