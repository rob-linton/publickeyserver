using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogPack
{
	
  	private TextField output = new TextField();

	// build
	public void Build(PackOptions opts)
	{

		Globals.ProgressSource.Clear();


		var ok = new Button("Pack to a File");
		ok.Clicked += async () => {

			Globals.UpdateProgressBar(0, 0, "Packing to File");

			opts.Output = output.Text.ToString();
			
			// report on progress
			var progress = new Progress<StatusUpdate>(StatusUpdate =>
			{
				Globals.UpdateProgressBar(StatusUpdate.Index, StatusUpdate.Count, "Packing");
			});

			int result = await Pack.Execute(opts, progress);
			
		};

		var outBox = new Button("Pack to Outbox");
		outBox.Clicked += async () => {

			Globals.UpdateProgressBar(0, 0, "Packing to Outbox");

			string timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();
			string filename = $"{timestamp}-{Guid.NewGuid()}.deadpack";
			opts.Output = Path.Join(Storage.GetDeadPackDirectoryOutbox(""), filename);
			
			// report on progress
			var progress = new Progress<StatusUpdate>(StatusUpdate =>
			{
				Globals.UpdateProgressBar(StatusUpdate.Index, StatusUpdate.Count, "Packing");
			});

			int result = await Pack.Execute(opts, progress);
			
		};

		var cancel = new Button("Close");
		cancel.Clicked += () =>
		{	
			Application.RequestStop();
		};

		var dialog = new Dialog ("", 0, 0, cancel, ok, outBox);
		
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
			Height = Dim.Fill() 
		};
		// set the output directory to the default download directory
		//output.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Downloads";
		string readableDate = DateTime.Now.ToString("yyyy-MM-dd-HH.mm.ss");
		output.Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{readableDate}.deadpack");

		Window viewOutput = new Window ("Output Location") {
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
		};
		
		Label progressLabel = new Label("") 
		{ 
			X = 2, 
			Y = Pos.Bottom(extractProgress) + 1, 
			Width = Dim.Fill() - 1, 
			Height = 1 
		};

		//
		// add the progress view list
		//
		Window viewProgress = new Window ("Packing Slip Verification") {
			X = 1,
			Y = Pos.Bottom(progressLabel) + 1, 
			Width = Dim.Fill () - 1,
			Height = Dim.Fill () - 2
		};
		var listViewProgress = new ListView(Globals.ProgressSource) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };
		
		// setup the progress
		Globals.SetupProgress(listViewProgress, extractProgress, progressLabel);

		viewProgress.Add(listViewProgress);
	
		dialog.Add (viewOutput, extractProgress, progressLabel, viewProgress);
		Application.Run (dialog);

		return;
	}

	
	
}