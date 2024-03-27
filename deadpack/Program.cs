#pragma warning disable 1998

using CommandLine;
using deadrop.Verbs;
using System.Collections.Generic;
using System;

namespace deadrop;

public class Program
{
	
	
	public static async Task<int> ParseOptions(string[] args)
	{
		// load up the settings file
		Settings settings = Storage.GetSettings();
		Globals.Domain = settings.Domain;
		Globals.Password = settings.Password;
		
		// if the first parameter does has a - then insert the word "gui"
		// this is a hack to allow the gui to be run without the word "gui"
		// or if there are no parameters then run the gui
		if ((args.Length > 0 && args[0].StartsWith("-")) || args.Length == 0)
		{
			List<string> newArgs = new();
			newArgs.Add("gui");
			newArgs.AddRange(args);
			args = newArgs.ToArray();
		}

		// now check to see if a deadpack file was passed in on the command line
		// if so, then we need to open it
		if (args.Length > 0)
		{
			if (args[0].EndsWith(".deadpack"))
			{
				List<string> newArgs = new();
				newArgs.Add("gui");
				newArgs.Add("-u");
				newArgs.AddRange(args);
				args = newArgs.ToArray();
			}
			else if (args[0].Contains('.') || args[0].Contains('/') || args[0].Contains('\\'))
			{
				List<string> newArgs = new();
				newArgs.Add("gui");
				newArgs.Add("-i");
				newArgs.AddRange(args);
				args = newArgs.ToArray();
			}
		}

		var opts = Parser.Default.ParseArguments<Options, PackOptions, UnpackOptions, CreateOptions, CertifyOptions, ListOptions, SendOptions, ReceiveOptions, VerifyOptions, GuiOptions, DeleteOptions, ActionOptions>(args);

		return opts.MapResult(
		(CreateOptions opts) 	=> Verbs.Create.Execute(opts).Result,
		(PackOptions opts) 		=> Verbs.Pack.Execute(opts).Result,
		(UnpackOptions opts) 	=> Verbs.Unpack.Execute(opts).Result,
		(CertifyOptions opts) 	=> Verbs.Certify.Execute(opts).Result,
		(ListOptions opts) 		=> Verbs.List.Execute(opts).Result,
		(SendOptions opts) 		=> Verbs.Send.Execute(opts).Result,
		(ReceiveOptions opts) 		=> Verbs.Receive.Execute(opts).Result,
		(VerifyOptions opts) 		=> Verbs.Verify.Execute(opts).Result,
		(GuiOptions opts) 		=> Verbs.Gui.Execute(opts).Result,
		(DeleteOptions opts) 		=> Verbs.Delete.Execute(opts).Result,
		(ActionOptions opts) 		=> Verbs.Action.Execute(opts).Result,
		errors => 1);
	}

    static async Task<int> Main(string[] args)
    {	try
		{
			return await ParseOptions(args);
		}
		catch (Exception e)
		{
			Console.WriteLine("An error occurred: ");
			Console.WriteLine(e.Message);
			return 1;
		}

    }
	
}

