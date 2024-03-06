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
	
		List<Alias> aliases = Storage.GetAliases();

		foreach (Alias a in aliases)
		{
			try
			{
				string alias = a.Name;
				string domain = Misc.GetDomain(opts, alias);

				(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, "", opts);

				if (valid)
				{
					Misc.LogLine("");
					Misc.LogLine($"{alias}");
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