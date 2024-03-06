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
		Build();
		return 0;
	}
	public static async void Build()
	{
		Application.Init ();

		ViewMenu menu = new ViewMenu();

        var winLeft = new Window ("Aliases") {
            X = 0,
            Y = 1,
            Width = 60,
            Height = Dim.Fill () - 1
        };

		var winRight = new Window ("DeadPacks") {
            X = 60,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 1
        };

		// add the list of aliases
		winLeft.Add(new ViewAliases());

		// add the list of deadpacks
		if (!String.IsNullOrEmpty(Globals.Location))
			winRight.Add(new ViewDeadPacks(Globals.Alias, Globals.Location));

		// Add both menu and win in a single call
		Application.Top.RemoveAll();
        Application.Top.Add (menu.Menu, winLeft, winRight);
        Application.Run ();
        Application.Shutdown ();

		return;
	}
}