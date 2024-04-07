using System;
using System.Text.Json.Serialization;

namespace suredrop;

public class ListFile
{
	[JsonPropertyName("key")]
	public required string Key { get; set; }

	[JsonPropertyName("size")]
	public required long Size { get; set; }

	[JsonPropertyName("last_modified")]
	public required DateTime LastModified { get; set; }
}