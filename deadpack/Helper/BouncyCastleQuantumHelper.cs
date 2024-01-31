
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
using Org.BouncyCastle.Pqc.Crypto.Utilities;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.BC;
using Org.BouncyCastle.Asn1.Oiw;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Asn1.Eac;

namespace deadrop;

public class BouncyCastleQuantumHelper
{

	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a pair of asymmetric cryptographic keys, consisting of a public key and a private key.
	/// </summary>
	/// <summary>
	/// Represents an asymmetric cipher key pair.
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
	/// <summary>
	/// Represents an asymmetric key pair used for encryption and decryption.
	/// </summary>
	/// <remarks>
	/// The AsymmetricCipherKeyPair class is used to store a pair of asymmetric keys, consisting of a public key and a private key.
	/// These keys are typically used in asymmetric encryption algorithms, where the public key is used for encryption and the private key is used for decryption.
	/// </remarks>
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
	/// <summary>
	/// Reads the Kyber key pair from the given AsymmetricCipherKeyPair.
	/// </summary>
	/// <param name="keyPair">The AsymmetricCipherKeyPair containing the Kyber key pair.</param>
	/// <returns>A tuple containing the public and private keys in PKCS8 format.</returns>
	public static (byte[], byte[]) ReadKyberKeyPair(AsymmetricCipherKeyPair keyPair)
	{
		SubjectPublicKeyInfo i = PqcSubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
		PrivateKeyInfo j = PqcPrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);

		var publicKey = i.ToAsn1Object().GetDerEncoded();
		var privateKey = j.ToAsn1Object().GetDerEncoded();

		return (publicKey, privateKey);
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Reads the Dilithium key pair from the given AsymmetricCipherKeyPair.
	/// </summary>
	/// <param name="keyPair">The AsymmetricCipherKeyPair containing the Dilithium key pair.</param>
	/// <returns>A tuple containing the public and private keys in PKCS8 format.</returns>
	public static (byte[], byte[]) ReadDilithiumKeyPair(AsymmetricCipherKeyPair keyPair)
	{
		SubjectPublicKeyInfo i = PqcSubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public);
		PrivateKeyInfo j = PqcPrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);

		var publicKey = i.ToAsn1Object().GetDerEncoded();
		var privateKey = j.ToAsn1Object().GetDerEncoded();

		return (publicKey, privateKey);
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Kyber public key.
	/// </summary>
	public static KyberPublicKeyParameters WriteKyberPublicKey(byte[] publicKey)
	{
		var key = (KyberPublicKeyParameters)PqcPublicKeyFactory.CreateKey(publicKey);

		return key;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Kyber private key.
	/// </summary>
	public static KyberPrivateKeyParameters WriteKyberPrivateKey(byte[] privateKey)
	{
		var key = (KyberPrivateKeyParameters)PqcPrivateKeyFactory.CreateKey(privateKey);

		return key;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Dilithium public key.
	/// </summary>
	public static DilithiumPublicKeyParameters WriteDilithiumPublicKey(byte[] publicKey)
	{
        var key = (DilithiumPublicKeyParameters)PqcPublicKeyFactory.CreateKey(publicKey);

		return key;
	}
	// --------------------------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a Dilithium private key.
	/// </summary>
	public static DilithiumPrivateKeyParameters WriteDilithiumPrivateKey(byte[] privateKey)
	{
		var key = (DilithiumPrivateKeyParameters)PqcPrivateKeyFactory.CreateKey(privateKey);

		return key;
	}
	
}