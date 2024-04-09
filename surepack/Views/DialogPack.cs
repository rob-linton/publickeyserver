using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace suredrop.Verbs;

public class DialogPack
{
	
  	private TextField output = new TextField();
	private Label progressLabel = new Label("");
	private ProgressBar progressBar = new ProgressBar ();
	private ProgressBar progressBarBlock = new ProgressBar ();
	

	// build
	public void Build(PackOptions opts)
	{
		bool error = false;

		Globals.ClearProgressSource();
		var progress = new Progress<StatusUpdate>(StatusUpdate =>
		{
			if (StatusUpdate.BlockCount != 0)
			{
				progressBarBlock.Fraction = StatusUpdate.BlockIndex / StatusUpdate.BlockCount;
			}
			else
			{
				progressBar.Fraction = StatusUpdate.Index / StatusUpdate.Count;
				if (!error)
				{
					if (String.IsNullOrEmpty(StatusUpdate.Status))
						progressLabel.Text = Misc.UpdateProgressBarLabel(StatusUpdate.Index, StatusUpdate.Count, "Packed");
					else
					{
						error = true;
						progressLabel.Text = StatusUpdate.Status;
					}
				}
			}		
		});

		var ok = new Button("Pack to a File");
		ok.Clicked += async () => {
			progressBar.Fraction = 0.0F;
			progressLabel.Text = Misc.UpdateProgressBarLabel(0, 0, "Packing");
			progressLabel.SetNeedsDisplay();
			opts.Output = output.Text.ToString() ?? "output.surepack";
			
			// report on progress and execute
			int result = await Pack.Execute(opts, progress);
			new DialogMessage(progressLabel.Text.ToString()??"", "Packing Complete");
			
		};

		var outBox = new Button("Pack to Outbox");
		outBox.Clicked += async () => {
			progressBar.Fraction = 0.0F;
			progressLabel.Text = Misc.UpdateProgressBarLabel(0, 0, "Packing");
			progressLabel.SetNeedsDisplay();

			string timestamp = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();
			string filename = $"{timestamp}-{Guid.NewGuid()}.surepack";
			opts.Output = Storage.GetSurePackDirectoryOutbox(filename);
			
			// report on progress and execute
			int result = await Pack.Execute(opts, progress);
			new DialogMessage(progressLabel.Text.ToString()??"", "Packing Complete");
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
			Height = Dim.Fill(),
			ColorScheme = Globals.BlueOnWhite
		};
		// set the output directory to the default download directory
		//output.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/Downloads";
		string readableDate = DateTime.Now.ToString("dd-MMM-yyyy hh.mmtt");
		output.Text = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", $"{readableDate}.surepack");

		FrameView viewOutput = new FrameView ("File Location") {
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
		progressBarBlock = new ProgressBar () {
			X = 2,
			Y = 4,
			Width = Dim.Fill () - 1,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};
		
		progressBar = new ProgressBar () {
			X = 2,
			Y = Pos.Bottom(progressBarBlock) + 1,
			Width = Dim.Fill () - 1,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};
		
		progressLabel = new Label("") 
		{ 
			X = 2, 
			Y = Pos.Bottom(progressBar) + 1, 
			Width = Dim.Fill () - 1, 
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
		var listViewProgress = new ListView(Globals.GetProgressSource()) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };

		viewProgress.Add(listViewProgress);
	
		dialog.Add (viewOutput, progressBar, progressBarBlock, progressLabel, viewProgress);
		Application.Run (dialog);

		return;
	}

	
	
}