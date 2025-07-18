/*
 * Copyright (c) 2025 PublicKeyServer Contributors
 * 
 * This work is licensed under the PublicKeyServer Non-Monetization Open Source License.
 * 
 * You may use, copy, modify, and distribute this work for any purpose, including
 * commercial purposes, provided you do not monetize it or charge any fees.
 * The software must remain free for all users.
 * 
 * See the LICENSE file in the root directory for complete license terms.
 */

#pragma warning disable 1998

using CommandLine;
using suredrop.Verbs;
using System.Collections.Generic;
using System;

namespace suredrop;

public class Program
{
	
	
	public static async Task<int> ParseOptions(string[] args)
	{
		// load up the settings file
		Settings settings = Storage.GetSettings();
		//Globals.Domain = settings.Domain;
		Globals.Password = settings.Password;
		
		// Special handling for --help and --version to show overall help
		if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "--version"))
		{
			// Don't modify args, let the parser handle it directly
		}
		// if the first parameter does has a - then insert the word "gui"
		// this is a hack to allow the gui to be run without the word "gui"
		// or if there are no parameters then run the gui
		else if ((args.Length > 0 && args[0].StartsWith("-")) || args.Length == 0)
		{
			List<string> newArgs = new();
			newArgs.Add("gui");
			newArgs.AddRange(args);
			args = newArgs.ToArray();
		}

		// now check to see if a surepack file was passed in on the command line
		// if so, then we need to open it
		if (args.Length > 0 && args[0] != "--help" && args[0] != "-h" && args[0] != "--version")
		{
			if (args[0].EndsWith(".surepack"))
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

		// Create a parser with custom settings to ensure help is always regenerated
		var parser = new Parser(with => 
		{
			with.HelpWriter = Console.Out;
			with.EnableDashDash = true;
			with.CaseInsensitiveEnumValues = true;
		});

		var opts = parser.ParseArguments<Options, PackOptions, UnpackOptions, CreateOptions, CertifyOptions, ListOptions, SendOptions, ReceiveOptions, VerifyOptions, GuiOptions, DeleteOptions, AboutOptions>(args);

		var result = opts.MapResult(
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
		(AboutOptions opts) 		=> Verbs.About.Execute(opts).Result,
		errors => {
			Console.WriteLine("\nFor detailed help and examples, visit:");
			Console.WriteLine(Misc.SanitizeForWindows("📚 Help Center: https://rob-linton.github.io/publickeyserver/"));
			Console.WriteLine(Misc.SanitizeForWindows("📖 User Manual: https://rob-linton.github.io/publickeyserver/HELP.html"));
			return 1;
		});

		return result;
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
			Console.WriteLine("\nFor help, visit: https://rob-linton.github.io/publickeyserver/HELP.html");
			return 1;
		}

    }
	
}

