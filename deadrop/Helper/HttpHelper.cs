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
		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new Exception($"POST failed: {response.StatusCode}: {response.Content.ReadAsStringAsync().Result}}}");
		}
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
		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new Exception($"PUT failed: {response.StatusCode}: {response.Content.ReadAsStringAsync().Result}}}");
		}
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
		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new Exception($"DELETE failed: {response.StatusCode}: {response.Content.ReadAsStringAsync().Result}}}");
		}

		return await response.Content.ReadAsStringAsync();
		
	}

	// stream a large file to the server
	public static async Task<string> PostFile(string url, Options opts, string filePath)
    {
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif
		Misc.LogLine(opts, $"- POSTFILE: {url}");

		using (var client = new HttpClient())
		using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
		using (var content = new StreamContent(fileStream))
		{
			content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

			try
			{
				var response = await client.PostAsync(url, content);
				if (response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					throw new Exception($"POSTFILE failed: {response.StatusCode}: {response.Content.ReadAsStringAsync().Result}}}");
				}
				var data = await response.Content.ReadAsStringAsync();
				return data;
			}
			catch (Exception ex)
			{
				Misc.LogLine(opts, $"POSTFILE failed: {ex.Message}");
				throw;
			}

			

			

		}
    }
}
