using System.Text.Json.Serialization;

namespace deadrop;

public class DeadPack
{
    [JsonPropertyName("subject")]
    public required string Subject { get; set; }

	[JsonPropertyName("message")]
    public required string Message { get; set; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; set; }

	[JsonPropertyName("files")]
	public required List<FileItem> Files { get; set; }

	[JsonPropertyName("from")]
	public required string From { get; set; }

	[JsonPropertyName("alias")]
	public required string Alias { get; set; }

	// filename
	[JsonPropertyName("filename")]
	public required string Filename { get; set; }

	[JsonPropertyName("recipients")]
	public required List<Recipient> Recipients { get; set; }

}