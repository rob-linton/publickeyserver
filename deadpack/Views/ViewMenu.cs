
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
				new MenuItem ("_Open DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Certify DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("--------------------------", "", () => {  
                }),
				 new MenuItem ("_Settings", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("--------------------------", "", () => {  
                }),
				new MenuItem ("_Quit", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.Q),
            }),
			new MenuBarItem ("_Alias", new MenuItem [] {
                new MenuItem ("New _Alias", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.A),
				new MenuItem ("_Delete Alias", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Certify Alias", "", () => { 
                    Application.RequestStop (); 
                }),
            }),
			new MenuBarItem ("_Refresh", new MenuItem [] {
                new MenuItem ("_Refresh DeadPacks (Send/Receive)", "", () => { 
                    MenuSend(false); 
                },null,null)
            }),
			new MenuBarItem ("_Help", new MenuItem [] {
				  new MenuItem ("_About", "", () => { 
                    Application.RequestStop (); 
                }),
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
            }),
        });
	}

	public static void MenuSend(bool auto)
	{
		SendOptions optsSend = new SendOptions();
		ReceiveOptions optsReceive = new ReceiveOptions(){ Force = true, Interval = 0 };
		new DialogSend().Build(optsSend, optsReceive, auto);
	}
}
