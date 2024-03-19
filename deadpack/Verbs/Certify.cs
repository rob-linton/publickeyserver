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
	public static async Task<int> Execute(CertifyOptions opts, IProgress<StatusUpdate> progress = null)
	{
		try
		{
			
			Misc.LogHeader();

			Misc.LogLine($"Certifying  {opts.Alias}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(Globals.Password))
			Globals.Password = Misc.GetPassword();

			string alias = opts.Alias;
			// if it is an email then swap it out for an alias
			if (alias.Contains("@"))
			{
				CertResult cert = await EmailHelper.GetAliasOrEmailFromServer(alias, false);
				alias = cert?.Alias ?? string.Empty;
			}

			string domain = Misc.GetDomain(alias);
			string email = opts.Email;
			if (String.IsNullOrEmpty(opts.Email) && opts.Alias.Contains("@"))
			{
				email = opts.Alias;
			}

			(bool valid, byte[] rootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, email);

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{alias}.root", Globals.Password);
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// and compare it to the rootfingerprint
			if (rootFingerprint.SequenceEqual(rootFingerprintFromFile))
				Misc.LogCheckMark($"Root fingerprint matches");
			else
				Misc.LogLine($"Invalid: Root fingerprint does not match");

			if (valid)
				Misc.LogLine($"\nValid: {alias}\n");
			else
				Misc.LogLine($"\nInvalid: {alias}\n");

		}
		catch (Exception ex)
		{
			try
			{
				progress?.Report(new StatusUpdate { Status = ex.Message });
				await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX
			}
			catch { }

			Misc.LogError("Unable to validate alias", ex.Message);
			return 1;
		}
		
		return 0;
	}
}