using System.Text.Json.Serialization;

namespace deadrop;

public class Status
{
	[JsonPropertyName("application")]
	public required string Application { get; set; }

	[JsonPropertyName("version")]
	public string? Version { get; set; }

	[JsonPropertyName("origin")]
	public string? Origin { get; set; }

	[JsonPropertyName("uptime")]
	public long? Uptime { get; set; }

	[JsonPropertyName("certs-served")]
	public long? CertsServed { get; set; }

	[JsonPropertyName("certs-enrolled")]
	public long? CertsEnrolled { get; set; }

	[JsonPropertyName("anonymous")]
	public string? anonymous { get; set; }

	[JsonPropertyName("root-ca-signature")]
	public required List<string> RootCaSignature { get; set; }

	[JsonPropertyName("max-bucket-files")]
	public long? MaxBucketFiles { get; set; }

	[JsonPropertyName("max-bucket-size")]
	public long? MaxBucketSize { get; set; }

	[JsonPropertyName("max-package-size")]
	public long? MaxPackageSize { get; set; }
	
}