using CommandLine;

namespace jdoe.Verbs;

public class Options
{
	[Option('v', "verbose", Default = 0, HelpText = "Set output to verbose messages.")]
	public int Verbose { get; set; } = 0;	
}
