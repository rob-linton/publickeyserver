using System;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
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

		// ------------------------------------------------------------------------------------------------------------------
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
		// ------------------------------------------------------------------------------------------------------------------
		public async static Task<string> GenerateAlias(string origin, string s3endpoint, string s3key, string s3secret, IWords words)
		{
			//
			// create a three letter word that hasn't been used before
			//
			string alias = "";

			int max = 0;
			while (true)
			{
				if (max > 10)
				{
					throw new Exception("Exceeded 10 attempts to get a unique alias");
				}

				// Generate a random first name
				//var randomizerFirstName = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Min = 3, Max = 3 });
				//alias = randomizerFirstName.Generate().Replace(" ", ".");
				alias = words.GetAlias(origin);


				// check if exists in s3
				using (var client = new AmazonS3Client(s3key, s3secret, RegionEndpoint.GetBySystemName(s3endpoint)))
				{
					try
					{
						bool exists = await AwsHelper.Exists(client, $"{alias}.pem");

						if (!exists)
							break;
					}
					catch 
					{
						// doesn't exist
						break;
					}
				};

				max++;
			}

			return alias;
		}
		// ------------------------------------------------------------------------------------------------------------------
	}
}

