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

	public static ColorScheme WhiteOnBlue;

	public static ColorScheme BlueOnWhite;
	

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

	public static void UpdateProgressBar(float index, float count, string action)
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
			try
			{
				Progressbar.Fraction = index / count;
			}
			catch { }

			if (index == count)
			{
				ProgressLabel.Text = $"{action} complete";
			}
			else if (index == 0)
			{
				ProgressLabel.Text = $"{action}...";
			}
			else
			{
				ProgressLabel.Text = $"{action} " + index.ToString() + " of " + count.ToString() + "...";
			}

		}
		catch (Exception e)
		{
			//Console.WriteLine(e.Message);
		}
	}
	
	public static void SetupProgress(ProgressBar progressBar, Label progressLabel)
	{
		Progressbar = progressBar;
		ProgressLabel = progressLabel;
	}


	public static List<string> ProgressSource;
	private static ProgressBar Progressbar;
	private static Label ProgressLabel;


    public const string Test = "test123";
}