using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogOpenDeadPack 
{
	// build
	public Enums.DialogReturn Build(dynamic e, string location)
	{
		DeadPack deadPack = (DeadPack)e.Value;

		Enums.DialogReturn result = Enums.DialogReturn.Cancel;

		var cancel = new Button("Close");
		cancel.Clicked += () => Application.RequestStop ();

		var extract = new Button("Extract");
		extract.Clicked += () => { Application.RequestStop (); result = Enums.DialogReturn.Extract; };

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog ("", width, height, cancel, extract);
		dialog.Border.BorderStyle = BorderStyle.None;
		dialog.ColorScheme = Colors.Base;

		//
		// add the from
		//
		var from = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill(),
			TabStop = false,
			Text = deadPack.From,
			ReadOnly = true,
			ColorScheme = Globals.StandardColors
		};


		Window viewFrom = new Window ("From") {
        	X = 1,
            Y = 1,
            Width = Dim.Percent(50) - 1,
            Height = 4,
			TabStop = false,
        };
		viewFrom.Border.BorderStyle = BorderStyle.Single;
		viewFrom.Add(from);

		//
		// add the Date Time
		//
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		dateTime = dateTime.AddSeconds(deadPack.Timestamp).ToLocalTime();
		string dt = dateTime.ToString("dd-MMM-yyyy hh:mmtt");


		var created = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 1,
			Height = Dim.Fill(),
			TabStop = false,
			Text = dt,
			ReadOnly = true,
			ColorScheme = Globals.StandardColors
		};


		Window viewCreated = new Window ("Created") {
        	X = Pos.Percent(50),
            Y = 1,
            Width = Dim.Percent(50),
            Height = 4,
			TabStop = false,
        };
		viewCreated.Border.BorderStyle = BorderStyle.Single;
		viewCreated.Add(created);


		//
		// add the subject
		//
		var subject = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 1,
			Height = Dim.Fill(),
			Text = deadPack.Subject,
			ReadOnly = true,
			TabStop = false,
			ColorScheme = Globals.StandardColors
		};

		Window viewSubject = new Window ("Subject") {
        	X = 1,
            Y = 5,
            Width = Dim.Fill () - 1,
            Height = 4,
			TabStop = false,
        };
		viewSubject.Border.BorderStyle = BorderStyle.Single;
		viewSubject.Add(subject);

		//
		// add the message body
		//
		var message = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill(),
			Text = deadPack.Message,
			ReadOnly = true,
			TabStop = false,
			ColorScheme = Globals.StandardColors
		};

		Window viewMessage = new Window ("Message") {
        	X = 1,
            Y = 9,
            Width = Dim.Fill () - 1,
            Height = 7,
			TabStop = false,
        };
		viewMessage.Border.BorderStyle = BorderStyle.Single;
		viewMessage.Add(message);

		//
		// add the list of files
		//
		// create 2 views left and right
		Window viewLeft = new Window ("Files") {
			X = 1,
			Y = 16,
			Width = Dim.Fill () - 61,
			Height = Dim.Fill () - 2
		};
		var files = new ListView(deadPack.Files)
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill() - 2,
			ColorScheme = Globals.YellowColors
		};

		viewLeft.Add(files);

		//
		// add the list of recipients
		//
		Window viewRight = new Window ("Recipients") {
        	X = dialog.Frame.Width - 63,
            Y = 16,
            Width = 60,
            Height = Dim.Fill () - 2,
			TabStop = false,
        };
		var recipients = new ListView(deadPack.Recipients) 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill()-2, 
			Height = Dim.Fill() - 2,
			TabStop = false,
		};
		
		viewRight.Add(recipients);

	
		dialog.Add (viewFrom, viewCreated, viewSubject, viewMessage, viewRight);
		dialog.Add (viewLeft);
		Application.Run (dialog);

		return result;
	}
}