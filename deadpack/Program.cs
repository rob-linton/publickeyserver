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

		// if the first parameter does not have a - then insert the word "gui"
		// this is a hack to allow the gui to be run without the word "gui"
		if ((args.Length > 0 && args[0].StartsWith("-")) || args.Length == 0)
		{
			List<string> newArgs = new();
			newArgs.Add("gui");
			newArgs.AddRange(args);
			args = newArgs.ToArray();
		}

		var opts = Parser.Default.ParseArguments<Options, PackOptions, UnpackOptions, CreateOptions, CertifyOptions, ListOptions, SendOptions, ReceiveOptions, VerifyOptions, GuiOptions>(args);

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
		errors => 1);
	}

    static async Task<int> Main(string[] args)
    {	
		return await ParseOptions(args);

    }
	
}

