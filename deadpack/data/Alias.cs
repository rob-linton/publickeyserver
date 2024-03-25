using System.Text.Json.Serialization;

namespace deadrop;

public class Alias
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

	[JsonPropertyName("filename")]
    public required string Filename { get; set; }

    [JsonPropertyName("timestamp")]
    public required long Timestamp { get; set; }

	public override string ToString()
	{	
		return $"{Name}"; 
	}

}