using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace suredrop;

public class ListResult
{
	[JsonPropertyName("files")]
	public required List<ListFile> Files { get; set; }

	[JsonPropertyName("count")]
	public required int Count { get; set; }

	[JsonPropertyName("size")]
	public required long Size { get; set; }

}