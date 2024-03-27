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

	[JsonPropertyName("total_deadpacks")]
    public long? TotalDeadPacks { get; set; }

	[JsonPropertyName("new_deadpacks_24hour")]
    public long? NewDeadPacks24Hour { get; set; }

	[JsonPropertyName("new_deadpacks_1hour")]
    public long? NewDeadPacks1Hour { get; set; }

	public override string ToString()
	{	
		string alert = "";

		//calculate alerts
		if (NewDeadPacks1Hour > 0)
			alert =  $" ({NewDeadPacks1Hour}/{NewDeadPacks24Hour}/{TotalDeadPacks})";
		else if (NewDeadPacks24Hour > 0)
			alert =  $" ({NewDeadPacks24Hour}/{TotalDeadPacks})";
		else if (TotalDeadPacks > 0)
			alert =  $" ({TotalDeadPacks})";
		else
			alert = "";
		
		

		if (String.IsNullOrEmpty(Email))
			return $"{Name} {alert}";
		else
			return $"{Name} ({Email}) {alert}";
	}

}