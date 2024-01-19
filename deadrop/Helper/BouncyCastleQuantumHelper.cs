
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
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;

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

		return keyPair;
	}
	// --------------------------------------------------------------------------------------------------------
	public static AsymmetricCipherKeyPair GenerateDilithiumKeyPair()
	{
		//
		// generate a Dilithium keypair using bouncy castle
		//
		var random = new SecureRandom();
		var keyGenParameters = new DilithiumKeyGenerationParameters(random, DilithiumParameters.Dilithium5);

		var dilithiumKeyPairGenerator = new DilithiumKeyPairGenerator();
		dilithiumKeyPairGenerator.Init(keyGenParameters);
		var keyPair = dilithiumKeyPairGenerator.GenerateKeyPair();

		return keyPair;
	}
	// --------------------------------------------------------------------------------------------------------
	public static (byte[], byte[]) ReadKyberKeyPair(AsymmetricCipherKeyPair keyPair)
	{
		KyberPublicKeyParameters KyberPublicKey = (KyberPublicKeyParameters)keyPair.Public;
		KyberPrivateKeyParameters KyberPrivateKey = (KyberPrivateKeyParameters)keyPair.Private;

		byte[] KyberPublicKeyDer = KyberPublicKey.GetEncoded();
		byte[] KyberPrivateKeyDer = KyberPrivateKey.GetEncoded();

		return (KyberPublicKeyDer, KyberPrivateKeyDer);
	}
	// --------------------------------------------------------------------------------------------------------
	public static (byte[], byte[]) ReadDilithiumKeyPair(AsymmetricCipherKeyPair keyPair)
	{
		DilithiumPublicKeyParameters DilithiumPublicKey = (DilithiumPublicKeyParameters)keyPair.Public;
		DilithiumPrivateKeyParameters DilithiumPrivateKey = (DilithiumPrivateKeyParameters)keyPair.Private;

		byte[] DilithiumPublicKeyDer = DilithiumPublicKey.GetEncoded();
		byte[] DilithiumPrivateKeyDer = DilithiumPrivateKey.GetEncoded();

		return (DilithiumPublicKeyDer, DilithiumPrivateKeyDer);
	}
	// --------------------------------------------------------------------------------------------------------
	public static string ReadKyberPublicDerFromKey(KyberPublicKeyParameters KyberPublicKey)
	{
		byte[] KyberPublicKeyDer = KyberPublicKey.GetEncoded();
		string sKyberPublicKeyDer = Convert.ToBase64String(KyberPublicKeyDer);

		return sKyberPublicKeyDer;
	}
	// --------------------------------------------------------------------------------------------------------
	public static string ReadKyberPrivateDerFromKey(KyberPrivateKeyParameters KyberPrivateKey)
	{
		byte[] KyberPrivateKeyDer = KyberPrivateKey.GetEncoded();
		string sKyberPrivateKeyDer = Convert.ToBase64String(KyberPrivateKeyDer);

		return sKyberPrivateKeyDer;
	}
	// --------------------------------------------------------------------------------------------------------
	public static string ReadDilithiumPublicDerFromKey(DilithiumPublicKeyParameters DilithiumPublicKey)
	{
		byte[] DilithiumPublicKeyDer = DilithiumPublicKey.GetEncoded();
		string sDilithiumPublicKeyDer = Convert.ToBase64String(DilithiumPublicKeyDer);

		return sDilithiumPublicKeyDer;
	}
	// --------------------------------------------------------------------------------------------------------
	public static string ReadDilithiumPrivateDerFromKey(DilithiumPrivateKeyParameters DilithiumPrivateKey)
	{
		byte[] DilithiumPrivateKeyDer = DilithiumPrivateKey.GetEncoded();
		string sDilithiumPrivateKeyDer = Convert.ToBase64String(DilithiumPrivateKeyDer);

		return sDilithiumPrivateKeyDer;
	}
	// --------------------------------------------------------------------------------------------------------
}