using System.Net.NetworkInformation;

namespace deadrop;

public class Storage
{
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
	public static string GetPrivateKey(string alias, string password)
	{
		string sNonce = Misc.GetDomainFromAlias(alias);
		byte[] nonce = sNonce.ToBytes();

		byte[] cipherText = File.ReadAllBytes($"{alias}.pem");

		byte[] key = password.ToBytes();

		byte[] plainText = BouncyCastleHelper.DecryptWithKey(cipherText, key, nonce);

		return plainText.FromBytes();
	}
	/*
	public static void StoreCert(string alias, string certPem)
	{
		// save it as PEM format
		File.WriteAllText($"{alias}.pem", certPem);
	}

	public static string GetCert(string alias)
	{
		return File.ReadAllText($"{alias}.pem");
	}
	*/
}