#pragma warning disable 1998
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;

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
		Console.WriteLine("\n====================================================");
		Console.WriteLine("deadpack v1.0...");
		Console.WriteLine("Deadrop's Encrypted Archive and Distribution PACKage");
		Console.WriteLine("Rob Linton, 2023");
		Console.WriteLine("====================================================\n");
		

		string toDomain = Misc.GetDomain(opts, opts.Alias);

		// get the input zip file
		opts.File = opts.File.Replace(".deadpack","") + ".deadpack";
		string zipFile = opts.File;

		Console.WriteLine($"Unpacking deadpack...");
		Console.WriteLine($"Input: {opts.File}");
		Console.WriteLine($"Recipient Alias: {opts.Alias}");
		Console.WriteLine($"Output: {opts.Output}");

		// get the output directory
		if (String.IsNullOrEmpty(opts.Output))
			opts.Output = Path.GetFileNameWithoutExtension(zipFile);

		// create it
		Directory.CreateDirectory(opts.Output);

		string outputDirectory = opts.Output;
		Console.WriteLine($"Output directory: {outputDirectory}\n");

		//string tmpOutputDirectory = "XXX" + outputDirectory;
		string tmpOutputDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

		try
		{
			

			// read the zip file and output all of the files to the output directory
			Console.WriteLine("  Extracting from zip file...");
			using (ZipArchive archive = ZipFile.OpenRead(zipFile))
			{
				Console.Write("  ");
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
			var envelope = JsonSerializer.Deserialize<Envelope>(envelopeJson) ?? throw new Exception("Could not deserialize envelope");
			
			// read the envelope signature
			string envelopeSignatureFile = Path.Combine(tmpOutputDirectory, "envelope.signature");
			byte[] envelopeSignature = File.ReadAllBytes(envelopeSignatureFile);

			// read the manifest signature
			string manifestSignatureFile = Path.Combine(tmpOutputDirectory, "manifest.signature");
			byte[] manifestSignature = File.ReadAllBytes(manifestSignatureFile);

			// now read the manifest
			string manifestFile = Path.Combine(tmpOutputDirectory, "manifest");
			byte[] manifestBytes = File.ReadAllBytes(manifestFile);

			string fromDomain = Misc.GetDomain(opts, envelope.From);

			// now get the "from" alias
			var fromX509 = await Misc.GetCertificate(opts, envelope.From);

			// now verify the alias
			Console.WriteLine($"\n- Verifying sender alias: {envelope.From}");
			(bool validAlias, byte[] fromFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(fromDomain, envelope.From, opts.Verbose);
			if (!validAlias)
			{
				Console.WriteLine($"Error: could not verify alias {envelope.From}");
				return 1;
			}

			// get the public key from the alias
			var fromPublicKey = fromX509.GetPublicKey();
			
			//
			// check the signature of the envelope
			//

			byte[] envelopeHash = BouncyCastleHelper.GetHashOfString(envelopeJson);				
			try{
				BouncyCastleHelper.verifySignature(envelopeHash, envelopeSignature, fromPublicKey);
				Console.WriteLine("- Envelope signature is valid");
			}
			catch(Exception ex)
			{
				Console.WriteLine("Envelope signature is *NOT* valid");
				if (opts.Verbose > 0)
					Console.WriteLine(ex.Message);
				return 1;
			}

			//
			// check the signature of the manifest
			//

			byte[] manifestHash = BouncyCastleHelper.GetHashOfBytes(manifestBytes);
			try{
				BouncyCastleHelper.verifySignature(manifestHash, manifestSignature, fromPublicKey);
				Console.WriteLine("- Manifest signature is valid");
			}
			catch(Exception ex)
			{
				Console.WriteLine("Manifest signature is *NOT* valid");
				if (opts.Verbose > 0)
					Console.WriteLine(ex.Message);
				return 1;
			}


			// now iterate through the manifest and see if our alias matches one of the to addresses
			bool found_alias = false;
			foreach (var recipient in envelope.To)
			{
				if (recipient.Alias == opts.Alias)
				{
					found_alias = true;
					//Console.WriteLine($"\nUsing alias: {recipient.Alias}");

					// now verify the alias
					Console.WriteLine($"- Verifying recipient alias: {opts.Alias}");
					(bool validToAlias, byte[] toFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(toDomain, opts.Alias, opts.Verbose);
					if (!validToAlias)
					{
						Console.WriteLine($"Error: could not verify alias {opts.Alias}");
						return 1;
					}		

					//
					// now valiate that they share the same root certificate
					//
					if (!fromFingerprint.SequenceEqual(toFingerprint))
					{
						Console.WriteLine($"Aliases do not share the same root certificate {envelope.From} -> {opts.Alias}");
						return 1;
					}
					else
					{
						Console.WriteLine($"- Aliases share the same root certificate: {envelope.From} -> {opts.Alias}");
					}

					// get the public key from the alias
					var toX509 = await Misc.GetCertificate(opts, opts.Alias);
					var toPublicKey = toX509.GetPublicKey();


					// we found our alias, so decrypt the private key
					string encryptedKeyBase64 = recipient.Key;
					byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

					string privateKeyPem = Storage.GetPrivateKey(opts.Alias, opts.Password);

					AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);

					// compare the two public keys and make sure that the private key is for the public key
					if (!keyPair.Public.Equals(toPublicKey))
					{
						Console.WriteLine($"Error: public key does not match for alias {opts.Alias}");
						return 1;
					}
					else
					{
						Console.WriteLine($"- Private key matches public certificate for alias: {opts.Alias}");
					}


					Console.WriteLine("- Decrypting key...");
					byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(encryptedKey, keyPair.Private);

					//
					// now we should have the key used to encrypt all of the files
					//
				
					// now decrypt the manifest
					Console.WriteLine("- Decrypting manifest...");

					byte[] nonce = envelope.From.ToLower().ToBytes();
					byte[] manifestJsonBytes = BouncyCastleHelper.DecryptWithKey(manifestBytes, key, nonce);

					string manifestJson = manifestJsonBytes.FromBytes();
					var manifest = JsonSerializer.Deserialize<Manifest>(manifestJson) ?? throw new Exception("Could not deserialize manifest");

					//
					// now unpack all of the files
					//

					Console.WriteLine("");

					// now decrypt each file
					foreach (FileItem file in manifest.Files)
					{
						Console.WriteLine($"  Unpacking {file.Name}");

						// create a file stream to write the file to
						using (FileStream fs = File.Create(Path.Combine(outputDirectory, file.Name)))
						{
							Console.Write("  ");
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
			Console.WriteLine("\nError: Unable to unpack files\n");
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