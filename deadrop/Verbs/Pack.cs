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
    public IEnumerable<string>? InputAliases { get; set; }

	[Option('o', "output", Default = "deadrop.zip", HelpText = "Output package file")]
    public string Output { get; set; } = "deadrop.zip";	

	[Option('f', "from", Required = true, HelpText = "From alias")]
    public required string From { get; set; }
}
class Pack 
{
	public static async Task<int> Execute(PackOptions opts)
	{
		try
		{
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

			Console.WriteLine("\nPacking the following files for dead drop:");
			Console.WriteLine("==========================================");
			foreach (string filePath in relativePaths)
			{
				Console.WriteLine(filePath);
			}

			// continue?
			Console.WriteLine("\nContinue? (Y/n)");

#if DEBUG
			string? answer = "y";
#else
		string? answer = Console.ReadLine();
#endif

			if (answer == null || answer.ToLower() != "n")
			{
				Console.WriteLine("\nCreating package...");
			}
			else
			{
				Console.WriteLine("\nAborting...");
				return 1;
			}

			// 
			// validate the sender
			//

			Console.WriteLine("\nValidating the sender...");
			string fromDomain = Misc.GetDomain(opts, opts.From);

			// first get the CA
			if (opts.Verbose > 0)
				Console.WriteLine($"GET: https://{fromDomain}/cacerts");

			var result = await HttpHelper.Get($"https://{fromDomain}/cacerts");
			var ca = JsonSerializer.Deserialize<CaCertsResult>(result);
			var cacerts = ca?.CaCerts;

			// now get the alias	
			if (opts.Verbose > 0)
				Console.WriteLine($"GET: https://{fromDomain}/cert/{Misc.GetAliasFromAlias(opts.From)}");

			result = await HttpHelper.Get($"https://{fromDomain}/cert/{Misc.GetAliasFromAlias(opts.From)}");

			var fromC = JsonSerializer.Deserialize<CertResult>(result);
			var fromCertificate = fromC?.Certificate;

			// now validate the certificate chain
			bool valid = false;
			if (fromCertificate != null && cacerts != null) // Add null check for cacerts
			{
				valid = BouncyCastleHelper.ValidateCertificateChain(fromCertificate, cacerts, fromDomain);
			}

			//
			// create the zip file
			//

			Console.WriteLine("\nPacking files...\n");

			// create an empty zip stream 
			using (FileStream zipFileStream = new FileStream(opts.Output, FileMode.Create))
			using (ZipArchive zip = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
			{
				foreach (string filePath in relativePaths)
				{
					Console.WriteLine($"{filePath}");

					List<string> blockList = new List<string>();

					// Ensure the file exists
					if (File.Exists(filePath))
					{
						// encrypt the file in chunks
						
						List<string> blockFileList = BouncyCastleHelper.EncryptFileInBlocks(filePath, key, nonce);

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

							Console.Write("#");
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
						Console.WriteLine($"\nFile not found: {filePath}");
					}
				}

				Console.WriteLine("");
				
				//
				// create the envelope
				//

				// now loop through each of the aliases and add them to the envelope
				Console.WriteLine("\nAddressing envelope...\n");
				if (opts.InputAliases != null)
				{
					foreach (string alias in opts.InputAliases)
					{
						try
						{
							string domain = Misc.GetDomain(opts, alias);

							// get their public key
							if (opts.Verbose > 0)
								Console.WriteLine($"GET: https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

							result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");
							var c = JsonSerializer.Deserialize<CertResult>(result);
							var certificate = c?.Certificate;

							if (certificate != null)
							{
								var x509 = BouncyCastleHelper.ReadCertificateFromPemString(certificate);
								var publicKey = x509.GetPublicKey();

								// encrypt the key with the public key
								byte[] encryptedKey = BouncyCastleHelper.EncryptWithPublicKey(key, publicKey);
								string sEncryptedKey = Convert.ToBase64String(encryptedKey);

								// add the encrypted key to the envelope
								recipients.Add(new Recipient { Alias = alias, Key = sEncryptedKey });

								Console.WriteLine($"{alias}");
							}
						}
						catch
						{
							Console.WriteLine($"Error: could not find alias {alias}");
						}
					}
				}

				Envelope envelope = new Envelope { 
					To = recipients, 
					From = opts.From,
					Created = createDate,
					Version = "1.0",
				};

				//envelope["asymmetric"] = "RSA2048";  
				//envelope["symmetric"] = "AES_GCM_256";
				//envelope["hash"] = "SHA256";

				string envelopeJson = JsonSerializer.Serialize(envelope);

				// get the from private key
				AsymmetricCipherKeyPair privateKey;
				try
				{
					string privateKeyPem = Storage.GetPrivateKey(opts.From, opts.Password);
					privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: could not read private key for {opts.From}");
					Console.WriteLine(ex.Message);

					// application exit
					return 1;
				}

				Console.WriteLine("\nSigning the envelope...");
				
				// sign the envelope
				byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);
				byte[] envelopeSignature = BouncyCastleHelper.SignData(envelopeHash, privateKey.Private);

				//
				// create the manifest
				//

				// add the signature to the manifest
				Manifest manifest = new Manifest 
				{
					Files = fileList,
					Signature = Convert.ToBase64String(envelopeSignature)
				};

				string manifestJson = JsonSerializer.Serialize(manifest);

				Console.WriteLine("Encrypting the manifest...");

				// encrypt the manifest
				byte[] encryptedManifest = BouncyCastleHelper.EncryptWithKey(manifestJson.ToBytes(), key, nonce);

				// write them both to a file
				File.WriteAllBytes("manifest", encryptedManifest);
				File.WriteAllText("envelope", envelopeJson);

				// add them to the zip file
				zip.CreateEntryFromFile("manifest", "manifest");
				zip.CreateEntryFromFile("envelope", "envelope");

				// now delete them
				File.Delete("manifest");
				File.Delete("envelope");

				// add a comment to the zip file
				zip.Comment = "This zip file was created by deadrop.org";
			}

			Console.WriteLine("\nDone\n");
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nError: Unable to pack files\n");
			if (opts.Verbose > 0)
				Console.WriteLine(ex.Message);
			return 1;
		}

		return 0;
	}
}