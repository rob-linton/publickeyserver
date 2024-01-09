using System.Text.Json.Serialization;

namespace deadrop;

class Manifest
{
	[JsonPropertyName("files")]
	public required List<FileItem> Files { get; set; }
}
