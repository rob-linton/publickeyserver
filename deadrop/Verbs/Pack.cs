#pragma warning disable 1998
using System.Formats.Asn1;
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;

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
					Misc.LogLine(opts, $"Recipient Alias: {alias}");
				}
			}

			Misc.LogLine($"Output: {opts.Output}");
			Misc.LogLine($"");

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
				Misc.LogLine($"\nCreating package {opts.Output}...");
			}
			else
			{
				Misc.LogLine("\nAborting...");
				return 1;
			}

			// 
			// validate the sender
			//

			Misc.LogLine(opts, $"\n- Validating sender alias  ->  {opts.From}");
			
			string fromDomain = Misc.GetDomain(opts, opts.From);
			(bool valid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, opts.From, opts);

			if (valid)
				Misc.LogLine(opts, $"- Alias {opts.From} is valid\n");
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

				Misc.LogLine("");

				//
				// get the private key
				//

				// get the from private key
				AsymmetricCipherKeyPair privateKey;
				try
				{
					string privateKeyPem = Storage.GetPrivateKey(opts.From, opts.Password);
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
						Misc.LogLine(opts, $"\n- Private key matches public certificate for alias {opts.From}");
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
							(bool aliasValid, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, opts);

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

								Misc.LogLine(opts, $"- Recipient Alias {alias} is valid");
								Misc.LogLine(opts, $"- Aliases share the same root certificate {opts.From} -> {alias}");
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

								// encrypt the key with the public key
								byte[] encryptedKey = BouncyCastleHelper.EncryptWithPublicKey(key, publicKey);
								string sEncryptedKey = Convert.ToBase64String(encryptedKey);

								// sign the alias
								//byte[] aliasHash = BouncyCastleHelper.GetHashOfString(alias);
								//byte[] aliasSignature = BouncyCastleHelper.SignData(aliasHash, privateKey.Private);

								// add the encrypted key to the envelope
								recipients.Add(new Recipient { Alias = alias, Key = sEncryptedKey });

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

			Misc.LogLine("\nDone\n");
		}
		catch (Exception ex)
		{
			Misc.LogLine(opts, "\nError: Unable to pack files\n");
			if (opts.Verbose > 0)
				Misc.LogLine(opts, ex.Message);
			return 1;
		}

		return 0;
	}
}