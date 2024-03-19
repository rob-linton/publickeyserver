using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using CommandLine;
using Terminal.Gui;
using Terminal.Gui.Trees;

namespace deadrop.Verbs;

public class DialogCreateAlias
{
	
  	private TextField domain = new TextField();
	private TextField email = new TextField();
	private TextField emailVerificationCode = new TextField();
	private Label progressLabel = new Label();

	// build
	public void Build()
	{
		//
		// dialog
		//
		var ok = new Button("Create Alias");
		ok.Clicked += async () => 
		{
			progressLabel.Text = "Creating Alias...";
			await System.Threading.Tasks.Task.Delay(100); // DO NOT REMOVE-REQUIRED FOR UX UPDATE
			

			var progress = new Progress<StatusUpdate>(StatusUpdate =>
			{
				progressLabel.Text = StatusUpdate.Status;
			});

			CreateOptions opts = new CreateOptions()
			{
				Domain = domain.Text.ToString(),
				Email = email.Text.ToString(),
				Token = emailVerificationCode.Text.ToString()
			};

			int result = await Create.Execute(opts, progress);
		};

		var cancel = new Button("Close");
		cancel.Clicked += () =>
		{	
			Application.RequestStop();
		};

		int width = Application.Top.Frame.Width;
		int height = Application.Top.Frame.Height;

		var dialog = new Dialog ("Create Alias", width, height, cancel, ok);

		dialog.Border.BorderStyle = BorderStyle.Double;
		dialog.ColorScheme = Colors.Base;

		
		//
		// add the domain 
		//
		domain = new TextField() 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill()-1, 
			Height = Dim.Fill(),
			ColorScheme = Globals.BlueOnWhite
		};
		domain.Text = Misc.GetDomain("");

		FrameView viewDomain = new FrameView ("Key Server") {
        	X = 1,
            Y = 1,
            Width = Dim.Fill () - 1,
            Height = 4,
        };
		viewDomain.Border.BorderStyle = BorderStyle.Single;
		viewDomain.Add(domain);

		//
		// add the optional email
		//
		Label emailInfo = new Label() 
		{ 
			X = 1, 
			Y = 1, 
			Width = Dim.Fill()-1, 
			Height = 1, 
			ColorScheme = Globals.WhiteOnBlue
		};
		emailInfo.Text = "You may associate an email address with this alias if you would like to be identified. This is optional. Otherwise, the alias will be anonymous.";
		
		Label emailLabel = new Label("Email Address")
		{
			X = 1,
			Y = Pos.Bottom(emailInfo) + 2,
			Width = Dim.Percent(60),
			Height = 1	
		};
		Label verificationLabel = new Label("Email Verification Code")
		{
			X = Pos.Percent(60) + 4,
			Y = Pos.Bottom(emailInfo) + 2,
			Width = Dim.Percent(20),
			Height = 1	
		};

		email = new TextField() 
		{ 
			X = 1, 
			Y = Pos.Bottom(emailLabel), 
			Width = Dim.Percent(60),
			Height = 1,
			ColorScheme = Globals.BlueOnWhite
		};

		emailVerificationCode = new TextField() 
		{ 
			X = Pos.Percent(60) + 4,
			Y = Pos.Bottom(emailLabel), 
			Width = Dim.Percent(20),
			Height = 1,
			ColorScheme = Globals.BlueOnWhite
		};

		Button verifyEmail = new Button("Get Verification Code")
		{
			X = Pos.Percent(80) + 6,
			Y = Pos.Bottom(emailLabel), 
			Width = 10,
			Height = 1	
		};
		verifyEmail.Clicked += async () => {

			// report on progress
			var progress = new Progress<StatusUpdate>(StatusUpdate =>
			{
				progressLabel.Text = StatusUpdate.Status;
			});

			// verify the email
			VerifyOptions verifyOpts = new VerifyOptions()
			{
				Email = email.Text.ToString(),
				Domain = domain.Text.ToString()	
			};
			int result = await Verify.Execute(verifyOpts, progress);
			//progressLabel.Text = "Email verification code sent. Please check your email.";
			//await System.Threading.Tasks.Task.Delay(100); // DO NOT REMOVE-REQUIRED FOR UX UPDATE
		};

		FrameView viewEmail = new FrameView ("Email (Optional)") {
        	X = 1,
            Y = Pos.Bottom(viewDomain) + 1,
            Width = Dim.Fill () - 1,
            Height = 9,
        };
		viewEmail.Border.BorderStyle = BorderStyle.Single;
		viewEmail.Add(emailInfo, emailLabel, verificationLabel, email, emailVerificationCode, verifyEmail);

		//
		// add the progress label
		//
		progressLabel = new Label() 
		{ 
			X = 2, 
			Y = Pos.Bottom(viewEmail) + 1, 
			Width = Dim.Fill()-1, 
			Height = 1, 
			ColorScheme = Colors.Error
		};

		//
		// add the progress view list
		//
		FrameView viewProgress = new FrameView ("Alias Creation Verification") {
			X = 1,
			Y = Pos.Bottom(progressLabel) + 1,
			Width = Dim.Fill () - 1,
			Height = Dim.Fill () - 2
		};
		var listViewProgress = new ListView(Globals.GetProgressSource()) { X = 1, Y = 1, Width = Dim.Fill()-2, Height = Dim.Fill() - 2 };
		viewProgress.Add(listViewProgress);
	
		dialog.Add (viewDomain, viewEmail, viewProgress, progressLabel);
		Application.Run (dialog);

		return;
	}

	
	
}