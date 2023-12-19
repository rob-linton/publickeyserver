#pragma warning disable 1998

using CommandLine;
using jdoe.Verbs;
using System.Collections.Generic;
using System;

namespace jdoe;

public class Program
{
	public static async Task<int> ParseOptions(string[] args)
	{

		var opts = Parser.Default.ParseArguments<Options, PackOptions, UnpackOptions, CreateOptions, VerifyOptions>(args);

		return opts.MapResult(
		(CreateOptions opts) 	=> Verbs.Create.Execute(opts).Result,
		(PackOptions opts) 		=> Verbs.Pack.Execute(opts).Result,
		(UnpackOptions opts) 	=> Verbs.Unpack.Execute(opts).Result,
		(VerifyOptions opts) 	=> Verbs.Verify.Execute(opts).Result,
		errors => 1);
	}

    static async Task<int> Main(string[] args)
    {	
		return await ParseOptions(args);
    }
}
