using System.Text.Json.Serialization;

namespace deadrop;

public class Recipient
{
    [JsonPropertyName("alias")]
    public required string Alias { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

	[JsonPropertyName("kyber_key")]
    public required string KyberKey { get; set; }

	// overide the tostring method
	public override string ToString()
	{
		return $"{Alias}";
	}
}