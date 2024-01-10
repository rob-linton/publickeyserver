using System.Text;
using deadrop.Verbs;

namespace deadrop;

public class HttpHelper 
{
	public static async Task<string> Get(string url, Options opts)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		Misc.LogLine(opts, $"- GET: {url}");

		using var client = new HttpClient();
		string ret = await client.GetStringAsync(url);
		Misc.LogLine1(opts, $"RESPONSE: {ret}");
		return ret;
	}

	public static async Task<string> Post(string url, string json, Options opts)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		Misc.LogLine(opts, $"- POST: {url}");
		Misc.LogLine1(opts, $"REQUEST: {json}");

		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PostAsync(url, content);
		string ret = await response.Content.ReadAsStringAsync();
		Misc.LogLine1(opts, $"RESPONSE: {ret}");
		return ret;
	}

	public static async Task<string> Put(string url, string json, Options opts)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		Misc.LogLine(opts, $"- PUT: {url}");
		Misc.LogLine1(opts, $"REQUEST: {json}");

		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PutAsync(url, content);
		string ret = await response.Content.ReadAsStringAsync();
		Misc.LogLine1(opts, $"RESPONSE: {ret}");
		return ret;
	}

	public static async Task<string> Delete(string url, Options opts)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif
		
		Misc.LogLine(opts, $"- DELETE: {url}");

		using var client = new HttpClient();
		var response = await client.DeleteAsync(url);
		return await response.Content.ReadAsStringAsync();
	}
}
