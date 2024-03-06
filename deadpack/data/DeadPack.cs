using System.Text.Json.Serialization;

namespace deadrop;

public class DeadPack
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

	[JsonPropertyName("filename")]
    public required string Filename { get; set; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; set; }

}