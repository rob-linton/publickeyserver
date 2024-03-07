using System;
using System.Runtime.CompilerServices;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogOpenDeadPack 
{
	// build
	public static Enums.DialogReturn Build(string alias, string deadpack, string deadpackFilename, string location)
	{
		Enums.DialogReturn result = Enums.DialogReturn.Cancel;

		var ok = new Button("Ok");
		ok.Clicked += () => { Application.RequestStop (); result = Enums.DialogReturn.Ok; };

		var cancel = new Button("Cancel");
		cancel.Clicked += () => Application.RequestStop ();

		var extract = new Button("Extract");
		extract.Clicked += () => { Application.RequestStop (); result = Enums.DialogReturn.Extract; };

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog ("Open DeadPack", width, height, ok, cancel, extract);

		var entry = new TextField () {
			X = 1, 
			Y = 1,
			Width = Dim.Fill (),
			Height = 1
		};
		dialog.Add (entry);
		Application.Run (dialog);

		return result;
	}
}