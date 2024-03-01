using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using deadrop.Verbs;

namespace deadrop;

public class EmailHelper
{
	public static async Task<CertResult> GetAliasFromEmail(string email, Options opts)
	{
		try
		{
			// get the domain for requests
			string domain = Misc.GetDomain(opts, "");

			bool found = false;
			bool first = true;
			string result = "";
			while (!found)
			try
			{
				result = await HttpHelper.Get($"https://{domain}/email/{email}", opts);
				Misc.LogLine($"Email found {email}");
				found = true;
			}
			catch (Exception ex)
			{ 
				if (first)
				{
					try
					{
						result = await HttpHelper.Post($"https://{domain}/verify/{email}?intro=true", "", opts);
					}
					catch 
					{ 
					}

					Misc.LogLine($"Email {email} does not exist yet, checking every minute <ctrl-c> to cancel...");
					first = false;
				}
				await Task.Delay(60000); // 1 minute
			}

			var c = JsonSerializer.Deserialize<CertResult>(result);

			return c;
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Unable to verify email", ex.Message);
			throw new Exception("Unable to verify email");
		}
	}
}