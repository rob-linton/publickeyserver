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

[Verb("about", false, HelpText = "Quick About to pack and unpack a file")]
public class AboutOptions : Options
{
  
}
class About 
{
	public static async Task<int> Execute(AboutOptions opts)
	{
		opts.Verbose = "1";
		Misc.LogHeader();

		return 0;
	}
}