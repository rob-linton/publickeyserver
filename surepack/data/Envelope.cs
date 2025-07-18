using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace suredrop;

public class Envelope
{

    [JsonPropertyName("to")]
    public required List<Recipient> To { get; set; }

    [JsonPropertyName("from")]
    public required string From { get; set; }

    [JsonPropertyName("created")]
    public required long Created { get; set; }

    [JsonPropertyName("version")]
    public required string Version { get; set; }


	[JsonPropertyName("key-type")]
    public string? KeyType { get; set; }

	[JsonPropertyName("pqe-key-type")]
    public string? PqeKeyType { get; set; }

	[JsonPropertyName("encryption-algorithm")]
    public string? EncryptionAlgorithm { get; set; }

	[JsonPropertyName("compression")]
    public string? Compression { get; set; }



	// load an envelope from a zip file
	public static Envelope LoadFromFile(string file)
	{
		// open the zip file
		using (ZipArchive archive = ZipFile.OpenRead(file))
		{
			// get the envelope
			var entry = archive.GetEntry("envelope") ?? throw new Exception("Could not find envelope.json in package");
			using (var stream = entry.Open())
			{
				Envelope envelope = JsonSerializer.Deserialize<Envelope>(stream) ?? throw new Exception("Could not deserialize envelope");
				return envelope;
			}
		}
	}


}

