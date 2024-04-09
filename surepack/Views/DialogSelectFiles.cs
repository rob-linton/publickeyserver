using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace suredrop.Verbs;

public class DialogSelectFiles
{
	// build
	public (string, bool, string[] source) Build(string title, string filename = "")
	{	
		if (String.IsNullOrEmpty(filename))
		{
			filename = "*";
		}

		string[] source = new string[0];
		TextField textBoxPath = new TextField("");
		TextField textBoxPattern = new TextField("");

		// return value
		
		bool recursive = false;
		//string lastSearch = "";

		// create a label
		var label = new Label("Enter path and filename (* wildcards ok)") { X = 1, Y = 1, Width = 30, Height = 1 };
		
		// create the listView
		var listView = new ListView(source) 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill() - 2, 
			Height = Dim.Fill() - 2,
		};

		// add a select folder button
		var selectFolder = new Button("Select Folder")
		{
			X = 1,
			Y = 4,
			//Width = 15,
			Height = 1
		};
		selectFolder.Clicked += () =>
		{
			var dialog = new OpenDialog("Select Folder", "Select");
			dialog.CanChooseFiles = false;
			dialog.CanChooseDirectories = true;
			Application.Run(dialog);
			
			// get the dialog result
			if (!dialog.Canceled)
			{
				// remove the current directory from the front
				textBoxPath.Text = dialog.FilePath.Replace(Environment.CurrentDirectory + Path.DirectorySeparatorChar, "");
				textBoxPath.Text = Path.Join(textBoxPath.Text.ToString(), "*");

				GetFiles(recursive, textBoxPath, listView, ref source);
			}
		};
	
		
		

		FrameView frame = new FrameView("Files")
		{
			X = 1,
			Y = 7,
			Width = Dim.Fill() - 1,
			Height = Dim.Fill() - 1,
			TabStop = false
		};
		frame.Border.BorderStyle = BorderStyle.Single;
		frame.Add(listView);
		
		
		// create a textbox
		textBoxPath = new TextField(filename) 
		{ 
			X = 1, 
			Y = Pos.Bottom(label), 
			Width = Dim.Fill() - 30, 
			Height = 1,
			ColorScheme = Globals.BlueOnWhite
		};
		// when enter is pressed
		textBoxPath.KeyDown += (e) => 
		{
			if (e.KeyEvent.Key == Key.Enter)
			{
				GetFiles(recursive, textBoxPath, listView, ref source);
			}
		};
		

		// create a button at the end of the textbox
		var search = new Button("Search")
		{
			X = Pos.Right(textBoxPath) + 1,
			Y = Pos.Bottom(textBoxPath) - 1,
			Width = 8,
			Height = 1
		};
		search.Clicked += () => 
		{
			GetFiles(recursive, textBoxPath, listView, ref source);
		};

		//
		// add a checkbox
		//
		var checkBox = new CheckBox("[ Recursive ]") 
		{ 
			X = Pos.Right(search) + 1, 
			Y = Pos.Bottom(textBoxPath) - 1,
			Width = 20, 
			Height = 1,
			Checked = false
		};
		checkBox.Toggled += (e) => 
		{
			recursive = checkBox.Checked; 
		};

		//
		// create the dialog
		//
		var ok = new Button("OK");
		ok.Clicked += () => 
		{ 
			GetFiles(recursive, textBoxPath, listView, ref source);
			Application.RequestStop(); 

		};


		var dialog = new Dialog (title, 0, 0, ok);
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;
		dialog.FocusFirst();
		
		
		
		dialog.Add(textBoxPath, label, search, checkBox, frame, selectFolder);
		Application.Run (dialog);

		return (textBoxPath.Text.ToString()??"", recursive, source);

	}
	private static void GetFiles(bool recursive, TextField textBoxPath, ListView listView, ref string[] source)
	{
		try
		{
			// get a list of files from the wildcard returning relative paths only
			string[] fullPaths = Misc.GetFiles(textBoxPath.Text.ToString()??"", recursive);
			
			// remove the "./" from the front of the path
			//source = fullPaths.Select(fullPath => fullPath.Replace($".{Path.DirectorySeparatorChar}", "")).ToArray();

			listView.SetSource(fullPaths);
		}
		catch (Exception ex)
		{
			new DialogMessage(ex.Message, "Error");
		}
	}
	
}