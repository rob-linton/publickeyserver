
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
			ListView listView = new ListView();

			// remove all of the existing widgets from this view
			RemoveAll();

			// get a list of deadpacks
			List<DeadPack> deadPacks = Storage.ListDeadPacks(alias, location, Globals.Password);

			// add the delete button
			Button delete = new Button("Delete") 
			{ 
				X = Pos.Right(this) - 13, 
				Y = 0, 
				Width = 8, 
				Height = 1 
			};
			delete.Clicked += async () => 
			{
				if (listView.Source.Count == 0)
				{
					return;
				}
				DeadPack deadPack = deadPacks.ElementAt(listView.SelectedItem);
				if (deadPack == null)
				{
					return;
				}
				// get the deadpack location
				File.Delete(deadPack.Filename);
				Build(alias, location);
			};

			// add the open button
			Button open = new Button("Open") 
			{ 
				X = Pos.Left(delete) - 8, 
				Y = 0, 
				Width = 6, 
				Height = 1 
			};
			open.Clicked += () => 
			{
				if (listView.Source.Count == 0)
				{
					return;
				}
				DeadPack deadPack = deadPacks.ElementAt(listView.SelectedItem);
				if (deadPack == null)
				{
					return;
				}
				new DialogOpenDeadPack().Build(deadPack);
			};

			// create the refresh button
			Button refresh = new Button("Refresh") 
			{ 
				X = 0, 
				Y = 0, 
				Width = 8, 
				Height = 1 
			};
			refresh.Clicked += () => 
			{
				Build(alias, location);
			};

			Button addDeadPack = new Button("+ DeadPack") { X = Pos.Right(refresh) + 1, Y = 0, Width = 8, Height = 1 };
			addDeadPack.Clicked += () => { new DialogCreateDeadPack().Build(Globals.Alias); };

			// add the heding
			var heading = new Label("") { X = 0, Y = 2, Width = Dim.Fill(), Height = 1 };
			heading.Text = DeadPack.Headings();

			listView = new ListView(deadPacks) 
			{ 
				X = 0, 
				Y = 3, 
				Width = Dim.Fill(), 
				Height = Dim.Fill() - 2 
			};
			listView.OpenSelectedItem += listView_OpenSelectedItem;
			listView.ColorScheme = Globals.StandardColors;
			Add(listView, heading, delete, open, refresh, addDeadPack);
		}
		catch (Exception ex)
		{
			if (!String.IsNullOrEmpty(ex.Message))
				new DialogError(ex.Message);
		}
	}

	private void listView_OpenSelectedItem(ListViewItemEventArgs e)
	{
		new DialogOpenDeadPack().Build(e.Value as DeadPack);
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