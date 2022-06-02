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

		// ---------------------------------------------------------------------
		// returns the CA cert in x.509
		// will create one if one does not already exist
		// ---------------------------------------------------------------------
		[Route("cacerts")]
		[Produces("application/x-x509-ca-cert")]
		[HttpGet]
		public async Task<IActionResult> cacerts()
		{
			// lets check to see if we have a cacert
			X509Certificate2 ca;
			byte[] raw;
			try
			{
				raw = System.IO.File.ReadAllBytes("cacert.pfx");

				// test to make sure we can read it
				ca = new X509Certificate2(raw);

			}
			catch (Exception e)
			{

#if DEBUG
				// one doesn't exist, so create it the first time
				AsymmetricCipherKeyPair subjectKeyPairCA = null;

				Console.WriteLine("Creating CA for development");
				ca = BouncyCastleHelper.CreateCertificateAuthorityCertificate("CN=" + "publickeyserver.org", ref subjectKeyPairCA, "");
				raw = ca.Export(X509ContentType.Pkcs12);
				System.IO.File.WriteAllBytes("cacert.pfx", raw);

				TextWriter textWriter = new StringWriter();
				PemWriter pemWriter = new PemWriter(textWriter);
				pemWriter.WriteObject(subjectKeyPairCA);
				pemWriter.Writer.Flush();
				System.IO.File.WriteAllText("cakeys.pem", textWriter.ToString());
#else
				Log.Error("Unable to get cacert", e);
				return StatusCode(StatusCodes.Status500InternalServerError, "Unable to get ca certs");
#endif
			}

			MemoryStream stream = new MemoryStream(raw);

			return Ok(stream);
		}
		// ---------------------------------------------------------------------
		[Route("cert")]
		[Produces("application/x-pkcs12")]
		[HttpGet]
		public async Task<IActionResult> cert(string alias)
		{
			byte[] raw = null;
			try
			{
				
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, alias);
				};
			}
			catch (Exception e)
			{
				Log.Error("Unable to get cert", e);
				return StatusCode(StatusCodes.Status500InternalServerError, "Unable to get cert");
			}

			MemoryStream stream = new MemoryStream(raw);

			return Ok(stream);
		}
		// ---------------------------------------------------------------------
		// returns a new x.509 certificate with the common name set to a random
		// set of three words and signed by the root CA
		// ---------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/x-pkcs12")]
		[HttpPost]
		public async Task<IActionResult> simpleenroll([FromBody] CreateKey createkey)
		{
			try
			{
				string key = createkey.key;
				List<string>[] servers = createkey.servers;
				List<string>[] data = createkey.data;

				//
				// create a three letter word that hasn't been used before
				//
				string alias = "";
				while (true)
				{
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
				}

				//
				// get the certificate private key
				//
				TextReader textReader = new StringReader(System.IO.File.ReadAllText("cakeys.pem"));
				PemReader pemReader = new PemReader(textReader);
				AsymmetricCipherKeyPair subjectKeyPairCA = (AsymmetricCipherKeyPair)pemReader.ReadObject();


				//
				// now create the certificate
				//
				Console.WriteLine("Creating Certificate - " + alias);
				X509Certificate2 cert = BouncyCastleHelper.CreateSelfSignedCertificateBasedOnCertificateAuthorityPrivateKey(alias, servers, data, "publickeyserver.org", subjectKeyPairCA.Private);
				byte[] raw = cert.Export(X509ContentType.Pkcs12);

				//
				// save the certificate
				//
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					using (var ms = new MemoryStream(raw))
					{
						var uploadRequest = new TransferUtilityUploadRequest
						{
							InputStream = ms,
							Key = alias,
							BucketName = GLOBALS.s3bucket,
							CannedACL = S3CannedACL.PublicRead
						};

						var fileTransferUtility = new TransferUtility(client);
						await fileTransferUtility.UploadAsync(uploadRequest);

						return Ok(ms);
					}

				};
			}
			catch (Exception e)
			{
				Log.Error("Unable to get cert", e);
				return StatusCode(StatusCodes.Status500InternalServerError, "Unable to create cert");
			}

		}
		// ---------------------------------------------------------------------
		// creates a private/public key pair in PEM format on behalf of the user
		// not secure, essentially used for testing
		// ---------------------------------------------------------------------
		[Route("keys")]
		[Produces("application/pem-certificate-chain")]
		[HttpGet]
		public Stream keys()
		{
			byte[] byteArray = Encoding.ASCII.GetBytes("");
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;
		}
		// ---------------------------------------------------------------------
	}
}
