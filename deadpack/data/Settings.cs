using System.Text.Json.Serialization;

namespace deadrop;

public class Settings
{
	[JsonPropertyName("domain")]
	public string? Domain { get; set; }
	
	[JsonPropertyName("password")]
	public string? Password { get; set; }
}