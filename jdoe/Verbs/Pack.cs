#pragma warning disable 1998
using CommandLine;

namespace jdoe.Verbs;

[Verb("pack", HelpText = "Create a package.")]
public class PackOptions : Options
{
   [Option('f', "file", Required = true, HelpText = "Input files to be processed.")]
    public IEnumerable<string>? InputFiles { get; set; }

	 [Option('a', "aliases", Required = true, HelpText = "Input destination aliases (comma delimited)")]
    public IEnumerable<string>? InputAliases { get; set; }
}
class Pack 
{
	public static async Task<int> Execute(Options opts)
	{
		return 0;
	}
}