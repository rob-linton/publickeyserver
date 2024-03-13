using System.Text.Json.Serialization;

namespace deadrop;

public class StatusUpdate
{
    [JsonPropertyName("index")]
    public required float Index { get; set; }

	[JsonPropertyName("count")]
    public required float Count { get; set; }

}