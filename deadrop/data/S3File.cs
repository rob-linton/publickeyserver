using System.Text.Json.Serialization;

namespace publickeyserver;

public class S3File
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; set; }

}