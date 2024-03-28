using System.Text.Json.Serialization;

namespace deadrop;

public class CertResult
{
	[JsonPropertyName("alias")]
	public required string Alias { get; set; }

	[JsonPropertyName("origin")]
	public string? Origin { get; set; }

	[JsonPropertyName("certificate")]
	public string? Certificate { get; set; }
}