
using System;
using System.Runtime.CompilerServices;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class ViewDeadPacks : Window
{
	string _location;

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
		_location = location;

		// remove all of the existing widgets from this view
		RemoveAll();

		// get a list of deadpacks
		List<DeadPack> deadPacks = Storage.ListDeadPacks(alias, location, Globals.Password);

		Button add = new Button("+ Add DeadPack") { X = Pos.Right(this) - 21, Y = 0, Width = 15, Height = 1 };
		
		Button back = new Button("<-") { X = 0, Y = 0, Width = 5, Height = 1 };
		back.Clicked += () => 
		{
			Globals.ViewLeft.Remove(Globals.ViewAliases);
			Globals.ViewLeft.Add(Globals.ViewAliases);
		};

		// add the heding
		var heading = new Label("DeadPacks") { X = 0, Y = 2, Width = Dim.Fill(), Height = 1 };
		heading.Text = $"Created              From                                      Subject";

		ListView listView = new ListView(deadPacks) { X = 0, Y = 3, Width = Dim.Fill(), Height = Dim.Fill() - 2};
		listView.OpenSelectedItem += listView_OpenSelectedItem;
		listView.ColorScheme = Globals.YellowColors;
		Add(back, add, heading, listView);
	}

	private void listView_OpenSelectedItem(ListViewItemEventArgs e)
	{
		DeadPack deadPack = (DeadPack)e.Value;

		string alias = Globals.Alias;
		string location = Globals.Location;

		
		
		var result = new DialogOpenDeadPack().Build(e, location);
		if (result == Enums.DialogReturn.Extract)
		{
			string input = deadPack.Filename;
			
			new DialogUnpack().Build(input, alias);
		}
	}
}	