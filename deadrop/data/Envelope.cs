using System.Text.Json.Serialization;

namespace deadrop;

public class Envelope
{
    [JsonPropertyName("to")]
    public required List<Recipient> To { get; set; }

    [JsonPropertyName("from")]
    public required string From { get; set; }

    [JsonPropertyName("created")]
    public required long Created { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }

}

