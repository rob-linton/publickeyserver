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

namespace publickeyserv.Controllers
{
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
		[Route("cacert")]
		[HttpGet]
		public Stream cacert()
		{
			byte[] byteArray = Encoding.ASCII.GetBytes("");
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;
		}
		// ---------------------------------------------------------------------
		[Route("cert")]
		[HttpGet]
		public Stream cert(string alias)
		{
			byte[] byteArray = Encoding.ASCII.GetBytes(alias);
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;
		}
		// ---------------------------------------------------------------------
		[Route("simpleenroll")]
		[HttpPost]
		public Stream simpleenroll()
		{

			dynamic body = Misc.GetRequestBodyDynamic(Request);

			string key = body["key"];

			foreach (JProperty kv in body)
			{
				if (kv.Name.ToLower().Trim() != "key")
				{
					Log.Information("OID => {0} : {1}", kv.Name, kv.Value.ToString());
				}

			}


			string alias = "";

			using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
			{
				while (true)
				{
					// Generate a random first name
					var randomizerFirstName = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Min = 3, Max = 3 });
					alias = randomizerFirstName.Generate();

					// check if exists in s3

					//bool exists = AwsHelper.Exists(client, alias).Result;
					bool exists = false;
					
					if (!exists)
						break;
				};
				
			}
		

			byte[] byteArray = Encoding.ASCII.GetBytes(alias);
			MemoryStream stream = new MemoryStream(byteArray);



			//
			// create the certificate
			//

			string x509Base64 = ""; // final certificate base64 encoded

			// temporarily create a new CA self signed certificate
			(X509Certificate2 x509CA, AsymmetricKeyParameter myCAprivateKey) = BouncyCastleHelper.InitCA("publickeyserver");

			// create a new certificate
			X509Certificate2 certificate = BouncyCastleHelper.CreateSelfSignedCertificateBasedOnCertificateAuthorityPrivateKey("CN=" + alias, "CN=" + "publickeyserver" + "CA", myCAprivateKey);

			//
			// save the certificate and the api details
			//
			using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
			{
				bool exists = AwsHelper.Put(client, alias, x509Base64).Result;
			};


			return stream;


			//return cert;

		}
		// ---------------------------------------------------------------------
	}
}

/*
 * "pubkey":"[base64 public key]",
    "api":"[api of app],
    "appname":"[name of app]",
*/