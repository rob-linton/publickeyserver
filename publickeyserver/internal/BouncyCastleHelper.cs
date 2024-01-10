using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

				DerObjectIdentifier endpoint_internal_name_ID = new DerObjectIdentifier(oid);
				GeneralNames endpoint_internal_name = new GeneralNames(new GeneralName(GeneralName.OtherName, data));
				certificateGenerator.AddExtension(endpoint_internal_name_ID, false, endpoint_internal_name);
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
		public static bool AddCertificateToSpecifiedStore(X509Certificate2 cert, StoreName st, StoreLocation sl)
		{
			bool bRet = false;

			try
			{
				X509Store store = new X509Store(st, sl);
				store.Open(OpenFlags.ReadWrite);
				store.Add(cert);

				store.Close();
			}
			catch
			{
				Console.WriteLine("An error occured");
			}

			return bRet;
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------

	}
}
