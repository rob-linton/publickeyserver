using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using suredrop.Verbs;

namespace suredrop;

public class Storage
{
	public static string EnvironmentGetFolderPathLocalApplicationData()
	{
		// linux
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			// get the linux home directory
			return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/.local/share";
		}
		// mac
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}
		// windows
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}
		else
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		}
	}

	// save the settings
	public static void SaveSettings(Settings settings)
	{
		// get the users home userdata directoru
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		Directory.CreateDirectory(Path.Join(localAppData, "surepack"));
		string  sureDropFolder = Path.Join(localAppData, "surepack");

		Directory.CreateDirectory(sureDropFolder);

		string s = JsonSerializer.Serialize(settings);

		// save it in json format
		File.WriteAllText(Path.Join(sureDropFolder, "settings.json"), s);
	}
	// get the settings
	public static Settings GetSettings()
	{
		try
		{
			// get the users home userdata directoru
			string localAppData = EnvironmentGetFolderPathLocalApplicationData();
			Directory.CreateDirectory(Path.Join(localAppData, "surepack"));
			string sureDropFolder = Path.Join(localAppData, "surepack");

			Directory.CreateDirectory(sureDropFolder);

			string settings = File.ReadAllText(Path.Join(sureDropFolder, "settings.json"));
			var deserializedSettings = JsonSerializer.Deserialize<Settings>(settings);
			return deserializedSettings ?? new Settings();
		}
		catch
		{
			return new Settings();
		}

	}
	public static void StoreCert(string alias, string cert, string location = "aliases")
	{
		// get the users home userdata directoru
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		Directory.CreateDirectory(Path.Join(localAppData, "surepack", location));
		string  sureDropFolder = Path.Join(localAppData, "surepack", location);

		Directory.CreateDirectory(sureDropFolder);

		// save it as PEM format
		File.WriteAllText(Path.Join(sureDropFolder, $"{alias}.pem"), cert);
	}

	public static string GetCert(string alias, string location = "aliases")
	{
		// get the users home userdata directoru
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		Directory.CreateDirectory(Path.Join(localAppData, "surepack", location));
		string  sureDropFolder = Path.Join(localAppData, "surepack", location);

		Directory.CreateDirectory(sureDropFolder);

		return File.ReadAllText(Path.Join(sureDropFolder, $"{alias}.pem"));
	}

	/// <summary>
	/// Stores the private key in encrypted form as a PEM file.
	/// </summary>
	/// <param name="alias">The alias associated with the private key.</param>
	/// <param name="privateKeyPem">The private key in PEM format.</param>
	/// <param name="password">The password used for encryption.</param>
	public static void StorePrivateKey(string alias, string privateKeyPem, string password)
	{
		// encrypt the private key
		byte[] msg = privateKeyPem.ToBytes();
		byte[] key = password.ToBytes();

		string sNonce = Misc.GetDomainFromAlias(alias);
		byte[] nonce = sNonce.ToBytes();

		byte[] cipherText = BouncyCastleHelper.EncryptWithKey(msg, key, nonce);

		// get the users home userdata directoru
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		Directory.CreateDirectory(Path.Join(localAppData, "surepack", "aliases"));
		string  sureDropFolder = Path.Join(localAppData, "surepack", "aliases");

		Directory.CreateDirectory(sureDropFolder);

		// save it as PEM format
		File.WriteAllBytes(Path.Join(sureDropFolder, $"{alias}"), cipherText);
	}
	/// <summary>
	/// Retrieves a list of aliases from the aliases directory.
	/// </summary>
	/// <returns>A list of aliases.</returns>
	public static List<Alias> GetAliases(string location = "aliases")
	{
		SortedList<string, Alias> aliases = new SortedList<string, Alias>();

		// get the users home userdata directoru
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		Directory.CreateDirectory(Path.Join(localAppData, "surepack", location));
		string  sureDropFolder = Path.Join(localAppData, "surepack", location);

		foreach (string file in Directory.EnumerateFiles(sureDropFolder, $"*.pem"))
		{
			string sAlias = Path.GetFileNameWithoutExtension(file).Replace($".pem", "");

			// load the alias into a x.509 
			string x509Pem = GetCert(sAlias, location);
			List<string> names = BouncyCastleHelper.GetAltNames(x509Pem);
			(string email, string aliasInternal) = BouncyCastleHelper.GetEmailFromAltNames(names);

			// get the unix timestamp of the file in seconds
			long timestamp = (long)(File.GetCreationTime(file) - new DateTime(1970, 1, 1)).TotalSeconds;
			
			Alias alias = new Alias 
			{ 
				Name = aliasInternal, 
				Timestamp = timestamp,
				Filename = file,
				Email = email
			};

			aliases.Add(sAlias, alias);
		}

		return aliases.Values.ToList();
	}

	public static void DeletePrivateKey(string alias)
	{
		try
		{
			// get the users home userdata directoru
			string localAppData = EnvironmentGetFolderPathLocalApplicationData();
			Directory.CreateDirectory(Path.Join(localAppData, "surepack", "aliases", "deleted"));
			string sureDropFolder = Path.Join(localAppData, "surepack", "aliases");
			string sureDropDeletedFolder = Path.Join(localAppData, "surepack", "aliases", "deleted");

			File.Move(Path.Join(sureDropFolder, $"{alias}.rsa"), Path.Join(sureDropDeletedFolder, $"{alias}.rsa"));
			File.Move(Path.Join(sureDropFolder, $"{alias}.dilithium"), Path.Join(sureDropDeletedFolder, $"{alias}.dilithium"));
			File.Move(Path.Join(sureDropFolder, $"{alias}.kyber"), Path.Join(sureDropDeletedFolder, $"{alias}.kyber"));
			File.Move(Path.Join(sureDropFolder, $"{alias}.root"), Path.Join(sureDropDeletedFolder, $"{alias}.root"));
			File.Move(Path.Join(sureDropFolder, $"{alias}.pem"), Path.Join(sureDropDeletedFolder, $"{alias}.pem"));
		}
		catch { }
	}

	/// <summary>
	/// Retrieves the private key associated with the specified alias and password.
	/// </summary>
	/// <param name="alias">The alias of the private key.</param>
	/// <param name="password">The password used to decrypt the private key.</param>
	/// <returns>The decrypted private key as a string.</returns>
	public static string GetPrivateKey(string alias, bool first = true)
	{
		// we get the password from the globals to avoid thread sync issues
		string password = Globals.Password??"";

		string sNonce = Misc.GetDomainFromAlias(alias);
		byte[] nonce = sNonce.ToBytes();

		// get the users home userdata directoru
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		Directory.CreateDirectory(Path.Join(localAppData, "surepack", "aliases"));
		string  sureDropFolder = Path.Join(localAppData, "surepack", "aliases");

		Directory.CreateDirectory(sureDropFolder);

		
		byte[] cipherText = File.ReadAllBytes(Path.Join(sureDropFolder, $"{alias}"));

		byte[]? key = password?.ToBytes();

		byte[] plainText;
		try
		{
			if (key != null)
			{
				plainText = BouncyCastleHelper.DecryptWithKey(cipherText, key, nonce);
			}
			else
			{
				throw new ArgumentNullException(nameof(key), "Key cannot be null.");
			}
		}
		catch 
		{
			if (first)
			{
				new DialogPassword();

				// now try again
				return GetPrivateKey(alias, false);
			}
			else
			{
				throw new Exception($"Incorrect password. Need help? Visit: https://rob-linton.github.io/publickeyserver/HELP.html#troubleshooting");
			}
		}
		return plainText.FromBytes();
	}

	public static string GetSurePackDirectoryInbox(string alias, string dir = "")
	{
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();

		Directory.CreateDirectory(Path.Join(localAppData, "surepack", "inbox", alias));
		string  sureDropFolder = Path.Join(localAppData, "surepack", "inbox", alias);
		if (string.IsNullOrEmpty(dir))
			return sureDropFolder;
		else
			return Path.Join(sureDropFolder, dir);
	}

	public static string GetSurePackDirectorySent(string alias, string dir = "")
	{
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();

		Directory.CreateDirectory(Path.Join(localAppData, "surepack", "sent", alias));
		string  sureDropFolder = Path.Join(localAppData, "surepack", "sent", alias);
		if (string.IsNullOrEmpty(dir))
			return sureDropFolder;
		else
			return Path.Join(sureDropFolder, dir);
	}

	public static string GetSurePackDirectoryOutbox(string dir = "")
	{
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();

		Directory.CreateDirectory(Path.Join(localAppData, "surepack", "outbox"));
		string  sureDropFolder = Path.Join(localAppData, "surepack", "outbox");
		if (string.IsNullOrEmpty(dir))
			return sureDropFolder;
		else
			return Path.Join(sureDropFolder, dir);
	}

	public static string GetSurePackDirectoryHistory(string dir = "")
	{
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();

		Directory.CreateDirectory(Path.Join(localAppData, "surepack", "history"));
		string  sureDropFolder = Path.Join(localAppData, "surepack", "history");
		if (string.IsNullOrEmpty(dir))
			return sureDropFolder;
		else
			return Path.Join(sureDropFolder, dir);
	}

	public static List<SurePack> ListSurePacks(string useAlias, string location)
	{
		string alias = useAlias;
		SortedList<long, SurePack> sorted = new SortedList<long, SurePack>();

		// get the users home userdata directory
		string localAppData = EnvironmentGetFolderPathLocalApplicationData();
		string sureDropFolder = "";
		if (location == "outbox")
		{
			Directory.CreateDirectory(Path.Join(localAppData, "surepack", location));
			sureDropFolder = Path.Join(localAppData, "surepack", location);
		}
		else
		{
			Directory.CreateDirectory(Path.Join(localAppData, "surepack", location, alias));
			sureDropFolder = Path.Join(localAppData, "surepack", location, alias);
		}

		// for the outbox we can't open the manifest, so only list the basic information
		{

			// get the private key for this alias
			long i = 1;
			Random random = new Random();
			foreach (string file in Directory.EnumerateFiles(sureDropFolder, "*.surepack"))
			{
				long size = new FileInfo(file).Length;

				Envelope envelope = Envelope.LoadFromFile(file);

				// if the alias is blank, then get it from the envelope
				if (String.IsNullOrEmpty(useAlias))
				{
					alias = envelope.From;
				}

				Manifest manifest = Manifest.LoadFromFile(file, alias);

				// convert the manifest base64
				byte[] base64Subject = Convert.FromBase64String(manifest.Subject);
				string subject = Encoding.UTF8.GetString(base64Subject);

				// do the same for the message
				byte[] base64Message = Convert.FromBase64String(manifest.Message);
				string message = Encoding.UTF8.GetString(base64Message);

				// get the timestamp from the envelope
				long timestamp = envelope.Created;

				// get the list of files in the surepack
				List<FileItem> files = manifest.Files;

				// get the from alias
				string fromAlias = envelope.From;
				List<Recipient> recipients = envelope.To;
				
				// remove the from alias from the recipients
				recipients.RemoveAll(r => r.Alias == fromAlias);

				SurePack surepack = new SurePack
				{
					Subject = subject,
					Message = message,
					Timestamp = timestamp,
					Files = files,
					From = fromAlias,
					Alias = alias,
					Filename = file,
					Recipients = recipients,
					Size = size
				};

				// create a random long

				try
				{
					string randomTimestamp = timestamp.ToString() + random.Next(1, 1000).ToString().PadLeft(3, '0');
					sorted.Add(Convert.ToInt64(randomTimestamp), surepack);
				}
				catch { }
				i++;

			}

			return sorted.Values.ToList().Reverse<SurePack>().ToList();
		}
	}
	// ------------------------------------------------------------------------------------------------------------------
	// count how many surepacks are in the directory
	public static long CountSurePacks(string dir)
	{
		long count = 0;
		foreach (string file in Directory.EnumerateFiles(dir, "*.surepack"))
		{
			count++;
		}
		return count;
	}
	// ------------------------------------------------------------------------------------------------------------------
	// count how many surepacks are in the directory that are younger than hours
	public static long CountNewSurePacksHour(string dir, double hours)
	{
		long count = 0;
		foreach (string file in Directory.EnumerateFiles(dir, "*.surepack"))
		{
			Envelope envelope = Envelope.LoadFromFile(file);

			// get current unix timestamp
			long current = (long)(DateTime.Now.ToUniversalTime().AddHours(-hours) - new DateTime(1970, 1, 1)).TotalSeconds;
			if (envelope.Created > current)
			{
				count++;
			}
		}
		return count;
	}
	// ------------------------------------------------------------------------------------------------------------------

	
}