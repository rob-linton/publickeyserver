using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogUnpack
{
	// build
	public Enums.DialogReturn Build(string input, String alias)
	{
		// get the current directory
		string currentDirectory = Environment.CurrentDirectory;

		Globals.Progress.Clear();

		Enums.DialogReturn result = Enums.DialogReturn.Cancel;

		var ok = new Button("Go");
		ok.Clicked += async () => { 
			UnpackOptions opts = new UnpackOptions()
			{
				Alias = alias,
				Output = currentDirectory,
				File = input
			};
			await Unpack.Execute(opts);
		};

		var cancel = new Button("Close");
		cancel.Clicked += () => Application.RequestStop ();

		var dialog = new Dialog ("", 0, 0, ok, cancel);
		
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;
		
		//
		// add the output location
		//
		var output = new TextView() { X = 1, Y = 1, Width = Dim.Fill()-1, Height = Dim.Fill() };
		output.Text = currentDirectory;
		//output.ColorScheme = Globals.YellowColors;

		Window viewOutput = new Window ("Output Location") {
        	X = 1,
            Y = 0,
            Width = Dim.Fill () - 1,
            Height = 4,
        };
		viewOutput.Border.BorderStyle = BorderStyle.Single;
		viewOutput.Add(output);

		//
		// add the progress view list
		//
		Window viewProgress = new Window ("Extract Verification") {
			X = 1,
			Y = 4,
			Width = Dim.Fill () - 1,
			Height = Dim.Fill () - 2
		};
		var listViewProgress = new ListView(Globals.Progress) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };
		Globals.ProgressListView = listViewProgress;

		viewProgress.Add(listViewProgress);
	
		dialog.Add (viewOutput, viewProgress);
		Application.Run (dialog);

		return result;
	}
}