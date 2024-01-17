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

		// save it as PEM format
		File.WriteAllBytes($"{alias}.pem", cipherText);
	}
	/// <summary>
	/// Retrieves a list of aliases from the current directory.
	/// </summary>
	/// <returns>A list of aliases.</returns>
	public static List<string> GetAliases()
	{
		List<string> aliases = new List<string>();

		foreach (string file in Directory.EnumerateFiles(Directory.GetCurrentDirectory(), "*.pem"))
		{
			string alias = Path.GetFileNameWithoutExtension(file).Replace(".privatekey", "");
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

		byte[] cipherText = File.ReadAllBytes($"{alias}.pem");

		byte[] key = password.ToBytes();

		byte[] plainText = BouncyCastleHelper.DecryptWithKey(cipherText, key, nonce);

		return plainText.FromBytes();
	}
	
}