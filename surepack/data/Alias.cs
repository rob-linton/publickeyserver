using System.Text.Json.Serialization;

namespace suredrop;

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

	[JsonPropertyName("total_surepacks")]
    public long? TotalSurePacks { get; set; }

	[JsonPropertyName("new_surepacks_24hour")]
    public long? NewSurePacks24Hour { get; set; }

	[JsonPropertyName("new_surepacks_1hour")]
    public long? NewSurePacks1Hour { get; set; }

	public override string ToString()
	{	
		string alert = "";

		//calculate alerts
		if (NewSurePacks1Hour > 0)
			alert =  $" ({NewSurePacks1Hour}/{NewSurePacks24Hour}/{TotalSurePacks})";
		else if (NewSurePacks24Hour > 0)
			alert =  $" ({NewSurePacks24Hour}/{TotalSurePacks})";
		else if (TotalSurePacks > 0)
			alert =  $" ({TotalSurePacks})";
		else
			alert = "";
		
		

		if (String.IsNullOrEmpty(Email))
			return $"{Name} {alert}";
		else
			return $"{Name} ({Email}) {alert}";
	}

}