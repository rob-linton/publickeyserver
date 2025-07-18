using suredrop.Verbs;
using Terminal.Gui;
using System.Reflection;

namespace suredrop;
public static class Globals
{
	// Current version - gets the assembly version at runtime
	public static string version => Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
	// 
	// current alias & location
	//
	public static string? Alias;
	public static string? Location;

	//
	// main views
	//
	public static ViewAliases? ViewAliases;
	public static ViewSurePacks? ViewSurePacks;

	//
	// left & right views
	//
	public static FrameView? ViewLeft;
	public static FrameView? ViewRight;

	//
	// global options
	//
	public static string? Password;
	public static string? Domain;
	public static int Verbose;


	//
	// colour schemes
	//
	public static ColorScheme? StandardColors; 

	public static ColorScheme? WhiteOnBlue;

	public static ColorScheme? BlueOnWhite;
	

	//
	// progress for the gui
	//
	public static void UpdateProgressSource(string message)
	{
		try
		{
			lock (ProgressSource??[])
			{
				ProgressSource?.Add(message);
			}
		}
		catch { }
	}

	public static void ClearProgressSource()
	{
		lock (ProgressSource??[])
		{
			ProgressSource?.Clear();
		}
	}

	// create the get set properties for the progress source
	
	public static List<string> GetProgressSource()
	{
		// no lock as it is read only
		return ProgressSource??[];
	}

	public static void SetProgressSource(List<string> value)
	{
		// no lock, call once initially
		ProgressSource = value;
	}

	private static List<string>? ProgressSource;

    public const string Test = "test123";
}