using System.Text;
using System.Text.Json;
using deadrop.Verbs;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.X509;

namespace deadrop;

public class Misc
{
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves the alias from the given alias and domain string.
	/// </summary>
	/// <param name="aliasAndDomain">The alias and domain string.</param>
	/// <returns>The alias extracted from the alias and domain string.</returns>
	public static string GetAliasFromAlias(string aliasAndDomain)
	{
		return aliasAndDomain.Split('.')[0];
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Extracts the domain from an alias and domain string.
	/// </summary>
	/// <param name="aliasAndDomain">The alias and domain string.</param>
	/// <returns>The domain extracted from the alias and domain string.</returns>
	public static string GetDomainFromAlias(string aliasAndDomain)
	{
		string[] bits = aliasAndDomain.Split('.');
		StringBuilder sb = new StringBuilder();
		for (int i = 1; i < bits.Length; i++)
		{
			sb.Append(bits[i]);
			if (i < bits.Length - 1)
			{
				sb.Append('.');
			}
		}
		return sb.ToString();
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Gets the domain based on the provided options and alias.
	/// If the domain is specified in the options, it is returned.
	/// If the domain is not specified and the alias is null or empty, "publickeyserver.org" is returned.
	/// If the domain is not specified and the alias is not null or empty, the domain is retrieved from the alias.
	/// </summary>
	/// <param name="opts">The options object containing the domain.</param>
	/// <param name="alias">The alias used to retrieve the domain.</param>
	/// <returns>The domain to be used.</returns>
	public static string GetDomain(Options opts, string alias)
	{
		if (opts.Domain != null && opts.Domain.Length > 0)
		{
			return opts.Domain;
		}
		else
		{	if (String.IsNullOrEmpty(alias))
			{
				return "publickeyserver.org";
			}
			else
			{
				return GetDomainFromAlias(alias);
			}
		}
		
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves an X509 certificate based on the provided options and alias.
	/// </summary>
	/// <param name="opts">The options for retrieving the certificate.</param>
	/// <param name="alias">The alias of the certificate.</param>
	/// <returns>The X509 certificate.</returns>
	public static async Task<X509Certificate> GetCertificate(Options opts, string alias)
	{

		string domain = Misc.GetDomain(opts, alias);

		// now get the "from" alias	
		var result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}", opts);

		var c = JsonSerializer.Deserialize<CertResult>(result) ?? throw new Exception("Could not deserialize cert result");
		var certificate = c.Certificate ?? throw new Exception("Could not get certificate from cert result");

		return BouncyCastleHelper.ReadCertificateFromPemString(certificate);
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a message to the console if the verbosity level is greater than 0.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The message to be logged.</param>
	public static void LogLine(Options opts, string message)
	{
		if (opts.Verbose > 0)
			Console.WriteLine(message);
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a message to the console if the verbosity level is greater than 1.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The message to be logged.</param>
	public static void LogLine1(Options opts, string message)
	{
		if (opts.Verbose > 1)
		{
			Console.WriteLine("--------------------------------------------------------------------------------");
			Console.WriteLine(message);
			Console.WriteLine("--------------------------------------------------------------------------------");
		}
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a message to the console.
	/// </summary>
	/// <param name="message">The message to be logged.</param>
	public static void LogLine(string message)
	{
		Console.WriteLine(message);
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs an error message with optional details.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The error message.</param>
	/// <param name="details">Optional details about the error.</param>
	public static void LogError(Options opts, string message, string details = "")
	{
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine($"\n*** ERROR: {message} ***\n");

		if (opts.Verbose > 0 && !String.IsNullOrEmpty(details))
			Console.Write($"*** ERROR: {details} ***\n");
		Console.ResetColor();
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a character to the console if the verbose level is greater than 0.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The message to be logged.</param>
	public static void LogChar(Options opts, string message)
	{
		if (opts.Verbose > 0)
			Console.Write(message);
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a character to the console.
	/// </summary>
	/// <param name="message">The character to be logged.</param>
	public static void LogChar(string message)
	{
		Console.Write(message);
	}
	// --------------------------------------------------------------------------------------------------------
	public static void LogCheckMark(string message)
	{
		message = message + "  ................................................................................................................";
		Console.WriteLine(message.Substring(0, 110) + "  [ \u2713 ]");
	}
	// --------------------------------------------------------------------------------------------------------
	// log a cross check mark
	public static void LogCross(string message)
	{
		message = message + "  ................................................................................................................";
		// print it in red
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(message.Substring(0, 110) + "  [ \u2717 ]");
		Console.ResetColor();
	}
	// --------------------------------------------------------------------------------------------------------
	public static void LogList(string col1, string col2, string col3, string col4)
	{
		string col1padded = col1 + ".                                                       ";
		string col2padded = col2 + "                                                        ";
		string col3padded = col3 + "                                                        ";
		string col4padded = col4 + "                                                        ";

		Console.WriteLine(col1padded.Substring(0, 3) + " " + col2padded.Substring(0, 80) + "  " + col3padded.Substring(0, 10) + "  " + col4padded.Substring(0, 20));
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Loads all bytes from the specified stream.
	/// </summary>
	/// <param name="stream">The stream to read from.</param>
	/// <returns>An array of bytes containing the data read from the stream.</returns>
	public static byte[] ReadAllBytes(Stream stream)
	{
		byte[] buffer = new byte[16 * 1024];
		using (MemoryStream ms = new MemoryStream())
		{
			int read;
			while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
			{
				ms.Write(buffer, 0, read);
			}
			return ms.ToArray();
		}
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs the header information for the DEADPACK application.
	/// </summary>
	public static void LogHeader()
	{
		LogArt();

		LogLine("================================================================================");
		LogLine("DEADPACK v1.0 (deadrop.org)");
		LogLine("Deadrop's Encrypted Archive and Distribution PACKage");
		LogLine("Copyright Rob Linton, 2023");
		LogLine("================================================================================\n");
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Displays the Dead Drop ASCII art logo in the console.
	/// </summary>
	public static void LogArt()
	{
		string art = 
@"
################################################################################
##########################  Dead Drop (deadrop.org)   ##########################
################################################################################
###############################                   ##############################
###########################                           ##########################
#########################                               ########################
########################                                 #######################
#######################*                                 *######################
#######################                                   ######################
#######################/                                 /######################
########################,    *####&\         /&####*    ,#######################
#########################   ##########     ##########   ########################
########################   ###########     ###########   #######################
#######################      #######/       \#######      ######################
#######################&              #####              &######################
##############################       #######       (############################
###############################                   ##############################
################,    ##########                   ##########    ,###############
##############&        &############|#|#|#|#|############&        &#############
#############               &#######################&               ############
##############&                   (###########(                   &#############
###########################/                         /##########################
#################################               ################################
################.*%&####&(             *#/             (&####&%*.(##############
#############                   .###############.                   ############
##############*           ,###########################,           *#############
################      ,###################################,      ###############
################################################################################
################### Anonymous End to End Encrypted File Drops ##################
################################################################################
";
		LogLine(art);
	}

	/// <summary>
	/// Formats the given size in bytes to a human-readable string representation.
	/// </summary>
	/// <param name="receiveSize">The size in bytes to be formatted.</param>
	/// <returns>A string representation of the formatted size.</returns>
	internal static string FormatBytes(long receiveSize)
	{
		// format the btyes to a pretty string
		string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		double len = receiveSize;
		int order = 0;
		while (len >= 1024 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1024;
		}
		string result = String.Format("{0:0.##} {1}", len, sizes[order]);
		return result;
	}
}