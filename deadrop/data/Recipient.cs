using System.Text.Json.Serialization;

namespace deadrop;

public class Recipient
{
    [JsonPropertyName("alias")]
    public required string Alias { get; set; }

    [JsonPropertyName("key")]
    public required string Key { get; set; }

}