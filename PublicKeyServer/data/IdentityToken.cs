using System.Text.Json.Serialization;

namespace suredrop;

class IdentityToken
{
	[JsonPropertyName("token")]
	public required string Token { get; set; }

	[JsonPropertyName("identity")]
	public required string Identity { get; set; }

	// identity-type
	[JsonPropertyName("identity-type")]
	public required string IdentityType { get; set; }

	// timestamp
	[JsonPropertyName("timestamp")]
	public required string Timestamp { get; set; }
}