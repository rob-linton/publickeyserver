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
	[Option('i', "input file name or wildcard", Required = true, HelpText = "Input file to be processed, (use wildcards for multiple)")]
    public required string File { get; set; }

	[Option('r', "Recurse subdirectories", HelpText = "Recurse sub directories")]
	public bool Recurse { get; set; } = false;

	[Option('a', "aliases", Required = true, HelpText = "Destination aliases/emails (comma delimited)")]
    public required IEnumerable<string>? InputAliases { get; set; }

	[Option('o', "output", HelpText = "Output package file")]
    public string? Output { get; set; }	

	[Option('m', "message", HelpText = "Optional message")]
    public string? Message { get; set; }

	[Option('s', "subject", HelpText = "Deadpack subject")]
    public string? Subject { get; set; }

	[Option('f', "from", Required = true, HelpText = "From alias")]
    public required string From { get; set; }
	
}
class Pack 
{
	public static async Task<int> Execute(PackOptions opts, IProgress<StatusUpdate> progress = null)
	{
		try
		{
			if (!String.IsNullOrEmpty(opts.Output) && !opts.Output.EndsWith(".deadpack"))
				opts.Output = opts.Output + ".deadpack";

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
			if (opts.Recurse)
				s = SearchOption.AllDirectories;

			// get a list of files from the wildcard returning relative paths only
			
			string[] relativePaths = Misc.GetFiles(opts.File, opts.Recurse);

			Misc.LogHeader();

			Misc.LogLine($"Deadpacking...");
			Misc.LogLine($"Input: {opts.File}");
			string r = opts.Recurse ? "Yes" : "No";
			Misc.LogLine($"Search Subdirectories: {r}");
			Misc.LogLine($"Sender Alias: {opts.From}");
				
			// first replace any email alias with their alias versions
			List<string> aliases = [opts.From];
			
			List<Task<CertResult>> tasks = new List<Task<CertResult>>();
			foreach (string aliasOrEmail in opts.InputAliases)
			{
				//CertResult cert = await EmailHelper.GetAliasFromEmail(aliasOrEmail, opts);
				tasks.Add(EmailHelper.GetAliasOrEmailFromServer(aliasOrEmail, true));	
			}
			
			// when all of the tasks have completed
			await Task.WhenAll(tasks);
			foreach (var task in tasks)
			{
				CertResult cert = task.Result;
				aliases.Add(cert?.Alias ?? string.Empty);
			}
			opts.InputAliases = aliases;

			Misc.LogLine($"Output: {opts.Output}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(Globals.Password))
			Globals.Password = Misc.GetPassword();

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
			string rootFingerprintFromFileString = Storage.GetPrivateKey($"{opts.From}.root");
			byte[] rootFingerprintFromFile = Convert.FromBase64String(rootFingerprintFromFileString);

			// 
			// validate the sender
			//

			Misc.LogLine($"\n- Validating sender alias  ->  {opts.From}");
			
			string fromDomain = Misc.GetDomain(opts.From);
			(bool valid, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, opts.From, "");

			// validate the rootfingerprint
			if (rootFingerprintFromFile.SequenceEqual(fromFingerprint))
				Misc.LogCheckMark($"Root fingerprint matches");
			else
				Misc.LogLine($"Invalid: Root fingerprint does not match");

			if (valid)
				Misc.LogCheckMark($"Alias {opts.From} is valid");
			else
			{
				Misc.LogError($"Alias {opts.From} is *NOT* valid");
				throw new Exception($"Alias {opts.From} is *NOT* valid");
				//return 1;
			}


			//
			// create the zip file
			//

			// set the output directory depending if it has been passed in in opts.output
			string outputFile = opts.Output;
			if (String.IsNullOrEmpty(opts.Output))
			{
				// get a unix timestamp in seconds
				string date = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds().ToString();
				//opts.Output = date + " " + opts.Output;
				outputFile = Storage.GetDeadPackDirectoryOutbox($"{date}-{Guid.NewGuid()}.deadpack");
			}
			

			// create an empty zip stream 
			using (FileStream zipFileStream = new FileStream(outputFile, FileMode.Create))
			using (ZipArchive zip = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
			{
				int index = 1;
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

						// update the progress bar if it is not null
						StatusUpdate statusUpdate = new StatusUpdate
						{
							Index = (float)index,
							Count = (float)relativePaths.Count()
						};

						progress?.Report(statusUpdate);
						await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

						index++;
					}
					else
					{
						Misc.LogError($"File not found: {filePath}");
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
					string privateKeyPem = Storage.GetPrivateKey($"{opts.From}.rsa");
					privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
				}
				catch (Exception ex)
				{
					Misc.LogError($"Error: could not read private key for {opts.From}", ex.Message);
					throw new Exception($"Error: could not read private key for {opts.From}");
					// application exit
					//return 1;
				}

				//
				// make sure the public key from the cert matches the private key
				//
				var fromX509 = await Misc.GetCertificate(opts.From);
				if (fromX509 != null)
				{
					var fromPublicKey = fromX509.GetPublicKey();

					// compare the two public keys and make sure that the private key is for the public key
					if (!privateKey.Public.Equals(fromPublicKey))
					{
						Misc.LogError($"Public key does not match private key for alias {opts.From}");
						throw new Exception($"Public key does not match private key for alias {opts.From}");
						//return 1;
					}
					else
					{
						Misc.LogCheckMark($"Private key matches public certificate for alias {opts.From}");
					}
				}
				else
				{
					Misc.LogError($"Could not find certificate for {opts.From}");
					throw new Exception($"Could not find certificate for {opts.From}");
					//return 1;
				}

				//
				// create the manifest
				//

				// base64 encode opts.Message
				string message = Convert.ToBase64String(Encoding.UTF8.GetBytes(opts.Message ?? ""));
				string subject = Convert.ToBase64String(Encoding.UTF8.GetBytes(opts.Subject ?? ""));

				// add the files to the manifest
				Manifest manifest = new Manifest 
				{
					Files = fileList,
					Subject = subject,
					Message = message
				};

				string manifestJson = JsonSerializer.Serialize(manifest);

				Misc.LogLine("- Encrypting the manifest...");

				// encrypt the manifest
				byte[] encryptedManifest = BouncyCastleHelper.EncryptWithKey(manifestJson.ToBytes(), key, nonce);

				//
				// create the envelope
				//

				// now loop through each of the aliases and add them to the envelope
				Misc.LogLine("- Addressing envelope...");
				if (opts.InputAliases != null)
				{
					foreach (string alias in opts.InputAliases)
					{
						
						try
						{
							

							// validate the alias
							Misc.LogLine($"- Validating recipient alias  ->  {alias}");
							string domain = Misc.GetDomain(alias);
							(bool aliasValid, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, alias, "");

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
									Misc.LogLine($"Aliases do not share the same root certificate {opts.From} -> {alias}");
									throw new Exception($"Aliases do not share the same root certificate {opts.From} -> {alias}");
									//return 1;
								}

								Misc.LogCheckMark($"Recipient Alias {alias} is valid");
								Misc.LogCheckMark($"Shared root certificate {opts.From} -> {alias}");
							}
							else
							{
								Misc.LogError($"Recipient Alias {alias} is *NOT* valid");
								throw new Exception($"Recipient Alias {alias} is *NOT* valid");
								//return 1;
							}

							var toX509 = await Misc.GetCertificate(alias);
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
								Misc.LogLine($"- Added alias {alias}");
							}
						}
						catch (Exception ex)
						{
							Misc.LogError($"Error: could not find alias {alias}", ex.Message);
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
				

				Misc.LogLine("- Signing the envelope...");
				
				// sign the manifest
				byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);
				byte[] envelopeSignature = BouncyCastleHelper.SignData(envelopeHash, privateKey.Private);

				Misc.LogLine("- Signing the manifest...");
				
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

			Misc.LogLine("\nDone. Only the following recipient aliases will be able to unpack this deadpack.\n");
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
			progress?.Report(new StatusUpdate { Status = ex.Message });
			await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

			Misc.LogError("Unable to pack files", ex.Message);
			return 1;
		}

		return 0;
	}
}