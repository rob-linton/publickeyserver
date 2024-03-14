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
		string lastSearch = "";
		

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
		listView.SelectedItemChanged += (e) => 
		{ 
			selected = e.Value.ToString();  
		};


		// create a textbox
		var textBox = new TextField("") { 
			X = 1, 
			Y = Pos.Bottom(label), 
			Width = Dim.Fill() - 20, 
			Height = 1,
			ColorScheme = Globals.BlueOnWhite
		};
		// on press enter
		
		textBox.KeyDown += async (e) => 
		{
			if (e.KeyEvent.Key == Key.Enter)
			{
				if (lastSearch == textBox.Text.ToString())
				{
					return;
				}
				lastSearch = textBox.Text.ToString();

				string toDomain = Misc.GetDomain(fromAlias);

				//
				// get a list of deadpacks from the server
				//
				string lookup = textBox.Text.ToString();
				LookupAlias(fromAlias, lookup, source, listView, lastSearch); 
			}
		};
		
		

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

			//
			// get a list of deadpacks from the server
			//
			string lookup = textBox.Text.ToString();
			LookupAlias(fromAlias, lookup, source, listView, lastSearch); 
						
		};
		

		//
		// create the dialog
		//
		var ok = new Button("OK");
		ok.Clicked += () => { Application.RequestStop();};

		var cancel = new Button("Cancel");
		cancel.Clicked += () => { Application.RequestStop(); selected = "";  };


		var dialog = new Dialog (title, 0, 0, ok, cancel);
		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;
		dialog.FocusFirst();
		
		dialog.Add(textBox, label, search, listView);
		Application.Run (dialog);

		return selected;

	}

	private async void LookupAlias(string fromAlias, string lookup, List<string> source, ListView listView, string lastSearch) 
	{
		string toDomain = Misc.GetDomain(fromAlias);

		//
		// get a list of deadpacks from the server
		//

		// sign it with the sender
		long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		string privateKeyPem = Storage.GetPrivateKey($"{fromAlias}.rsa", Globals.Password);
		AsymmetricCipherKeyPair privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);

		string domain = Misc.GetDomainFromAlias(fromAlias);
		byte[] data = $"{fromAlias}{unixTimestamp.ToString()}{domain}".ToBytes();
		byte[] signature = BouncyCastleHelper.SignData(data, privateKey.Private);
		string base64Signature = Convert.ToBase64String(signature);

		// get a list of deadpacks from the server
		//string lookup = textBox.Text.ToString();
		string result = await HttpHelper.Get($"https://{toDomain}/lookup/{lookup}?alias={fromAlias}&timestamp={unixTimestamp}&signature={base64Signature}");


		//string result = await HttpHelper.Get($"https://" + toDomain + "/lookup/" + textBox.Text.ToString());

		// parse the json
		ListResult files = JsonSerializer.Deserialize<ListResult>(result) ?? throw new Exception($"Could not parse json: {result}");
		source.Clear();
		foreach (var file in files.Files)
		{
			string alias = file.Key;
			if (!source.Contains(alias))
				source.Add(alias);
		}
		listView.SetSource(source);
		listView.SetFocus();

		lastSearch = lookup;
	}
}