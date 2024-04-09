using System.Text.Json.Serialization;

namespace suredrop;

public class StatusUpdate
{
	[JsonPropertyName("index")]
	public float Index { get; set; } = 0;

	[JsonPropertyName("count")]
	public float Count { get; set; } = 0;
	
	[JsonPropertyName("block-index")]
	public float BlockIndex { get; set; } = 0;
	
	[JsonPropertyName("block-count")]
	public float BlockCount { get; set; } = 0;

	[JsonPropertyName("status")]
    public string? Status { get; set; }

}