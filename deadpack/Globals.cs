using deadrop.Verbs;
using Terminal.Gui;

namespace deadrop;
public static class Globals
{
	// 
	// current alias & location
	//
	public static string Alias;
	public static string Location;

	//
	// main views
	//
	public static ViewAliases ViewAliases;
	public static ViewDeadPacks ViewDeadPacks;

	//
	// left & right views
	//
	public static FrameView ViewLeft;
	public static FrameView ViewRight;

	//
	// global options
	//
	public static string Password;
	public static string Domain;
	public static int Verbose;

	//
	// colour schemes
	//
	public static ColorScheme StandardColors; 

	public static ColorScheme YellowColors;

	//
	// progress for the gui
	//
	public static List<string> ProgressSource;
	public static ListView ProgressListView;
	public static ProgressBar Progressbar;
	public static Label ProgressLabel;


    public const string Test = "test123";
}