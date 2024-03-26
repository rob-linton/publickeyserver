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

	[JsonPropertyName("email")]
    public string? Email { get; set; }


	public override string ToString()
	{	if (String.IsNullOrEmpty(Email))
			return $"{Name}";
		else
			return $"{Name} ({Email})";
	}

}