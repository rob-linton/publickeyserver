
using System;
using Microsoft.VisualBasic;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class ViewAliases : Window
{
	public ViewAliases()
	{	
		// set the border style
		Border.BorderStyle = BorderStyle.None;

		Build();
	}

	public void Build()
	{
		// remove all of the existing widgets from this view
		RemoveAll();

		// get the aliases
		List<Alias> aliases = Storage.GetAliases();

		List<string> source = new List<string>();
		//source.Add("[all]");
		foreach (var a in aliases)
		{
			source.Add(a.Name);
		}

		// create the list view
		Button addDeadPack = new Button("+ DeadPack") { X = Pos.Right(this) - 17, Y = 0, Width = 15, Height = 1 };
		var addAlias = new Button("+ Alias") { X = Pos.Left(addDeadPack) - 13, Y = 0, Width = 11, Height = 1 };
		

		var received = new Label("Inbox") { X = 0, Y = Pos.Bottom(addAlias), Width = Dim.Fill(), Height = 1 };
		var listViewReceived = new ListView(source) { X = 2, Y = Pos.Bottom(received), Width = Dim.Fill(), Height = Dim.Percent(45)};
		listViewReceived.ColorScheme = Globals.YellowColors;
		listViewReceived.OpenSelectedItem += listView_OpenInbox;

		var sent = new Label("Sent") { X = 0, Y = Pos.Bottom(listViewReceived) + 1, Width = Dim.Fill(), Height = 1 };
		var listViewSent = new ListView(source) { X = 2, Y = Pos.Bottom(sent), Width = Dim.Fill(), Height = Dim.Fill() - 2 };
		listViewSent.ColorScheme = Globals.YellowColors;
		listViewSent.OpenSelectedItem += listView_OpenSent;

		var outbox = new ListView(new List<string>() { "Outbox" }) { X = 0, Y = Pos.Bottom(listViewSent) + 1, Width = Dim.Fill(), Height = 1 };
		listViewReceived.OpenSelectedItem += listView_OpenInbox;
		outbox.OpenSelectedItem += listView_OpenOutbox;
		outbox.ColorScheme = Globals.YellowColors;
		Add(received, listViewReceived, sent, listViewSent, outbox, addAlias, addDeadPack);		
		
		
	}

	private void listView_OpenInbox(ListViewItemEventArgs e)
	{
		Globals.Alias = e.Value.ToString();
		Globals.Location = "inbox";
		Globals.ViewDeadPacks.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight.FocusFirst();
	}

	private void listView_OpenSent(ListViewItemEventArgs e)
	{
		Globals.Alias = e.Value.ToString();
		Globals.Location = "sent";
		Globals.ViewDeadPacks.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight.FocusFirst();
	}

	private void listView_OpenOutbox(ListViewItemEventArgs e)
	{
		Globals.Alias = "";
		Globals.Location = "outbox";
		Globals.ViewDeadPacks.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight.FocusFirst();
	}

	public override bool ProcessKey (KeyEvent keyEvent)
	{
		if (keyEvent.Key == Key.CursorRight)
		{
			Globals.ViewDeadPacks.FocusFirst();
			return true;
		}
		return base.ProcessKey (keyEvent);
	}

	
}
