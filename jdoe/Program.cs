using CommandLine;
namespace jdoe;

public class Options
{
    [Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
    public IEnumerable<string>? InputFiles { get; set; }

	[Option('v', "verbose", Default = false, HelpText = "Set output to verbose messages.")]
	public bool Verbose { get; set; } = false;
}

class Program
{
    static void Main(string[] args)
    {
		Console.WriteLine("Hello World!");

        Parser.Default.ParseArguments<Options>(args)
           .WithParsed<Options>(o =>
           {
               if (o.Verbose!)
               {
                   Console.WriteLine($"Verbose mode is enabled. Verbose level: {o.Verbose}");
               }

               Console.WriteLine($"Read files: {string.Join(",", o.InputFiles!)}");
           });
    }
}
