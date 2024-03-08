using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using deadrop.Verbs;

namespace deadrop;

public class EmailHelper
{
	public static async Task<CertResult> GetAliasOrEmailFromServer(string emailOrAlias, bool create)
	{
		// get the domain for requests
			string domain = Misc.GetDomain("");

		// if there is no @ then just return the email as it will be an alias
		if (!emailOrAlias.Contains("@"))
		{
			var result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(emailOrAlias)}");
			var c = JsonSerializer.Deserialize<CertResult>(result);
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
			catch (Exception ex)
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

			return c;
		}
		catch (Exception ex)
		{
			Misc.LogError("Unable to verify email", ex.Message);
			throw new Exception("Unable to verify email");
		}
	}
}