using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogSelectFiles
{
	// build
	public (string, bool, string[] source) Build(string title)
	{	
		string[] source = new string[0];

		// return value
		string filename = "";
		bool recursive = false;

		// create a label
		var label = new Label("Enter path or file, (* wildcards ok)") { X = 1, Y = 1, Width = Dim.Fill() - 2, Height = 1 };
	
		// create a textbox
		var textBox = new TextField("*") { X = 1, Y = Pos.Bottom(label), Width = Dim.Fill() - 30, Height = 1 };

		// create the listView
		var listView = new ListView(source) { X = 1, Y = Pos.Bottom(textBox)+1, Width = Dim.Fill() - 2, Height = Dim.Fill() - 2};

		// create a button at the end of the textbox
		var search = new Button("Search")
		{
			X = Pos.Right(textBox) + 1,
			Y = Pos.Bottom(textBox)-1,
			Width = 8,
			Height = 1
		};
		search.Clicked += () => 
		{ 
			// get the current directory
			string currentDirectory = Directory.GetCurrentDirectory();

			SearchOption s = SearchOption.TopDirectoryOnly;
			if (recursive)
				s = SearchOption.AllDirectories;

			// get a list of files from the wildcard returning relative paths only
			string[] fullPaths = Directory.GetFiles(currentDirectory, textBox.Text.ToString(), s);

			// Convert full paths to relative paths
        	source = fullPaths.Select(fullPath => fullPath.Substring(currentDirectory.Length).TrimStart(Path.DirectorySeparatorChar)).ToArray();
			listView.SetSource(source);
		};

		//
		// add a checkbox
		//
		var checkBox = new CheckBox("[ Recursive ]") 
		{ 
			X = Pos.Right(search) + 1, 
			Y = Pos.Bottom(textBox)-1, 
			Width = 20, 
			Height = 1,
			Checked = false
		};
		checkBox.Toggled += (e) => 
		{
			recursive = e; 
		};



		//
		// create the dialog
		//
		var ok = new Button("OK");
		ok.Clicked += () => { Application.RequestStop(); filename = textBox.Text.ToString();};


		var dialog = new Dialog (title, 0, 0, ok);
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;

		
		
		
		dialog.Add(label, textBox, search, checkBox, listView);
		Application.Run (dialog);

		return (filename, recursive, source);

	}
}