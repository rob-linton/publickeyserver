using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Dilithium;
using Org.BouncyCastle.Security;

namespace jdoe;

public class BouncyCastleHelper
{
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

		return keyPair;
    }
}
