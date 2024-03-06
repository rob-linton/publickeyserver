
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

		ListView listView = new ListView(source) { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
		//listView.OpenSelectedItem += listView_OpenSelectedItem;
		Add(listView);
	}
}	