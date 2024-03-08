using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace deadrop;

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

	// overide the tostring method
	public override string ToString()
	{
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		dateTime = dateTime.AddSeconds(Ctime).ToLocalTime();
		string d = dateTime.ToString("dd-MMM-yyyy hh:mmtt");

		string s = Size.ToString().PadRight(10);
		
		
		return $"{d}  {s} {Name}"; 
	}
}
