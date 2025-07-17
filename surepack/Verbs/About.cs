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

[Verb("about", false, HelpText = "Display version and about information")]
public class AboutOptions : Options
{
  
}
class About 
{
	public static async Task<int> Execute(AboutOptions opts)
	{
		opts.Verbose = "1";
		Misc.LogHeader();
		
		Misc.WriteLine("FEATURES:");
		Misc.WriteLine("  ✓ End-to-End Encryption with AES-256-GCM");
		Misc.WriteLine("  ✓ Post-Quantum Cryptography (Kyber1024 + Dilithium5)");
		Misc.WriteLine("  ✓ Anonymous Transfer Option");
		Misc.WriteLine("  ✓ Perfect Forward Secrecy");
		Misc.WriteLine("  ✓ Zero-Knowledge Server Architecture");
		Misc.WriteLine("");
		Misc.WriteLine("GETTING HELP:");
		Misc.WriteLine("  📚 Full Documentation: https://rob-linton.github.io/publickeyserver/");
		Misc.WriteLine("  📖 User Manual: https://rob-linton.github.io/publickeyserver/HELP.html");
		Misc.WriteLine("  🐛 Report Issues: https://github.com/rob-linton/publickeyserver/issues");
		Misc.WriteLine("  💻 Source Code: https://github.com/rob-linton/publickeyserver");
		Misc.WriteLine("");
		Misc.WriteLine("QUICK START:");
		Misc.WriteLine("  1. Create an alias:     surepack create");
		Misc.WriteLine("  2. Pack files:          surepack pack -i file.pdf -a recipient -f you -o package.surepack");
		Misc.WriteLine("  3. Send package:        surepack send -i package.surepack");
		Misc.WriteLine("  4. Receive packages:    surepack receive -a your-alias");
		Misc.WriteLine("  5. Unpack files:        surepack unpack -i package.surepack -o output-folder");
		Misc.WriteLine("");
		Misc.WriteLine("For detailed help on any command, use: surepack <command> --help");

		return 0;
	}
}