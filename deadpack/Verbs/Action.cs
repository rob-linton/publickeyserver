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

[Verb("action", false, HelpText = "Quick action to pack and unpack a file")]
public class ActionOptions : Options
{
  
}
class Action 
{
	public static async Task<int> Execute(ActionOptions opts)
	{
		Misc.LogHeader();
		Misc.LogLine($"Actioning...");
		Misc.LogLine($"");
	
		

		return 0;
	}
}