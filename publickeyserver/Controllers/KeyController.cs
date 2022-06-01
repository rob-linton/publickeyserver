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
		[Route("cacert")]
		[Produces("application/x-x509-ca-cert")]
		[HttpGet]
		public Stream cacerts()
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
				// one doesn't exist, so create it the first time
				AsymmetricCipherKeyPair subjectKeyPairCA = null;

				Console.WriteLine("Creating CA");
				ca = BouncyCastleHelper.CreateCertificateAuthorityCertificate("CN=" + "publickeyserver.org", ref subjectKeyPairCA, "");
				raw = ca.Export(X509ContentType.Pkcs12);
				System.IO.File.WriteAllBytes("cacert.pfx", raw);

				TextWriter textWriter = new StringWriter();
				PemWriter pemWriter = new PemWriter(textWriter);
				pemWriter.WriteObject(subjectKeyPairCA);
				pemWriter.Writer.Flush();
				System.IO.File.WriteAllText("cakeys.pem", textWriter.ToString());
			}

			MemoryStream stream = new MemoryStream(raw);

			return stream;
		}
		// ---------------------------------------------------------------------
		/*
		 * Cert lookup is not done here
		 * 
		[Route("cert")]
		[HttpGet]
		public Stream cert(string alias)
		{
			byte[] byteArray = Encoding.ASCII.GetBytes(alias);
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;
		}
		*/
		// ---------------------------------------------------------------------
		// returns a new x.509 certificate with the common name set to a random
		// set of three words and signed by the root CA
		// ---------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/x-pkcs12")]
		[HttpPost]
		public Stream simpleenroll()
		{

			byte[] byteArray = Encoding.ASCII.GetBytes("");
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;

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
			byte[] byteArray = Encoding.ASCII.GetBytes('');
			MemoryStream stream = new MemoryStream(byteArray);

			return stream;
		}
		// ---------------------------------------------------------------------
	}
}
