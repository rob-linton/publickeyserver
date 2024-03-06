
using System;
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
		var add = new Button("+ Add Alias") { X = Pos.Right(this) - 18, Y = 0, Width = 11, Height = 1 };
		var received = new Label("Inbox") { X = 0, Y = Pos.Bottom(add), Width = Dim.Fill(), Height = 1 };
		var listViewReceived = new ListView(source) { X = 2, Y = Pos.Bottom(received), Width = Dim.Fill(), Height = Dim.Percent(45)};
		listViewReceived.OpenSelectedItem += listView_OpenInbox;

		var sent = new Label("Sent") { X = 0, Y = Pos.Bottom(listViewReceived) + 1, Width = Dim.Fill(), Height = 1 };
		var listViewSent = new ListView(source) { X = 2, Y = Pos.Bottom(sent), Width = Dim.Fill(), Height = Dim.Fill() - 2 };
		listViewSent.OpenSelectedItem += listView_OpenSent;

		var outbox = new ListView(new List<string>() { "Outbox" }) { X = 0, Y = Pos.Bottom(listViewSent) + 1, Width = Dim.Fill(), Height = 1 };
		outbox.OpenSelectedItem += listView_OpenOutbox;

		Add(add, received, listViewReceived, sent, listViewSent, outbox);
		
	}

	private void listView_OpenInbox(ListViewItemEventArgs e)
	{
		Globals.Alias = e.Value.ToString();
		Globals.Location = "inbox";
		Globals.ViewRight.Remove(Globals.ViewDeadPacks);
		Globals.ViewDeadPacks.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight.Add(Globals.ViewDeadPacks);
	}

	private void listView_OpenSent(ListViewItemEventArgs e)
	{
		Globals.Alias = e.Value.ToString();
		Globals.Location = "sent";
		Globals.ViewRight.Remove(Globals.ViewDeadPacks);
		Globals.ViewDeadPacks.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight.Add(Globals.ViewDeadPacks);
	}

	private void listView_OpenOutbox(ListViewItemEventArgs e)
	{
		Globals.Alias = e.Value.ToString();
		Globals.Location = "outbox";
		Globals.ViewRight.Remove(Globals.ViewDeadPacks);
		Globals.ViewDeadPacks.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight.Add(Globals.ViewDeadPacks);
	}

	
}
