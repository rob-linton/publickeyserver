using System.Net.NetworkInformation;

namespace deadrop;

public class Storage
{
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
	/// Retrieves a list of aliases from the current directory.
	/// </summary>
	/// <returns>A list of aliases.</returns>
	public static List<Alias> GetAliases()
	{
		List<Alias> aliases = new List<Alias>();

		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "aliases"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "aliases");

		foreach (string file in Directory.EnumerateFiles(deadDropFolder, "*.rsa"))
		{
			string sAlias = Path.GetFileNameWithoutExtension(file).Replace(".rsa", "");
			
			// get the unix timestamp of the file in seconds
			long timestamp = (long)(File.GetCreationTime(file) - new DateTime(1970, 1, 1)).TotalSeconds;
			
			Alias alias = new Alias 
			{ 
				Name = sAlias, 
				Timestamp = timestamp,
				Filename = file 
			};

			aliases.Add(alias);
		}

		return aliases;
	}
	/// <summary>
	/// Retrieves the private key associated with the specified alias and password.
	/// </summary>
	/// <param name="alias">The alias of the private key.</param>
	/// <param name="password">The password used to decrypt the private key.</param>
	/// <returns>The decrypted private key as a string.</returns>
	public static string GetPrivateKey(string alias, string password)
	{
		string sNonce = Misc.GetDomainFromAlias(alias);
		byte[] nonce = sNonce.ToBytes();

		// get the users home userdata directoru
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "aliases"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "aliases");

		Directory.CreateDirectory(deadDropFolder);

		
		byte[] cipherText = File.ReadAllBytes(Path.Join(deadDropFolder, $"{alias}"));

		byte[] key = password.ToBytes();

		byte[] plainText;
		try
		{
			plainText = BouncyCastleHelper.DecryptWithKey(cipherText, key, nonce);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\n*** Invalid passphrase ***\n");
			throw new Exception($"Unable to decrypt private key for {alias}", ex);
		}
		return plainText.FromBytes();
	}

	public static string GetDeadPackDirectoryInbox(string alias, string dir)
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "inbox", alias));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "inbox", alias);
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static string GetDeadPackDirectorySent(string alias, string dir)
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "sent", alias));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "sent", alias);
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static string GetDeadPackDirectoryOutbox(string dir)
	{
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", "outbox"));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", "outbox");
		if (string.IsNullOrEmpty(dir))
			return deadDropFolder;
		else
			return Path.Join(deadDropFolder, dir);
	}

	public static List<DeadPack> ListDeadPacks(string alias, string location)
	{
		List<DeadPack> deadpacks = new List<DeadPack>();

		// get the users home userdata directory
		string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		Directory.CreateDirectory(Path.Join(localAppData, "deadpack", location));
		string  deadDropFolder = Path.Join(localAppData, "deadpack", location);

		foreach (string file in Directory.EnumerateFiles(deadDropFolder, "*.deadpack"))
		{
			string name = Path.GetFileNameWithoutExtension(file).Replace(".deadpack", "");

			string[] bits = name.Split(' ');
			string date = bits[0];
			long timestamp = long.Parse(date);

			// concatanate all of the bits back together again
			string shortName = string.Join(" ", bits.Skip(1));

			DeadPack deadpack = new DeadPack 
			{ 
				Name = shortName, 
				Timestamp = timestamp,
				Filename = file 
			};

			deadpacks.Add(deadpack);
		}

		return deadpacks;
	}
	
}