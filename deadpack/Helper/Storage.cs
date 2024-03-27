using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using deadrop.Verbs;

namespace deadrop;

public class Storage
{
	// save the settings
	public static void SaveSettings(Settings settings)
	{
		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack");

		Directory.CreateDirectory(deadDropFolder);

		string s = JsonSerializer.Serialize(settings);

		// save it in json format
		File.WriteAllText(Path.Join(deadDropFolder, "settings.json"), s);
	}
	// get the settings
	public static Settings GetSettings()
	{
		try
		{
			// get the users home userdata directoru
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			Directory.CreateDirectory(Path.Join(localAppData, "deadpack"));
			string deadDropFolder = Path.Join(localAppData, "deadpack");

			Directory.CreateDirectory(deadDropFolder);

			string settings = File.ReadAllText(Path.Join(deadDropFolder, "settings.json"));
			return JsonSerializer.Deserialize<Settings>(settings);
		}
		catch
		{
			return new Settings();
		}

	}
	public static void StoreCert(string alias, string cert, string location = "aliases")
	{
		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", location));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", location);

		Directory.CreateDirectory(deadDropFolder);

		// save it as PEM format
		File.WriteAllText(Path.Join(deadDropFolder, $"{alias}.pem"), cert);
	}

	public static string GetCert(string alias, string location = "aliases")
	{
		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", location));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", location);

		Directory.CreateDirectory(deadDropFolder);

		return File.ReadAllText(Path.Join(deadDropFolder, $"{alias}.pem"));
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
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "aliases"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "aliases");

		Directory.CreateDirectory(deadDropFolder);

		// save it as PEM format
		File.WriteAllBytes(Path.Join(deadDropFolder, $"{alias}"), cipherText);
	}
	/// <summary>
	/// Retrieves a list of aliases from the aliases directory.
	/// </summary>
	/// <returns>A list of aliases.</returns>
	public static List<Alias> GetAliases(string location = "aliases")
	{
		SortedList<string, Alias> aliases = new SortedList<string, Alias>();

		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", location));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", location);

		foreach (string file in Directory.EnumerateFiles(deadDropFolder, $"*.pem"))
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
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "aliases", "deleted"));
			string deadDropFolder = Path.Join(localAppData, "deadpack", "aliases");
			string deadDropDeletedFolder = Path.Join(localAppData, "deadpack", "aliases", "deleted");

			File.Move(Path.Join(deadDropFolder, $"{alias}.rsa"), Path.Join(deadDropDeletedFolder, $"{alias}.rsa"));
			File.Move(Path.Join(deadDropFolder, $"{alias}.dilithium"), Path.Join(deadDropDeletedFolder, $"{alias}.dilithium"));
			File.Move(Path.Join(deadDropFolder, $"{alias}.kyber"), Path.Join(deadDropDeletedFolder, $"{alias}.kyber"));
			File.Move(Path.Join(deadDropFolder, $"{alias}.root"), Path.Join(deadDropDeletedFolder, $"{alias}.root"));
			File.Move(Path.Join(deadDropFolder, $"{alias}.pem"), Path.Join(deadDropDeletedFolder, $"{alias}.pem"));
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
		string password = Globals.Password;

		string sNonce = Misc.GetDomainFromAlias(alias);
		byte[] nonce = sNonce.ToBytes();

		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "aliases"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "aliases");

		Directory.CreateDirectory(deadDropFolder);

		
		byte[] cipherText = File.ReadAllBytes(Path.Join(deadDropFolder, $"{alias}"));

		byte[] key = password?.ToBytes();

		byte[] plainText;
		try
		{
			plainText = BouncyCastleHelper.DecryptWithKey(cipherText, key, nonce);
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
				throw new Exception($"Incorrect password");
			}
		}
		return plainText.FromBytes();
	}

	public static string GetDeadPackDirectoryInbox(string alias, string dir = "")
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "inbox", alias));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "inbox", alias);
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static string GetDeadPackDirectorySent(string alias, string dir = "")
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "sent", alias));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "sent", alias);
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static string GetDeadPackDirectoryOutbox(string dir = "")
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "outbox"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "outbox");
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static string GetDeadPackDirectoryHistory(string dir = "")
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "history"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "history");
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static List<DeadPack> ListDeadPacks(string useAlias, string location)
	{
		string alias = useAlias;
		SortedList<long, DeadPack> sorted = new SortedList<long, DeadPack>();

		// get the users home userdata directory
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		string deadDropFolder = "";
		if (location == "outbox")
		{
			Directory.CreateDirectory(Path.Join(localAppData, "deadpack", location));
			deadDropFolder = Path.Join(localAppData, "deadpack", location);
		}
		else
		{
			Directory.CreateDirectory(Path.Join(localAppData, "deadpack", location, alias));
			deadDropFolder = Path.Join(localAppData, "deadpack", location, alias);
		}

		// for the outbox we can't open the manifest, so only list the basic information
		{

			// get the private key for this alias
			long i = 1;
			Random random = new Random();
			foreach (string file in Directory.EnumerateFiles(deadDropFolder, "*.deadpack"))
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

				// get the list of files in the deadpack
				List<FileItem> files = manifest.Files;

				// get the from alias
				string fromAlias = envelope.From;
				List<Recipient> recipients = envelope.To;
				
				// remove the from alias from the recipients
				recipients.RemoveAll(r => r.Alias == fromAlias);

				DeadPack deadpack = new DeadPack
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
					sorted.Add(Convert.ToInt64(randomTimestamp), deadpack);
				}
				catch { }
				i++;

			}

			return sorted.Values.ToList().Reverse<DeadPack>().ToList();
		}
	}
	// ------------------------------------------------------------------------------------------------------------------
	// count how many deadpacks are in the directory
	public static long CountDeadPacks(string dir)
	{
		long count = 0;
		foreach (string file in Directory.EnumerateFiles(dir, "*.deadpack"))
		{
			count++;
		}
		return count;
	}
	// ------------------------------------------------------------------------------------------------------------------
	// count how many deadpacks are in the directory that are younger than hours
	public static long CountNewDeadPacksHour(string dir, double hours)
	{
		long count = 0;
		foreach (string file in Directory.EnumerateFiles(dir, "*.deadpack"))
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