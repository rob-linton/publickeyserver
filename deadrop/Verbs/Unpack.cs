#pragma warning disable 1998
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;

namespace deadrop.Verbs;

[Verb("unpack", HelpText = "Unpack a package.")]
public class UnpackOptions : Options
{
   [Option('i', "input", Required = true, HelpText = "Input deadrop file to be processed")]
    public required string File { get; set; }

	[Option('o', "output", Default = "", HelpText = "Output directory")]
	public string? Output { get; set; }	

	[Option('a', "alias", Required = true, HelpText = "Alias to use")]
    public required string Alias { get; set; }
}

class Unpack 
{
	public static async Task<int> Execute(UnpackOptions opts)
	{
		Console.WriteLine("\nUnpacking the following file for dead drop:");
		Console.WriteLine("===========================================");
		Console.WriteLine($"\nFile: {opts.File}\n");

		string domain = Misc.GetDomain(opts, opts.Alias);

		// get the input zip file
		string zipFile = opts.File;

		// get the output directory
		if (String.IsNullOrEmpty(opts.Output))
			opts.Output = Path.GetFileNameWithoutExtension(zipFile);

		// create it
		Directory.CreateDirectory(opts.Output);

		string outputDirectory = opts.Output;
		Console.WriteLine($"Output directory: {outputDirectory}");

		//string tmpOutputDirectory = "~" + outputDirectory;
		string tmpOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

		try
		{
			// read the zip file and output all of the files to the output directory
			Console.WriteLine("Extracting from zip file...");
			using (ZipArchive archive = ZipFile.OpenRead(zipFile))
			{
				foreach (ZipArchiveEntry entry in archive.Entries)
				{
					string destinationPath = Path.GetFullPath(Path.Combine(tmpOutputDirectory, entry.FullName));

					if (Path.GetFileName(destinationPath).Length == 0)
					{
						// if the destination path is a directory, create it
						Directory.CreateDirectory(destinationPath);
					}
					else
					{
						// if the destination path is a file, create the directory and write the file
						string directoryPath = Path.GetDirectoryName(destinationPath) ?? string.Empty;
						Directory.CreateDirectory(directoryPath);
						entry.ExtractToFile(destinationPath, true);
						Console.Write("#");
					}
				}
			}
			Console.WriteLine("");


			// now read the envelope
			string envelopeFile = Path.Combine(tmpOutputDirectory, "envelope");
			string envelopeJson = File.ReadAllText(envelopeFile);
			var envelope = JsonSerializer.Deserialize<Envelope>(envelopeJson);
			if (envelope == null)
			{
				Console.WriteLine("\nError: could not read envelope");
				return 1;
			}

			// now iterate through the manifest and see if our alias matches one of the to addresses
			bool found_alias = false;
			foreach (var recipient in envelope.To)
			{
				if (recipient.Alias == opts.Alias)
				{
					found_alias = true;
					Console.WriteLine($"\nFound alias: {recipient.Alias}\n");

					// we found our alias, so decrypt the key
					string encryptedKeyBase64 = recipient.Key;
					byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

					string privateKeyPem = Storage.GetPrivateKey(opts.Alias, opts.Password);

					AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);

					Console.WriteLine("Decrypting key...");
					byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(encryptedKey, keyPair.Private);


					//
					// now we should have the key used to encrypt all of the files
					//

					// now decrypt the manifest
					Console.WriteLine("Decrypting manifest...");
					string manifestFile = Path.Combine(tmpOutputDirectory, "manifest");
					byte[] encryptedManifest = File.ReadAllBytes(manifestFile);

					byte[] nonce = envelope.From.ToLower().ToBytes();
					byte[] manifestJsonBytes = BouncyCastleHelper.DecryptWithKey(encryptedManifest, key, nonce);

					string manifestJson = manifestJsonBytes.FromBytes();
					var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson);
					if (manifest == null)
					{
						Console.WriteLine("Error: could not read manifest");
						return 1;
					}

					// checking the signature on the envelope
					Console.WriteLine("Verifying envelope signature...");
					byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);

					// now get the from alias	
					if (opts.Verbose > 0)
						Console.WriteLine($"GET: https://{domain}/cert/{Misc.GetAliasFromAlias(envelope.From)}");

					var result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(envelope.From)}");

					var c = JsonSerializer.Deserialize<CertResult>(result);
					var fromCertificate = c?.Certificate;

					// now get the CA
					if (opts.Verbose > 0)
						Console.WriteLine($"GET: https://{domain}/cacerts");

					result = await HttpHelper.Get($"https://{domain}/cacerts");
					var ca = JsonSerializer.Deserialize<CaCertsResult>(result);
					var cacerts = ca?.CaCerts;

					// now validate the certificate chain
					bool valid = false;
					if (fromCertificate != null && cacerts != null) // Add null check for cacerts
					{
						valid = BouncyCastleHelper.ValidateCertificateChain(fromCertificate, cacerts);
					}

					// now check the signature
					if (valid)
					{
						var x509 = BouncyCastleHelper.ReadCertificateFromPemString(fromCertificate);
						var fromPublicKey = x509.GetPublicKey();

						try{
							BouncyCastleHelper.verifySignature(envelopeHash, Convert.FromBase64String(manifest.Signature), fromPublicKey);
							Console.WriteLine("Signature is valid");
						}
						catch(Exception ex)
						{
							Console.WriteLine("Signature is *NOT* valid");
							if (opts.Verbose > 0)
								Console.WriteLine(ex.Message);
							return 1;
						}
					}
					else
					{
						Console.WriteLine("Signature is *NOT* valid");
						return 1;
					}


					//Console.WriteLine("Decrypting files...");

					// now decrypt each file
					foreach (FileItem file in manifest.Files)
					{
						Console.WriteLine($"Decrypting {file.Name}...");

						// create a file stream to write the file to
						using (FileStream fs = File.Create(Path.Combine(outputDirectory, file.Name)))
						{

							// decrypt the file and populate the file stream
							foreach (var block in file.Blocks)
							{
								string blockFile = Path.Combine(tmpOutputDirectory, block);
								byte[] encryptedBlock = File.ReadAllBytes(blockFile);

								byte[] decryptedBlock = BouncyCastleHelper.DecryptWithKey(encryptedBlock, key, nonce);

								fs.Write(decryptedBlock, 0, decryptedBlock.Length);
								Console.Write("#");
							}
						}

						// convert the unix timestamp to a datetime
						DateTime createDate = DateTimeOffset.FromUnixTimeSeconds(file.Ctime).DateTime;
						DateTime modifiedDate = DateTimeOffset.FromUnixTimeSeconds(file.Mtime).DateTime;

						// now set the date attributes
						File.SetCreationTime(Path.Combine(outputDirectory, file.Name), createDate);
						File.SetLastWriteTime(Path.Combine(outputDirectory, file.Name), modifiedDate);
					}
					Console.WriteLine("\n\nDone\n");
				}
			}

			if (!found_alias)
			{
				Console.WriteLine($"Error: could not find alias {opts.Alias} in deadpack");
				return 1;
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine("\nUnable to unpack files\n");
			if (opts.Verbose > 0)
				Console.WriteLine(ex.Message);
			return 1;
		}
		finally
		{
			// clean up
			if (Directory.Exists(tmpOutputDirectory))
				Directory.Delete(tmpOutputDirectory, true);
		}
		
		return 0;
	}
}