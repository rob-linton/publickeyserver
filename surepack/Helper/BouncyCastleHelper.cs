
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
//using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Digests;
using System.Text.Json;
using System.Collections;
using suredrop.Verbs;
using System.Data;

namespace suredrop;

public class BouncyCastleHelper
{
	// --------------------------------------------------------------------------------------------------------
	private const int KEY_BIT_SIZE = 256;

	private const int MAC_BIT_SIZE = 128;

	private const int NONCE_BIT_SIZE = 128;
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a pair of asymmetric cryptographic keys, consisting of a public key and a private key.
	/// </summary>
	public static AsymmetricCipherKeyPair GenerateKeyPair(int keySize)
    {
        // Create a generator for RSA key pair
        var generator = new RsaKeyPairGenerator();
        generator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));

        // Generate the key pair
        AsymmetricCipherKeyPair keyPair = generator.GenerateKeyPair();

		return keyPair;
    }

	public static List<string> GetAltNames(string targetCertificatePem)
	{
		List<string> names = new List<string>();

		// get the certificate
		Org.BouncyCastle.X509.X509Certificate cert = ReadCertificateFromPemString(targetCertificatePem);

		// get the alt names
		var altNames = cert.GetSubjectAlternativeNames();

		// check if the alias is in the alt names
		foreach (var altName in altNames)
		{
			if (altName[1] != null)
			{
				names.Add(altName[1]?.ToString()!);
			}
		}

		return names;
	}

	public static (string email, string alias) GetEmailFromAltNames(List<string> altNames)
	{
		string email = "";
		string alias = "";
		foreach (string name in altNames)
		{
			if (name.Contains("@"))
			{
				email = name;
			}
			else
			{
				alias = name;
			}
		}

		return (email, alias);
	}

	public static bool CheckIfCertificateAltNamesMatch(string alias, string email, string targetCertificatePem)
	{
		// get the certificate
		Org.BouncyCastle.X509.X509Certificate cert = ReadCertificateFromPemString(targetCertificatePem);

		// get the alt names
		var altNames = cert.GetSubjectAlternativeNames();

		// check if the alias is in the alt names
		if (altNames != null)
		{
			bool aliasFound = false;
			bool emailFound = false;
			foreach (var altName in altNames)
			{
				if (altName[1].ToString() == alias)
				{
					Misc.LogCheckMark($"Alias {alias} is in the SubjectAlternativeNames");
					aliasFound = true;
				}
				if (altName[1].ToString() == email)
				{
					Misc.LogCheckMark($"Email {email} is in the SubjectAlternativeNames");
					emailFound = true;
				}
			}

			if (String.IsNullOrEmpty(email))
			{
				if (aliasFound)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				if (aliasFound && emailFound)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}
		return false;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Validates the certificate chain using the target certificate, intermediate and root certificates,
	/// common name, and options provided.
	/// </summary>
	/// <param name="targetCertificatePem">The PEM string of the target certificate.</param>
	/// <param name="intermediateAndRootCertificatePems">The list of PEM strings of intermediate and root certificates.</param>
	/// <param name="commonName">The common name to check against the certificate.</param>
	/// <returns>A tuple containing a boolean indicating if the certificate chain is valid and the fingerprint of the root certificate.</returns>
	public static (bool, byte[]) ValidateCertificateChain(string targetCertificatePem, List<string> intermediateAndRootCertificatePems, string commonName)
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

               { if (!child.IssuerDN.Equivalent(parent.SubjectDN))
                
                    throw new CertificateException("*** ERROR: Issuer/Subject DN mismatch ***");
                }
				Misc.LogCheckMark("Subject name matches the parent certificate's Issuer name");

				// throws an exception if not valid
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
				Misc.LogCheckMark("Certificate dates are valid");

				// does the parent have authority to sign the child?
				Asn1Object? asn1Object = GetAsn1Object(parent, X509Extensions.KeyUsage);

				if (asn1Object == null)
				{
					throw new CertificateException("*** Error: KeyUsage extension not found in the certificate. ***");
				}

				KeyUsage keyUsageCheck = new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign);

				// check if asn1Object == keyUsageCheck
				if (!keyUsageCheck.Equals(asn1Object))
				{
					throw new CertificateException("*** Error: KeyUsage extension does not allow signing. ***");
				}
				Misc.LogCheckMark($"Parent has authority to sign the child {i + 1} of {chain.Length}");

				Misc.LogLine($"  Certificate {i + 1} of {chain.Length} is valid: {parent.SubjectDN}");
            }

            // get the fingerprint of the root certificate
			byte[] fingerprint = GetFingerprint(intermediateAndRootCertificatePems[intermediateAndRootCertificatePems.Count-1]);

			//DisplayVisualFingerprint(fingerprint);
			//CertificateFingerprint.DisplayCertificateFingerprintFromString(fingerprint);
			Misc.LogCheckMark($"Certificate is valid");

            return (true, fingerprint);
        }
        catch (Exception ex)
        {
            Misc.LogLine($"*** Error: Certificate chain validation failed: {ex.Message} ***");
            return (false, new byte[0]);
        }
    }
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Retrieves the ASN.1 object from the specified X.509 certificate using the given object identifier (OID).
	/// </summary>
	/// <param name="certificate">The X.509 certificate.</param>
	/// <param name="oid">The object identifier (OID) of the extension.</param>
	/// <returns>The ASN.1 object if found; otherwise, null.</returns>
	private static Asn1Object? GetAsn1Object(X509Certificate certificate, DerObjectIdentifier oid)
	{
		Asn1OctetString akiBytes = certificate.GetExtensionValue(oid);
		if (akiBytes == null) 
		{
			return null;
		}

		return Asn1Object.FromByteArray(akiBytes.GetOctets());
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Checks if the common name is a member of the full name.
	/// </summary>
	/// <param name="fullName">The full name to check.</param>
	/// <param name="shortName">The common name to check.</param>
	/// <returns>True if the common name is a member of the full name; otherwise, false.</returns>
	public static bool CheckIfCommonNameIsAMember(string fullName, string shortName)
	{
		fullName = fullName.Replace("CN=", "").Replace("OU=", "").Replace("O=", "").Replace("C=", "").Replace("ST=", "").Replace("L=", "").Replace(" ", "").ToLower();
		shortName = shortName.Replace("CN=", "").Replace("OU=", "").Replace("O=", "").Replace("C=", "").Replace("ST=", "").Replace("L=", "").Replace(" ", "").ToLower();

		if (fullName.EndsWith(shortName))
		{
			Misc.LogCheckMark($"CommonName {shortName} is a member of {fullName}");
			return true;
		}		

#if DEBUG
		Misc.LogCross($"DEBUG CommonName {shortName} is a member of {fullName}");
		// allow domain mismatches in debug mode
		return true;
#else
		return false;
#endif

		
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Computes the SHA256 hash of the given byte array.
	/// </summary>
	/// <param name="b">The byte array to compute the hash for.</param>
	/// <returns>The computed hash as a byte array.</returns>
	public static byte[] GetHashOfBytes(byte[] b)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(b);
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Computes the SHA256 hash of the specified file.
	/// </summary>
	/// <param name="filename">The path of the file to compute the hash for.</param>
	/// <returns>The computed hash as a byte array.</returns>
	public static byte[] GetHashOfFile(string filename)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(File.ReadAllBytes(filename));
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Computes the SHA256 hash of a given string.
	/// </summary>
	/// <param name="s">The string to compute the hash for.</param>
	/// <returns>The computed hash as a byte array.</returns>
	public static byte[] GetHashOfString(string s)
	{
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(s.ToBytes());
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Converts a byte array hash to a hexadecimal string representation.
	/// </summary>
	/// <param name="hash">The byte array hash to convert.</param>
	/// <returns>The hexadecimal string representation of the hash.</returns>
	public static string ConvertHashToString(byte[] hash)
    {
        // Convert the byte array to a hexadecimal string
        string hashString = BitConverter.ToString(hash);

        // Remove the hyphens
        hashString = hashString.Replace("-", "");

        return hashString;
    }
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Calculates the fingerprint of a certificate in PEM format.
	/// </summary>
	/// <param name="pemString">The certificate in PEM format.</param>
	/// <returns>The fingerprint of the certificate.</returns>
	public static byte[] GetFingerprint(string pemString)
	{
		// make sure the certificate is valid
		Org.BouncyCastle.X509.X509Certificate cert = ReadCertificateFromPemString(pemString);

		// now get the fingerprint
		using SHA256 sha256 = SHA256.Create();
		byte[] hashBytes = sha256.ComputeHash(pemString.ToBytes());
		return hashBytes;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Reads a PEM string from an AsymmetricKeyParameter object.
	/// </summary>
	/// <param name="key">The AsymmetricKeyParameter object.</param>
	/// <returns>The PEM string representation of the key.</returns>
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
	/// <summary>
	/// Reads an X.509 certificate from a PEM string.
	/// </summary>
	/// <param name="pemString">The PEM string containing the certificate.</param>
	/// <returns>The X.509 certificate.</returns>
	public static X509Certificate ReadCertificateFromPemString(string pemString)
    {
		using StringReader reader = new StringReader(pemString);
		PemReader pemReader = new PemReader(reader);
		return (X509Certificate)pemReader.ReadObject();
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a pair of asymmetric cryptographic keys.
	/// </summary>
	public static AsymmetricCipherKeyPair ReadKeyPairFromPemString(string pemString)
	{
		using StringReader reader = new StringReader(pemString);
		PemReader pemReader = new PemReader(reader);
		return (AsymmetricCipherKeyPair)pemReader.ReadObject();
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Generates a 256-bit random byte array using a secure random number generator.
	/// </summary>
	/// <returns>A byte array containing 256 random bits.</returns>
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
	/// <summary>
	/// Signs the provided data using the specified private key.
	/// </summary>
	/// <param name="message">The data to be signed.</param>
	/// <param name="privateKey">The private key used for signing.</param>
	/// <returns>The generated signature.</returns>
	public static byte[] SignData(byte[] message, AsymmetricKeyParameter privateKey)
	{

		var signer = SignerUtilities.GetSigner("SHA512WITHRSA");
		signer.Init(true, privateKey);
		signer.BlockUpdate(message, 0, message.Length);

		byte[] signature = signer.GenerateSignature();

		return signature;

	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Verifies the signature of a message using the provided public key.
	/// </summary>
	/// <param name="message">The message to verify.</param>
	/// <param name="signature">The signature to verify.</param>
	/// <param name="publicKey">The public key used for verification.</param>
	public static void VerifySignature(byte[] message, byte[] signature, AsymmetricKeyParameter publicKey)
	{

		var signer = SignerUtilities.GetSigner("SHA512WITHRSA");
		signer.Init(false, publicKey);
		signer.BlockUpdate(message, 0, message.Length);

		bool isValid =  signer.VerifySignature(signature);
		if (!isValid)
		{
			throw new Exception("ERROR: Signature is not valid");
		}		
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Encrypts a file in blocks using a specified key and nonce.
	/// </summary>
	/// <param name="filename">The path of the file to be encrypted.</param>
	/// <param name="key">The encryption key.</param>
	/// <param name="nonce">The nonce value.</param>
	/// <returns>A list of file chunks generated during the encryption process.</returns>
	public static async Task<List<string>> EncryptFileInBlocks(string filename, byte[] key, byte[] nonce, IProgress<StatusUpdate>? progress = null)
	{
		List<string> chunkList = new List<string>();

		// read the file in 1 MB chunks
		byte[] buffer = new byte[1024 * 1024];

		// get the filesize
		FileInfo fi = new FileInfo(filename);
		long fileSize = fi.Length;

		// calculate the number of chunks
		long chunkCount = fileSize / buffer.Length;
		
		// loop through each 1mb chunk of the file
		using (FileStream fs = new FileStream(filename, FileMode.Open))
		{
			int bytesRead = 0;
			int chunkNumber = 0;
			while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
			{
				progress?.Report(new StatusUpdate { BlockIndex = chunkNumber, BlockCount = chunkCount});
				await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX

				byte[] chunk = new byte[bytesRead];
				Array.Copy(buffer, chunk, bytesRead);
			
				// compress the chunk
				byte[] compressedChunk = Misc.CompressBytes(chunk);

				// encrypt the chunk
				byte[] encryptedChunk = EncryptWithKey(compressedChunk, key, nonce);

				// save the chunk to a file
				File.WriteAllBytes($"{filename}.suredrop{chunkNumber}", encryptedChunk);
				chunkList.Add($"{filename}.suredrop{chunkNumber}");

				chunkNumber++;
			}

			return chunkList;
		}
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Decrypts the given cipher text using the provided private key.
	/// </summary>
	/// <param name="cipherText">The cipher text to be decrypted.</param>
	/// <param name="privateKey">The private key used for decryption.</param>
	/// <returns>The decrypted data.</returns>
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
	/// <summary>
	/// Encrypts the specified message using the provided public key.
	/// </summary>
	/// <param name="message">The message to be encrypted.</param>
	/// <param name="publicKey">The public key used for encryption.</param>
	/// <returns>The encrypted message.</returns>
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
	/// <summary>
	/// Encrypts a message using a given key and optional non-secret payload.
	/// </summary>
	/// <param name="messageToEncrypt">The message to encrypt.</param>
	/// <param name="key">The encryption key.</param>
	/// <param name="nonSecretPayload">Optional non-secret payload to include in the encryption.</param>
	/// <returns>The encrypted message.</returns>
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
		/// <summary>
		/// Decrypts an encrypted message using a key and non-secret payload.
		/// </summary>
		/// <param name="encryptedMessage">The encrypted message to decrypt.</param>
		/// <param name="key">The key used for decryption.</param>
		/// <param name="nonSecretPayload">The non-secret payload associated with the encrypted message.</param>
		/// <returns>The decrypted plain text message.</returns>
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
		/// <summary>
		/// Verifies the alias asynchronously by retrieving the CA certificates and the alias certificate from the specified domain.
		/// Then, it validates the certificate chain using BouncyCastleHelper.ValidateCertificateChain method.
		/// </summary>
		/// <param name="domain">The domain to retrieve the certificates from.</param>
		/// <param name="alias">The alias to verify.</param>
		/// <returns>A tuple containing a boolean indicating the validity of the alias and the fingerprint of the certificate.</returns>
		public static async Task<(bool, byte[])> VerifyAliasAsync(string domain, string alias, string email)
		{
			

			// first get the CA
			var result = await HttpHelper.Get($"https://{domain}/cacerts");
			var ca = JsonSerializer.Deserialize<CaCertsResult>(result);
			var cacerts = ca?.CaCerts;

			// now get the alias	
			result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

			var c = JsonSerializer.Deserialize<CertResult>(result);
			var certificate = c?.Certificate;

			// now validate the certificate chain
			bool valid = false;
			byte[] fingerprint = new byte[0];
			if (certificate != null && cacerts != null) // Add null check for cacerts
			{
				(valid, fingerprint) = BouncyCastleHelper.ValidateCertificateChain(certificate, cacerts, domain);
			}

			bool validAltName = CheckIfCertificateAltNamesMatch(alias, email, certificate!);

		if (valid)
		{
			string historyAlias = Storage.GetSurePackDirectoryHistory($"{alias}.pem");

			// get a list of private keys
			List<Alias> privateKeys = Storage.GetAliases();

			// check if this alias is in the list
			bool found = false;
			foreach (Alias a in privateKeys)
			{
				if (a.Name == alias)
				{
					found = true;
					break;
				}
			}

			// if valid then save the certificate if it is not an existing alias
			if (!found)
				File.WriteAllText(historyAlias, certificate);

			return (true, fingerprint);
		}
		else
			return (false, new byte[0]);
		}
		// --------------------------------------------------------------------------------------------------------
		public static string GetCustomExtensionData(X509Certificate cert, string oid)
		{
			// Get the custom extension
			Asn1OctetString asn1OctetStr = cert.GetExtensionValue(new DerObjectIdentifier(oid));
			if (asn1OctetStr == null) return ""; // Extension not found

			// Decode the extension
			Asn1Object asn1Object = Asn1Object.FromByteArray(asn1OctetStr.GetOctets());
			GeneralNames generalNames = GeneralNames.GetInstance(asn1Object);

			foreach (GeneralName generalName in generalNames.GetNames())
			{
				if (generalName.TagNo == GeneralName.OtherName)
				{
					OtherName otherName = OtherName.GetInstance(generalName.Name.ToAsn1Object());

					if (otherName.TypeID.ToString() == oid)
					{
						Asn1Encodable otherNameValue = otherName.Value;

						// Check if the value is an instance of DerUtf8String
						if (otherNameValue is DerUtf8String utf8String)
						{
							return utf8String.GetString();
						}
						else
						{
							// Handle other types of encodable values as necessary
							// For instance, if it's a tagged object or another ASN.1 type
						}
					}
				}
			}

			return ""; // Data not found or format not as expected
		}
		// --------------------------------------------------------------------------------------------------------
}
