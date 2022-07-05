using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Util;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RandomDataGenerator.FieldOptions;
using RandomDataGenerator.Randomizers;
using System.Text;
using Serilog;
using Amazon.S3.Model;
using Newtonsoft.Json.Linq;

using System.Security.Cryptography.X509Certificates;
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
using Org.BouncyCastle.Utilities.Net;
using Org.BouncyCastle.X509.Extension;

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Org.BouncyCastle.OpenSsl;

namespace publickeyserver.Controllers
{
	// ---------------------------------------------------------------------
	//
	// based loosley on RFC 7030
	//

	// differences:
	// returns an x.509
	// accepts a PEM public key
	// no reenroll
	// no CSR
	// pre-set params

	// simpleenroll accepts a POST (supply your own public key in PEM and
	// returns an X.509 without the private key)
	// or GET (returns an x.509 with the private key embedded but created for
	// you)
	// ---------------------------------------------------------------------
	[ApiController]
	//[Route("[controller]")]
	public class KeyController : ControllerBase
	{

		private readonly ILogger<Controller> _logger;

		public KeyController(ILogger<Controller> logger)
		{
			_logger = logger;
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// returns the CA cert in x.509
		// will create one if one does not already exist
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("cacerts")]
		[Produces("application/x-pem-file")]
		[HttpGet]
		public async Task<IActionResult> CaCerts()
		{
			// lets check to see if we have a cacert
			X509Certificate2 ca;
			byte[] rawEncrypted;
			byte[] raw;

			try
			{
				rawEncrypted = System.IO.File.ReadAllBytes("cacert.pfx");

				//
				// now decrypt it
				//

				//TODO///////////////
				raw = rawEncrypted;//
				/////////////////////

				// test to make sure we can read it
				ca = new X509Certificate2(raw);

			}
			catch (Exception e)
			{
				//
				// when debuggin automatically create a cacert and key if we don't already have one
				//

				#if DEBUG

				// one doesn't exist, so create it the first time
				AsymmetricCipherKeyPair subjectKeyPairCA = null;

				Console.WriteLine("Creating CA for development");
				ca = BouncyCastleHelper.CreateCertificateAuthorityCertificate("CN=" + "publickeyserver.org", ref subjectKeyPairCA, "");

				// used to save a binary version of the x.509
				raw = ca.Export(X509ContentType.Pkcs12);
				System.IO.File.WriteAllBytes("cacert.pfx", raw);

				// save the PEM format version of the ca
				using (TextWriter textWriter = new StringWriter())
				{
					PemWriter pemWriter = new PemWriter(textWriter);
					pemWriter.WriteObject(ca);
					pemWriter.Writer.Flush();
					System.IO.File.WriteAllText("cacert.pem", textWriter.ToString());
				}

				using (TextWriter textWriter = new StringWriter())
				{
					PemWriter pemWriter = new PemWriter(textWriter);
					pemWriter.WriteObject(subjectKeyPairCA);
					pemWriter.Writer.Flush();
					System.IO.File.WriteAllText("cakeys.pem", textWriter.ToString());
				}
					
					
				#else

				Log.Error("Unable to get cacerts", e);
				return StatusCode(StatusCodes.Status500InternalServerError, "Unable to get ca certs");

				#endif
			}

			MemoryStream stream = new MemoryStream(raw);

			return Ok(stream);
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// get a cert from the cert store using the alias
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("cert")]
		[Produces("application/x-pem-file")]
		[HttpGet]
		public async Task<IActionResult> Cert(string alias)
		{
			byte[] raw;
			try
			{
				
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, alias);
				};
			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to get cert");
				return StatusCode(StatusCodes.Status500InternalServerError, "Unable to get cert");
			}

			using (MemoryStream stream = new MemoryStream(raw))
			{
				return Ok(stream);
			}
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// returns a new x.509 certificate with the common name set to a random
		// set of three words and signed by the root CA
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/x-pem-file")]
		[HttpPost]
		public async Task<IActionResult> SimpleEnroll([FromBody] CreateKey createkey)
		{
			try
			{


				//
				// get the user public key
				//
				string key = createkey.key;
				AsymmetricCipherKeyPair requestorPublicKey;
				using (TextReader textReader = new StringReader(key))
				{
					PemReader pemReader = new PemReader(textReader);
					requestorPublicKey = (AsymmetricCipherKeyPair)pemReader.ReadObject();
				}

				//
				// get the list of servers
				//
				List<string> servers = new List<string>(); 
				try
				{
					servers = createkey.servers;
				}
				catch { }

				//
				// get the list of optional data
				//
				List<string> data = new List<string>();
				try
				{
					data = createkey.data;
				}
				catch { }
				

				//
				// create a three letter word that hasn't been used before
				//
				string alias = "";
				int max = 0;
				while (true)
				{
					if (max > 1000)
					{
						throw new Exception("Exceeded 1000 attempts to get a unique alias");
					}

					// Generate a random first name
					var randomizerFirstName = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Min = 3, Max = 3 });
					alias = randomizerFirstName.Generate();

					// check if exists in s3
					using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
					{
						bool exists = await AwsHelper.Exists(client, alias);
						if (exists)
							break;
					};

					max++;
				}

				//
				// get the CA private key
				//
				AsymmetricCipherKeyPair subjectKeyPairCA;
				using (TextReader textReader = new StringReader(System.IO.File.ReadAllText("cakeys.pem")))
				{
					PemReader pemReader = new PemReader(textReader);
					subjectKeyPairCA = (AsymmetricCipherKeyPair)pemReader.ReadObject();
				}
					
				//
				// now create the certificate
				//
				Log.Information("Creating Certificate - " + alias);
				X509Certificate2 cert = BouncyCastleHelper.CreateCertificateBasedOnCertificateAuthorityPrivateKey(alias, servers, data, "publickeyserver.org", subjectKeyPairCA.Private, requestorPublicKey.Public);
				byte[] raw = cert.Export(X509ContentType.Pkcs12);

				// get the PEM format version
				string certPEM;
				using (TextWriter textWriter = new StringWriter())
				{
					PemWriter pemWriter = new PemWriter(textWriter);
					pemWriter.WriteObject(cert);
					pemWriter.Writer.Flush();
					certPEM = textWriter.ToString();
				}

				//
				// save the certificate in S3
				//
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					await AwsHelper.Put(client, alias, certPEM.ToBytes());
				}

				// now return it
				using (MemoryStream stream = new MemoryStream(certPEM.ToBytes()))
				{
					return Ok(stream);
				}

			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to create cert");
				return StatusCode(StatusCodes.Status500InternalServerError, "Unable to create cert");
			}

		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// creates a private/public key pair in PEM format on behalf of the user
		// not secure, essentially used for testing
		// -------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("serverkeygen")]
		[Produces("application/x-pem-file")]
		[HttpGet]
		public async Task<IActionResult> ServerKeyGen()
		{
			const int keyStrength = 2048;
			AsymmetricCipherKeyPair keyPair = null;

			// Generating Random Numbers
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);

			// generate key pair
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
			RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			keyPair = keyPairGenerator.GenerateKeyPair();

			string keysPEM;
			using (TextWriter textWriter = new StringWriter())
			{
				PemWriter pemWriter = new PemWriter(textWriter);
				pemWriter.WriteObject(keyPair);
				pemWriter.Writer.Flush();
				keysPEM = textWriter.ToString();
			}

			// now return it
			using (MemoryStream stream = new MemoryStream(keysPEM.ToBytes()))
			{
				return Ok(stream);
			}
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
	}
}
