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

		var ok = new Button("Ok");
		ok.Clicked += () => { Application.RequestStop (); result = Enums.DialogReturn.Ok; };

		var cancel = new Button("Cancel");
		cancel.Clicked += () => Application.RequestStop ();

		var extract = new Button("Extract");
		extract.Clicked += () => { Application.RequestStop (); result = Enums.DialogReturn.Extract; };

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog ("", width, height, ok, cancel, extract);
		dialog.Border.BorderStyle = BorderStyle.None;
		dialog.ColorScheme = Colors.Base;

		//
		// add the from
		//
		var from = new TextView() { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() };
		from.Text = deadPack.From;
		from.ReadOnly = true;
		from.ColorScheme = Globals.StandardColors;
		

		Window viewFrom = new Window ("From") {
        	X = 1,
            Y = 1,
            Width = Dim.Percent(50) - 1,
            Height = 4,
        };
		viewFrom.Border.BorderStyle = BorderStyle.Single;
		viewFrom.Add(from);

		//
		// add the Date Time
		//
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
		dateTime = dateTime.AddSeconds(deadPack.Timestamp).ToLocalTime();
		string dt = dateTime.ToString("dd-MMM-yyyy hh:mmtt");


		var created = new TextView() { X = 1, Y = 1, Width = Dim.Fill()-1, Height = Dim.Fill() };
		created.Text = dt;
		created.ReadOnly = true;
		created.ColorScheme = Globals.StandardColors;
		

		Window viewCreated = new Window ("Created") {
        	X = Pos.Percent(50),
            Y = 1,
            Width = Dim.Percent(50),
            Height = 4,
        };
		viewCreated.Border.BorderStyle = BorderStyle.Single;
		viewCreated.Add(created);


		//
		// add the subject
		//
		var subject = new TextView() { X = 1, Y = 1, Width = Dim.Fill()-1, Height = Dim.Fill() };
		subject.Text = deadPack.Subject;
		subject.ReadOnly = true;
		subject.ColorScheme = Globals.StandardColors;

		Window viewSubject = new Window ("Subject") {
        	X = 1,
            Y = 5,
            Width = Dim.Fill () - 1,
            Height = 4,
        };
		viewSubject.Border.BorderStyle = BorderStyle.Single;
		viewSubject.Add(subject);

		//
		// add the message body
		//
		var message = new TextView() { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() };
		message.Text = deadPack.Message;
		message.ReadOnly = true;
		message.ColorScheme = Globals.StandardColors;

		Window viewMessage = new Window ("Message") {
        	X = 1,
            Y = 9,
            Width = Dim.Fill () - 1,
            Height = 7,
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
		var files = new ListView(deadPack.Files) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };
		files.ColorScheme = Globals.StandardColors;

		viewLeft.Add(files);

		//
		// add the list of recipients
		//
		Window viewRight = new Window ("Recipients") {
        	X = dialog.Frame.Width - 63,
            Y = 16,
            Width = 60,
            Height = Dim.Fill () - 2
        };
		var recipients = new ListView(deadPack.Recipients) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };
		viewRight.Add(recipients);

	
		dialog.Add (viewFrom, viewCreated, viewSubject, viewMessage, viewRight);
		dialog.Add (viewLeft);
		Application.Run (dialog);

		return result;
	}
}