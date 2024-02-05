using System.Text.Json.Serialization;

namespace deadrop;

public class CaCertsResult
{
	[JsonPropertyName("origin")]
	public string? Origin { get; set; }
	
	[JsonPropertyName("cacerts")]
	public required List<string> CaCerts { get; set; }
}