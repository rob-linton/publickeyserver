#pragma warning disable 1998
using System.IO.Compression;
using System.Text.Json;
using CommandLine;
using Org.BouncyCastle.Crypto;

namespace jdoe.Verbs;

[Verb("unpack", HelpText = "Unpack a package.")]
public class UnpackOptions : Options
{
   [Option('i', "input", Required = true, HelpText = "Input dedrp file to be processed")]
    public string File { get; set; } = "";

	[Option('o', "output", Default = "", HelpText = "Output directory")]
	public string Output { get; set; } = "";	

	[Option('a', "alias", Required = true, HelpText = "Alias to use")]
    public string Alias { get; set; } = "";
}

class Unpack 
{
	public static async Task<int> Execute(UnpackOptions opts)
	{
		string domain = Misc.GetDomain(opts, opts.Alias);

		// get the input zip file
		string zipFile = opts.File;

		// get the output directory
		if (opts.Output.Length == 0)
			opts.Output = Path.GetFileNameWithoutExtension(zipFile);

		string outputDirectory = opts.Output;
		string tmpOutputDirectory = "~" + outputDirectory;

		// read the zip file and output all of the files to the output directory
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
					Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
					entry.ExtractToFile(destinationPath, true);
				}
			}
		}

		// now read the envelope
		string envelopeFile = Path.Combine(tmpOutputDirectory, "envelope");
		string envelopeJson = File.ReadAllText(envelopeFile);
		var envelope = JsonSerializer.Deserialize<Envelope>(envelopeJson);
		
		// now iterate through the manifest and see if our alias matches one
		foreach (var item in envelope.to)
		{
			if (item.Key == opts.Alias)
			{
				Console.WriteLine($"Found alias: {item.Key}");

				// we found our alias, so decrypt the key
				string encryptedKeyBase64 = item.Value;
				byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

				string privateKeyPem = Storage.GetPrivateKey(opts.Alias, opts.Password);
				
				AsymmetricCipherKeyPair keyPair = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);

				byte[] key = BouncyCastleHelper.DecryptWithPrivateKey(encryptedKey, keyPair.Private);

				//
				// now we should have the key used to encrypt all of the files
				//

				// now decrypt the manifest
				string manifestFile = Path.Combine(tmpOutputDirectory, "manifest");
				byte[] encryptedManifest = File.ReadAllBytes(manifestFile);

				byte[] nonce = Misc.GetDomainFromAlias(envelope.from).ToBytes();
				byte[] manifestJsonBytes = BouncyCastleHelper.DecryptWithKey(encryptedManifest, key, nonce);

				
			}
		}
		// get the key




		
		return 0;
	}
}