using System.IO.Compression;
using System.Text;
using System.Text.Json;
using suredrop.Verbs;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Tls.Crypto;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;

namespace suredrop;

public class Misc
{
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Replaces Unicode characters with ASCII equivalents for Windows compatibility.
	/// </summary>
	/// <param name="input">The input string that may contain Unicode characters.</param>
	/// <returns>A string with Unicode characters replaced by ASCII equivalents on Windows.</returns>
	public static string SanitizeForWindows(string input)
	{
		// Only apply replacements on Windows
		if (Environment.OSVersion.Platform == PlatformID.Win32NT)
		{
			// Replace common Unicode characters with ASCII equivalents
			input = input.Replace("\u2713", "V");  // Checkmark ✓ -> V
			input = input.Replace("\u2717", "X");  // Cross ✗ -> X
			input = input.Replace("\u2714", "V");  // Heavy checkmark ✔ -> V
			input = input.Replace("\u2718", "X");  // Heavy cross ✘ -> X
			input = input.Replace("\U0001F4DA", "[i]");  // Books emoji 📚 -> [i]
			input = input.Replace("\U0001F4D6", "[i]");  // Open book emoji 📖 -> [i]
			input = input.Replace("\U0001F4A1", "TIP:");  // Lightbulb emoji 💡 -> TIP:
			// Add more replacements as needed
		}
		return input;
	}
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
	public static byte[] GetBytesFromZip(string filename, string archive)
	{
		using (ZipArchive zip = ZipFile.OpenRead(archive))
		{
			foreach (ZipArchiveEntry entry in zip.Entries)
			{
				if (entry.FullName == filename)
				{
					using (var stream = entry.Open())
					{
						return ReadAllBytes(stream);
					}
				}
			}
		}
		
		// Return null or throw an exception if the file is not found
		throw new Exception("File not found in archive");
	}
	public static string GetTextFromZip(string filename, string archive)
	{
		using (ZipArchive zip = ZipFile.OpenRead(archive))
		{
			foreach (ZipArchiveEntry entry in zip.Entries)
			{
				if (entry.FullName == filename)
				{
					using (var stream = entry.Open())
					{
						return new StreamReader(stream).ReadToEnd();
					}
				}
			}
		}
		
		// Return null or throw an exception if the file is not found
		throw new Exception("File not found in archive");
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
	public static string GetDomain(string alias)
	{
		if (Globals.Domain != null && Globals.Domain.Length > 0)
		{
			return Globals.Domain;
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
	public static async Task<X509Certificate> GetCertificate(string alias)
	{

		// is the alias an email address
		string url = "";
		if (alias.Contains("@"))
		{
			string domain = Misc.GetDomain("");
			url = $"https://{domain}/email/{alias}";

			// get a list of files from the s3 bucket in this folder


			
		}
		else
		{
			string domain = Misc.GetDomain(alias);
			url = $"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}";
		}

		

		// now get the "from" alias	
		var result = await HttpHelper.Get(url);

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
	public static void LogLine(string message)
	{
		File.AppendAllLines("output.log", new string[] { message });	
		if (Globals.Verbose > 0)
			Console.WriteLine(SanitizeForWindows(message));
	}
	public static void WriteLine(string message)
	{
		File.AppendAllLines("output.log", new string[] { message });
		if (Globals.Verbose >= 0)
			Console.WriteLine(SanitizeForWindows(message));
	}
	public static void LogLine1(string message)
	{
		File.AppendAllLines("output.log", new string[] { message });
		if (Globals.Verbose > 1)
			Console.WriteLine(SanitizeForWindows(message));
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a message to the console if the verbosity level is greater than 1.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The message to be logged.</param>
	public static void LogLine2(string message)
	{
		//File.AppendAllLines("output.log", new string[] { message });
		if (Globals.Verbose > 2)
		{
			Console.WriteLine("--------------------------------------------------------------------------------");
			Console.WriteLine(SanitizeForWindows(message));
			Console.WriteLine("--------------------------------------------------------------------------------");
		}
	}
	
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs an error message with optional details.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The error message.</param>
	/// <param name="details">Optional details about the error.</param>
	public static void LogError(string message, string details = "", string stack = "")
	{
		if (Globals.Verbose > 0)
		{
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(SanitizeForWindows($"\n{message}\n"));

			if (!String.IsNullOrEmpty(details))
				Console.Write(SanitizeForWindows($"{details}\n"));
			
			Console.ResetColor();
			Console.WriteLine("\nNeed help? Visit: https://rob-linton.github.io/publickeyserver/HELP.html");
		}

		// log it to a file by writing it
		// to the error log file
		File.AppendAllLines("error.log", new string[] { message, details, stack});	

	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a critical error message that is always displayed regardless of verbosity setting.
	/// This should be used for exception handlers to ensure users always see error messages.
	/// </summary>
	/// <param name="message">The error message.</param>
	/// <param name="details">Optional details about the error (e.g., exception message).</param>
	public static void LogCriticalError(string message, string details = "")
	{
		// Always print error messages directly to console, regardless of verbose setting
		Console.ForegroundColor = ConsoleColor.Red;
		Console.WriteLine(SanitizeForWindows($"\n{message}\n"));
		
		if (!String.IsNullOrEmpty(details))
			Console.WriteLine(SanitizeForWindows($"{details}\n"));
		
		Console.ResetColor();
		Console.WriteLine("\nNeed help? Visit: https://rob-linton.github.io/publickeyserver/HELP.html");
		
		// Also log it to the error log file
		File.AppendAllLines("error.log", new string[] { message, details });
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs a character to the console if the verbose level is greater than 0.
	/// </summary>
	/// <param name="opts">The options object.</param>
	/// <param name="message">The message to be logged.</param>
	public static void LogChar(string message)
	{
		if (Globals.Verbose > 0)
			Console.Write(SanitizeForWindows(message));
	}
	// --------------------------------------------------------------------------------------------------------
	public static void LogCheckMark(string message)
	{
		File.AppendAllLines("output.log", new string[] { message });
		message = "[ \u2713 ]  " + message;

		Globals.UpdateProgressSource(message);
		
		if (Globals.Verbose > 0)
		{
			Console.WriteLine(SanitizeForWindows(message));
		}
	}
	// --------------------------------------------------------------------------------------------------------
	// log an informational message
	public static void LogInfo(string message)
	{
		File.AppendAllLines("output.log", new string[] { message });
		message = "[ i ]  " + message;
		Globals.UpdateProgressSource(message);
		
		if (Globals.Verbose > 0)
		{
			Console.WriteLine(SanitizeForWindows(message));
		}
	}
	// --------------------------------------------------------------------------------------------------------
	// log a cross check mark
	public static void LogCross(string message)
	{
		File.AppendAllLines("output.log", new string[] { message });
		message = "[ X ]  " + message;
		Globals.UpdateProgressSource(message);
	
		if (Globals.Verbose >= 0)
		{
			// print it in red
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine(SanitizeForWindows(message));
			Console.ResetColor();
		}
	}
	// --------------------------------------------------------------------------------------------------------
	public static void LogList(string col1, string col2, string col3, string col4)
	{
		if (Globals.Verbose > 0)
		{
			string col1padded = col1 + ".                                                       ";
			string col2padded = col2 + "                                                        ";
			string col3padded = col3 + "                                                        ";
			string col4padded = col4 + "                                                        ";

			Console.WriteLine(SanitizeForWindows(col1padded.Substring(0, 3) + " " + col2padded.Substring(0, 80) + "  " + col3padded.Substring(0, 10) + "  " + col4padded.Substring(0, 20)));
		}
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
	public static byte[] CompressBytesBrotli(byte[] data)
	{
		using (MemoryStream output = new MemoryStream())
		{
			using (BrotliStream dstream = new BrotliStream(output, CompressionLevel.SmallestSize))
			{
				dstream.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}
	}

	public static byte[] DecompressBytesBrotli(byte[] data)
	{
		using (MemoryStream input = new MemoryStream(data))
		{
			using (MemoryStream output = new MemoryStream())
			{
				using (BrotliStream dstream = new BrotliStream(input, CompressionMode.Decompress))
				{
					dstream.CopyTo(output);
				}
				return output.ToArray();
			}
		}
	}

	public static byte[] CompressBytesGZip(byte[] data)
	{
		using (MemoryStream output = new MemoryStream())
		{
			using (GZipStream dstream = new GZipStream(output, CompressionLevel.Optimal))
			{
				dstream.Write(data, 0, data.Length);
			}
			return output.ToArray();
		}
	}

	public static byte[] DecompressBytesGZip(byte[] data)
	{
		using (MemoryStream input = new MemoryStream(data))
		{
			using (MemoryStream output = new MemoryStream())
			{
				using (GZipStream dstream = new GZipStream(input, CompressionMode.Decompress))
				{
					dstream.CopyTo(output);
				}
				return output.ToArray();
			}
		}
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Logs the header information for the SUREPACK application.
	/// </summary>
	public static void LogHeader()
	{
		if (Globals.Verbose > 0)
		{
			LogArt();

			LogLine("===================================================================================================");
			LogLine($"SUREPACK {Globals.version} (publickeyserver.org)");
			LogLine("Post Quantum Cryptography (PQC) using the Crystal Kyber and Dilithium algorithms");
			LogLine("Acknowledgement to the BouncyCastle C# Crypto library");
			LogLine("");
			LogLine("📚 Help Center: https://rob-linton.github.io/publickeyserver/");
			LogLine("📖 SurePack Manual: https://rob-linton.github.io/publickeyserver/HELP.html");
			LogLine("");
			LogLine("💡 Tip: To run with minimal output, use -v 0 (e.g., surepack pack -v 0 ...)");
			LogLine("===================================================================================================\n");
		}
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Displays the Sure Drop ASCII art logo in the console.
	/// </summary>

	public static void LogArt()
	{
		string art = GetArt();
		Console.WriteLine(art);
	}

	public static string GetArt()
	{
		string art = @"";
		return art;
	}

	/// <summary>
	/// Formats the given size in bytes to a human-readable string representation.
	/// </summary>
	/// <param name="receiveSize">The size in bytes to be formatted.</param>
	/// <returns>A string representation of the formatted size.</returns>
	internal static string FormatBytes(long receiveSize)
	{
		if (receiveSize == 0)
		{
			return "0 B";
		}
		
		// format the btyes to a pretty string
		string[] sizes = { "B", "KB", "MB", "GB", "TB" };
		double len = receiveSize;
		int order = 0;
		while (len >= 1000 && order < sizes.Length - 1)
		{
			order++;
			len = len / 1000;
		}
		string result = String.Format("{0:0.##} {1}", len, sizes[order]);
		return result;
	}

	// function to format seconds
	public static string FormatSeconds(long seconds)
	{
		// format the seconds to a pretty string
		TimeSpan t = TimeSpan.FromSeconds(seconds);
		string result = t.ToString(@"hh") + " Hours," + t.ToString(@"mm") + " Minutes & " + t.ToString(@"ss") + " Seconds";
		return result;
	}

	// fuction to request a password from the console
	public static string GetPassword()
	{
		string prompt = "Please enter passphrase: ";
		Console.Write(prompt);
		string password = "";
		ConsoleKeyInfo info = Console.ReadKey(true);
		while (info.Key != ConsoleKey.Enter)
		{
			if (info.Key != ConsoleKey.Backspace)
			{
				Console.Write("*");
				password += info.KeyChar;
			}
			else if (info.Key == ConsoleKey.Backspace)
			{
				if (!string.IsNullOrEmpty(password))
				{
					// remove one character from the list of password characters
					password = password.Substring(0, password.Length - 1);
					// get the location of the cursor
					int pos = Console.CursorLeft;
					// move the cursor to the left by one character
					Console.SetCursorPosition(pos - 1, Console.CursorTop);
					// replace it with space
					Console.Write(" ");
					// move the cursor to the left by one character again
					Console.SetCursorPosition(pos - 1, Console.CursorTop);
				}
			}
			info = Console.ReadKey(true);
		}
		// add a new line because user pressed enter at the end of their password
		Console.WriteLine();
		return password;
	}

	public static string Pad(string input, int length)
	{
		string s = input + "                                                                                                                  ";
		return s.Substring(0, length);
	}

	// method to take a path and possible wildcard and create two parameters to Directory.GetFiles
	public static string[] GetFiles(string path, bool recursive = false)
	{

		// if the path does not start with a directory seperator, add "./" to the front
		if (!path.StartsWith(Path.DirectorySeparatorChar.ToString()) && !path.StartsWith(".") && !path.StartsWith(".."))
		{
			path = $".{Path.DirectorySeparatorChar}" + path;
		}

		string[] files;
		// check if the path contains a wildcard
		if (path.Contains("*"))
		{
			
			// get the directory from the path
			string directory = Path.GetDirectoryName(path) ?? string.Empty;
			// get the search pattern from the path
			string searchPattern = Path.GetFileName(path);
			
			Misc.LogLine($"Wildcard found: {directory}  / {searchPattern}");

			// get the files
			files = Directory.GetFiles(directory, searchPattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}
		else
		{
			// Check if the path is a file or directory
			if (File.Exists(path))
			{
				// It's a single file
				Misc.LogLine($"Single file: {path}");
				files = new string[] { path };
			}
			else if (Directory.Exists(path))
			{
				// It's a directory
				Misc.LogLine($"Directory: {path} / *");
				files = Directory.GetFiles(path, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
			}
			else
			{
				// Path doesn't exist
				throw new FileNotFoundException($"The specified path does not exist: {path}");
			}
		}

		return files;
	}


	public static string UpdateProgressBarLabel(float index, float count, string action, bool pre = false)
	{
		try
		{
			if (!pre)
			{
				if (index == 0 && count == 0)
				{
					return $"{action}...";
				}
				else if (index == count)
				{
					return $"Complete";
				}
				else if (index == 0)
				{
					return $"{action}...";
				}
				else
				{
					return $"{action} " + index.ToString() + " of " + count.ToString() + "...";
				}
			}
			else
			{
				if (index == count)
				{
					return $"{action} complete";
				}
				else if (index == 0)
				{
					return $"{action} " + (index+1).ToString() + " of " + count.ToString() + "...";
				}
				else
				{
					return $"{action} " + (index+1).ToString() + " of " + count.ToString() + "...";
				}
			}

		}
		catch 
		{
			return $"{action}...";
		}
	}
}