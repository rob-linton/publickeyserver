#pragma warning disable 1998
using System.Formats.Asn1;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.Pqc.Crypto.Utilities;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace deadrop.Verbs;

[Verb("pack", HelpText = "Create a package.")]
public class PackOptions : Options
{
	[Option('i', "input", Required = true, HelpText = "Input file to be processed, (use wildcards for multiple)")]
    public required string File { get; set; }

	[Option('s', "subdirectories", HelpText = "Traverse sub directories")]
	public bool subdirectories { get; set; } = false;

	[Option('a', "aliases", Required = true, HelpText = "Destination aliases (comma delimited)")]
    public required IEnumerable<string>? InputAliases { get; set; }

	[Option('o', "output", Default = "package", HelpText = "Output package file")]
    public string Output { get; set; } = "package.deadpack";	

	[Option('f', "from", Required = true, HelpText = "From alias")]
    public required string From { get; set; }
	
}
class Pack 
{
	public static async Task<int> Execute(PackOptions opts)
	{
		try
		{
			opts.Output = opts.Output.Replace(".deadpack","") + ".deadpack";

			long createDate = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
			byte[] nonce = opts.From.ToLower().ToBytes();

			// get a 256 bit random key from bouncy castle
			byte[] key = BouncyCastleHelper.Generate256BitRandom();

			// create the manifest
			List<FileItem> fileList = new List<FileItem>();

			// create the envelope
			List<Recipient> recipients = new List<Recipient>();

			// get the current directory
			string currentDirectory = Directory.GetCurrentDirectory();

			SearchOption s = SearchOption.TopDirectoryOnly;
			if (opts.subdirectories)
				s = SearchOption.TopDirectoryOnly;

			// get a list of files from the wildcard returning relative paths only
			string[] fullPaths = Directory.GetFiles(currentDirectory, opts.File, s);

			// Convert full paths to relative paths
        	string[] relativePaths = fullPaths.Select(fullPath =>
            fullPath.Substring(currentDirectory.Length).TrimStart(Path.DirectorySeparatorChar)).ToArray();

			Misc.LogHeader();

			Misc.LogLine($"Deadpacking...");
			Misc.LogLine($"Input: {opts.File}");
			Misc.LogLine($"Search Subdirectories: {opts.subdirectories}");
			Misc.LogLine($"Sender Alias: {opts.From}");

			if (opts.InputAliases != null)
			{
				foreach (var alias in opts.InputAliases)
				{
					Misc.LogLine($"Recipient Alias: {alias}");
				}
			}

			Misc.LogLine($"Output: {opts.Output}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(opts.Password))
			opts.Password = Misc.GetPassword();

			Misc.LogLine("Files to be deadpacked:");
			foreach (string filePath in relativePaths)
			{
				Misc.LogLine("  " + filePath);
			}

			// continue?
			Misc.LogLine("\nContinue? (Y/n)");

#if DEBUG
			string? answer = "y";
#else
			string? answer = Console.ReadLine();
#endif

			if (answer == null || answer.ToLower() != "n")
			{
				Misc.LogLine($"\nCreating package {opts.Output}...\n");
			}
			else
			{
				Misc.LogLine("\nAborting...");
				return 1;
			}

			// now load the root fingerprint from a file
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{opts.From}.root", opts.Password);
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// 
			// validate the sender
			//

			Misc.LogLine(opts, $"\n- Validating sender alias  ->  {opts.From}");
			
			string fromDomain = Misc.GetDomain(opts, opts.From);
			(bool valid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, opts.From, "", opts);

			// validate the rootfingerprint
			if (rootFingerprintFromFile.SequenceEqual(fromFingerprint))
				Misc.LogCheckMark($"Root fingerprint matches");
			else
				Misc.LogLine($"Invalid: Root fingerprint does not match");

			if (valid)
				Misc.LogCheckMark($"Alias {opts.From} is valid");
			else
			{
				Misc.LogError(opts, $"Alias {opts.From} is *NOT* valid");
				return 1;
			}


			//
			// create the zip file
			//

			//Misc.LogLine(opts, "  Packing files...");
			
			// create an empty zip stream 
			using (FileStream zipFileStream = new FileStream(opts.Output, FileMode.Create))
			using (ZipArchive zip = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
			{
				foreach (string filePath in relativePaths)
				{
					Misc.LogLine($"\n  deadpacking {filePath}");

					List<string> blockList = new List<string>();

					// Ensure the file exists
					if (File.Exists(filePath))
					{
						// encrypt the file in chunks
						
						List<string> blockFileList = BouncyCastleHelper.EncryptFileInBlocks(filePath, key, nonce);

						Misc.LogChar("  ");
						// Add each chunk to the zip file
						foreach (string chunk in blockFileList)
						{
							// get the hash of the chunk
							byte[] hash = BouncyCastleHelper.GetHashOfFile(chunk);
							string sHash = BouncyCastleHelper.ConvertHashToString(hash);

							zip.CreateEntryFromFile(chunk, sHash);

							// add the entry to the list
							blockList.Add(sHash);

							// delete the chunk
							File.Delete(chunk);

							Misc.LogChar("#");
						}

						FileInfo fileInfo = new FileInfo(filePath);
						
						// add the list of chunks to the manifest
						fileList.Add(new FileItem 
						{ 
							Name = filePath, 
							Size = fileInfo.Length,
							Type = fileInfo.Extension,
							Mtime = ((DateTimeOffset)fileInfo.LastWriteTimeUtc).ToUnixTimeSeconds(),
							Ctime = ((DateTimeOffset)fileInfo.CreationTimeUtc).ToUnixTimeSeconds(),
							Blocks = blockList
						});

					}
					else
					{
						Misc.LogError(opts, $"File not found: {filePath}");
					}
				}

				Misc.LogLine("\n");

				//
				// get the private key
				//

				// get the from private key
				AsymmetricCipherKeyPair privateKey;
				try
				{
					string privateKeyPem = Storage.GetPrivateKey($"{opts.From}.rsa", opts.Password);
					privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
				}
				catch (Exception ex)
				{
					Misc.LogError(opts, $"Error: could not read private key for {opts.From}", ex.Message);

					// application exit
					return 1;
				}

				//
				// make sure the public key from the cert matches the private key
				//
				var fromX509 = await Misc.GetCertificate(opts, opts.From);
				if (fromX509 != null)
				{
					var fromPublicKey = fromX509.GetPublicKey();

					// compare the two public keys and make sure that the private key is for the public key
					if (!privateKey.Public.Equals(fromPublicKey))
					{
						Misc.LogError(opts, $"Public key does not match private key for alias {opts.From}");
						return 1;
					}
					else
					{
						Misc.LogCheckMark($"Private key matches public certificate for alias {opts.From}");
					}
				}
				else
				{
					Misc.LogError(opts, $"Could not find certificate for {opts.From}");
					return 1;
				}

				//
				// create the manifest
				//

				// add the files to the manifest
				Manifest manifest = new Manifest 
				{
					Files = fileList,
					Name = opts.Output,
				};

				string manifestJson = JsonSerializer.Serialize(manifest);

				Misc.LogLine(opts, "- Encrypting the manifest...");

				// encrypt the manifest
				byte[] encryptedManifest = BouncyCastleHelper.EncryptWithKey(manifestJson.ToBytes(), key, nonce);

				//
				// create the envelope
				//

				// now loop through each of the aliases and add them to the envelope
				Misc.LogLine(opts, "- Addressing envelope...");
				if (opts.InputAliases != null)
				{
					foreach (string alias in opts.InputAliases)
					{
						try
						{
							// validate the alias
							Misc.LogLine(opts, $"- Validating recipient alias  ->  {alias}");
							string domain = Misc.GetDomain(opts, alias);
							(bool aliasValid, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, "", opts);

							// validate the fingerprint
							if (toFingerprint.SequenceEqual(rootFingerprintFromFile))
								Misc.LogCheckMark($"Root fingerprint matches");
							else
								Misc.LogLine($"Invalid: Root fingerprint does not match");

							if (valid)
							{
								//
								// the root certificates must match between the sender and the recipient
								//

								if (!fromFingerprint.SequenceEqual(toFingerprint))
								{
									Misc.LogLine(opts, $"Aliases do not share the same root certificate {opts.From} -> {alias}");
									return 1;
								}

								Misc.LogCheckMark($"Recipient Alias {alias} is valid");
								Misc.LogCheckMark($"Shared root certificate {opts.From} -> {alias}");
							}
							else
							{
								Misc.LogError(opts, $"Recipient Alias {alias} is *NOT* valid");
								return 1;
							}

							var toX509 = await Misc.GetCertificate(opts, alias);
							if (toX509 != null)
							{
								var publicKey = toX509.GetPublicKey();

								//
								// post quantum
								//
								string oid = "1.3.6.1.4.1.57055";
								string data = BouncyCastleHelper.GetCustomExtensionData(toX509, oid);
								byte[] dataBytes = Convert.FromBase64String(data);
								string sData = Encoding.UTF8.GetString(dataBytes);
								var jData = JsonSerializer.Deserialize<CustomExtensionData>(sData);
								byte[] kyberPublicKeyBytes = Convert.FromBase64String(jData?.KyberKey ?? throw new Exception("Could not get Kyber public key from certificate"));
								AsymmetricKeyParameter kyberPublicKey = BouncyCastleQuantumHelper.WriteKyberPublicKey(kyberPublicKeyBytes);
								
								var random = new SecureRandom(); // <-- this is the random number generator
								var myKyberKemGenerator = new KyberKemGenerator(random); // <-- this is the key encapsulation mechanism
								var encapsulatedSecret = myKyberKemGenerator.GenerateEncapsulated(kyberPublicKey); // <-- this is the encapsulated secret
								var cipherText = encapsulatedSecret.GetEncapsulation(); // <-- this is the cipher text
								string sCipherText = Convert.ToBase64String(cipherText); //	<-- this is the cipher text as a string

								// this  is the encapsulated secret that we will send to the recipient
								var quantumSecret = encapsulatedSecret.GetSecret();	// <-- this is the secret
								
								
								Misc.LogCheckMark($"Encrypted alias {alias} key with RSA");

								// encrypt the key with the public key
								byte[] encryptedKey = BouncyCastleHelper.EncryptWithPublicKey(key, publicKey); // <-- this is the encrypted key
								//string sEncryptedKey = Convert.ToBase64String(encryptedKey);

								// now encrypt the sEncryptedKey with quantumSecret
								byte[] encryptedEncryptedKey = BouncyCastleHelper.EncryptWithKey(encryptedKey, quantumSecret, nonce); // <-- this is the encrypted key double encrypted with kyber
								string sEncryptedEncryptedKey = Convert.ToBase64String(encryptedEncryptedKey); // <-- this is the encrypted key double encrypted with kyber as a string

								Misc.LogCheckMark($"Re-encrypted alias {alias} key with Post Quantum Kyber");

								// add the encrypted key to the envelope
								recipients.Add(new Recipient { Alias = alias, Key = sEncryptedEncryptedKey, KyberKey = sCipherText });
								Misc.LogLine(opts, $"- Added alias {alias}");
							}
						}
						catch (Exception ex)
						{
							Misc.LogError(opts, $"Error: could not find alias {alias}", ex.Message);
						}
					}
				}

			
				Envelope envelope = new Envelope { 
					To = recipients, 
					From = opts.From,
					Created = createDate,
					Version = "1.0"
				};

				//envelope["asymmetric"] = "RSA2048";  
				//envelope["symmetric"] = "AES_GCM_256";
				//envelope["hash"] = "SHA256";

				string envelopeJson = JsonSerializer.Serialize(envelope);
				

				Misc.LogLine(opts, "- Signing the envelope...");
				
				// sign the manifest
				byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);
				byte[] envelopeSignature = BouncyCastleHelper.SignData(envelopeHash, privateKey.Private);

				Misc.LogLine(opts, "- Signing the manifest...");
				
				// sign the manifest
				byte[] manifestHash = BouncyCastleHelper.GetHashOfBytes(encryptedManifest);
				byte[] manifestSignature = BouncyCastleHelper.SignData(manifestHash, privateKey.Private);

				// write them all to a file
				File.WriteAllBytes("manifest", encryptedManifest);
				File.WriteAllBytes("manifest.signature", manifestSignature);
				File.WriteAllText("envelope", envelopeJson);
				File.WriteAllBytes("envelope.signature", envelopeSignature);

				// add them to the zip file
				zip.CreateEntryFromFile("manifest", "manifest");
				zip.CreateEntryFromFile("envelope", "envelope");
				zip.CreateEntryFromFile("manifest.signature", "manifest.signature");
				zip.CreateEntryFromFile("envelope.signature", "envelope.signature");


				// now delete them
				File.Delete("manifest");
				File.Delete("envelope");
				File.Delete("manifest.signature");
				File.Delete("envelope.signature");

				// add a comment to the zip file
				zip.Comment = "This zip file was created by deadrop.org";
			}

			Misc.LogLine("\nDone. Only the following anonymous recipient aliases will be able to unpack this deadpack.\n");
			if (opts.InputAliases != null)
			{
				foreach (var alias in opts.InputAliases)
				{
					Misc.LogLine($"Recipient Alias: {alias}");
				}
			}
			Misc.LogLine("");
		}
		catch (Exception ex)
		{
			Misc.LogError(opts, "Unable to pack files", ex.Message);
			return 1;
		}

		return 0;
	}
}