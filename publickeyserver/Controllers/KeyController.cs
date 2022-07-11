#pragma warning disable CS1998

using System;
using System.Collections.Generic;
using System.IO;
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
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.OpenSsl;
using Newtonsoft.Json;
using System.Diagnostics;

namespace publickeyserver
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

	// simpleenroll accepts a POST (supply your own public key in PEM RSA2048 and
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
		// returns the simple key server status
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("status")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> Status()
		{

			TimeSpan runtime = DateTime.Now - Process.GetCurrentProcess().StartTime;

			Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();

			ret["origin"] = GLOBALS.origin;
			ret["uptime"] = runtime.Seconds;
			ret["certs_served"] = GLOBALS.status_certs_served;
			ret["certs_enrolled"] = GLOBALS.status_certs_enrolled;

			// lets check to see if we have a cacert
			Org.BouncyCastle.X509.X509Certificate ca;
			string certPEM = "";

			try
			{
				byte[] cacertBytes = System.IO.File.ReadAllBytes($"cacert.{GLOBALS.origin}.pem");

				byte[] cacertDecrypted = BouncyCastleHelper.DecryptWithKey(cacertBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());

				certPEM = cacertDecrypted.FromBytes();


				// test to make sure we can read it
				ca = (Org.BouncyCastle.X509.X509Certificate)BouncyCastleHelper.fromPEM(certPEM);

				ret["status"] = "OK";

			}
			catch (Exception e)
			{
				ret["status"] = "NOT OK";
			}


			Response.StatusCode = StatusCodes.Status200OK;
			

			return new JsonResult(ret);
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

			// lets check to see if we have a cacert
			Org.BouncyCastle.X509.X509Certificate ca;
			string certPEM = "";

			try
			{
				byte[] cacertBytes = System.IO.File.ReadAllBytes($"cacert.{GLOBALS.origin}.pem");

				byte[] cacertDecrypted = BouncyCastleHelper.DecryptWithKey(cacertBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());

				certPEM = cacertDecrypted.FromBytes();


				// test to make sure we can read it
				ca = (Org.BouncyCastle.X509.X509Certificate)BouncyCastleHelper.fromPEM(certPEM);

			}
			catch (Exception e)
			{
				Log.Error(e, "No CA Certificate");
				return Misc.err(Response, "No CA Certificat", Help.cacerts);
			}


			Response.StatusCode = StatusCodes.Status200OK;
			Dictionary<string, dynamic> ret = new Dictionary<string, dynamic>();

			List<string> cacerts = new List<string>();

			cacerts.Add(certPEM);
			ret["origin"] = GLOBALS.origin;
			ret["cacerts"] = cacerts;
			ret["help"] = Help.cacerts;

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
			try
			{
				byte[] raw;
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, $"{alias}.pem");
				};

				string cert = raw.FromBytes();


				Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["certificate"] = cert;
				ret["help"] = Help.cert;

				GLOBALS.status_certs_served++;
				return new JsonResult(ret);

			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to get cert");
				return Misc.err(Response, "Unable to get cert", Help.cert);
			}
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// returns a new x.509 certificate with the common name set to a random
		// set of three words and signed by the root CA
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/json")]
		[HttpPost]
		public async Task<IActionResult> SimpleEnroll([FromServices] IWords words)
		{
			try
			{
				dynamic createkey = Misc.GetRequestBodyDynamic(Request);

				//
				// get the user public key
				//
				AsymmetricKeyParameter publickeyRequestor = null;
				try
				{
					string key = createkey.key;
					publickeyRequestor = (AsymmetricKeyParameter)BouncyCastleHelper.fromPEM(key);

				}
				catch
				{
					return Misc.err(Response, "Invalid JSON key parameter. A public key in PEM format is mandatory.", Help.simpleenroll);
				}


				//
				// get the list of optional data
				//
				string data = createkey.data;
				string dataBase64 = "";
				if (!String.IsNullOrEmpty(data))
				{
					dynamic json = JsonConvert.DeserializeObject(data);

					Byte[] dataBytes = Encoding.UTF8.GetBytes(data);
					dataBase64 = Convert.ToBase64String(dataBytes);
				}

				


				//
				// create a three letter word that hasn't been used before
				//
				string alias = "";
				
				int max = 0;
				while (true)
				{
					if (max > 10)
					{
						return Misc.err(Response, "Exceeded 10 attempts to get a unique alias", Help.simpleenroll);
					}

					// Generate a random first name
					//var randomizerFirstName = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Min = 3, Max = 3 });
					//alias = randomizerFirstName.Generate().Replace(" ", ".");
					alias = words.GetAlias(GLOBALS.origin);


					// check if exists in s3
					using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
					{
						try
						{
							bool exists = await AwsHelper.Exists(client, $"{alias}.{GLOBALS.origin}.pem");
							
							if (!exists)
								break;
						}
						catch (Exception e)
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
				byte[] cakeysBytes = System.IO.File.ReadAllBytes($"cakeys.{GLOBALS.origin}.pem");
				byte[] cakeysDecrypted = BouncyCastleHelper.DecryptWithKey(cakeysBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());
				string cakeysPEM = cakeysDecrypted.FromBytes();
				AsymmetricCipherKeyPair cakeys = (AsymmetricCipherKeyPair)BouncyCastleHelper.fromPEM(cakeysPEM);

				//
				// now create the certificate
				//
				Log.Information("Creating Certificate - " + alias);
				Org.BouncyCastle.X509.X509Certificate cert = BouncyCastleHelper.CreateCertificateBasedOnCertificateAuthorityPrivateKey(alias, dataBase64, GLOBALS.origin, cakeys.Private, publickeyRequestor);

				// convert to PEM
				string certPEM = BouncyCastleHelper.toPEM(cert);

				Log.Information(certPEM);

				//
				// save the certificate in S3
				//
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					await AwsHelper.Put(client, $"{alias}.pem", certPEM.ToBytes());
				}


				Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["publickey"] = BouncyCastleHelper.toPEM(publickeyRequestor);
				ret["certificate"] = certPEM;
				ret["help"] = Help.simpleenroll;


				GLOBALS.status_certs_enrolled++;
				return new JsonResult(ret);

			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to create cert");
				return Misc.err(Response, "Unable to create cert", Help.simpleenroll);
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
			AsymmetricCipherKeyPair privatekeyACP = null;

			// Generating Random Numbers
			CryptoApiRandomGenerator randomGenerator = new CryptoApiRandomGenerator();
			SecureRandom random = new SecureRandom(randomGenerator);

			// generate key pair
			KeyGenerationParameters keyGenerationParameters = new KeyGenerationParameters(random, Defines.keyStrength);
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
			ret["help"] = Help.serverkeygen;

			return new JsonResult(ret);

		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
	}
}
