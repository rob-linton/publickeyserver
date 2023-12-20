#pragma warning disable 1998
using System.Formats.Asn1;
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;

namespace jdoe.Verbs;

[Verb("pack", HelpText = "Create a package.")]
public class PackOptions : Options
{
   [Option('i', "input", Required = true, HelpText = "Input file to be processed, (use wildcards for multiple)")]
    public string File { get; set; } = "*.*";

	[Option('a', "aliases", Required = true, HelpText = "Destination aliases (comma delimited)")]
    public IEnumerable<string>? InputAliases { get; set; }

	[Option('o', "output", Default = "dedrp.zip", HelpText = "Output package file")]
    public string Output { get; set; } = "dedrp.zip";	

	[Option('f', "from", Required = true, HelpText = "From alias")]
    public string From { get; set; } = "";	
}
class Pack 
{
	public static async Task<int> Execute(PackOptions opts)
	{
		try
		{
			// get a 256 bit random key from bouncy castle
			byte[] key = BouncyCastleHelper.Generate256BitRandom();

			// create the manifest
			Dictionary<string, dynamic> manifest = new Dictionary<string, dynamic>();
			Dictionary<string, dynamic> fileList = new Dictionary<string, dynamic>();

			// create the envelope
			List<dynamic> envelope = new List<dynamic>();

			// get the current directory
			string currentDirectory = Directory.GetCurrentDirectory();

			// get a list of file from the wildcard 
			string[] InputFiles = Directory.GetFiles(currentDirectory, opts.File);


			Console.WriteLine("\nPacking the following files for dead drop:");
			Console.WriteLine("==========================================");
			foreach (string filePath in InputFiles)
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
				Console.WriteLine("\nPacking...");
			}
			else
			{
				Console.WriteLine("\nAborting...");
				return 1;
			}


			// create an empty zip stream 
			using (FileStream zipFileStream = new FileStream(opts.Output, FileMode.Create))
			using (ZipArchive zip = new ZipArchive(zipFileStream, ZipArchiveMode.Create))
			{
				foreach (string filePath in InputFiles)
				{
					Console.Write($"\nPacking {filePath} ");

					List<string> chunkList = new List<string>();

					// Ensure the file exists
					if (File.Exists(filePath))
					{
						// encrypt the file in chunks
						List<string> chunkFileList = BouncyCastleHelper.EncryptFileInChunks(filePath, key, opts.Domain.ToBytes());

						// Add each chunk to the zip file
						foreach (string chunk in chunkFileList)
						{
							// get the hash of the chunk
							byte[] hash = BouncyCastleHelper.GetHashOfFile(chunk);
							string sHash = BouncyCastleHelper.ConvertHashToString(hash);

							zip.CreateEntryFromFile(chunk, sHash);

							// add the entry to the list
							chunkList.Add(sHash);

							// delete the chunk
							File.Delete(chunk);

							Console.Write("#");
						}

						// add the list of chunks to the manifest
						fileList[filePath] = chunkList;


					}
					else
					{
						Console.WriteLine($"File not found: {filePath}");
					}


				}

				Console.WriteLine("");
				Console.WriteLine("\nCreating manifest...");

				// add the filelist to the manifest
				manifest["files"] = fileList;

				// now loop through each of the aliases and add them to the envelope
				Console.WriteLine("\nAddressing envelope:");
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
								
							var result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");
							var c = JsonSerializer.Deserialize<CertResult>(result);
							var certificate = c?.certificate;

							if (certificate != null)
							{
								var x509 = BouncyCastleHelper.ReadCertificateFromPemString(certificate);
								var publicKey = x509.GetPublicKey();

								// encrypt the key with the public key
								byte[] encryptedKey = BouncyCastleHelper.EncryptWithPublicKey(key, publicKey);
								string sEncryptedKey = Convert.ToBase64String(encryptedKey);

								// add the encrypted key to the envelope
								envelope.Add(new { alias = alias, key = sEncryptedKey });

								Console.WriteLine($"adding {alias}");
							}
						}
						catch
						{
							Console.WriteLine($"Error: could not find alias {alias}");
						}
					}
				}

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

				// sign the envelope
				byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);
				byte[] envelopeSignature = BouncyCastleHelper.SignData(envelopeHash, privateKey.Private);

				// add the signature to the manifest
				manifest["envelope_signature"] = Convert.ToBase64String(envelopeSignature);
				manifest["envelope_signature_hash"] = "SHA256";
				manifest["from"] = opts.From;
				manifest["to"] = opts.InputAliases ?? new string[0];
				manifest["created"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");


				string manifestJson = JsonSerializer.Serialize(manifest);

				// encrypt the manifest
				byte[] encryptedManifest = BouncyCastleHelper.EncryptWithKey(manifestJson.ToBytes(), key, opts.Domain.ToBytes());

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
				zip.Comment = "This zip file was created by dedrp.com";
			}

			Console.WriteLine("\nDone\n");
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nUnable to pack files\n");
			if (opts.Verbose > 0)
				Console.WriteLine(ex.Message);
			return 1;
		}

		return 0;
	}
}