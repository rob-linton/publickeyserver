using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogSelectFromList
{
	// build
	public string Build(List<string> source, string title)
	{	
		// add a listview
		string selected = source[0];

		var ok = new Button("OK");
		ok.Clicked += () => Application.RequestStop ();
	
		var dialog = new Dialog (title, 80, Application.Top.Frame.Height - 6, ok);
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;

		
		var listView = new ListView(source) { X = 1, Y = 1, Width = Dim.Fill() - 2, Height = Dim.Fill() - 2};
		// on select
		listView.OpenSelectedItem += (e) => { Application.RequestStop (); selected = e.Value.ToString()??""; };
		// on selected item changed
		listView.SelectedItemChanged += (e) => { selected = e.Value.ToString()??""; };
		

		dialog.Add(listView);
		
		Application.Run (dialog);

		return selected;

	}
}