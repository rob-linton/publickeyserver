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
			
			Misc.LogHeader();

			Misc.LogLine($"Verifying...");

			Misc.LogLine(opts, $"Verifying  {opts.Alias}");
			Misc.LogLine($"");

			string domain = Misc.GetDomain(opts, opts.Alias);

			(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, opts.Alias, opts);

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{opts.Alias}.root", opts.Password);
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// and compare it to the rootfingerprint
			if (rootFingerprint.SequenceEqual(rootFingerprintFromFile))
				Misc.LogCheckMark($"Root fingerprint matches");
			else
				Misc.LogLine($"Invalid: Root fingerprint does not match");

			if (valid)
				Misc.LogLine($"\nValid: {opts.Alias}\n");
			else
				Misc.LogLine($"\nInvalid: {opts.Alias}\n");

		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Unable to validate alias", ex.Message);
			return 1;
		}
		
		return 0;
	}
}