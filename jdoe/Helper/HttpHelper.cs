using System.Text;

namespace jdoe;

public class HttpHelper 
{
	public static async Task<string> Get(string url)
	{
		using var client = new HttpClient();
		return await client.GetStringAsync(url);
	}

	public static async Task<string> Post(string url, string json)
	{
		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PostAsync(url, content);
		return await response.Content.ReadAsStringAsync();
	}

	public static async Task<string> Put(string url, string json)
	{
		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PutAsync(url, content);
		return await response.Content.ReadAsStringAsync();
	}

	public static async Task<string> Delete(string url)
	{
		using var client = new HttpClient();
		var response = await client.DeleteAsync(url);
		return await response.Content.ReadAsStringAsync();
	}
}
