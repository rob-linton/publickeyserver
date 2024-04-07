#pragma warning disable 1998
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.X509;

namespace suredrop.Verbs;

[Verb("unpack", HelpText = "Unpack a package.")]
public class UnpackOptions : Options
{
   [Option('i', "input", Required = true, HelpText = "Input suredrop file to be processed")]
    public required string File { get; set; }

	[Option('o', "output", Default = "", HelpText = "Output directory")]
	public string? Output { get; set; }

	[Option('a', "alias", HelpText = "Alias to use")]
	public string Alias { get; set; } = "";
}

class Unpack 
{
	public static async Task<int> Execute(UnpackOptions opts, IProgress<StatusUpdate>? progress = null)
	{
		Misc.LogHeader();
		Misc.LogLine($"Unpacking surepack...");
		Misc.LogLine($"Input: {opts.File}");
		Misc.LogLine($"Output directory: {opts.Output}");
		
		if (!File.Exists(opts.File))
			opts.File = Storage.GetSurePackDirectoryInbox(opts.Alias, opts.File);

		//Misc.LogLine($"Recipient Alias: {alias}");

		if (!String.IsNullOrEmpty(opts.Alias))
		{
			return await ExecuteInternal(opts, opts.Alias);
		}
		else
		{
			// loop through all of the aliases
			foreach (var a in Storage.GetAliases())
			{
				string alias = a.Name;
				Misc.LogLine($"\nChecking alias {alias}...");
				int result = await ExecuteInternal(opts, alias, progress);
				if (result == 0)
					return result;
			}
			Misc.LogError("Error unpacking package, no valid alias found");
			return 1;
		}
	}

	public static async Task<int> ExecuteInternal(UnpackOptions opts, string alias, IProgress<StatusUpdate>? progress = null)
	{
		try
		{
			CertResult cert = await EmailHelper.GetAliasOrEmailFromServer(alias, false);
			alias = cert.Alias;
			string toDomain = Misc.GetDomain(alias);

			// get the input zip file
			opts.File = opts.File.Replace(".surepack", "") + ".surepack";
			string zipFile = opts.File;

			if (String.IsNullOrEmpty(Globals.Password))
			Globals.Password = Misc.GetPassword();

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{alias}.root");
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// get the output directory
			if (String.IsNullOrEmpty(opts.Output))
				opts.Output = Path.GetFileNameWithoutExtension(zipFile);

			// create it
			Directory.CreateDirectory(opts.Output);

			string outputDirectory = opts.Output;
			
			if (String.IsNullOrEmpty(Globals.Password))
				Globals.Password = Misc.GetPassword();

			try
			{
				Misc.LogLine("");

				// now read the envelope
				string envelopeJson = Misc.GetTextFromZip("envelope", zipFile);
				var envelope = JsonSerializer.Deserialize<Envelope>(envelopeJson) ?? throw new Exception("Could not deserialize envelope");

				// read the envelope signature
				byte[] envelopeSignature = Misc.GetBytesFromZip("envelope.signature", zipFile);

				// read the manifest signature
				byte[] manifestSignature = Misc.GetBytesFromZip("manifest.signature", zipFile);

				// now read the manifest
				byte[] manifestBytes = Misc.GetBytesFromZip("manifest", zipFile);

				string fromDomain = Misc.GetDomain(envelope.From);

				// now get the "from" alias
				var fromX509 = await Misc.GetCertificate(envelope.From);

				// now verify the alias
				Misc.LogLine($"\n- Verifying sender alias: {envelope.From}");
				(bool validAlias, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, envelope.From, "");

				// verify fingerprint
				if (fromFingerprint.SequenceEqual(rootFingerprintFromFile))
					Misc.LogCheckMark($"Root fingerprint matches");
				else
					Misc.LogLine($"Invalid: Root fingerprint does not match");

				if (!validAlias)
				{
					Misc.LogError($"Could not verify alias {envelope.From}");
					throw new Exception($"Could not verify alias {envelope.From}");
					//return 1;
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
					Misc.LogCheckMark("Envelope signature is valid");
				}
				catch (Exception ex)
				{
					Misc.LogError("Envelope signature is *NOT* valid");
					Misc.LogLine(ex.Message);
					throw new Exception("Envelope signature is *NOT* valid", ex);
					//return 1;
				}

				//
				// check the signature of the manifest
				//

				byte[] manifestHash = BouncyCastleHelper.GetHashOfBytes(manifestBytes);
				try
				{
					BouncyCastleHelper.VerifySignature(manifestHash, manifestSignature, fromPublicKey);
					Misc.LogCheckMark("Manifest signature is valid");
				}
				catch (Exception ex)
				{
					Misc.LogError("Manifest signature is *NOT* valid", ex.Message);
					throw new Exception("Manifest signature is *NOT* valid", ex);
					//return 1;
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
						Misc.LogLine($"- Verifying recipient alias: {alias}");
						(bool validToAlias, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, alias, "");

						// verify alias
						if (toFingerprint.SequenceEqual(rootFingerprintFromFile))
							Misc.LogCheckMark($"Root fingerprint matches");
						else
							Misc.LogLine($"Invalid: Root fingerprint does not match");

						if (!validToAlias)
						{
							Misc.LogError($"Could not verify alias {alias}");
							throw new Exception($"Could not verify alias {alias}");
							//return 1;
						}

						//
						// now valiate that they share the same root certificate
						//
						if (!fromFingerprint.SequenceEqual(toFingerprint))
						{
							Misc.LogError($"Aliases do not share the same root certificate {envelope.From} -> {alias}");
							throw new Exception($"Aliases do not share the same root certificate {envelope.From} -> {alias}");
							//return 1;
						}
						else
						{
							Misc.LogCheckMark($"Aliases share the same root certificate: {envelope.From} -> {alias}");
						}

						// get the public key from the alias
						var toX509 = await Misc.GetCertificate(alias);
						var toPublicKey = toX509.GetPublicKey();

						// we found our alias, so decrypt the private key
						string encryptedKeyBase64 = recipient.Key;
						byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

						string encryptedKyberKeyBase64 = recipient.KyberKey;
						byte[] encryptedKyberKey = Convert.FromBase64String(encryptedKyberKeyBase64);

						string privateKeyPem = Storage.GetPrivateKey($"{alias}.rsa");

						AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);

						// compare the two public keys and make sure that the private key is for the public key
						if (!keyPair.Public.Equals(toPublicKey))
						{
							Misc.LogError($"Error: public key does not match for alias {alias}");
							throw new Exception($"Error: public key does not match for alias {alias}");
							//return 1;
						}
						else
						{
							Misc.LogCheckMark($"Private key matches public certificate for alias: {alias}");
						}

						//
						// post quantum cryptography
						//
						// get the kyber private key
						KyberKeyParameters kyberPrivateKey;
						try
						{
							string privateKeyKyber = Storage.GetPrivateKey($"{alias}.kyber");
							byte[] privateKeyKyberBytes = Convert.FromBase64String(privateKeyKyber);
							kyberPrivateKey = BouncyCastleQuantumHelper.WriteKyberPrivateKey(privateKeyKyberBytes);
						}
						catch (Exception ex)
						{
							Misc.LogError($"Error: could not read kyber private key for {alias}", ex.Message);
							throw new Exception($"Error: could not read kyber private key for {alias}", ex);
							// application exit
							//return 1;
						}

						// now decrypt the key using kyber
						//Misc.LogLine(opts, "- Decrypting kyber key...");
						var myKemExtractor = new KyberKemExtractor(kyberPrivateKey);
						var kyberSecret = myKemExtractor.ExtractSecret(encryptedKyberKey);

						// now decrypt the key
						byte[] decryptedEncryptedKey = BouncyCastleHelper.DecryptWithKey(encryptedKey, kyberSecret, envelope.From.ToLower().ToBytes());


						Misc.LogLine("- Decrypting key...");
						byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(decryptedEncryptedKey, keyPair.Private);

						//
						// now we should have the key used to encrypt all of the files
						//

						// now decrypt the manifest
						Misc.LogLine("- Decrypting manifest...");

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
							using (FileStream fs = File.Create(Path.Join(outputDirectory,file.Name)))
							{
								Misc.LogChar("  ");
								// decrypt the file and populate the file stream
								foreach (var block in file.Blocks)
								{
									byte[] encryptedBlock = Misc.GetBytesFromZip(block, zipFile);

									byte[] decryptedBlock = BouncyCastleHelper.DecryptWithKey(encryptedBlock, key, nonce);

									// decompress the block
									byte[] decompressedBlock = decryptedBlock = Misc.DecompressBytes(decryptedBlock);
									
									fs.Write(decompressedBlock, 0, decompressedBlock.Length);
									Misc.LogChar("#");
								}
							}

							// convert the unix timestamp to a datetime
							DateTime createDate = DateTimeOffset.FromUnixTimeSeconds(file.Ctime).DateTime;
							DateTime modifiedDate = DateTimeOffset.FromUnixTimeSeconds(file.Mtime).DateTime;

							// now set the date attributes
							File.SetCreationTime(file.Name, createDate);
							File.SetLastWriteTime(file.Name, modifiedDate);

							// update the progress bar if it is not null
							//Globals.UpdateProgressBar((float)manifest.Files.IndexOf(file) + 1, (float)manifest.Files.Count);
							StatusUpdate statusUpdate = new StatusUpdate
							{
								Index = (float)manifest.Files.IndexOf(file) + 1,
								Count = (float)manifest.Files.Count
							};

							progress?.Report(statusUpdate);
							await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR PROGRESS BAR
						}
						Misc.WriteLine($"\n\nYour files are located in {opts.Output}");
						Misc.WriteLine("\nDone\n");
					}
				}

				if (!found_alias)
				{
					Misc.LogLine($"Error: could not find alias {alias} in surepack");
					throw new Exception($"Error: could not find alias {alias} in surepack");
				}
			}
			catch (Exception ex)
			{
				Misc.LogError("Unable to unpack package", ex.Message);
				throw new Exception("Unable to unpack package", ex);
			}

			progress?.Report(new StatusUpdate { Status = "Unpacked OK" });
			await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

			return 0;
		}
		catch (Exception ex)
		{
			progress?.Report(new StatusUpdate { Status = ex.Message });
			await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

			Misc.LogError("Error unpacking package", ex.Message);
			return 1;
		}
	}
}