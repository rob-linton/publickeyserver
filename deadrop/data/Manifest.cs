using System.Text.Json.Serialization;

namespace deadrop;

class Manifest
{
	

	[JsonPropertyName("signature")]
	public string? Signature { get; set; }


	[JsonPropertyName("files")]
	public required List<FileItem> Files { get; set; }
}
