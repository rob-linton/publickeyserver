using System.Text.Json.Serialization;

namespace deadrop;

class SimpleEnrollResult
{
	[JsonPropertyName("alias")]
	public string? Alias { get; set; }

	[JsonPropertyName("origin")]
	public string? Origin { get; set; }

	[JsonPropertyName("publickey")]
	public string? Publickey { get; set; }

	[JsonPropertyName("certificate")]
	public string? Certificate { get; set; }
}