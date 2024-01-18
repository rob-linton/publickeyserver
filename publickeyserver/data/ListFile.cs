using System;
using System.Text.Json.Serialization;

namespace publickeyserver;

class ListFile
{
	[JsonPropertyName("key")]
	public string Key { get; set; }

	[JsonPropertyName("size")]
	public long Size { get; set; }

	[JsonPropertyName("last_modified")]
	public DateTime LastModified { get; set; }
}