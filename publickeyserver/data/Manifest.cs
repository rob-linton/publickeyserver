using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace publickeyserver;

class Manifest
{
	[JsonPropertyName("files")]
	public required List<FileItem> Files { get; set; }
}
