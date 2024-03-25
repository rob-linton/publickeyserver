
using deadrop;
using deadrop.Verbs;
using Terminal.Gui;

class ViewMenu
{
	// public property for the menu
	public MenuBar Menu { get; set; }

	public ViewMenu()
	{
		// menubar
		 Menu = new MenuBar (new MenuBarItem [] {
            new MenuBarItem ("_DeadPack", new MenuItem [] {
				new MenuItem ("New _DeadPack", "", () => { 
                    new DialogCreateDeadPack().Build(Globals.Alias); 
                },null,null,Key.CtrlMask | Key.D),
				new MenuItem ("_Open DeadPack From Disk", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("--------------------------", "", () => {  
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
                new MenuItem ("_Send/Receive DeadPacks", "", () => { 
                    MenuSend(false); 
                },null,null)
            }),
			new MenuBarItem ("_Help", new MenuItem [] {
				  new MenuItem ("_About", "", () => { 
                    ShowAbout(); 
                }),
				/*
				new MenuItem ("_Update", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("-------------", "", () => {  
                }),
                new MenuItem ("_Overview", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Aliases", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_DeadPacks", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Encryption", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Key Servers", "", () => { 
                    Application.RequestStop (); 
                }),
				*/
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
		DeleteOptions deleteOptions = new DeleteOptions() {Alias = Globals.Alias};
		await Delete.Execute(deleteOptions);
		Gui.Build();
	}

	// show the message dialog
	public static void ShowAbout()
	{
		string message = 
@"
    DeadPack is a secure messaging system that uses the DeadDrop protocol.    
Written by Rob Linton, 2024.
Version 1.0.0

More information can be found at:
https://github/rob-linton/deadrop
";

		MessageBox.ErrorQuery("About", message, "Ok");
	}
}
