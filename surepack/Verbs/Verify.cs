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

[Verb("verify", HelpText = "Request email verification code to associate an email with an alias")]
public class VerifyOptions : Options
{
  [Option('e', "email", Required = true, HelpText = "Email to be verified")]
    public required string Email { get; set; }	
}
class Verify 
{
	public static async Task<int> Execute(VerifyOptions opts, IProgress<StatusUpdate>? progress = null)
	{
		StatusUpdate statusUpdate = new StatusUpdate();
		try
		{
			if (String.IsNullOrEmpty(opts.Email	))
			{
				Misc.LogError("Email is required");
				throw new Exception("Email is required");
				//return 1;
			}

			Misc.LogHeader();
			Misc.LogLine($"Verifying...");

			// get the domain for requests
			string domain = Misc.GetDomain("");

			Misc.LogLine($"Domain: {domain}");
			Misc.LogLine($"");

			Misc.LogLine("- verifying email...");

			var result = await HttpHelper.Post($"https://{domain}/verify/email/{opts.Email}", "");

			// update the progress bar if it is not null
			statusUpdate.Status = result.Replace("\"", "");

			progress?.Report(statusUpdate);
			await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

			Misc.WriteLine($"\n{result}\n");
		}
		catch (Exception ex)
		{
			statusUpdate.Status = ex.Message;

			progress?.Report(statusUpdate);
			await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

			Misc.LogCriticalError("Unable to verify email", ex.Message);
			return 1;
		}
		
		return 0;
	}
}