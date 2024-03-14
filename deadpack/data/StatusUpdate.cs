using System.Text.Json.Serialization;

namespace deadrop;

public class StatusUpdate
{
	[JsonPropertyName("index")]
	public float Index { get; set; } = 0;

	[JsonPropertyName("count")]
	public float Count { get; set; } = 0;

	[JsonPropertyName("status")]
    public string? Status { get; set; }


}