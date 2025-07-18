using System.Text.Json.Serialization;

namespace suredrop;

public class EmailToken
{
	[JsonPropertyName("token")]
	public string? Token { get; set; }

	[JsonPropertyName("email")]
	public string? Email { get; set; }

	// timestamp
	[JsonPropertyName("timestamp")]
	public string? Timestamp { get; set; }
}