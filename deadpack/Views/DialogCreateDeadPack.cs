using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogCreateDeadPack
{
	// build
	public Enums.DialogReturn Build(string alias = "")
	{


		TextView from = null;
		TextView subject = null;
		TextView message = null;
		string deadPackMessage = "";
		List<string> deadPackRecipients = new List<string>();
		string[] deadPackFiles = new string[0];
		bool recursive = false;
		string filename = "";


		Enums.DialogReturn result = Enums.DialogReturn.Cancel;

		var cancel = new Button("Close");
		cancel.Clicked += () => Application.RequestStop ();

		var extract = new Button("Create");
		extract.Clicked += () => 
		{
			PackOptions opts = new PackOptions()
			{
				File = filename,
				Recurse = recursive,
				InputAliases = deadPackRecipients,
				Output = "",
				Message = message.Text.ToString(),
				Subject = subject.Text.ToString(),
				From = from.Text.ToString(),
			};
			
			new DialogPack().Build(opts); 

			//Application.RequestStop (); 
		};

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog ("", width, height, cancel, extract);
		dialog.Border.BorderStyle = BorderStyle.None;
		dialog.ColorScheme = Colors.Base;

		
		
		//
		// add the from
		//
		from = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill(),
			TabStop = false,
			ReadOnly = true,
			ColorScheme = Globals.BlueOnWhite
		};
		
		if (string.IsNullOrEmpty(alias))
		{
			// get a list of aliases
			List<Alias> aliases = Storage.GetAliases();
			List<string> source = new List<string>();
			foreach (var a in aliases)
			{
				source.Add(a.Name);
			}

			// create the select from list dialog
			DialogSelectFromList dialogSelectFromList = new DialogSelectFromList();
			from.Text = dialogSelectFromList.Build(source, "Select Alias");
		}

		FrameView viewFrom = new FrameView ("From") {
        	X = 1,
            Y = 1,
            Width = Dim.Percent(50) - 1,
            Height = 4,
			TabStop = false,
        };
		viewFrom.Border.BorderStyle = BorderStyle.Single;
		viewFrom.Add(from);

		// add a button
		//
		var addAlias = new Button("+ Select Alias")
		{
			X = 8,
			Y = 1,
			Width = 10,
			Height = 1
		
		};
		addAlias.Clicked += () => 
		{
			// get a list of aliases
			List<Alias> aliases = Storage.GetAliases();
			List<string> source = new List<string>();
			foreach (var a in aliases)
			{
				source.Add(a.Name);
			}

			// create the select from list dialog
			DialogSelectFromList dialogSelectFromList = new DialogSelectFromList();
			from.Text = dialogSelectFromList.Build(source, "Select Alias");
			
		};

		//
		// add the Date Time
		//
		DateTime dateTime = DateTime.UtcNow;
		string dt = dateTime.ToString("dd-MMM-yyyy hh.mmtt");


		var created = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 1,
			Height = Dim.Fill(),
			TabStop = false,
			Text = dt,
			ReadOnly = true,
			ColorScheme = Globals.BlueOnWhite
		};


		FrameView viewCreated = new FrameView ("Created") {
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
		subject = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 1,
			Height = Dim.Fill(),
			ReadOnly = false,
			TabStop = true,
			Multiline = false,
			ColorScheme = Globals.BlueOnWhite
		};

		FrameView viewSubject = new FrameView ("Subject") {
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
		message = new TextView
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill(),
			ReadOnly = false,
			TabStop = true,
			ColorScheme = Globals.BlueOnWhite
		};

		FrameView viewMessage = new FrameView ("Message") {
        	X = 1,
            Y = 9,
            Width = Dim.Fill () - 1,
            Height = 7,
			TabStop = true,
        };
		viewMessage.Border.BorderStyle = BorderStyle.Single;
		viewMessage.Add(message);

		//
		// add the list of files
		//
		// create 2 views left and right
		FrameView viewLeft = new FrameView ("Files") {
			X = 1,
			Y = 16,
			Width = Dim.Fill () - 61,
			Height = Dim.Fill () - 2
		};
		var files = new ListView(deadPackFiles)
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill() - 2,
			ColorScheme = Globals.StandardColors
		};
		viewLeft.Add(files);

		//
		// add the files add button
		//
		var addFiles = new Button("+ Add")
		{
			X = 9,
			Y = Pos.Top(viewLeft),
			Width = 10,
			Height = 1
		
		};
		addFiles.Clicked += () => 
		{
			(filename, recursive, deadPackFiles) = new DialogSelectFiles().Build("Select Files");
			files.SetSource(deadPackFiles);
		};

		//
		// add the list of recipients
		//
		FrameView viewRight = new FrameView ("Recipients") {
        	X = dialog.Frame.Width - 63,
            Y = 16,
            Width = 60,
            Height = Dim.Fill () - 2,
			TabStop = false,
        };
		var recipients = new ListView(deadPackRecipients) 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill()-2, 
			Height = Dim.Fill() - 2,
			TabStop = false,
		};
		viewRight.Add(recipients);

		//
		// add the files add button
		//
		var addRecipient = new Button("+ Add")
		{
			X = dialog.Frame.Width - 50,
            Y = 16,
			Width = 10,
			Height = 1
		
		};
		addRecipient.Clicked += async () => 
		{
			string lookupAlias = await new DialogSelectAliases().Build("Select Alias", from.Text.ToString());
			deadPackRecipients.Add(lookupAlias);
			recipients.SetSource(deadPackRecipients);
		};


	
		dialog.Add (viewFrom, addAlias, viewCreated, viewSubject, viewMessage, viewRight, viewLeft, addFiles, addRecipient);
		Application.Run (dialog);

		return result;

	}
}