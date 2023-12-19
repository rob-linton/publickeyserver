
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

namespace jdoe;

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
	public static bool ValidateCertificateChain(string targetCertificatePem, List<string> intermediateAndRootCertificatePems)
    {
        try
        {
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
                    throw new CertificateException("Issuer/Subject DN mismatch");
                }

                child.Verify(parent.GetPublicKey());
				if (!child.IsValidNow)
				{
					throw new CertificateException("Certificate not valid now");
				}
            }

            // Optionally, check the root certificate separately here
			byte[] fingerprint = GetFingerprint(intermediateAndRootCertificatePems[intermediateAndRootCertificatePems.Count-1]);

			//DisplayVisualFingerprint(fingerprint);
			CertificateFingerprint.DisplayCertificateFingerprintFromString(fingerprint);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Certificate chain validation failed: {ex.Message}");
            return false;
        }
    }
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
}
