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

		Globals.StandardColors = new ColorScheme()
		{
			Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
			Focus = Application.Driver.MakeAttribute(Color.White, Color.Blue),
			HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
			HotFocus = Application.Driver.MakeAttribute(Color.White, Color.Blue),
		};

		ViewMenu menu = new ViewMenu();

        Globals.ViewLeft = new Window ("Aliases") {
            X = 0,
            Y = 1,
            Width = 60,
            Height = Dim.Fill () - 1
        };

		Globals.ViewRight = new Window ("DeadPacks") {
            X = 60,
            Y = 1,
            Width = Dim.Fill (),
            Height = Dim.Fill () - 1
        };

		// add the list of aliases
		Globals.ViewAliases = new ViewAliases();
		Globals.ViewLeft.Add(Globals.ViewAliases);

		// add the list of deadpacks
		Globals.ViewDeadPacks = new ViewDeadPacks(Globals.Alias, Globals.Location);
		Globals.ViewRight.Add(Globals.ViewDeadPacks);
		

		// Add both menu and win in a single call
		Application.Top.RemoveAll();
        Application.Top.Add (menu.Menu, Globals.ViewLeft, Globals.ViewRight);
        Application.Run ();
        Application.Shutdown ();

		return;
	}
}