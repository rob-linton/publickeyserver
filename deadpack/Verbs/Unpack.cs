#pragma warning disable 1998
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.X509;

namespace deadrop.Verbs;

[Verb("unpack", HelpText = "Unpack a package.")]
public class UnpackOptions : Options
{
   [Option('i', "input", Required = true, HelpText = "Input deadrop file to be processed")]
    public required string File { get; set; }

	[Option('o', "output", Default = "", HelpText = "Output directory")]
	public string? Output { get; set; }

	[Option('a', "alias", HelpText = "Alias to use")]
	public string Alias { get; set; } = "";
}

class Unpack 
{
	public static async Task<int> Execute(UnpackOptions opts)
	{
		Misc.LogHeader();
		Misc.LogLine($"Unpacking deadpack...");
		Misc.LogLine($"Input: {opts.File}");
		Misc.LogLine($"Output directory: {opts.Output}");
			
		//Misc.LogLine($"Recipient Alias: {alias}");

		if (!String.IsNullOrEmpty(opts.Alias))
		{
			return await ExecuteInternal(opts, opts.Alias);
		}
		else
		{
			// loop through all of the aliases
			foreach (var alias in Storage.GetAliases())
			{
				Misc.LogLine($"\nChecking alias {alias}...");
				int result = await ExecuteInternal(opts, alias);
				if (result == 0)
					return result;
			}
			Misc.LogError(opts, "Error unpacking package, no valid alias found");
			return 1;
		}
	}

	public static async Task<int> ExecuteInternal(UnpackOptions opts, string alias)
	{
		try
		{

			string toDomain = Misc.GetDomain(opts, alias);

			// get the input zip file
			opts.File = opts.File.Replace(".deadpack", "") + ".deadpack";
			string zipFile = opts.File;

			

			if (String.IsNullOrEmpty(opts.Password))
			opts.Password = Misc.GetPassword();

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{alias}.root", opts.Password);
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// get the output directory
			if (String.IsNullOrEmpty(opts.Output))
				opts.Output = Path.GetFileNameWithoutExtension(zipFile);

			// create it
			Directory.CreateDirectory(opts.Output);

			string outputDirectory = opts.Output;
			

			if (String.IsNullOrEmpty(opts.Password))
				opts.Password = Misc.GetPassword();


			try
			{
				Misc.LogLine(opts, "");

				// now read the envelope
				string envelopeJson = Misc.GetTextFromZip("envelope", zipFile);
				var envelope = JsonSerializer.Deserialize<Envelope>(envelopeJson) ?? throw new Exception("Could not deserialize envelope");

				// read the envelope signature
				byte[] envelopeSignature = Misc.GetBytesFromZip("envelope.signature", zipFile);

				// read the manifest signature
				byte[] manifestSignature = Misc.GetBytesFromZip("manifest.signature", zipFile);

				// now read the manifest
				byte[] manifestBytes = Misc.GetBytesFromZip("manifest", zipFile);

				string fromDomain = Misc.GetDomain(opts, envelope.From);

				// now get the "from" alias
				var fromX509 = await Misc.GetCertificate(opts, envelope.From);

				// now verify the alias
				Misc.LogLine(opts, $"\n- Verifying sender alias: {envelope.From}");
				(bool validAlias, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, envelope.From, "", opts);

				// verify fingerprint
				if (fromFingerprint.SequenceEqual(rootFingerprintFromFile))
					Misc.LogCheckMark($"Root fingerprint matches", opts);
				else
					Misc.LogLine($"Invalid: Root fingerprint does not match");

				if (!validAlias)
				{
					Misc.LogError(opts, $"Could not verify alias {envelope.From}");
					return 1;
				}

				// get the public key from the alias
				var fromPublicKey = fromX509.GetPublicKey();

				//
				// check the signature of the envelope
				//

				byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);
				try
				{
					BouncyCastleHelper.VerifySignature(envelopeHash, envelopeSignature, fromPublicKey);
					Misc.LogCheckMark("Envelope signature is valid", opts);
				}
				catch (Exception ex)
				{
					Misc.LogError(opts, "Envelope signature is *NOT* valid");
					if (opts.Verbose > 0)
						Misc.LogLine(opts, ex.Message);
					return 1;
				}

				//
				// check the signature of the manifest
				//

				byte[] manifestHash = BouncyCastleHelper.GetHashOfBytes(manifestBytes);
				try
				{
					BouncyCastleHelper.VerifySignature(manifestHash, manifestSignature, fromPublicKey);
					Misc.LogCheckMark("Manifest signature is valid", opts);
				}
				catch (Exception ex)
				{
					Misc.LogError(opts, "Manifest signature is *NOT* valid", ex.Message);
					return 1;
				}

				// now iterate through the manifest and see if our alias matches one of the to addresses
				bool found_alias = false;
				foreach (var recipient in envelope.To)
				{
					if (recipient.Alias == alias)
					{
						found_alias = true;
						//Misc.LogLine(opts, $"\nUsing alias: {recipient.Alias}");

						// now verify the alias
						Misc.LogLine(opts, $"- Verifying recipient alias: {alias}");
						(bool validToAlias, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, alias, "", opts);

						// verify alias
						if (toFingerprint.SequenceEqual(rootFingerprintFromFile))
							Misc.LogCheckMark($"Root fingerprint matches", opts);
						else
							Misc.LogLine($"Invalid: Root fingerprint does not match");

						if (!validToAlias)
						{
							Misc.LogError(opts, $"Could not verify alias {alias}");
							return 1;
						}

						//
						// now valiate that they share the same root certificate
						//
						if (!fromFingerprint.SequenceEqual(toFingerprint))
						{
							Misc.LogError(opts, $"Aliases do not share the same root certificate {envelope.From} -> {alias}");
							return 1;
						}
						else
						{
							Misc.LogCheckMark($"Aliases share the same root certificate: {envelope.From} -> {alias}", opts);
						}

						// get the public key from the alias
						var toX509 = await Misc.GetCertificate(opts, alias);
						var toPublicKey = toX509.GetPublicKey();

						// we found our alias, so decrypt the private key
						string encryptedKeyBase64 = recipient.Key;
						byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

						string encryptedKyberKeyBase64 = recipient.KyberKey;
						byte[] encryptedKyberKey = Convert.FromBase64String(encryptedKyberKeyBase64);

						string privateKeyPem = Storage.GetPrivateKey($"{alias}.rsa", opts.Password);

						AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);

						// compare the two public keys and make sure that the private key is for the public key
						if (!keyPair.Public.Equals(toPublicKey))
						{
							Misc.LogError(opts, $"Error: public key does not match for alias {alias}");
							return 1;
						}
						else
						{
							Misc.LogCheckMark($"Private key matches public certificate for alias: {alias}", opts);
						}

						//
						// post quantum cryptography
						//
						// get the kyber private key
						KyberKeyParameters kyberPrivateKey;
						try
						{
							string privateKeyKyber = Storage.GetPrivateKey($"{alias}.kyber", opts.Password);
							byte[] privateKeyKyberBytes = Convert.FromBase64String(privateKeyKyber);
							kyberPrivateKey = BouncyCastleQuantumHelper.WriteKyberPrivateKey(privateKeyKyberBytes);
						}
						catch (Exception ex)
						{
							Misc.LogError(opts, $"Error: could not read kyber private key for {alias}", ex.Message);

							// application exit
							return 1;
						}

						// now decrypt the key using kyber
						//Misc.LogLine(opts, "- Decrypting kyber key...");
						var myKemExtractor = new KyberKemExtractor(kyberPrivateKey);
						var kyberSecret = myKemExtractor.ExtractSecret(encryptedKyberKey);

						// now decrypt the key
						byte[] decryptedEncryptedKey = BouncyCastleHelper.DecryptWithKey(encryptedKey, kyberSecret, envelope.From.ToLower().ToBytes());


						Misc.LogLine(opts, "- Decrypting key...");
						byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(decryptedEncryptedKey, keyPair.Private);

						//
						// now we should have the key used to encrypt all of the files
						//

						// now decrypt the manifest
						Misc.LogLine(opts, "- Decrypting manifest...");

						byte[] nonce = envelope.From.ToLower().ToBytes();
						byte[] manifestJsonBytes = BouncyCastleHelper.DecryptWithKey(manifestBytes, key, nonce);

						string manifestJson = manifestJsonBytes.FromBytes();
						var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson) ?? throw new Exception("Could not deserialize manifest");

						//
						// now unpack all of the files
						//

						//Misc.LogLine(opts, "");

						// now decrypt each file
						foreach (FileItem file in manifest.Files)
						{
							Misc.LogLine($"\n  Unpacking {file.Name}");

							// create a file stream to write the file to
							using (FileStream fs = File.Create(file.Name))
							{
								Misc.LogChar("  ");
								// decrypt the file and populate the file stream
								foreach (var block in file.Blocks)
								{
									byte[] encryptedBlock = Misc.GetBytesFromZip(block, zipFile);

									byte[] decryptedBlock = BouncyCastleHelper.DecryptWithKey(encryptedBlock, key, nonce);

									fs.Write(decryptedBlock, 0, decryptedBlock.Length);
									Misc.LogChar("#");
								}
							}

							// convert the unix timestamp to a datetime
							DateTime createDate = DateTimeOffset.FromUnixTimeSeconds(file.Ctime).DateTime;
							DateTime modifiedDate = DateTimeOffset.FromUnixTimeSeconds(file.Mtime).DateTime;

							// now set the date attributes
							File.SetCreationTime(file.Name, createDate);
							File.SetLastWriteTime(file.Name, modifiedDate);
						}
						Misc.LogLine($"\n\nYour files are located in {opts.Output}");
						Misc.LogLine("\nDone\n");
					}
				}

				if (!found_alias)
				{
					Misc.LogLine(opts, $"Error: could not find alias {alias} in deadpack");
					return 1;
				}
			}
			catch (Exception ex)
			{
				Misc.LogError(opts, "Unable to unpack package", ex.Message);
				return 1;
			}

			return 0;
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Error unpacking package", ex.Message);
			return 1;
		}
	}
}