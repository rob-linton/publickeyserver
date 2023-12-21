using System.Text.Json.Serialization;

namespace deadrop;

class CaCertsResult
{
	[JsonPropertyName("origin")]
	public string? Origin { get; set; }
	
	[JsonPropertyName("cacerts")]
	public required List<string> CaCerts { get; set; }
}