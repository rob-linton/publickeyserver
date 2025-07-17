
using System.Text;
using suredrop;
using suredrop.Verbs;
using Terminal.Gui;

class ViewMenu
{
	// public property for the menu
	public MenuBar Menu { get; set; }

	public ViewMenu()
	{
		// menubar
		 Menu = new MenuBar (new MenuBarItem [] {
            new MenuBarItem ("_SurePack", new MenuItem [] {
				new MenuItem ("New _SurePack", "", () => { 
                    new DialogCreateSurePack().Build(Globals.Alias!); 
                },null,null,Key.CtrlMask | Key.D),
				new MenuItem ("_Open SurePack From Disk", "", () => 
				{
					OpenDialog openDialog = new OpenDialog();
					openDialog.CanChooseFiles = true;
					openDialog.CanChooseDirectories = false;
					openDialog.AllowedFileTypes = new string[] { "surepack" };

					Application.Run (openDialog);
					if (!openDialog.Canceled)
					{
						var filePaths = openDialog.FilePaths;

						string file = filePaths[0];
						OpenSurepack(file);
					}
                    
                },null,null,Key.CtrlMask | Key.O),
				new MenuItem ("--------------------------------", "", () => {  
                }),
				//new MenuItem ("_Settings", "", () => { 
                //    Application.RequestStop (); 
                //}),
				//new MenuItem ("--------------------------", "", () => {  
                //}),
				new MenuItem ("_Quit", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.Q),
            }),
			new MenuBarItem ("_Alias", new MenuItem [] {
                new MenuItem ("New _Alias", "", () => { 
                    new DialogCreateAlias().Build(); 
                },null,null,Key.CtrlMask | Key.A),
				new MenuItem ("_Delete Alias", "", () => { 
                    MenuDelete (); 
                }),
            }),
			new MenuBarItem ("_Send/Receive", new MenuItem [] {
                new MenuItem ("_Send/Receive SurePacks", "", () => { 
                    MenuSend(false); 
                },null,null)
            }),
			new MenuBarItem ("_Help", new MenuItem [] {
                new MenuItem ("_User Manual", "", () => { 
                    try {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://rob-linton.github.io/publickeyserver/HELP.html",
                            UseShellExecute = true
                        });
                    } catch {
                        new DialogMessage("Please visit: https://rob-linton.github.io/publickeyserver/HELP.html", "Help");
                    }
                }),
                new MenuItem ("_Documentation Hub", "", () => { 
                    try {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "https://rob-linton.github.io/publickeyserver/",
                            UseShellExecute = true
                        });
                    } catch {
                        new DialogMessage("Please visit: https://rob-linton.github.io/publickeyserver/", "Help");
                    }
                }),
				new MenuItem ("--------------------------------", "", () => {  
                }),
                new MenuItem ("_About SurePack", "", () => { 
                    ShowAbout(); 
                },null,null,Key.F1),
            }),
        });
	}

	public static void MenuSend(bool auto)
	{
		SendOptions optsSend = new SendOptions();
		ReceiveOptions optsReceive = new ReceiveOptions(){ Force = true, Interval = 0 };
		new DialogSend().Build(optsSend, optsReceive, auto);
	}

	public async static void MenuDelete()
	{
		DeleteOptions deleteOptions = new DeleteOptions() {Alias = Globals.Alias!};
		await Delete.Execute(deleteOptions);
		Gui.Build();
	}

	// show the message dialog
	public static void ShowAbout()
	{
		string message = 
$@"SurePack {Globals.version}
Secure File Transfer with Post-Quantum Cryptography

SurePack is a secure (and optionally anonymous) package delivery 
service that uses the SureDrop protocol with military-grade encryption.

FEATURES:
✓ End-to-End Encryption (AES-256-GCM)
✓ Post-Quantum Cryptography (Kyber1024 + Dilithium5)
✓ Anonymous Transfer Option
✓ Perfect Forward Secrecy
✓ Zero-Knowledge Server Architecture

GETTING HELP:
📚 Documentation: https://rob-linton.github.io/publickeyserver/
📖 User Manual: https://rob-linton.github.io/publickeyserver/HELP.html

Press F1 at any time for help.";

		MessageBox.Query("About SurePack", message, "Ok");
	}

	public static void OpenSurepack(string file)
	{
		// get a list of private aliases
		List<Alias> aliases = Storage.GetAliases();
		foreach (Alias alias in aliases)
		{
			try
			{
				long size = new FileInfo(file).Length;

				Envelope envelope = Envelope.LoadFromFile(file);

				Manifest manifest = Manifest.LoadFromFile(file, alias.Name);

				// convert the manifest base64
				byte[] base64Subject = Convert.FromBase64String(manifest.Subject);
				string subject = Encoding.UTF8.GetString(base64Subject);

				// do the same for the message
				byte[] base64Message = Convert.FromBase64String(manifest.Message);
				string message = Encoding.UTF8.GetString(base64Message);

				// get the timestamp from the envelope
				long timestamp = envelope.Created;

				// get the list of files in the surepack
				List<FileItem> files = manifest.Files;

				// get the from alias
				string fromAlias = envelope.From;
				List<Recipient> recipients = envelope.To;

				// remove the from alias from the recipients
				recipients.RemoveAll(r => r.Alias == fromAlias);

				SurePack surepack = new SurePack
				{
					Subject = subject,
					Message = message,
					Timestamp = timestamp,
					Files = files,
					From = fromAlias,
					Alias = alias.Name,
					Filename = file,
					Recipients = recipients,
					Size = size
				};

				new DialogOpenSurePack().Build(surepack);
				return;
			}
			catch 
			{
				continue;
			}
		}
		new DialogError("Could not open surepack");
	}
}
