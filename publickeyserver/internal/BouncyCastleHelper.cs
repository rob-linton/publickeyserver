using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using Org.BouncyCastle.OpenSsl;
using System.IO;
using System.Collections.Generic;

namespace publickeyserver
{
	public class BouncyCastleHelper
	{
		public BouncyCastleHelper()
		{
		}

		// https://stackoverflow.com/questions/36712679/bouncy-castles-x509v3certificategenerator-setsignaturealgorithm-marked-obsolete/50833528

		/*
		static void example()
		{
			//Console.WriteLine(ExecuteCommand("netsh http delete sslcert ipport=0.0.0.0:4443"));
			var applicationId = ((GuidAttribute)typeof(Program).Assembly.GetCustomAttributes(typeof(GuidAttribute), true)[0]).Value;
			var certSubjectName = "TEST";
			var sslCert = ExecuteCommand("netsh http show sslcert 0.0.0.0:4443");
			Console.WriteLine();

			if (sslCert.IndexOf(applicationId, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				Console.WriteLine("This implies we can start running.");
				Console.WriteLine(ExecuteCommand("netsh http delete sslcert ipport=0.0.0.0:4443"));
				//store.Remove(certs.First(x => x.Subject.Contains(certSubjectName)));
			}

			AsymmetricCipherKeyPair subjectKeyPair = null;
			Console.WriteLine("Creating CA");
			X509Certificate2 certificateAuthorityCertificate = CreateCertificateAuthorityCertificate("CN=" + certSubjectName, ref subjectKeyPair, "password");

			Console.WriteLine("Adding CA to Store");
			AddCertificateToSpecifiedStore(certificateAuthorityCertificate, StoreName.Root, StoreLocation.LocalMachine);

			Console.WriteLine("Creating certificate based on CA");
			X509Certificate2 certificate = CreateSelfSignedCertificateBasedOnCertificateAuthorityPrivateKey("CN=" + certSubjectName, new List<string>[0], new List<string>[0], "CN=" + certSubjectName, subjectKeyPair.Private);
			Console.WriteLine("Adding certificate to Store");
			AddCertificateToSpecifiedStore(certificate, StoreName.My, StoreLocation.LocalMachine);

			Console.WriteLine(ExecuteCommand($"netsh http add sslcert ipport=0.0.0.0:4443 certhash={certificate.Thumbprint} appid={{{applicationId}}}"));

			// Check to see if our cert exists
			// If the cert does not exist create it then bind it to the port
			// If the cert does exist then check the port it is bound to
			// If the port and thumbprint match and applicationId match continue
			// Else throw exception
			// See here for more netsh commands https://msdn.microsoft.com/en-us/library/ms733791(v=vs.110).aspx
		}
		*/

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		public static Org.BouncyCastle.X509.X509Certificate CreateCertificateBasedOnCertificateAuthorityPrivateKey(string subjectName, List<string> servers, List<string> data, string issuerName, AsymmetricKeyParameter issuerPrivKey, AsymmetricKeyParameter requestorPublicKey)
		{
			// *************************
			// *** THIS NEEDS REWORK ***
			// *************************

			// take public key
			// take alias
			// take custom data (optional) 
			// take server list (optional)

			//const int keyStrength = 2048;

		
			// create the factory
			ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerPrivKey);

			// The Certificate Generator
			X509V3CertificateGenerator certificateGenerator = new X509V3CertificateGenerator();
			//certificateGenerator.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage((new ArrayList() { new DerObjectIdentifier("1.3.6.1.5.5.7.3.1") })));

			// Serial Number
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);
			BigInteger serialNumber = BigIntegers.CreateRandomInRange(BigInteger.One, BigInteger.ValueOf(Int64.MaxValue), random);
			certificateGenerator.SetSerialNumber(serialNumber);

			// Signature Algorithm
			//const string signatureAlgorithm = "SHA512WITHRSA";
			//certificateGenerator.SetSignatureAlgorithm(signatureAlgorithm);

			// Issuer and Subject Name
			X509Name subjectDN = new X509Name("CN=" + subjectName);
			X509Name issuerDN = new X509Name("CN=" + issuerName);
			certificateGenerator.SetIssuerDN(issuerDN);
			certificateGenerator.SetSubjectDN(subjectDN);

			// Valid For
			DateTime notBefore = DateTime.UtcNow.Date;
			DateTime notAfter = notBefore.AddYears(20);

			certificateGenerator.SetNotBefore(notBefore);
			certificateGenerator.SetNotAfter(notAfter);

			// add the server data
			for (int i = 0; i < servers.Count; i++)
			{
				string server = servers[i];
				string oid = "1.3.6.1.4.1.57055.0." + i.ToString();

				DerObjectIdentifier endpoint_internal_name_ID = new DerObjectIdentifier(oid);
				GeneralNames endpoint_internal_name = new GeneralNames(new GeneralName(GeneralName.OtherName, server));
				certificateGenerator.AddExtension(endpoint_internal_name_ID, false, endpoint_internal_name);
			}


			// add the other data
			for (int i = 0; i < data.Count; i++)
			{
				string dataItem = data[i];
				string oid = "1.3.6.1.4.1.57055.1." + i.ToString();

				DerObjectIdentifier endpoint_internal_name_ID = new DerObjectIdentifier(oid);
				GeneralNames endpoint_internal_name = new GeneralNames(new GeneralName(GeneralName.OtherName, dataItem));
				certificateGenerator.AddExtension(endpoint_internal_name_ID, false, endpoint_internal_name);
			}





			// Subject Public Key
			//AsymmetricCipherKeyPair subjectKeyPair;
			//var keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
			//var keyPairGenerator = new RsaKeyPairGenerator();
			//keyPairGenerator.Init(keyGenerationParameters);
			//subjectKeyPair = keyPairGenerator.GenerateKeyPair();
			//certificateGenerator.SetPublicKey(subjectKeyPair.Public);
			certificateGenerator.SetPublicKey(requestorPublicKey);

			// Generating the Certificate
			//AsymmetricCipherKeyPair issuerKeyPair = subjectKeyPair;

			// selfsign certificate
			Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);

			return certificate;

			// convert into an x509
			//X509Certificate2 x509 = new X509Certificate2(certificate.GetEncoded());
			//return x509;

			// correcponding private key
			//PrivateKeyInfo info = PrivateKeyInfoFactory.CreatePrivateKeyInfo(subjectKeyPair.Private);


			// merge into X509Certificate2
			//X509Certificate2 x509 = new X509Certificate2(certificate.GetEncoded());

			//Asn1Sequence seq = (Asn1Sequence)Asn1Object.FromByteArray(info.ParsePrivateKey().GetDerEncoded());
			//if (seq.Count != 9)
			//{
				//throw new PemException("malformed sequence in RSA private key");
			//}

			//RsaPrivateKeyStructure rsa = RsaPrivateKeyStructure.GetInstance(seq); //new RsaPrivateKeyStructure(seq);
			//RsaPrivateCrtKeyParameters rsaparams = new RsaPrivateCrtKeyParameters(
				//rsa.Modulus, rsa.PublicExponent, rsa.PrivateExponent, rsa.Prime1, rsa.Prime2, rsa.Exponent1, rsa.Exponent2, rsa.Coefficient);

			// ----------
			//var parms = DotNetUtilities.ToRSAParameters(rsaparams as RsaPrivateCrtKeyParameters);
			//var rsa2 = RSA.Create();
			//rsa2.ImportParameters(parms);
			//x509.PrivateKey = rsa2;
			// ----------
			//x509.PrivateKey = DotNetUtilities.ToRSA(rsaparams);
			// ----------

			//return x509;

		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		//
		// create a self signed CA certificate
		//
		public static Org.BouncyCastle.X509.X509Certificate CreateCertificateAuthorityCertificate(string subjectName, ref AsymmetricCipherKeyPair subjectKeyPairCA)
		{
			const int keyStrength = 2048;

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
			DateTime notAfter = notBefore.AddYears(2);

			certificateGenerator.SetNotBefore(notBefore);
			certificateGenerator.SetNotAfter(notAfter);

			// Subject Public Key
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
			RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			subjectKeyPairCA = keyPairGenerator.GenerateKeyPair();


			certificateGenerator.SetPublicKey(subjectKeyPairCA.Public);

			// Generating the Certificate
			AsymmetricCipherKeyPair issuerKeyPair = subjectKeyPairCA;
			ISignatureFactory signatureFactory = new Asn1SignatureFactory("SHA512WITHRSA", issuerKeyPair.Private);
			// selfsign certificate
			Org.BouncyCastle.X509.X509Certificate certificate = certificateGenerator.Generate(signatureFactory);


			return certificate;



			// simple way to convert that does not carry the private key over
			//X509Certificate2 x509 = new X509Certificate2(certificate.GetEncoded());

			// or this is another way
			//byte[] x5092 = DotNetUtilities.ToX509Certificate(certificate).Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs12, "password");

			// old way to carry the private key
			// --------------------------
			/*
			var store = new Pkcs12Store();
			var certificateEntry = new X509CertificateEntry(certificate);
			store.SetCertificateEntry("publickeyserverCA", certificateEntry);
			store.SetKeyEntry("publickeyserverCA", new AsymmetricKeyEntry(subjectKeyPair.Private), new[] { certificateEntry });
			var stream = new MemoryStream();
			store.Save(stream, password.ToCharArray(), random);
			X509Certificate2 x509 = new X509Certificate2(stream.ToArray(), password, X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
			*/
			// --------------------------


			//return x509;

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
