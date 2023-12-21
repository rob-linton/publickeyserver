
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.OpenSsl;
using System.IO;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.X509.Store;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Security.Certificates;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Digests;
using System.Text.Json;

namespace deadrop;

public class BouncyCastleHelper
{
	// --------------------------------------------------------------------------------------------------------
	private const int KEY_BIT_SIZE = 256;

	private const int MAC_BIT_SIZE = 128;

	private const int NONCE_BIT_SIZE = 128;
	// --------------------------------------------------------------------------------------------------------
	public static AsymmetricCipherKeyPair GenerateKeyPair(int keySize)
    {
		
        // Create a generator for RSA key pair
        var generator = new RsaKeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));

        // Generate the key pair
        AsymmetricCipherKeyPair keyPair = generator.GenerateKeyPair();

        // Write the private key to a file
        using (TextWriter privateKeyTextWriter = new StringWriter())
        {
            PemWriter pemWriter = new PemWriter(privateKeyTextWriter);
            pemWriter.WriteObject(keyPair.Private);
            pemWriter.Writer.Flush();

            //File.WriteAllText("privateKey.pem", privateKeyTextWriter.ToString());
        }

        // Write the public key to a file
        using (TextWriter publicKeyTextWriter = new StringWriter())
        {
            PemWriter pemWriter = new PemWriter(publicKeyTextWriter);
            pemWriter.WriteObject(keyPair.Public);
            pemWriter.Writer.Flush();

            //File.WriteAllText("publicKey.pem", publicKeyTextWriter.ToString());
        }

		return keyPair;
    }
	// --------------------------------------------------------------------------------------------------------
	public static bool ValidateCertificateChain(string targetCertificatePem, List<string> intermediateAndRootCertificatePems, string commonName)
    {
        try
        {
			Console.WriteLine("Validating certificate chain...");

            X509CertificateParser parser = new X509CertificateParser();

            // Parse the target certificate from PEM string
            Org.BouncyCastle.X509.X509Certificate targetCert = ReadCertificateFromPemString(targetCertificatePem);

            // Parse intermediate and root certificates
            Org.BouncyCastle.X509.X509Certificate[] chain = new Org.BouncyCastle.X509.X509Certificate[intermediateAndRootCertificatePems.Count];
            for (int i = 0; i < intermediateAndRootCertificatePems.Count; i++)
            {
                chain[i] = ReadCertificateFromPemString(intermediateAndRootCertificatePems[i]);
            }

            // Check each certificate in the chain
            for (int i = 0; i < chain.Length; i++)
            {
                Org.BouncyCastle.X509.X509Certificate child = (i == 0) ? targetCert : chain[i - 1];
                Org.BouncyCastle.X509.X509Certificate parent = chain[i];

                if (!child.IssuerDN.Equivalent(parent.SubjectDN))
                {
                    throw new CertificateException("*** ERROR: Issuer/Subject DN mismatch ***");
                }

				// trows an exception if not valid
                child.Verify(parent.GetPublicKey());

				// check if the commonname is a member
				if (!CheckIfCommonNameIsAMember(child.SubjectDN.ToString(), commonName))
				{
					throw new CertificateException("*** Error: CommonName is not a member ***");
				}

				// check if the certificate is valid now
				if (!child.IsValidNow)
				{
					throw new CertificateException("*** Error: Certificate not valid now ***");
				}

				Console.WriteLine($"Certificate {i + 1} of {chain.Length} is valid");
            }

            // Optionally, check the root certificate separately here
			//byte[] fingerprint = GetFingerprint(intermediateAndRootCertificatePems[intermediateAndRootCertificatePems.Count-1]);

			//DisplayVisualFingerprint(fingerprint);
			//CertificateFingerprint.DisplayCertificateFingerprintFromString(fingerprint);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"*** Error: Certificate chain validation failed: {ex.Message} ***");
            return false;
        }
    }
	// --------------------------------------------------------------------------------------------------------
	public static bool CheckIfCommonNameIsAMember(string fullName, string shortName)
	{
		fullName = fullName.Replace("CN=", "").Replace("OU=", "").Replace("O=", "").Replace("C=", "").Replace("ST=", "").Replace("L=", "").Replace(" ", "").ToLower();
		shortName = shortName.Replace("CN=", "").Replace("OU=", "").Replace("O=", "").Replace("C=", "").Replace("ST=", "").Replace("L=", "").Replace(" ", "").ToLower();

		if (fullName.EndsWith(shortName))
		{
			return true;
		}		

#if DEBUG
		// allow domain mismatches in debug mode
		return true;
#else
		return false;
#endif

		
	}
	// --------------------------------------------------------------------------------------------------------
	public static byte[] GetHashOfFile(string filename)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(File.ReadAllBytes(filename));
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	public static byte[] GetHashOfString(string s)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(s.ToBytes());
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	public static string ConvertHashToString(byte[] hash)
    {
        // Convert the byte array to a hexadecimal string
        string hashString = BitConverter.ToString(hash);

        // Remove the hyphens
        hashString = hashString.Replace("-", "");

        return hashString;
    }
	// --------------------------------------------------------------------------------------------------------
	public static byte[] GetFingerprint(string pemString)
	{
		// make sure the certificate is valid
		Org.BouncyCastle.X509.X509Certificate cert = ReadCertificateFromPemString(pemString);
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(pemString.ToBytes());
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	private static void DisplayVisualFingerprint(string fingerprint)
    {
        // Define the formatting parameters
        int bytesPerBlock = 2; // Number of bytes in each block
        int blocksPerGroup = 4; // Number of blocks in each group
        int groupsPerLine = 2; // Number of groups per line

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("\nCA Certificate Fingerprint (SHA-256):");
        sb.AppendLine(new string('-', 43)); // Decorative line

        int index = 0;

        while (index < fingerprint.Length)
        {
            // Add a block
            if (index + bytesPerBlock * 2 <= fingerprint.Length)
            {
                sb.Append(fingerprint.Substring(index, bytesPerBlock * 2));
            }
            else
            {
                // Append the remaining characters
                sb.Append(fingerprint.Substring(index));
            }

            index += bytesPerBlock * 2;

            // Formatting: add spaces and line breaks
            bool isEndOfBlock = (index / (bytesPerBlock * 2)) % blocksPerGroup == 0;
            bool isEndOfLine = (index / (bytesPerBlock * 2)) % (blocksPerGroup * groupsPerLine) == 0;
            if (index < fingerprint.Length)
            {
                sb.Append(isEndOfLine ? "\n" + new string('-', 43) + "\n" : (isEndOfBlock ? "  |  " : " "));
            }
        }

        sb.AppendLine("\n" + new string('-', 43)); // Decorative line
        Console.WriteLine(sb.ToString());
    }
	// --------------------------------------------------------------------------------------------------------
	public static string ReadPemStringFromKey(AsymmetricKeyParameter key)
	{
		using TextWriter publicKeyTextWriter = new StringWriter();
		PemWriter pemWriter = new PemWriter(publicKeyTextWriter);
		pemWriter.WriteObject(key);
		pemWriter.Writer.Flush();

		string sKey = publicKeyTextWriter.ToString()!;

		return sKey;
	}
	// --------------------------------------------------------------------------------------------------------
	public static Org.BouncyCastle.X509.X509Certificate ReadCertificateFromPemString(string pemString)
    {
		using StringReader reader = new StringReader(pemString);
		PemReader pemReader = new PemReader(reader);
		return (Org.BouncyCastle.X509.X509Certificate)pemReader.ReadObject();
	}
	// --------------------------------------------------------------------------------------------------------
	public static AsymmetricCipherKeyPair ReadKeyPairFromPemString(string pemString)
	{
		using StringReader reader = new StringReader(pemString);
		PemReader pemReader = new PemReader(reader);
		return (AsymmetricCipherKeyPair)pemReader.ReadObject();
	}
	// --------------------------------------------------------------------------------------------------------
	public static bool ValidateCertificateChainDotNet(string primaryCertificatePath, List<string> intermediateCertificatesPaths)
    {
        try
        {
            X509CertificateParser certParser = new X509CertificateParser();

            // Load the primary certificate (e.g., the end-entity certificate)
            X509Certificate2 primaryCert = new X509Certificate2(primaryCertificatePath);
            Org.BouncyCastle.X509.X509Certificate bcPrimaryCert = certParser.ReadCertificate(primaryCert.RawData);

            // Load intermediate certificates
            X509Chain chain = new X509Chain();
            foreach (var certPath in intermediateCertificatesPaths)
            {
                X509Certificate2 intermediateCert = new X509Certificate2(certPath);
                chain.ChainPolicy.ExtraStore.Add(intermediateCert);
            }

            // Perform the chain validation
            bool isValid = chain.Build(primaryCert);
            if (!isValid)
            {
                foreach (X509ChainStatus chainStatus in chain.ChainStatus)
                {
                    Console.WriteLine($"Chain error: {chainStatus.StatusInformation}");
                }
            }

            return isValid;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Certificate chain validation failed: {ex.Message}");
            return false;
        }
    }
	// --------------------------------------------------------------------------------------------------------
	public static byte[] Generate256BitRandom()
    {
        // Create a secure random number generator using Bouncy Castle
        SecureRandom random = new SecureRandom();

        // Create a buffer for 256 bits (32 bytes)
        byte[] randomBytes = new byte[32];

        // Fill the buffer with random bytes
        random.NextBytes(randomBytes);

        return randomBytes;
    }
	// --------------------------------------------------------------------------------------------------------
	public static byte[] SignData(byte[] message, AsymmetricKeyParameter privateKey)
	{
		//
		// sign the message using the private key
		//

		// Create an RSA engine for signing
		IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());
		engine.Init(true, privateKey); // true for signing

		// Sign the data
		
		byte[] signature = engine.ProcessBlock(message, 0, message.Length);

		// return the signature and the message
		return signature;
	}
	// --------------------------------------------------------------------------------------------------------
	public static void verifySignature(byte[] message, byte[] signature, AsymmetricKeyParameter publicKey)
	{
		//
		// verify the message using the public key
		//

		// Create an RSA engine for verification
		IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());
		engine.Init(false, publicKey); // false for verification

	}
	// --------------------------------------------------------------------------------------------------------
	public static List<string> EncryptFileInBlocks(string filename, byte[] key, byte[] nonce)
	{
		List<string> chunkList = new List<string>();

		// read the file in 1 MB chunks
		byte[] buffer = new byte[1024 * 1024];
		
		// loop through each 1mb chunk of the file
		using (FileStream fs = new FileStream(filename, FileMode.Open))
		{
			int bytesRead = 0;
			int chunkNumber = 0;
			while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
			{
				byte[] chunk = new byte[bytesRead];
				Array.Copy(buffer, chunk, bytesRead);

				// encrypt the chunk
				byte[] encryptedChunk = EncryptWithKey(chunk, key, nonce);

				// save the chunk to a file
				File.WriteAllBytes($"{filename}.deadrop{chunkNumber}", encryptedChunk);
				chunkList.Add($"{filename}.deadrop{chunkNumber}");

				chunkNumber++;
			}

			return chunkList;
		}
	}
	// --------------------------------------------------------------------------------------------------------
	public static byte[] DecryptWithPrivateKey(byte[] cipherText, AsymmetricKeyParameter privateKey)
	{
		//
		// decrypt the message using the private key
		//

		// Create an RSA engine for decryption
		IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());
		engine.Init(false, privateKey); // false for decryption

		// Decrypt the data
		return engine.ProcessBlock(cipherText, 0, cipherText.Length);
	}
	// --------------------------------------------------------------------------------------------------------
	public static byte[] EncryptWithPublicKey(byte[] message, AsymmetricKeyParameter publicKey)
	{
		//
		// encrypt the message using the public key
		//

		// Create an RSA engine for encryption
        IAsymmetricBlockCipher engine = new Pkcs1Encoding(new RsaEngine());
        engine.Init(true, publicKey); // true for encryption

        // Encrypt the data
        return engine.ProcessBlock(message, 0, message.Length);

	}
	// --------------------------------------------------------------------------------------------------------
	public static byte[] EncryptWithKey(
			byte[] messageToEncrypt,
			byte[] key,
			byte[] nonSecretPayload
			)
		{

			SecureRandom _random = new SecureRandom();

			if (key == null || key.Length != KEY_BIT_SIZE / 8)
			{
				using (SHA256 sha256Hash = SHA256.Create())
				{
					key = sha256Hash.ComputeHash(key??new byte[] { });
				}
			}

			//Non-secret Payload Optional
			nonSecretPayload = nonSecretPayload ?? new byte[] { };

			//Using random nonce large enough not to repeat
			var nonce = new byte[NONCE_BIT_SIZE / 8];
			_random.NextBytes(nonce, 0, nonce.Length);

			var cipher = new GcmBlockCipher(new AesEngine());
			var parameters = new AeadParameters(new KeyParameter(key), MAC_BIT_SIZE, nonce, nonSecretPayload);
			cipher.Init(true, parameters);

			//Generate Cipher Text With Auth Tag
			var cipherText = new byte[cipher.GetOutputSize(messageToEncrypt.Length)];
			var len = cipher.ProcessBytes(messageToEncrypt, 0, messageToEncrypt.Length, cipherText, 0);
			cipher.DoFinal(cipherText, len);

			//Assemble Message
			using (var combinedStream = new MemoryStream())
			{
				using (var binaryWriter = new BinaryWriter(combinedStream))
				{
					//Prepend Authenticated Payload
					binaryWriter.Write(nonSecretPayload);
					//Prepend Nonce
					binaryWriter.Write(nonce);
					//Write Cipher Text
					binaryWriter.Write(cipherText);
				}
				return combinedStream.ToArray();
			}
		}
		// --------------------------------------------------------------------------------------------------------
		public static byte[] DecryptWithKey(
			byte[] encryptedMessage,
			byte[] key,
			byte[] nonSecretPayload
			)
		{

			int nonSecretPayloadLength = nonSecretPayload.Length;

			//User Error Checks
			if (key == null || key.Length != KEY_BIT_SIZE / 8)
			{
				using (SHA256 sha256Hash = SHA256.Create())
				{
					key = sha256Hash.ComputeHash(key??new byte[] { });
				}
			}

			if (encryptedMessage == null || encryptedMessage.Length == 0)
			{
				throw new ArgumentException("Encrypted Message Required!", "encryptedMessage");
			}

			using (var cipherStream = new MemoryStream(encryptedMessage))
			using (var cipherReader = new BinaryReader(cipherStream))
			{
				//Grab Payload
				var nonSecretPayloadMsg = cipherReader.ReadBytes(nonSecretPayloadLength);

				if (nonSecretPayload.FromBytes() != nonSecretPayloadMsg.FromBytes())
				{
					throw new Exception("Non Secret Payload Does not Match!");
				}

				//Grab Nonce
				var nonce = cipherReader.ReadBytes(NONCE_BIT_SIZE / 8);

				var cipher = new GcmBlockCipher(new AesEngine());
				var parameters = new AeadParameters(new KeyParameter(key), MAC_BIT_SIZE, nonce, nonSecretPayload);
				cipher.Init(false, parameters);

				//Decrypt Cipher Text
				var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - nonSecretPayloadLength - nonce.Length);
				var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

				
				var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
				cipher.DoFinal(plainText, len);
		

				return plainText;
			}
		}
		// --------------------------------------------------------------------------------------------------------
		public static async Task<bool> VerifyAliasAsync(string domain, string alias, int verbose)
		{
			

			// first get the CA
			if (verbose > 0)
				Console.WriteLine($"GET: https://{domain}/cacerts");

			var result = await HttpHelper.Get($"https://{domain}/cacerts");
			var ca = JsonSerializer.Deserialize<CaCertsResult>(result);
			var cacerts = ca?.CaCerts;

			// now get the alias	
			if (verbose > 0)
				Console.WriteLine($"GET: https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

			result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

			var c = JsonSerializer.Deserialize<CertResult>(result);
			var certificate = c?.Certificate;

			// now validate the certificate chain
			bool valid = false;
			if (certificate != null && cacerts != null) // Add null check for cacerts
			{
				valid = BouncyCastleHelper.ValidateCertificateChain(certificate, cacerts, domain);
			}

			if (valid)
				return true;
			else
				return false;
		}
}
