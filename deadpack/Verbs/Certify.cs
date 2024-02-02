#pragma warning disable 1998
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace deadrop.Verbs;

[Verb("certify", HelpText = "Certify an alias.")]
public class CertifyOptions : Options
{
  	[Option('a', "alias", Required = true, HelpText = "Alias to be certified")]
	public required string Alias { get; set; }	

	[Option('e', "email", HelpText = "Email to be certified in alias")]
	public string Email { get; set; } = "";	

}
class Certify 
{
	public static async Task<int> Execute(CertifyOptions opts)
	{
		try
		{
			
			Misc.LogHeader();

			Misc.LogLine($"Certifying  {opts.Alias}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(opts.Password))
			opts.Password = Misc.GetPassword();

			string domain = Misc.GetDomain(opts, opts.Alias);

			(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, opts.Alias, opts.Email, opts);

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