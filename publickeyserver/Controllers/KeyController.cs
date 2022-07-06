#pragma warning disable CS1998

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
using System.Net.Mime;

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
	// returns an X.509
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
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> CaCerts()
		{

			string help = "https://github.com/rob-linton/publickeyserver/wiki/Ca-Certs";

			// lets check to see if we have a cacert
			Org.BouncyCastle.X509.X509Certificate ca;
			string certPEM = "";

			try
			{
				certPEM = System.IO.File.ReadAllText("cacert.pem");


				// test to make sure we can read it
				ca = (Org.BouncyCastle.X509.X509Certificate)BouncyCastleHelper.fromPEM(certPEM);

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
				ca = BouncyCastleHelper.CreateCertificateAuthorityCertificate(GLOBALS.origin, ref subjectKeyPairCA);

				certPEM = BouncyCastleHelper.toPEM(ca);
				System.IO.File.WriteAllText("cacert.pem", certPEM);
				System.IO.File.WriteAllText("cakeys.pem", BouncyCastleHelper.toPEM(subjectKeyPairCA));


#else

				Log.Error("Unable to get cacerts", e);
				return Misc.err(Response, "Unable to get cacerts", help);

#endif
			}


			Response.StatusCode = StatusCodes.Status200OK;
			Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();

			List<string> cacerts = new List<string>();

			cacerts.Add(certPEM);
			ret["origin"] = GLOBALS.origin;
			ret["cacerts"] = cacerts;

			return new JsonResult(ret);
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// get a cert from the cert store using the alias
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("cert")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> Cert(string alias)
		{
			const string help = "https://github.com/rob-linton/publickeyserver/wiki/Cert";

			try
			{
				byte[] raw;
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, alias);
				};

				string cert = Encoding.UTF8.GetString(raw);


				Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["certificate"] = cert;

				return new JsonResult(ret);

			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to get cert");
				return Misc.err(Response, "Unable to get cert", help);
			}
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// returns a new x.509 certificate with the common name set to a random
		// set of three words and signed by the root CA
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/json")]
		[HttpPost]
		public async Task<IActionResult> SimpleEnroll()
		{
			string help = "https://github.com/rob-linton/publickeyserver/wiki/Simple-Enroll";

			try
			{
				dynamic createkey = Misc.GetRequestBodyDynamic(Request);

				//
				// get the user public key
				//
				AsymmetricKeyParameter requestorPublicKey = null;
				try
				{
					string key = createkey.key;
					requestorPublicKey = (AsymmetricKeyParameter)BouncyCastleHelper.fromPEM(key);

				}
				catch
				{
					return Misc.err(Response, "Invalid JSON key parameter. A public key in PEM format is mandatory.", help);
				}

				//
				// get the list of servers
				//
				List<string> servers = new List<string>(); 
				try
				{
					servers = createkey.servers;
					if (servers == null)
						servers = new List<string>();
				}
				catch { }

				//
				// get the list of optional data
				//
				List<string> data = new List<string>();
				try
				{
					data = createkey.data;
					if (data == null)
						data = new List<string>();
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
					alias = randomizerFirstName.Generate().Replace(" ", ".");
					

					// check if exists in s3
					using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
					{
						try
						{
							bool exists = await AwsHelper.Exists(client, alias);
						}
						catch
						{
							// doesn't exist
							break;
						}
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
				Org.BouncyCastle.X509.X509Certificate cert = BouncyCastleHelper.CreateCertificateBasedOnCertificateAuthorityPrivateKey(alias, servers, data, GLOBALS.origin, subjectKeyPairCA.Private, requestorPublicKey);

				// convert to PEM
				string certPEM = BouncyCastleHelper.toPEM(cert);

				Log.Information(certPEM);

				//
				// save the certificate in S3
				//
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					//await AwsHelper.Put(client, alias, certPEM.ToBytes());
				}


				Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["publickey"] = BouncyCastleHelper.toPEM(requestorPublicKey);
				ret["certificate"] = certPEM;

				return new JsonResult(ret);

			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to create cert");
				return Misc.err(Response, "Unable to create cert", help);
			}

		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// creates a private/public key pair in PEM format on behalf of the user
		// not secure, essentially used for testing
		// -------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("serverkeygen")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> ServerKeyGen()
		{
			const int keyStrength = 2048;
			AsymmetricCipherKeyPair privatekeyACP = null;

			// Generating Random Numbers
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);

			// generate key pair
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, keyStrength);
			RsaKeyPairGenerator keyPairGenerator = new RsaKeyPairGenerator();
			keyPairGenerator.Init(keyGenerationParameters);
			privatekeyACP = keyPairGenerator.GenerateKeyPair();

			AsymmetricKeyParameter publickeyACP = privatekeyACP.Public;

			// convert to PEM
			string privatekey = BouncyCastleHelper.toPEM(privatekeyACP);
			string publickey = BouncyCastleHelper.toPEM(publickeyACP);

			// now return it


			Response.StatusCode = StatusCodes.Status200OK;
			Dictionary<string, string> ret = new Dictionary<string, string>();

			
			ret["origin"] = GLOBALS.origin;
			ret["publickey"] = publickey;
			ret["privatekey"] = privatekey;

			return new JsonResult(ret);

		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
	}
}
