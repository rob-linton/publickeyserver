namespace jdoe;

class Envelope
{
	public string? from { get; set; }
	public required List<KeyValuePair<string, string>> to { get; set; }
}