using System.Text.Json.Serialization;

namespace suredrop;

public class CustomExtensionData
{
	[JsonPropertyName("kyber_key")]
	public string? KyberKey { get; set; }

	[JsonPropertyName("dilithium_key")]
	public string? DilithiumKey { get; set; }

	[JsonPropertyName("identity")]
	public string? Identity { get; set; }

	[JsonPropertyName("token")]
	public string? Token { get; set; }

}