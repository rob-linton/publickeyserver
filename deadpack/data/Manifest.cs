using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.Utilities;

namespace deadrop;

public class Manifest
{
	[JsonPropertyName("name")]
    public required string Name { get; set; }

	[JsonPropertyName("files")]
	public required List<FileItem> Files { get; set; }

	public static Manifest LoadFromFile(string file, AsymmetricCipherKeyPair keyPairtt, string alias, string password)
	{
		// open the zip file
		using (ZipArchive archive = ZipFile.OpenRead(file))
		{
			// get the envelope
			var entry = archive.GetEntry("manifest") ?? throw new Exception("Could not find manifest in package");
			byte[] encryptedManifest = new byte[entry.Length];
			using (var stream = entry.Open())
			{
				// zip stream does not support ReadAllBytes, so use our own
				encryptedManifest = Misc.ReadAllBytes(stream);

				// get the envelope
				Envelope envelope = Envelope.LoadFromFile(file);
				foreach (var recipient in envelope.To)
				{
					if (recipient.Alias == alias)
					{
						string encryptedKeyBase64 = recipient.Key;
						byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

						string encryptedKyberKeyBase64 = recipient.KyberKey;
						byte[] encryptedKyberKey = Convert.FromBase64String(encryptedKyberKeyBase64);

						string privateKeyPem = Storage.GetPrivateKey($"{alias}.rsa", password);

						AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);


						//
						// post quantum cryptography
						//
						// get the kyber private key
						KyberKeyParameters kyberPrivateKey;
						
						string privateKeyKyber = Storage.GetPrivateKey($"{alias}.kyber", password);
						byte[] privateKeyKyberBytes = Convert.FromBase64String(privateKeyKyber);
						kyberPrivateKey = BouncyCastleQuantumHelper.WriteKyberPrivateKey(privateKeyKyberBytes);
						

						// now decrypt the key using kyber
						//Misc.LogLine(opts, "- Decrypting kyber key...");
						var myKemExtractor = new KyberKemExtractor(kyberPrivateKey);
						var kyberSecret = myKemExtractor.ExtractSecret(encryptedKyberKey);

						// now decrypt the key
						byte[] decryptedEncryptedKey = BouncyCastleHelper.DecryptWithKey(encryptedKey, kyberSecret, envelope.From.ToLower().ToBytes());

						byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(decryptedEncryptedKey, keyPair.Private);

						//
						// now we should have the key used to encrypt all of the files
						//

						// now decrypt the manifest
						byte[] nonce = envelope.From.ToLower().ToBytes();
						byte[] manifestJsonBytes = BouncyCastleHelper.DecryptWithKey(encryptedManifest, key, nonce);

						string manifestJson = manifestJsonBytes.FromBytes();
						var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson) ?? throw new Exception("Could not deserialize manifest");

						// return it
						return manifest;
					}
				}
			}
		}

		// if we get here, we didn't find the alias
		throw new Exception($"Could not find alias {alias} in package");
	}
}
