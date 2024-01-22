using System.Text.Json.Serialization;

namespace deadrop;

class CustomExtensionData
{
	[JsonPropertyName("kyber_key")]
	public string? KyberKey { get; set; }

	[JsonPropertyName("dilithium_key")]
	public string? DilithiumKey { get; set; }

}