
using System;
using System.Runtime.CompilerServices;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace suredrop.Verbs;

public class ViewSurePacks : Window
{
	

	public ViewSurePacks(string alias, string location)
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
			
			ListView listView = new ListView();

			// remove all of the existing widgets from this view
			RemoveAll();

			// get a list of surepacks
			List<SurePack> surePacks = Storage.ListSurePacks(alias, location);

			// add the delete button
			Button delete = new Button("Delete") 
			{ 
				X = Pos.Right(this) - 13, 
				Y = 0, 
				Width = 8, 
				Height = 1 
			};
			delete.Clicked += () => 
			{
				if (listView.Source.Count == 0)
				{
					return;
				}
				SurePack surePack = surePacks.ElementAt(listView.SelectedItem);
				if (surePack == null)
				{
					return;
				}
				// get the surepack location
				File.Delete(surePack.Filename);
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
				SurePack surePack = surePacks.ElementAt(listView.SelectedItem);
				if (surePack == null)
				{
					return;
				}
				new DialogOpenSurePack().Build(surePack);
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

			Button addSurePack = new Button("+ SurePack") { X = Pos.Right(refresh) + 1, Y = 0, Width = 8, Height = 1 };
			addSurePack.Clicked += () => { new DialogCreateSurePack().Build(Globals.Alias??""); };

			// add the heding
			var heading = new Label("") { X = 0, Y = 2, Width = Dim.Fill(), Height = 1 };
			heading.Text = SurePack.Headings();

			listView = new ListView(surePacks) 
			{ 
				X = 0, 
				Y = 3, 
				Width = Dim.Fill(), 
				Height = Dim.Fill() - 2 
			};
			listView.OpenSelectedItem += listView_OpenSelectedItem;
			listView.ColorScheme = Globals.StandardColors;
			Add(listView, heading, delete, open, refresh, addSurePack);
		}
		catch (Exception ex)
		{
			if (!String.IsNullOrEmpty(ex.Message))
				new DialogError(ex.Message);
		}
	}

	private void listView_OpenSelectedItem(ListViewItemEventArgs e)
	{
		SurePack surePack = (SurePack)e.Value;
		new DialogOpenSurePack().Build(surePack!);
	}

	private void listView_LeftArrow(ListViewItemEventArgs e)
	{
		Globals.ViewLeft?.Remove(Globals.ViewAliases);
		Globals.ViewLeft?.Add(Globals.ViewAliases);
	}

	public override bool ProcessKey (KeyEvent keyEvent)
	{
		if (keyEvent.Key == Key.CursorLeft)
		{
			Globals.ViewAliases?.FocusFirst();
			return true;
		}
		return base.ProcessKey (keyEvent);
	}
}	