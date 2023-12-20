using CommandLine;

namespace jdoe.Verbs;

public class Options
{
	[Option('v', "verbose", Default = 0, HelpText = "Set output to verbose messages.")]
	public int Verbose { get; set; } = 0;	

	[Option('p', "password", Default = "", HelpText = "Enter password")]
	public string Password { get; set; } = "";	

	[Option('d', "domain", HelpText = "Domain name")]
	public string Domain { get; set; } = "";

}
