using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogUnpack
{
	
  	private TextField output = new TextField();

	// build
	public void Build(string input, String alias)
	{

		Globals.ProgressSource.Clear();

		var ok = new Button("Go");
		ok.Clicked += async () => { 
			
			Globals.UpdateProgressBar(0, 0, "Unpacking");
			
			UnpackOptions opts = new UnpackOptions()
			{
				Alias = alias,
				Output = output.Text.ToString(),
				File = input
			};
			
			// report on progress
			var progress = new Progress<StatusUpdate>(StatusUpdate =>
			{
				Globals.UpdateProgressBar(StatusUpdate.Index, StatusUpdate.Count, "Unpacking");
			});

			if (String.IsNullOrEmpty(alias))
				await Unpack.Execute(opts, progress);
			else
				await Unpack.ExecuteInternal(opts, alias, progress);
			
		};

		var cancel = new Button("Close");
		cancel.Clicked += () =>
		{	
			Application.RequestStop();
		};

		var dialog = new Dialog ("", 0, 0, cancel, ok);
		
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;
		
		//
		// add the output location
		//
		output = new TextField() 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill()-1, 
			Height = Dim.Fill(),
			ColorScheme = Globals.BlueOnWhite
		};
		// set the output directory to the default download directory
		string readableDate = DateTime.Now.ToString("dd-MMM-yyyy hh.mmtt");
		output.Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{readableDate}.deadpack");
		
		

		FrameView viewOutput = new FrameView ("Output Location") {
        	X = 1,
            Y = 0,
            Width = Dim.Fill () - 1,
            Height = 4,
        };
		viewOutput.Border.BorderStyle = BorderStyle.Single;
		viewOutput.Add(output);

		//
		// add the progress bar
		//
		ProgressBar extractProgress = new ProgressBar () {
			X = 2,
			Y = 4,
			Width = Dim.Fill () - 1,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};
		
		Label progressLabel = new Label("") 
		{ 
			X = 2, 
			Y = Pos.Bottom(extractProgress) + 1, 
			Width = Dim.Fill() - 1, 
			Height = 1,
			ColorScheme = Globals.WhiteOnBlue
		};

		//
		// add the progress view list
		//
		FrameView viewProgress = new FrameView ("Packing Slip Verification") {
			X = 1,
			Y = Pos.Bottom(progressLabel) + 1, 
			Width = Dim.Fill () - 1,
			Height = Dim.Fill () - 2
		};
		var listViewProgress = new ListView(Globals.ProgressSource) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };
		
		// setup the progress
		Globals.SetupProgress(extractProgress, progressLabel);

		viewProgress.Add(listViewProgress);
	
		dialog.Add (viewOutput, extractProgress, progressLabel, viewProgress);
		Application.Run (dialog);

		return;
	}

	
	
}