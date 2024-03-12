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
	public static void UpdateProgressMessage(string message)
	{
		try
		{
			ProgressSource.Add(message);
			ProgressListView.SetNeedsDisplay();
			ProgressListView.Redraw(ProgressListView.Bounds);
		}
		catch { }
	}

	public static void UpdateProgressBar(float index, float count)
	{
		ProgressIndex = index;
		ProgressCount = count;
	}
	public static void UpdateProgressBar()
	{
		try
		{
			Progressbar.Fraction = ProgressIndex / ProgressCount;
			ProgressLabel.Text = "Unpacking " + ProgressIndex.ToString() + " of " + ProgressCount.ToString() + "...";
			Progressbar.SetNeedsDisplay();
			ProgressLabel.SetNeedsDisplay();
			ProgressLabel.Redraw(ProgressLabel.Bounds);
			Progressbar.Redraw(Progressbar.Bounds);
			Application.MainLoop.Driver.Wakeup ();
		}
		catch (Exception e)
		{ 
			//Console.WriteLine(e.Message);
		}
	}
	public static float ProgressIndex;
	public static float ProgressCount;
	public static List<string> ProgressSource;
	public static ListView ProgressListView;
	public static ProgressBar Progressbar;
	public static Label ProgressLabel;



    public const string Test = "test123";
}