#pragma warning disable 1998
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Terminal.Gui;


namespace deadrop.Verbs;

[Verb("gui", HelpText = "Run the Gui")]
public class GuiOptions : Options
{
  
}
class Gui 
{
	public static async Task<int> Execute(GuiOptions opts)
	{
		Application.Init ();
        
		// menubar
		var menu = new MenuBar (new MenuBarItem [] {
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
                }),
            }),
			new MenuBarItem ("_File", new MenuItem [] {
                new MenuItem ("_New DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Open DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Certify DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
            }),
			new MenuBarItem ("_Alias", new MenuItem [] {
                new MenuItem ("_New Alias", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Delete Alias", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Certify Alias", "", () => { 
                    Application.RequestStop (); 
                }),
            }),
			new MenuBarItem ("_Send/Receive", new MenuItem [] {
                new MenuItem ("_Send DeadPack", "", () => { 
                    Application.RequestStop (); 
                }),
				new MenuItem ("_Check for New DeadPacks", "", () => { 
                    Application.RequestStop (); 
                }),
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

        var winLeft = new Window ("Alias") {
            X = 0,
            Y = 1,
            Width = 60,
            Height = Dim.Fill () - 1
        };

		var winRight = new Window () {
            X = 60,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 1
        };

		// add the list of aliases
		winLeft.Add(new AliasList());

        // Add both menu and win in a single call
        Application.Top.Add (menu, winLeft, winRight);
        Application.Run ();
        Application.Shutdown ();

		return 0;
	}
}