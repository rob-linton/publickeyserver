
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
                new MenuItem ("_About", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Update", "", () => { 
                    Application.RequestStop (); 
                }),
				 new MenuItem ("_Settings", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Quit", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.Q),
            }),
			new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("New _DeadPack", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.D),
				new MenuItem ("_Open DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Certify DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
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
			new MenuBarItem ("_Send/Receive", new MenuItem [] {
                new MenuItem ("_Send Pending DeadPacks", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.S),
				new MenuItem ("_Refresh New DeadPacks", "", () => { 
                    Application.RequestStop (); 
                },null,null,Key.CtrlMask | Key.R),
            }),
			new MenuBarItem ("_Help", new MenuItem [] {
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
}
