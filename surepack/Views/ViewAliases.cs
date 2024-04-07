
using System;
using Microsoft.VisualBasic;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace suredrop.Verbs;

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
		try
		{
			Globals.ViewSurePacks?.RemoveAll();
		}
		catch { }

		// get the aliases
		List<Alias> aliasesReceived = Storage.GetAliases();
		List<Alias> aliasesSent = Storage.GetAliases();

		// get the received directory
		
		// now count how many surepacks are in each alias directory
		foreach (var a in aliasesReceived)
		{
			string receivedDir = Storage.GetSurePackDirectoryInbox(a.Name);

			// get the count of surepacks
			a.TotalSurePacks = Storage.CountSurePacks(receivedDir);

			// get the count of surepacks in the last 24 hours
			a.NewSurePacks24Hour = Storage.CountNewSurePacksHour(receivedDir, 24);

			// get the count of surepacks in the last hour
			a.NewSurePacks1Hour = Storage.CountNewSurePacksHour(receivedDir, 1);
		}

		foreach (var a in aliasesSent)
		{
			string sentDir = Storage.GetSurePackDirectorySent(a.Name);

			// get the count of surepacks
			a.TotalSurePacks = Storage.CountSurePacks(sentDir);

			// get the count of surepacks in the last 24 hours
			a.NewSurePacks24Hour = Storage.CountNewSurePacksHour(sentDir, 24);

			// get the count of surepacks in the last hour
			a.NewSurePacks1Hour = Storage.CountNewSurePacksHour(sentDir, 1);
		}

		// create the list view

		// add refresh button
		var refresh = new Button("Refresh") { X = 0, Y = 0, Width = 8, Height = 1 };
		refresh.Clicked += () => 
		{ 
			Globals.ViewSurePacks?.RemoveAll();
			Build();
		};

		var addAlias = new Button("+ Alias") { X = Pos.Right(refresh) + 1, Y = 0, Width = 11, Height = 1 };
		addAlias.Clicked += () => 
		{ 
			if (String.IsNullOrEmpty(Globals.Password))
			{
				new DialogPassword();
			}
			new DialogCreateAlias().Build();
			this.Build();
		};

		// add delete button
		var delete = new Button("Delete") { X = Pos.Right(this) - 15, Y = 0, Width = 15, Height = 1 };
		delete.Clicked += async () => 
		{
			if (string.IsNullOrEmpty(Globals.Alias))
			{
				new DialogError("You must select an alias.");
				return;
			}
			DeleteOptions deleteOptions = new DeleteOptions() {Alias = Globals.Alias};
			await Delete.Execute(deleteOptions);
			Globals.ViewSurePacks?.RemoveAll();
			Build();
		};


		var received = new Label("Received") { X = 0, Y = Pos.Bottom(addAlias) + 1, Width = Dim.Fill(), Height = 1 };
		var listViewReceived = new ListView(aliasesReceived) { X = 2, Y = Pos.Bottom(received), Width = Dim.Fill(), Height = Dim.Percent(45)};
		listViewReceived.ColorScheme = Globals.StandardColors;
		listViewReceived.OpenSelectedItem += listView_OpenInbox;
		listViewReceived.SelectedItemChanged += (args) => { Globals.Alias = (args.Value as Alias)?.Name.ToString(); };

		var sent = new Label("Sent") { X = 0, Y = Pos.Bottom(listViewReceived) + 1, Width = Dim.Fill(), Height = 1 };
		var listViewSent = new ListView(aliasesSent) { X = 2, Y = Pos.Bottom(sent), Width = Dim.Fill(), Height = Dim.Fill() - 2 };
		listViewSent.ColorScheme = Globals.StandardColors;
		listViewSent.OpenSelectedItem += listView_OpenSent;
		listViewSent.SelectedItemChanged += (args) => { Globals.Alias = (args.Value as Alias)?.ToString(); };

		var outbox = new ListView(new List<string>() { "Outbox" }) { X = 0, Y = Pos.Bottom(listViewSent) + 1, Width = Dim.Fill(), Height = 1 };
		listViewReceived.OpenSelectedItem += listView_OpenInbox;
		outbox.OpenSelectedItem += listView_OpenOutbox;
		outbox.ColorScheme = Globals.StandardColors;
		Add(received, listViewReceived, sent, listViewSent, outbox, addAlias, delete, refresh);		


		// Open the first surepack
		_ = Application.MainLoop.AddIdle(() =>
		{
			try
			{
				Alias? a = aliasesReceived.FirstOrDefault();
				Globals.Alias = a!.Name;
				Globals.Location = "inbox";
				Globals.ViewSurePacks?.Build(Globals.Alias, Globals.Location);
				Globals.ViewRight?.FocusFirst();
			}
			catch { }
			return false; // cancel the idle so it only runs once
		});

	}

	private void listView_OpenInbox(ListViewItemEventArgs e)
	{
		Alias? a = e.Value as Alias;
		Globals.Alias = a!.Name;
		Globals.Location = "inbox";
		Globals.ViewSurePacks?.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight?.FocusFirst();
	}

	private void listView_OpenSent(ListViewItemEventArgs e)
	{
		Alias? a = e.Value as Alias;
		Globals.Alias = a!.Name;
		Globals.Location = "sent";
		Globals.ViewSurePacks?.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight?.FocusFirst();
	}

	private void listView_OpenOutbox(ListViewItemEventArgs e)
	{
		Globals.Alias = "";
		Globals.Location = "outbox";
		Globals.ViewSurePacks?.Build(Globals.Alias, Globals.Location);
		Globals.ViewRight?.FocusFirst();
	}

	public override bool ProcessKey (KeyEvent keyEvent)
	{
		if (keyEvent.Key == Key.CursorRight)
		{
			Globals.ViewSurePacks?.FocusFirst();
			return true;
		}
		return base.ProcessKey (keyEvent);
	}

	
}
