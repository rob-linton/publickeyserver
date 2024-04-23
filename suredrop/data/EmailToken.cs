using System.Text.Json.Serialization;

namespace suredrop;

class IdentityToken
{
	[JsonPropertyName("token")]
	public string? Token { get; set; }

	[JsonPropertyName("identity")]
	public string? Identity { get; set; }

	// timestamp
	[JsonPropertyName("timestamp")]
	public string? Timestamp { get; set; }
}