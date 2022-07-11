using System;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;

namespace publickeyserver
{
	public class ControllerHelper
	{
		public ControllerHelper()
		{
		}

		public static AsymmetricCipherKeyPair GenerateKey()
		{
			AsymmetricCipherKeyPair privatekeyACP = null;

			// Generating Random Numbers
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);

			// generate key pair
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, Defines.keyStrength);
			RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			privatekeyACP = keyPairGenerator.GenerateKeyPair();

			return privatekeyACP;

		}
	}
}

