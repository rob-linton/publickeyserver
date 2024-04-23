#pragma warning disable 1998
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace suredrop.Verbs;

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
				string domain = Misc.GetDomain(alias);

				try
				{
					(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, "");

					if (valid)
					{
						Misc.LogLine("");
						Misc.WriteLine($"{alias}");
						Misc.LogLine("");
					}
					else
						Misc.LogLine($"*** Invalid: {alias}");
				}
				catch (Exception ex)
				{
					Misc.LogError("Unable to validate alias", ex.Message);
				}
			}
			catch (Exception ex)
			{
				Misc.LogError("Unable to validate alias", ex.Message);
			}
		}

		return 0;
	}
}