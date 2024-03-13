using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogSelectAliases
{
	// build
	// public static async Task<int> Execute(PackOptions opts)
	public async Task<string> Build(string title, string fromAlias)
	{	
		string selected = "";
		List<string> source = new List<string>();
		

		// create a label
		var label = new Label("Enter alias, (partial ok)") { X = 1, Y = 1, Width = Dim.Fill() - 2, Height = 1 };
	
		// create the listView
		var listView = new ListView(source) { X = 1, Y = Pos.Bottom(label)+3, Width = Dim.Fill() - 2, Height = Dim.Fill() - 2};
		// on open
		listView.OpenSelectedItem += (e) => 
		{ 
			Application.RequestStop (); 
			selected = e.Value.ToString(); 
		};


		// create a textbox
		var textBox = new TextField("") { X = 1, Y = Pos.Bottom(label), Width = Dim.Fill() - 20, Height = 1 };
		// on press enter
		/*
		textBox.KeyPress += async (e) => 
		{
			if (e.KeyEvent.Key == Key.Enter)
			{
				string toDomain = Misc.GetDomain(fromAlias);
				
				// get a list of deadpacks from the server
				string result = await HttpHelper.Get($"https://" + toDomain + "/lookup/" + textBox.Text.ToString());

				// parse the json
				ListResult files = JsonSerializer.Deserialize<ListResult>(result) ?? throw new Exception($"Could not parse json: {result}");
				foreach (var file in files.Files)
				{
					string alias = file.Key.Replace(".pem", "").Replace("cert/", "");
					source.Add(alias);
				}
				listView.SetSource(source);
			}
		};
		*/

		// create a button at the end of the textbox
		var search = new Button("Search")
		{
			X = Pos.Right(textBox) + 1,
			Y = Pos.Bottom(textBox)-1,
			Width = 8,
			Height = 1
		};
		search.Clicked += async () => 
		{ 
			string toDomain = Misc.GetDomain(fromAlias);
			string search = textBox.Text.ToString();
			if (string.IsNullOrEmpty(search))
			{
				return;
			}

			// get a list of deadpacks from the server
			string result = await HttpHelper.Get($"https://" + toDomain + "/lookup/" + search);

			// parse the json
			ListResult files = JsonSerializer.Deserialize<ListResult>(result) ?? throw new Exception($"Could not parse json: {result}");
			foreach (var file in files.Files)
			{
				string alias = file.Key.Replace(".pem", "").Replace("cert/", "");
				source.Add(alias);
			}
			listView.SetSource(source);
						
		};

		//
		// create the dialog
		//
		var ok = new Button("OK");
		ok.Clicked += () => { Application.RequestStop();};

		var cancel = new Button("Cancel");
		cancel.Clicked += () => { Application.RequestStop(); selected = "";  };


		var dialog = new Dialog (title, 0, 0, ok);
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;

		
		dialog.Add(label, textBox, search, listView);
		Application.Run (dialog);

		return selected;

	}
	
}