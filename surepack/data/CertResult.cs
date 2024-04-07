using System.Text.Json.Serialization;

namespace suredrop;

public class CertResult
{
	[JsonPropertyName("alias")]
	public required string Alias { get; set; }

	[JsonPropertyName("origin")]
	public string? Origin { get; set; }

	[JsonPropertyName("certificate")]
	public string? Certificate { get; set; }
}