using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogSend
{

	string _currentReceiveAlias = "";

	// build
	public void Build(SendOptions optsSend, ReceiveOptions optsReceive, bool auto = false)
	{
		TextField input = new TextField();
		Globals.ClearProgressSource();

		ProgressBar overallProgressSend = new ProgressBar();
		ProgressBar fileProgressSend = new ProgressBar();
		Label progressLabelSend = new Label(Misc.UpdateProgressBarLabel(0, 0, "Sending"));
		Label bytesSend = new Label("");

		ProgressBar overallProgressReceive = new ProgressBar();
		ProgressBar fileProgressReceive = new ProgressBar();
		Label progressLabelReceive = new Label(Misc.UpdateProgressBarLabel(0, 0, "Receiving"));
		Label bytesReceive = new Label("");
		
		bool errorSend = false;
		bool errorReceive = false;

		//
		// send progress reporters
		//

		// report on overall progress
		var progressOverallSend = new Progress<StatusUpdate>(StatusUpdate =>
		{
			overallProgressSend.Fraction = StatusUpdate.Index / StatusUpdate.Count;
			if (!errorSend)
			{
				if (String.IsNullOrEmpty(StatusUpdate.Status))
					progressLabelSend.Text = Misc.UpdateProgressBarLabel(StatusUpdate.Index, StatusUpdate.Count, "Sending", true);
				else
				{
					errorSend = true;
					progressLabelSend.Text = StatusUpdate.Status;
				}
			}
		});

		// report on file progress
		var progressFileSend = new Progress<StatusUpdate>(StatusUpdate =>
		{
			fileProgressSend.Fraction = StatusUpdate.Index / StatusUpdate.Count;
			//string bytes = Misc.FormatBytes((long)StatusUpdate.Index);
			string bytes = Math.Round(StatusUpdate.Index / 1000 / 1000, 0).ToString() + " MB";
			bytesSend.Text = bytes;
		});

		//
		// receive progress reporters
		//
		var progressOverallAliasReceive = new Progress<StatusUpdate>(StatusUpdate =>
		{
			if (!string.IsNullOrEmpty(StatusUpdate.Status))
			{
				_currentReceiveAlias = StatusUpdate.Status;
				progressLabelReceive.Text = $"Checking {_currentReceiveAlias}";
			}
			else
			{
				_currentReceiveAlias = "";
				progressLabelReceive.Text = "";
			}

			
		});

		var progressOverallReceive = new Progress<StatusUpdate>(StatusUpdate =>
		{
			overallProgressReceive.Fraction = StatusUpdate.Index / StatusUpdate.Count;
			if (!errorReceive)
			{
				if (String.IsNullOrEmpty(StatusUpdate.Status))
					progressLabelReceive.Text = Misc.UpdateProgressBarLabel(StatusUpdate.Index, StatusUpdate.Count, $"Receiving {_currentReceiveAlias}", true);
				else
				{
					errorReceive = true;
					progressLabelReceive.Text = StatusUpdate.Status;
				}
			}
			
		});

		// report on file progress
		var progressFileReceive = new Progress<StatusUpdate>(StatusUpdate =>
		{
			fileProgressReceive.Fraction = StatusUpdate.Index / StatusUpdate.Count;
			//string bytes = Misc.FormatBytes((long)StatusUpdate.Index);
			string bytes = Math.Round(StatusUpdate.Index / 1000 / 1000, 0).ToString() + " MB";
			bytesReceive.Text = bytes;
		});

		var ok = new Button("Send/Receive");
		ok.Clicked += async () => {

			optsSend.File = input.Text.ToString();
			await Send.Execute(optsSend, progressFileSend, progressOverallSend);
			fileProgressSend.Fraction = 1.0F;
			overallProgressSend.Fraction = 1.0F;
			progressLabelSend.Text = "Sending complete";
			
			await Receive.Execute(optsReceive, progressFileReceive, progressOverallReceive, progressOverallAliasReceive);
			overallProgressReceive.Fraction = 1.0F;
			fileProgressReceive.Fraction = 1.0F;
			progressLabelReceive.Text = "Receiving complete";

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
		
		FrameView viewInput = new FrameView ("Input DeadPack file to send (Optional)") {
        	X = 1,
            Y = 0,
            Width = Dim.Fill () - 1,
            Height = 4,
        };
		viewInput.Border.BorderStyle = BorderStyle.Single;
		viewInput.Add(input);

		//
		// add the send progress bars
		//
		
		fileProgressSend = new ProgressBar () {
			X = 2,
			Y = Pos.Bottom(viewInput) + 1,
			Width = Dim.Fill () - 2,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};

		bytesSend = new Label("") 
		{ 
			X = Pos.Right(fileProgressSend) - 12, 
			Y = Pos.Bottom(fileProgressSend),
			Width = 11, 
			Height = 1,
			ColorScheme = Globals.WhiteOnBlue,
			TextAlignment = TextAlignment.Right
		};

		overallProgressSend = new ProgressBar () {
			X = 2,
			Y = Pos.Bottom(fileProgressSend) + 1,
			Width = Dim.Fill () - 2,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};
		
		progressLabelSend = new Label("") 
		{ 
			X = 2, 
			Y = Pos.Bottom(overallProgressSend) + 1, 
			Width = Dim.Fill() - 1, 
			Height = 1,
			ColorScheme = Globals.WhiteOnBlue
		};

		FrameView frameTitleSend = new FrameView("Send") 
		{ 
			X = 2, 
			Y = 1, 
			Width = Dim.Fill() - 1, 
			Height = 12,
			ColorScheme = Globals.WhiteOnBlue
		};
		frameTitleSend.Add(viewInput, fileProgressSend, overallProgressSend, progressLabelSend, bytesSend);

		//
		// add the receive progress bars
		//

		fileProgressReceive = new ProgressBar () {
			X = 2,
			Y = 1,
			Width = Dim.Fill () - 2,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};

		bytesReceive = new Label("") 
		{ 
			X = Pos.Right(fileProgressReceive) - 11, 
			Y = Pos.Bottom(fileProgressReceive),
			Width = 10, 
			Height = 1,
			ColorScheme = Globals.WhiteOnBlue,
			TextAlignment = TextAlignment.Right
		};

		overallProgressReceive = new ProgressBar () {
			X = 2,
			Y = Pos.Bottom(bytesReceive),
			Width = Dim.Fill () - 2,
			Height = 1,
			Fraction = 0.0F,
			ColorScheme = Globals.WhiteOnBlue
		};
		
		progressLabelReceive = new Label("") 
		{ 
			X = 2, 
			Y = Pos.Bottom(overallProgressReceive) + 1, 
			Width = Dim.Fill() - 1, 
			Height = 1,
			ColorScheme = Globals.WhiteOnBlue
		};

		FrameView frameTitleReceive = new FrameView("Receive") 
		{ 
			X = 2, 
			Y = Pos.Bottom(frameTitleSend) + 2, 
			Width = Dim.Fill() - 1, 
			Height = 9,
			ColorScheme = Globals.WhiteOnBlue
		};
		frameTitleReceive.Add(fileProgressReceive, overallProgressReceive, progressLabelReceive, bytesReceive);
		
		//
		// add the progress view list
		//
		/*
		FrameView viewProgress = new FrameView ("Packing Slip Verification") {
			X = 1,
			Y = Pos.Bottom(progressLabel) + 1, 
			Width = Dim.Fill () - 1,
			Height = Dim.Fill () - 2
		};
		var listViewProgress = new ListView(Globals.GetProgressSource()) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };

		viewProgress.Add(listViewProgress);
		*/

		dialog.Add (frameTitleSend, frameTitleReceive);
		Application.Run (dialog);

		return;
	}

	
	
}