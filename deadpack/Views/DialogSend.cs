using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogSend
{
	
  	private TextField input = new TextField();
	private Label progressLabel = new Label("");

	// build
	public void Build(SendOptions opts)
	{

		Globals.ClearProgressSource();
		ProgressBar overallProgress = new ProgressBar();
		ProgressBar fileProgress = new ProgressBar();
		progressLabel.Text = Misc.UpdateProgressBarLabel(0, 0, "Sending");
		Label bytesSent = new Label("");
		bool error = false;

		
		var ok = new Button("Send");
		ok.Clicked += async () => {

			

			opts.File = input.Text.ToString();
			
			// report on overall progress
			var progressOverall = new Progress<StatusUpdate>(StatusUpdate =>
			{
				overallProgress.Fraction = StatusUpdate.Index / StatusUpdate.Count;
				if (!error)
				{
					if (String.IsNullOrEmpty(StatusUpdate.Status))
						progressLabel.Text = Misc.UpdateProgressBarLabel(StatusUpdate.Index, StatusUpdate.Count, "Sending", true);
					else
					{
						error = true;
						progressLabel.Text = StatusUpdate.Status;
					}
				}
				
			});

			// report on file progress
			var progressFile = new Progress<StatusUpdate>(StatusUpdate =>
			{
				fileProgress.Fraction = StatusUpdate.Index / StatusUpdate.Count;
				string bytes = Math.Round(StatusUpdate.Index / 1000 / 1000, 0).ToString() + " MB";
				bytesSent.Text = bytes;
			});

			int result = await Send.Execute(opts, progressFile, progressOverall);
			
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
		// add the input location
		//
		input = new TextField() 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill()-1, 
			Height = Dim.Fill(),
			ColorScheme = Globals.BlueOnWhite
		};
		
		FrameView viewInput = new FrameView ("Input DeadPack file (leave blank to send all in the outbox)") {
        	X = 1,
            Y = 0,
            Width = Dim.Fill () - 1,
            Height = 4,
        };
		viewInput.Border.BorderStyle = BorderStyle.Single;
		viewInput.Add(input);

		//
		// add the progress bars
		//

		fileProgress = new ProgressBar () {
			X = 2,
			Y = 4,
			Width = Dim.Fill () - 1,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};

		bytesSent = new Label("") 
		{ 
			X = Pos.Right(fileProgress) - 12, 
			Y = 5,
			Width = 10, 
			Height = 1,
			ColorScheme = Globals.WhiteOnBlue,
			TextAlignment = TextAlignment.Right
		};

		overallProgress = new ProgressBar () {
			X = 2,
			Y = Pos.Bottom(fileProgress) + 1,
			Width = Dim.Fill () - 2,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};
		
		progressLabel = new Label("") 
		{ 
			X = 2, 
			Y = Pos.Bottom(overallProgress) + 1, 
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
		var listViewProgress = new ListView(Globals.GetProgressSource()) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };

		viewProgress.Add(listViewProgress);
	
		dialog.Add (viewInput, fileProgress, overallProgress, progressLabel, viewProgress, bytesSent);
		Application.Run (dialog);

		return;
	}

	
	
}