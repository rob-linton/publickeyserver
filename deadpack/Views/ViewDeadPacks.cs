
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

		Build(alias, location);
	}


	public void Build(string alias, string location)
	{
		// remove all of the existing widgets from this view
		RemoveAll();

		// get a list of deadpacks
		List<DeadPack> deadPacks = Storage.ListDeadPacks(alias, location);

		List<string> source = new List<string>();
		foreach (var deadPack in deadPacks)
		{
			string row = deadPack.Timestamp + " " + deadPack.Name;
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
		//listView.OpenSelectedItem += listView_OpenSelectedItem;
		Add(back, add, listView);
	}
}	