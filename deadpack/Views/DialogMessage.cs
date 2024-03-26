using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogMessage
{
	public DialogMessage(string message, string title = "Message")
	{
		if (String.IsNullOrEmpty(message))
			message = "OK";

		MessageBox.ErrorQuery(title, "\n    " + message + "    \n", "Ok");
		//Build(message);
	}
	
  	private TextView output = new TextView();

	// build
	public void Build(String message)
	{

		var cancel = new Button("Close");
		cancel.Clicked += () =>
		{	
			Application.RequestStop();
		};

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog (" Message ", 110, height - 10, cancel);
		
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;
		
		//
		// add the output location
		//
		output = new TextView() 
		{ 
			X = 5, 
			Y = Pos.Center(), 
			Width = 100, 
			Height = Dim.Fill() - 5,
			ColorScheme = Globals.WhiteOnBlue,
			Multiline = true,
			ReadOnly = true,
		};
		output.Text = message;
		
	
		dialog.Add (output);
		Application.Run (dialog);

		return;
	}

	
	
}