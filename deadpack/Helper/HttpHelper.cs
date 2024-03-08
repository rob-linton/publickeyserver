using System.Net;
using System.Net.Http.Headers;
using System.Text;
using deadrop.Verbs;

namespace deadrop;

public class HttpHelper 
{
	/// <summary>
	/// Sends a GET request to the specified URL and returns the response as a string.
	/// </summary>
	/// <param name="url">The URL to send the GET request to.</param>
	/// <param name="opts">The options for the request.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the response as a string.</returns>
	public static async Task<string> Get(string url)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		Misc.LogLine2($"- GET: {url}");

		using var client = new HttpClient();
		string ret = await client.GetStringAsync(url);
		Misc.LogLine2($"RESPONSE: {ret}");
		return ret;
	}

	/// <summary>
	/// Sends a POST request to the specified URL with the provided JSON data.
	/// </summary>
	/// <param name="url">The URL to send the POST request to.</param>
	/// <param name="json">The JSON data to send in the request body.</param>
	/// <param name="opts">The options for the request.</param>
	/// <returns>A task representing the asynchronous operation. The task result contains the response from the server.</returns>
	public static async Task<string> Post(string url, string json)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		Misc.LogLine2($"- POST: {url}");
		Misc.LogLine2($"REQUEST: {json}");

		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PostAsync(url, content);
		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new Exception($"{response.Content.ReadAsStringAsync().Result}}}");
		}
		string ret = await response.Content.ReadAsStringAsync();
		Misc.LogLine2($"RESPONSE: {ret}");
		return ret;
	}

	/// <summary>
	/// Sends a PUT request to the specified URL with the provided JSON payload.
	/// </summary>
	/// <param name="url">The URL to send the request to.</param>
	/// <param name="json">The JSON payload to send with the request.</param>
	/// <param name="opts">The options for the request.</param>
	/// <returns>The response from the server as a string.</returns>
	public static async Task<string> Put(string url, string json, Options opts)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif

		Misc.LogLine2($"- PUT: {url}");
		Misc.LogLine2($"REQUEST: {json}");

		using var client = new HttpClient();
		var content = new StringContent(json, Encoding.UTF8, "application/json");
		var response = await client.PutAsync(url, content);
		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new Exception($"{response.Content.ReadAsStringAsync().Result}}}");
		}
		string ret = await response.Content.ReadAsStringAsync();
		Misc.LogLine2($"RESPONSE: {ret}");
		return ret;
	}

	// delete
	/// <summary>
	/// Sends a DELETE request to the specified URL and returns the response content as a string.
	/// </summary>
	/// <param name="url">The URL to send the DELETE request to.</param>
	/// <param name="opts">The options for the request.</param>
	/// <returns>The response content as a string.</returns>
	public static async Task<string> Delete(string url, Options opts)
	{
#if DEBUG
		url = url.Replace("https://", "http://");	
#endif
		
		Misc.LogLine2($"- DELETE: {url}");

		using var client = new HttpClient();
		var response = await client.DeleteAsync(url);
		if (response.StatusCode != System.Net.HttpStatusCode.OK)
		{
			throw new Exception($"{response.Content.ReadAsStringAsync().Result}}}");
		}

		return await response.Content.ReadAsStringAsync();
		
	}

	// stream a large file to the server
	/// <summary>
	/// Posts a file to the specified URL using HTTP POST method.
	/// </summary>
	/// <param name="url">The URL to which the file will be posted.</param>
	/// <param name="opts">The options for the request.</param>
	/// <param name="filePath">The path of the file to be posted.</param>
	/// <returns>A task that represents the asynchronous operation. The task result contains the response data as a string.</returns>
	public static async Task<string> PostFile(string url, string filePath)
    {
		

#if DEBUG
		url = url.Replace("https://", "http://");	
#endif
		Misc.LogLine2($"- POSTFILE: {url}");

		using (var client = new HttpClient())
		using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
		using (var content = new ProgressContent(fileStream))
		{
			content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

			try
			{
				var response = await client.PostAsync(url, content);
				if (response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					throw new Exception($"{response.Content.ReadAsStringAsync().Result}}}");
				}
				var data = await response.Content.ReadAsStringAsync();
				
				// remove the double quotes
				data = data.Substring(1, data.Length - 2);

				return data;
			}
			catch (Exception ex)
			{
				Misc.LogLine($"POSTFILE failed: {ex.Message}");
				throw;
			}

		}
    }

	/// <summary>
	/// Asynchronously retrieves a file from the specified URL and saves it to the specified file path.
	/// </summary>
	/// <param name="url">The URL of the file to retrieve.</param>
	/// <param name="opts">The options for retrieving the file.</param>
	/// <param name="saveFilePath">The file path where the retrieved file will be saved.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	public static async Task GetFile(string url, Options opts, string saveFilePath)
	{
		const int OneMB = 1024 * 1024;
		int bytesDownloaded = 0;
		int totalBytesDownloaded = 0;
		int hashesPrinted = 0;

	#if DEBUG
		url = url.Replace("https://", "http://");
	#endif
		Misc.LogLine($"- GETFILE: {url}");

		using (var client = new HttpClient())
		using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
		{
			if (response.StatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception($"{response.StatusCode}");
			}

			using (var fileStream = new FileStream(saveFilePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
			using (var responseStream = await response.Content.ReadAsStreamAsync())
			{
				var buffer = new byte[4096];
				int bytesRead;

				while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					await fileStream.WriteAsync(buffer, 0, bytesRead);
					totalBytesDownloaded += bytesRead;

					// Print "#" for each 1MB downloaded
					if (totalBytesDownloaded / OneMB > bytesDownloaded)
					{
						hashesPrinted++;
						Console.Write("#");
						bytesDownloaded++;
					}

					if (hashesPrinted > 109)
					{
						hashesPrinted = 0;
						Console.WriteLine();
					}
				}
			}
		}
	}


}


// ------------------------------------------------------------------------------------------------------------------------------------------------------
// helper class for showing content upload progress 
// ------------------------------------------------------------------------------------------------------------------------------------------------------
public class ProgressContent : HttpContent
{
    private const int OneMB = 1024 * 1024;
    private readonly Stream _content;
    private readonly int _bufferSize;

/// <summary>
/// Initializes a new instance of the <see cref="ProgressContent"/> class with the specified content and buffer size.
/// </summary>
/// <param name="content">The stream content.</param>
/// <param name="bufferSize">The size of the buffer used for reading the content. The default value is 4096.</param>
    public ProgressContent(Stream content, int bufferSize = 4096)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        _bufferSize = bufferSize;

        this.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
    }

	/// <summary>
	/// Serializes the content of the HttpHelper to the specified stream asynchronously.
	/// </summary>
	/// <param name="stream">The stream to which the content will be serialized.</param>
	/// <param name="context">The transport context.</param>
	/// <returns>A task representing the asynchronous operation.</returns>
	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		var buffer = new byte[_bufferSize];
		var bytesRead = 0;
		var totalBytesRead = 0;
		int hashesPrinted = 0;

		_content.Seek(0, SeekOrigin.Begin);

		while ((bytesRead = await _content.ReadAsync(buffer, 0, buffer.Length)) > 0)
		{
			await stream.WriteAsync(buffer, 0, bytesRead);
			totalBytesRead += bytesRead;

			if (totalBytesRead / OneMB > (totalBytesRead - bytesRead) / OneMB)
			{
				hashesPrinted++;
				Console.Write("#");
			}

			if (hashesPrinted > 109)
			{
				hashesPrinted = 0;
				Console.WriteLine();
			}
			
		}
	}

/// <summary>
/// Tries to compute the length of the content.
/// </summary>
/// <param name="length">The computed length of the content.</param>
/// <returns>True if the length was successfully computed, false otherwise.</returns>
    protected override bool TryComputeLength(out long length)
    {
        length = _content.Length;
        return true;
    }
}