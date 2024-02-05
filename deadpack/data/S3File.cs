using System.Text.Json.Serialization;

namespace deadrop;

public class S3File
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; set; }

}