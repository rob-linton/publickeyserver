
using System;
using System.Runtime.CompilerServices;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class ViewDeadPacks : Window
{
	public ViewDeadPacks(string alias, string location)
	{	
		// set the border style
		Border.BorderStyle = BorderStyle.None;
		if (string.IsNullOrEmpty(alias) && string.IsNullOrEmpty(location))
		{
			return;
		}
		else
		{
			Build(alias, location);
		}
	}


	public void Build(string alias, string location)
	{
		// remove all of the existing widgets from this view
		RemoveAll();

		// get a list of deadpacks
		List<DeadPack> deadPacks = Storage.ListDeadPacks(alias, location, Globals.Password);

		List<string> source = new List<string>();
		foreach (var deadPack in deadPacks)
		{
			// convert deadpack.timestamp to a human readable date time
			DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dateTime = dateTime.AddSeconds(deadPack.Timestamp).ToLocalTime();
			string row = dateTime.ToString("d-MMM-yyyy h:mmtt") + "   " + deadPack.Subject;

			source.Add(row);
		}

		Button add = new Button("+ Add DeadPack") { X = Pos.Right(this) - 21, Y = 0, Width = 15, Height = 1 };
		
		Button back = new Button("<-") { X = 0, Y = 0, Width = 5, Height = 1 };
		back.Clicked += () => 
		{
			Globals.ViewLeft.Remove(Globals.ViewAliases);
			Globals.ViewLeft.Add(Globals.ViewAliases);
		};

		ListView listView = new ListView(source) { X = 0, Y = 2, Width = Dim.Fill(), Height = Dim.Fill() - 2};
		listView.OpenSelectedItem += listView_OpenSelectedItem;
		Add(back, add, listView);
	}

	private void listView_OpenSelectedItem(ListViewItemEventArgs e)
	{
		string alias = Globals.Alias;
		string location = Globals.Location;

		string[] bits = e.Value.ToString().Split(' ');
		string deadpack = string.Join(" ", bits.Skip(4));
		string deadpackFilename = deadpack + ".deadpack";
		
		var result = DialogOpenDeadPack.Build(alias, deadpack, deadpackFilename, location);

	}
}	