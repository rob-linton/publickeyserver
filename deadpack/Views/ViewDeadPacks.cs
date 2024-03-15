
using System;
using System.Runtime.CompilerServices;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class ViewDeadPacks : Window
{
	string _location;

	public ViewDeadPacks(string alias, string location)
	{	try
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
		catch (Exception ex)
		{
			new DialogError(ex.Message);
		}
	}


	public void Build(string alias, string location)
	{
		try
		{
			_location = location;

			// remove all of the existing widgets from this view
			RemoveAll();

			// get a list of deadpacks
			List<DeadPack> deadPacks = Storage.ListDeadPacks(alias, location, Globals.Password);




			// add the heding
			var heading = new Label("DeadPacks") { X = 0, Y = 2, Width = Dim.Fill(), Height = 1 };
			heading.Text = $"Created              From                                      Subject";

			ListView listView = new ListView(deadPacks) { X = 0, Y = 3, Width = Dim.Fill(), Height = Dim.Fill() - 2 };
			listView.OpenSelectedItem += listView_OpenSelectedItem;
			listView.ColorScheme = Globals.StandardColors;
			Add(listView, heading);
		}
		catch (Exception ex)
		{
			new DialogError(ex.Message);
		}
	}

	private void listView_OpenSelectedItem(ListViewItemEventArgs e)
	{
		DeadPack deadPack = (DeadPack)e.Value;

		string alias = Globals.Alias;
		string location = Globals.Location;
		
		new DialogOpenDeadPack().Build(e);
	}

	private void listView_LeftArrow(ListViewItemEventArgs e)
	{
		Globals.ViewLeft.Remove(Globals.ViewAliases);
		Globals.ViewLeft.Add(Globals.ViewAliases);
	}

	public override bool ProcessKey (KeyEvent keyEvent)
	{
		if (keyEvent.Key == Key.CursorLeft)
		{
			Globals.ViewAliases.FocusFirst();
			return true;
		}
		return base.ProcessKey (keyEvent);
	}
}	