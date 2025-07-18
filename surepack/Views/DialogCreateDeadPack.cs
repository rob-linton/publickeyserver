using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using NStack;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace suredrop.Verbs;

public class DialogCreateSurePack
{
	// build
	public Enums.DialogReturn Build(string alias = "", string filename = "")
	{


		TextView from = new TextView();
		TextView subject = new TextView();
		TextView message = new TextView();
		//string surePackMessage = "";
		List<string> surePackRecipients = new List<string>();
		string[] surePackFiles = new string[0];
		bool recursive = false;
		RadioGroup compression = new RadioGroup();
		//string filename = "";


		Enums.DialogReturn result = Enums.DialogReturn.Cancel;

		var cancel = new Button("Close")
		{
		};
		cancel.Clicked += () => Application.RequestStop ();

		var extract = new Button("Create")
		{ 
			Enabled = false
		};
		extract.Clicked += () => 
		{
			PackOptions opts = new PackOptions()
			{
				File = filename,
				Recurse = recursive,
				InputAliases = surePackRecipients,
				Output = "",
				Message = message.Text.ToString(),
				Subject = subject.Text.ToString(),
				From = from.Text.ToString()??"",
				Compression = compression.SelectedItem == 0 ? "GZip" : compression.SelectedItem == 1 ? "Brotli" : "None", 
			};
			
			new DialogPack().Build(opts); 

			Application.RequestStop (); 
		};

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog ("Create SurePack", width, height, cancel, extract);
		dialog.Border.BorderStyle = BorderStyle.Double;
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
			ColorScheme = Globals.WhiteOnBlue
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
		else
		{
			from.Text = alias;
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
			ColorScheme = Globals.WhiteOnBlue
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
			Width = Dim.Fill() - 1,
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

		var files = new ListView(surePackFiles)
		{
			X = 1,
			Y = 1,
			Width = Dim.Fill() - 2,
			Height = Dim.Fill() - 2,
			ColorScheme = Globals.StandardColors
		};
		viewLeft.Add(files);

		//
		// add the compression type
		//
		// add a radio button
		compression = new RadioGroup(new ustring[] {"GZip", "Brotli", "None"})
		{
			X = Pos.Right(files) - 10,
			Y = 1,
			Width = 10,
			Height = 6
		};
		viewLeft.Add(compression);

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
			(filename, recursive, surePackFiles) = new DialogSelectFiles().Build("Select Files", filename);
			string[] listFiles = Misc.GetFiles(filename, false);
			files.SetSource(listFiles);
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
		var recipients = new ListView(surePackRecipients) 
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
			List<string> lookupAlias = await new DialogSelectAliases().Build("Select Alias", from.Text.ToString()??"");
			foreach (var a in lookupAlias)
			{
				// if it does not already exist then add it
				if (!surePackRecipients.Contains(a))
				{
					surePackRecipients.Add(a);
					extract.Enabled = true;
				}
			}
			recipients.SetSource(surePackRecipients);
		};

		

		dialog.Ready += () => 
		{
			if (!String.IsNullOrEmpty(filename))
			{
				string[] listFiles = Misc.GetFiles(filename, false);
				files.SetSource(listFiles);
			}
		};
	
		dialog.Add (viewFrom, addAlias, viewCreated, viewSubject, viewMessage, viewRight, viewLeft, addFiles, addRecipient);
		Application.Run (dialog);

		return result;

	}
}