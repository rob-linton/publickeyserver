#pragma warning disable 1998
using CommandLine;

namespace jdoe.Verbs;

[Verb("unpack", HelpText = "Unpack a package.")]
public class UnpackOptions : Options
{
   [Option('f', "file", Required = true, HelpText = "Package file to be processed.")]
    public IEnumerable<string>? InputFiles { get; set; }
}

class Unpack 
{
	public static async Task<int> Execute(UnpackOptions opts)
	{
		return 0;
	}
}