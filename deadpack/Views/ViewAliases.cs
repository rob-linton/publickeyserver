
using System;
using Terminal.Gui;

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
		foreach (var a in aliases)
		{
			source.Add("  " + a.Name);
		}

		// create the list view
		var add = new Button("+ Add Alias") { X = Pos.Right(this) - 18, Y = 0, Width = 11, Height = 1 };
		var received = new Label("Inbox") { X = 0, Y = Pos.Bottom(add), Width = Dim.Fill(), Height = 1 };
		var listView = new ListView(source) { X = 1, Y = Pos.Bottom(received), Width = Dim.Fill(), Height = Dim.Fill() - 4 };
		listView.OpenSelectedItem += listView_OpenSelectedItem;

		var sent = new ListView(new List<string>() { "Sent" }) { X = 0, Y = Pos.Bottom(listView) + 1, Width = Dim.Fill(), Height = 1 };
		sent.OpenSelectedItem += listView_OpenSent;

		var outbox = new ListView(new List<string>() { "Outbox" }) { X = 0, Y = Pos.Bottom(sent) + 1, Width = Dim.Fill(), Height = 1 };
		outbox.OpenSelectedItem += listView_OpenOutbox;

		Add(add, received, listView, sent, outbox);
	}

	private void listView_OpenSelectedItem(ListViewItemEventArgs e)
	{
		Console.WriteLine("Selected: " + e.Value);
	}

	private void listView_OpenSent(ListViewItemEventArgs e)
	{
		Console.WriteLine("Selected: " + e.Value);
	}

	private void listView_OpenOutbox(ListViewItemEventArgs e)
	{
		Console.WriteLine("Selected: " + e.Value);
	}

	
}
