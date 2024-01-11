using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace publickeyserver;

public class FileItem
{
	[JsonPropertyName("name")]
	public required string Name { get; set; }

	[JsonPropertyName("size")]
	public required long Size { get; set; }

	[JsonPropertyName("type")]
	public required string Type { get; set; }

	[JsonPropertyName("mtime")]
	public required long Mtime { get; set; }

	[JsonPropertyName("ctime")]
	public required long Ctime { get; set; }

	[JsonPropertyName("blocks")]
	public required List<string> Blocks { get; set; }
}
