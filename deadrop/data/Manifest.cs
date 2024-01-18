using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Org.BouncyCastle.Crypto;

namespace deadrop;

class Manifest
{
	[JsonPropertyName("name")]
    public required string Name { get; set; }

	[JsonPropertyName("files")]
	public required List<FileItem> Files { get; set; }

	public static Manifest LoadFromFile(string file, AsymmetricCipherKeyPair keyPair, string alias)
	{
		// open the zip file
		using (ZipArchive archive = ZipFile.OpenRead(file))
		{
			// get the envelope
			var entry = archive.GetEntry("manifest") ?? throw new Exception("Could not find envelope.json in package");
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

						byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(encryptedKey, keyPair.Private);

						byte[] nonce = envelope.From.ToLower().ToBytes();

						byte[] manifestBytes = BouncyCastleHelper.DecryptWithKey(encryptedManifest, key, nonce);

						string manifestJson = manifestBytes.FromBytes();
						Manifest manifest = JsonSerializer.Deserialize<Manifest>(manifestJson) ?? throw new Exception("Could not deserialize manifest");

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
