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
  	[Option('u', "unpack", HelpText = "Unpack deadrop file")]
    public string? UnpackFile { get; set; }

	[Option('i', "input", HelpText = "Input file for packing")]
    public string? File { get; set; }
}
class Gui 
{
	public static async Task<int> Execute(GuiOptions opts)
	{
		Build(opts);
		return 0;
	}
	public static async void Build(GuiOptions opts = null)
	{
		try
		{
			// start the Gui
			Application.Init();

			// Open the deadpack if a file is passed in
			Application.MainLoop.AddIdle(() => 
			{ 
				if (!String.IsNullOrEmpty(opts.UnpackFile))
				{
					ViewMenu.OpenDeadpack(opts.UnpackFile);
					Application.RequestStop();
				}
				else if (!String.IsNullOrEmpty(opts.File))
				{
					new DialogCreateDeadPack().Build("", opts.File); 
					Application.RequestStop();
				}
				return false; // cancel the idle so it only runs once
			});

			Application.Top.ColorScheme = new ColorScheme()
			{
				Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				Focus = Application.Driver.MakeAttribute(Color.White, Color.Gray),
				HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				HotFocus = Application.Driver.MakeAttribute(Color.White, Color.Gray),
			};

			Globals.StandardColors = new ColorScheme()
			{
				Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				Focus = Application.Driver.MakeAttribute(Color.White, Color.Gray),
				HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				HotFocus = Application.Driver.MakeAttribute(Color.White, Color.Gray),
			};

			Globals.WhiteOnBlue = new ColorScheme()
			{
				Normal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				Focus = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Blue),
				HotFocus = Application.Driver.MakeAttribute(Color.White, Color.Blue),
			};

			Globals.BlueOnWhite = new ColorScheme()
			{
				Normal = Application.Driver.MakeAttribute(Color.White, Color.Cyan),
				Focus = Application.Driver.MakeAttribute(Color.White, Color.Cyan),
				HotNormal = Application.Driver.MakeAttribute(Color.White, Color.Cyan),
				HotFocus = Application.Driver.MakeAttribute(Color.White, Color.Cyan),
			};

			Globals.SetProgressSource(new List<string>());
			Globals.Verbose = 0;

			ViewMenu menu = new ViewMenu();
			ViewStatusBar statusBar = new ViewStatusBar();

			Globals.ViewLeft = new FrameView("Aliases")
			{
				X = 0,
				Y = 1,
				Width = 80,
				Height = Dim.Fill() - 1,
				ColorScheme = Globals.StandardColors
			};

			Globals.ViewRight = new FrameView("DeadPacks")
			{
				X = 80,
				Y = 1,
				Width = Dim.Fill(),
				Height = Dim.Fill() - 1,
				ColorScheme = Globals.StandardColors
			};

			Globals.ViewAliases = new ViewAliases();

			Globals.ViewLeft.Add(Globals.ViewAliases);


			// add the list of deadpacks
			Globals.ViewDeadPacks = new ViewDeadPacks(Globals.Alias, Globals.Location);
			Globals.ViewRight.Add(Globals.ViewDeadPacks);


			// Add both menu and win in a single call
			Application.Top.RemoveAll();
			Application.Top.Add(menu.Menu, statusBar.StatusBar, Globals.ViewRight);
			Application.Top.Add(Globals.ViewLeft);
			
			

			Application.Run();
			
			Application.Shutdown();

			return;
		}
		catch (Exception e)
		{
			DialogError error = new DialogError(e.Message);
		}
	}

	
}