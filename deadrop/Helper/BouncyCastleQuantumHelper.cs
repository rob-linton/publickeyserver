
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
using deadrop.Verbs;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;

namespace deadrop;

public class BouncyCastleQuantumHelper
{

	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a pair of asymmetric cryptographic keys, consisting of a public key and a private key.
	/// </summary>
	public static AsymmetricCipherKeyPair GenerateKyberKeyPair()
	{
		//
		// generate a kyber keypair using bouncy castle
		//
		var random = new SecureRandom();
		var keyGenParameters = new KyberKeyGenerationParameters(random, KyberParameters.kyber1024);

		var kyberKeyPairGenerator = new KyberKeyPairGenerator();
		kyberKeyPairGenerator.Init(keyGenParameters);

		// generate key pair for Alice
		AsymmetricCipherKeyPair keyPair = kyberKeyPairGenerator.GenerateKeyPair();

		/* debugging
		// Write the private key to a file
		using (TextWriter privateKeyTextWriter = new StringWriter())
		{
			PemWriter pemWriter = new PemWriter(privateKeyTextWriter);
			pemWriter.WriteObject(keyPair.Private);
			pemWriter.Writer.Flush();

			File.WriteAllText("privateKey.pem", privateKeyTextWriter.ToString());
		}

		// Write the public key to a file
		using (TextWriter publicKeyTextWriter = new StringWriter())
		{
			PemWriter pemWriter = new PemWriter(publicKeyTextWriter);
			pemWriter.WriteObject(keyPair.Public);
			pemWriter.Writer.Flush();

			File.WriteAllText("publicKey.pem", publicKeyTextWriter.ToString());
		}
		*/

		return keyPair;
	}
}