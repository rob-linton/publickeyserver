namespace jdoe;

class CaCertsResult
{
	public string? origin { get; set; }
	public required List<string> cacerts { get; set; }
}