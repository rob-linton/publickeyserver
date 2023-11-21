using CommandLine;
namespace jdoe;

public class Options
{
    [Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
    public IEnumerable<string>? InputFiles { get; set; }

	[Option('v', "verbose", Default = false, HelpText = "Set output to verbose messages.")]
	public int Verbose { get; set; } = 0;

	 [Option('a', "aliases", Required = true, HelpText = "Input destination aliases")]
    public IEnumerable<string>? InputAliases { get; set; }
}



public class Program
{
	public static Options ParseOptions(string[] args)
	{
		Options? result = null;

		Parser.Default.ParseArguments<Options>(args)
		.WithParsed<Options>(o => result = o);

		return result!;
	}

    static void Main(string[] args)
    {
		Console.WriteLine("Hello World!");
		
		var options = ParseOptions(args);
        
		if (options.Verbose > 0)
		{
			{
				Console.WriteLine($"Verbose mode is enabled. Verbose level: {options.Verbose}");
			}

			Console.WriteLine($"Read files: {string.Join(",", options.InputFiles!)}");
			Console.WriteLine($"Aliases: {string.Join(",", options.InputAliases!)}");
        };
    }
}
