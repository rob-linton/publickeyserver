using System.Net.Http;
using System.Text;
using System.Threading.Tasks;


namespace publickeyserver;

public class HttpHelper 
{
	public static async Task<string> Get(string url)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

	

		using var client = new HttpClient();
		string ret = await client.GetStringAsync(url);
	
		return ret;
	}

	public static async Task<string> Post(string url, string json)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif


		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PostAsync(url, content);
		string ret = await response.Content.ReadAsStringAsync();
		
		return ret;
	}

	public static async Task<string> Put(string url, string json)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PutAsync(url, content);
		string ret = await response.Content.ReadAsStringAsync();
		return ret;
	}

	public static async Task<string> Delete(string url)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif
		using var client = new HttpClient();
		var response = await client.DeleteAsync(url);
		return await response.Content.ReadAsStringAsync();
	}
}
