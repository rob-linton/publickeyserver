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

			var result = await HttpHelper.Get($"https://{domain}/email/{email}", opts);

			var c = JsonSerializer.Deserialize<CertResult>(result);

			return c;
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Unable to verify email", ex.Message);
			return new CertResult() { Alias = ""};
		}
	}
}