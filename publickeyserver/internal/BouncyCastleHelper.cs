using System;
using System.Security.Cryptography;
using System.Text;
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
using System.Collections.Generic;
using Org.BouncyCastle.Security.Certificates;
using System.Threading.Tasks;
using System.Text.Json;

namespace publickeyserver
{
	public class BouncyCastleHelper
	{
		// --------------------------------------------------------------------------------------------------------
		private const int KEY_BIT_SIZE = 256;

		private const int MAC_BIT_SIZE = 128;

		private const int NONCE_BIT_SIZE = 128;
		// --------------------------------------------------------------------------------------------------------
		public BouncyCastleHelper()
		{
			
		}
		// --------------------------------------------------------------------------------------------------------
		public static void CheckCAandCreate(
			string origin,
			string password
			)
		{
			// first check if we have a sub ca
			if (!File.Exists($"subcacert.{origin}.pem"))
			{
				// if we don't then check if we have a root ca
				if (!File.Exists($"cacert.{origin}.pem"))
				{
					// if we dont create the root ca
					CreateEncryptedCA(origin, password);
				}

				//
				// get the CA private key
				//
				byte[] cakeysBytes = System.IO.File.ReadAllBytes($"SAVE-ME-OFFLINE-cakeys.{GLOBALS.origin}.pem");
				byte[] cakeysDecrypted = BouncyCastleHelper.DecryptWithKey(cakeysBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());
				string cakeysPEM = cakeysDecrypted.FromBytes();
				AsymmetricCipherKeyPair cakeys = (AsymmetricCipherKeyPair)BouncyCastleHelper.fromPEM(cakeysPEM);

				// create the sub ca
				CreateEncryptedSubCA(origin, password, cakeys);
			}


		}
		// --------------------------------------------------------------------------------------------------------
		public static void CreateEncryptedCA(
			string origin,
			string password
			)
		{
			// one doesn't exist, so create it the first time
			AsymmetricCipherKeyPair privatekeyCA = null;

			Console.WriteLine("Creating CA");
			Org.BouncyCastle.X509.X509Certificate ca = CreateCertificateAuthorityCertificate(GLOBALS.origin, ref privatekeyCA);
			string cacertPEM = BouncyCastleHelper.toPEM(ca);
			string cakeysPEM = BouncyCastleHelper.toPEM(privatekeyCA);

			byte[] cacertBytes = cacertPEM.ToBytes();
			byte[] cakeysBytes = cakeysPEM.ToBytes();

			byte[] cacertEncrypted = EncryptWithKey(cacertBytes, password.ToBytes(), origin.ToBytes());
			byte[] cakeysEncrypted = EncryptWithKey(cakeysBytes, password.ToBytes(), origin.ToBytes());

			System.IO.File.WriteAllBytes($"cacert.{origin}.pem", cacertEncrypted);
			System.IO.File.WriteAllBytes($"SAVE-ME-OFFLINE-cakeys.{origin}.pem", cakeysEncrypted);
		}
		// --------------------------------------------------------------------------------------------------------
		public static void CreateEncryptedSubCA(
			string origin,
			string password,
			AsymmetricCipherKeyPair privatekeyCA
			)
		{
			// one doesn't exist, so create it the first time
			AsymmetricCipherKeyPair privatekeySubCA = null;

			Console.WriteLine("Creating Sub CA");
			Org.BouncyCastle.X509.X509Certificate ca = CreateSubCertificateAuthorityCertificate(GLOBALS.origin, ref privatekeySubCA, privatekeyCA);
			string cacertPEM = BouncyCastleHelper.toPEM(ca);
			string cakeysPEM = BouncyCastleHelper.toPEM(privatekeySubCA);

			byte[] cacertBytes = cacertPEM.ToBytes();
			byte[] cakeysBytes = cakeysPEM.ToBytes();

			byte[] cacertEncrypted = EncryptWithKey(cacertBytes, password.ToBytes(), origin.ToBytes());
			byte[] cakeysEncrypted = EncryptWithKey(cakeysBytes, password.ToBytes(), origin.ToBytes());

			System.IO.File.WriteAllBytes($"subcacert.{origin}.pem", cacertEncrypted);
			System.IO.File.WriteAllBytes($"subcakeys.{origin}.pem", cakeysEncrypted);
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
					key = sha256Hash.ComputeHash(key);
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
					key = sha256Hash.ComputeHash(key);
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
		public static Org.BouncyCastle.X509.X509Certificate CreateCertificateBasedOnCertificateAuthorityPrivateKey(
			string subjectName,
			string data,
			string issuerName,
			AsymmetricKeyParameter issuerPrivKey,
			AsymmetricKeyParameter requestorPublicKey
			)
		{

		
			// create the factory
			ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerPrivKey);

			// the Certificate Generator
			X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();

			// serial Number
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);
			BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
			certificateGenerator.SetSerialNumber(serialNumber);

			// Issuer and Subject Name
			X509Name subjectDN = new X509Name("CN=" + subjectName);
			X509Name issuerDN = new X509Name("CN=" + issuerName);
			certificateGenerator.SetIssuerDN(issuerDN);
			certificateGenerator.SetSubjectDN(subjectDN);

			// Valid For
			DateTime notBefore = DateTime.UtcNow.Date;
			DateTime notAfter = notBefore.AddDays(397);

			certificateGenerator.SetNotBefore(notBefore);
			certificateGenerator.SetNotAfter(notAfter);

			// add the other data
			if (!String.IsNullOrEmpty(data))
			{	
				string oid = "1.3.6.1.4.1.57055";

				// Create an OID for the custom extension
				DerObjectIdentifier customExtensionOid = new DerObjectIdentifier(oid);

				// Create the OtherName ASN.1 structure
				Asn1EncodableVector otherNameVec = new Asn1EncodableVector();
				otherNameVec.Add(new DerObjectIdentifier(oid)); // OID representing the type of OtherName
				otherNameVec.Add(new DerTaggedObject(true, 0, new DerUtf8String(data))); // The actual data, encoded as desired
				OtherName otherName = OtherName.GetInstance(new DerSequence(otherNameVec));

				// Create a GeneralName with the OtherName
				GeneralNames generalNames = new GeneralNames(new GeneralName(GeneralName.OtherName, otherName.ToAsn1Object()));

				// Add the extension to the certificate generator
				certificateGenerator.AddExtension(customExtensionOid, false, generalNames);
				
			}
			

			// set the public key
			certificateGenerator.SetPublicKey(requestorPublicKey);

			//
			// adding policies
			//
			KeyUsage keyUsage = new KeyUsage(KeyUsage.KeyEncipherment);
			certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, keyUsage);

			certificateGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));

			// generate the certificate
			Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

			return certificate;

		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static string GetCustomExtensionData(X509Certificate cert, string oid)
		{
			// Get the custom extension
			Asn1OctetString asn1OctetStr = cert.GetExtensionValue(new DerObjectIdentifier(oid));
			if (asn1OctetStr == null) return null; // Extension not found

			// Decode the extension
			Asn1Object asn1Object = Asn1Object.FromByteArray(asn1OctetStr.GetOctets());
			GeneralNames generalNames = GeneralNames.GetInstance(asn1Object);

			foreach (GeneralName generalName in generalNames.GetNames())
			{
				if (generalName.TagNo == GeneralName.OtherName)
				{
					Asn1Sequence otherNameSeq = (Asn1Sequence)generalName.Name.ToAsn1Object();
					OtherName otherName = OtherName.GetInstance(otherNameSeq);

					if (otherName.TypeID.ToString() == oid)
					{
						Asn1Encodable otherNameValue = otherName.Value;
						// Assuming the data is a DER UTF8String
						DerUtf8String utf8String = DerUtf8String.GetInstance((Asn1TaggedObject)otherNameValue, false);
						return utf8String.GetString();
					}
				}
			}

			return null; // Data not found or format not as expected
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		//
		// create a self signed CA certificate
		//
		public static Org.BouncyCastle.X509.X509Certificate CreateCertificateAuthorityCertificate(
			string subjectName,
			ref AsymmetricCipherKeyPair subjectKeyPairCA
			)
		{
			

			// Generating Random Numbers
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);

			// The Certificate Generator
			X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();

			// Serial Number
			BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
			certificateGenerator.SetSerialNumber(serialNumber);

			// Issuer and Subject Name
			X509Name subjectDN = new X509Name("CN=" + "root." + subjectName);
			X509Name issuerDN = new X509Name("CN=" + GLOBALS.origin);
			certificateGenerator.SetIssuerDN(issuerDN);
			certificateGenerator.SetSubjectDN(subjectDN);

			// Valid For
			DateTime notBefore = DateTime.UtcNow.Date;
			DateTime notAfter = notBefore.AddYears(20);

			certificateGenerator.SetNotBefore(notBefore);
			certificateGenerator.SetNotAfter(notAfter);

			// Subject Public Key
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, Defines.caKeyStrength);
			RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			subjectKeyPairCA = keyPairGenerator.GenerateKeyPair();


			certificateGenerator.SetPublicKey(subjectKeyPairCA.Public);

			// Generating the Certificate
			AsymmetricCipherKeyPair issuerKeyPair = subjectKeyPairCA;
			ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerKeyPair.Private);

			//
			// adding policies
			//
			KeyUsage keyUsage = new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign);
			certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, keyUsage);

			certificateGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(true));

			// selfsign certificate
			Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

			return certificate;
		}
		// --------------------------------------------------------------------------------------------------------
		public static Org.BouncyCastle.X509.X509Certificate CreateSubCertificateAuthorityCertificate(
			string subjectName,
			ref AsymmetricCipherKeyPair subjectKeyPairSubCA,
			AsymmetricCipherKeyPair subjectKeyPairCA
			)
		{


			// Generating Random Numbers
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);

			// The Certificate Generator
			X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();

			// Serial Number
			BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
			certificateGenerator.SetSerialNumber(serialNumber);

			// Issuer and Subject Name
			X509Name subjectDN = new X509Name("CN=" + subjectName);
			X509Name issuerDN = new X509Name("CN=" + GLOBALS.origin);
			certificateGenerator.SetIssuerDN(issuerDN);
			certificateGenerator.SetSubjectDN(subjectDN);

			// Valid For
			DateTime notBefore = DateTime.UtcNow.Date;
			DateTime notAfter = notBefore.AddYears(20);

			certificateGenerator.SetNotBefore(notBefore);
			certificateGenerator.SetNotAfter(notAfter);

			// Subject Public Key
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, Defines.caKeyStrength);
			RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			subjectKeyPairSubCA = keyPairGenerator.GenerateKeyPair();


			certificateGenerator.SetPublicKey(subjectKeyPairSubCA.Public);

			// Generating the Certificate
			AsymmetricCipherKeyPair issuerKeyPair = subjectKeyPairSubCA;
			ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", subjectKeyPairCA.Private);

			//
			// adding policies
			//
			KeyUsage keyUsage = new KeyUsage(KeyUsage.KeyCertSign | KeyUsage.CrlSign);
			certificateGenerator.AddExtension(X509Extensions.KeyUsage, true, keyUsage);

			certificateGenerator.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(true));

			

			// selfsign certificate
			Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

			return certificate;
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static string toPEM (object o)
		{
			StringBuilder CertPem = new StringBuilder();
			PemWriter CSRPemWriter = new PemWriter(new StringWriter(CertPem));
			CSRPemWriter.WriteObject(o);
			CSRPemWriter.Writer.Flush();

			
			return CertPem.ToString();
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static object fromPEM(string p)
		{
			using (TextReader textReader = new StringReader(p))
			{
				PemReader pemReader = new PemReader(textReader);
				return pemReader.ReadObject();
			}		
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
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

					if (!child.IssuerDN.Equivalent(parent.SubjectDN))
					{
						throw new CertificateException("*** ERROR: Issuer/Subject DN mismatch ***");
					}

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
					
				}

				// get the fingerprint of the root certificate
				byte[] fingerprint = GetFingerprint(intermediateAndRootCertificatePems[intermediateAndRootCertificatePems.Count-1]);

				//DisplayVisualFingerprint(fingerprint);
				//CertificateFingerprint.DisplayCertificateFingerprintFromString(fingerprint);

				return (true, fingerprint);
			}
			catch (Exception ex)
			{
				return (false, new byte[0]);
			}
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static Org.BouncyCastle.X509.X509Certificate ReadCertificateFromPemString(string pemString)
		{
			using StringReader reader = new StringReader(pemString);
			PemReader pemReader = new PemReader(reader);
			return (Org.BouncyCastle.X509.X509Certificate)pemReader.ReadObject();
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
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
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static byte[] GetFingerprint(string pemString)
		{
			// make sure the certificate is valid
			Org.BouncyCastle.X509.X509Certificate cert = ReadCertificateFromPemString(pemString);

			// now get the fingerprint
			using SHA256 sha256 = SHA256.Create();
			byte[] hashBytes = sha256.ComputeHash(pemString.ToBytes());
			return hashBytes;
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		private static Asn1Object? GetAsn1Object(Org.BouncyCastle.X509.X509Certificate certificate, DerObjectIdentifier oid)
		{
			Asn1OctetString akiBytes = certificate.GetExtensionValue(oid);
			if (akiBytes == null) 
			{
				return null;
			}

			return Asn1Object.FromByteArray(akiBytes.GetOctets());
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static async Task<(bool, byte[])> VerifyAliasAsync(string domain, string alias)
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

			if (valid)
				return (true, fingerprint);
			else
				return (false, new byte[0]);
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static bool VerifySignature(byte[] message, byte[] signature, AsymmetricKeyParameter publicKey)
		{
			var signer = SignerUtilities.GetSigner("SHA512WITHRSA");
			signer.Init(false, publicKey);
			signer.BlockUpdate(message, 0, message.Length);

			return signer.VerifySignature(signature);
					
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static byte[] GetHashOfString(string s)
		{
			using SHA256 sha256 = SHA256.Create();
			byte[] hashBytes = sha256.ComputeHash(s.ToBytes());
			return hashBytes;
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static string ConvertHashToString(byte[] hash)
		{
			// Convert the byte array to a hexadecimal string
			string hashString = BitConverter.ToString(hash);

			// Remove the hyphens
			hashString = hashString.Replace("-", "");

			return hashString;
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
	}
}
