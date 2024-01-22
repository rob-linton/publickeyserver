using System.Text.Json.Serialization;

namespace publickeyserver;

public class Recipient
{
    [JsonPropertyName("alias")]
    public required string Alias { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

	[JsonPropertyName("kyber_key")]
    public required string KyberKey { get; set; }

}