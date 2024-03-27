using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogPassword : Dialog
{
	public DialogPassword()
	{
		Build();
	}
	
  	private TextField password = new TextField();

	// build
	public string Build()
	{

		var cancel = new Button("Close")
		{
			X = Pos.Percent(50) - 8,
			Y = 3,
			Width = 8,
			Height = 1
		
		};
		cancel.Clicked += () =>
		{	
			Application.RequestStop();
		};

		var ok = new Button("Ok")
		{
			X = Pos.Right(cancel) + 1,
			Y = 3,
			Width = 8,
			Height = 1
		};
		ok.Clicked += () =>
		{	
			if (password.Text.ToString().Length == 0)
			{
				return;
			}
			Globals.Password = password.Text.ToString();
			Application.RequestStop();
		};

		//var dialog = new Dialog ("Enter Passphrase", 80, 6, cancel, ok);
		Title = "Enter Passphrase";
		Width = 80;
		Height = 6;
		
		
		Border.BorderStyle = BorderStyle.Double;
		ColorScheme = Colors.Base;
		
		//
		// add the passphrase
		//
		password = new TextField() 
		{ 
			X = Pos.Center() - 33, 
			Y = Pos.Center(), 
			Width = 60, 
			Height = 1,
			ColorScheme = Globals.BlueOnWhite,
			ReadOnly = false,
			Secret = true
		};
		if (!String.IsNullOrEmpty(Globals.Password))
			password.Text = Globals.Password?.ToString();

		// add a show password button
		var show = new Button("Show")
		{
			X = Pos.Right(password) + 1,
			Y = Pos.Top(password),
			Width = 8,
			Height = 1
		};
		show.Clicked += () =>
		{
			password.Secret = !password.Secret;
			password.SetNeedsDisplay();
		};
		
		Add (password, cancel, ok, show);
		Application.Run (this);

		

		return password.Text.ToString(); 
	}

	public override bool ProcessKey (KeyEvent keyEvent)
	{
		if (keyEvent.Key == Key.Enter)
		{
			if (password.Text.ToString().Length == 0)
			{
				return true;
			}
			Globals.Password = password.Text.ToString();
			Application.RequestStop();
			return true;
		}
		return base.ProcessKey (keyEvent);

	}

}