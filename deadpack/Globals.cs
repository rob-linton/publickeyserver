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
		}
		catch { }
	}

	public static void UpdateProgressBar(float index, float count)
	{
		if (index > count)
		{
			index = count;
		}
		if (count == 0)
		{
			count = 1;
		}
		try
		{
			Progressbar.Fraction = index / count;
			if (index == count)
			{
				ProgressLabel.Text = "Unpacking complete";
			}
			else
			{
				ProgressLabel.Text = "Unpacking " + index.ToString() + " of " + count.ToString() + "...";
			}

		}
		catch (Exception e)
		{
			//Console.WriteLine(e.Message);
		}
	}
	
	public static void SetupProgress(ListView progressListView, ProgressBar progressBar, Label progressLabel)
	{
		ProgressListView = progressListView;
		Progressbar = progressBar;
		ProgressLabel = progressLabel;
	}


	public static List<string> ProgressSource;
	private static ListView ProgressListView;
	private static ProgressBar Progressbar;
	private static Label ProgressLabel;



    public const string Test = "test123";
}