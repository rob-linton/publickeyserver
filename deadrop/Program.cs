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

		var opts = Parser.Default.ParseArguments<Options, PackOptions, UnpackOptions, CreateOptions, VerifyOptions, ListOptions, SendOptions, ReceiveOptions>(args);

		return opts.MapResult(
		(CreateOptions opts) 	=> Verbs.Create.Execute(opts).Result,
		(PackOptions opts) 		=> Verbs.Pack.Execute(opts).Result,
		(UnpackOptions opts) 	=> Verbs.Unpack.Execute(opts).Result,
		(VerifyOptions opts) 	=> Verbs.Verify.Execute(opts).Result,
		(ListOptions opts) 		=> Verbs.List.Execute(opts).Result,
		(SendOptions opts) 		=> Verbs.Send.Execute(opts).Result,
		(ReceiveOptions opts) 		=> Verbs.Receive.Execute(opts).Result,
		errors => 1);
	}

    static async Task<int> Main(string[] args)
    {	
		return await ParseOptions(args);

    }
}
