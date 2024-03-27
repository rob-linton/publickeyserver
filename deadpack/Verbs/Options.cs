using CommandLine;

namespace deadrop.Verbs;

public class Options
{
	[Option('v', "verbose", Default = "0", HelpText = "Set output to verbose messages.")]
	public string Verbose
	{
		get 
		{
			return Globals.Verbose.ToString(); 
		}
		set 
		{ 
			try
			{
				Globals.Verbose = Int32.Parse(value);
			}
			catch
			{
				Globals.Verbose = 0;
			}
		}
	}

	[Option('p', "passphrase", HelpText = "Enter password")]
	public string Password
	{
		get { return Globals.Password; }
		set { Globals.Password = value; }
	}

	[Option('d', "domain", HelpText = "Domain name")]
	public string Domain
	{
		get { return Globals.Domain; }
		set { Globals.Domain = value; }
	}

}
