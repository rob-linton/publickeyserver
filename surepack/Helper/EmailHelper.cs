using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using suredrop.Verbs;

namespace suredrop;

public class EmailHelper
{
	public static async Task<CertResult> GetAliasOrEmailFromServer(string emailOrAlias, bool create)
	{
		// get the domain for requests
			string domain = Misc.GetDomain(emailOrAlias);

		// if there is no @ then just return the email as it will be an alias
		if (!emailOrAlias.Contains('@'))
		{
			string a = Misc.GetAliasFromAlias(emailOrAlias);
			var result = await HttpHelper.Get($"https://{domain}/cert/{a}");
			var c = JsonSerializer.Deserialize<CertResult>(result);
			if (c == null)
			{
				throw new Exception("Unable to deserialize CertResult.");
			}
			return c;
		}

		try
		{
			

			string result = "";
			
			try
			{
				result = await HttpHelper.Get($"https://{domain}/email/{emailOrAlias}");
				Misc.LogCheckMark($"Email found {emailOrAlias}");
			}
			catch (Exception)
			{ 

				if (create)
				{
					try
					{
						result = await HttpHelper.Post($"https://{domain}/verify/{emailOrAlias}?intro=true", "");
					}
					catch 
					{ 
					}

					Misc.LogCross($"Email found {emailOrAlias}");
					Misc.LogCheckMark($"Email sent {emailOrAlias}");
				}
				else		
				{
					Misc.LogCross($"Email found {emailOrAlias}");
				}
			}

			var c = JsonSerializer.Deserialize<CertResult>(result);

			if (c == null)
			{
				throw new Exception("Unable to deserialize CertResult.");
			}

			return c;
		}
		catch (Exception ex)
		{
			Misc.LogError("Unable to verify email", ex.Message);
			throw new Exception("Unable to verify email");
		}
	}
}