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
using Org.BouncyCastle.Asn1.Ocsp;
using System.Reflection.Metadata;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Routing;
using Org.BouncyCastle.X509;


namespace publickeyserver
{
	public class KeyHelper
	{
		// ------------------------------------------------------------------------------------------------------------
		public KeyHelper()
		{
		
		}
		// ------------------------------------------------------------------------------------------------------------
		public static async Task<Dictionary<string, string>> CreateKey(AsymmetricKeyParameter publickeyRequestor, IWords words, string dataBase64, string identity, string identityType)
		{
				// get a three word alias
				string alias = await ControllerHelper.GenerateAlias(GLOBALS.origin, GLOBALS.s3endpoint, GLOBALS.s3key, GLOBALS.s3secret, words);

				//
				// get the CA private key
				//
				byte[] cakeysBytes = System.IO.File.ReadAllBytes(Path.Combine(GLOBALS.CertificateDirectory, $"subcakeys.{GLOBALS.origin}.pem"));
				byte[] cakeysDecrypted = BouncyCastleHelper.DecryptWithKey(cakeysBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());
				string cakeysPEM = cakeysDecrypted.FromBytes();
				AsymmetricCipherKeyPair cakeys = (AsymmetricCipherKeyPair)BouncyCastleHelper.fromPEM(cakeysPEM);

				//
				// now create the certificate
				//
				Log.Information("Creating Certificate - " + alias);
				Org.BouncyCastle.X509.X509Certificate cert = BouncyCastleHelper.CreateCertificateBasedOnCertificateAuthorityPrivateKey(alias, identity, dataBase64, GLOBALS.origin, cakeys.Private, publickeyRequestor);

				// convert to PEM
				string certPEM = BouncyCastleHelper.toPEM(cert);

				Log.Information(certPEM);

				//
				// save the certificate in S3
				//
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					await AwsHelper.Put(client, $"{GLOBALS.origin}/cert/{alias}.pem", certPEM.ToBytes());
					if (String.IsNullOrEmpty(identity) == false)
					{
						await AwsHelper.Put(client, $"{GLOBALS.origin}/identity/{identity}/{alias}.pem", certPEM.ToBytes());
					}
				}


				//Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["publickey"] = BouncyCastleHelper.toPEM(publickeyRequestor);
				ret["certificate"] = certPEM;
				ret["help"] = Help.simpleenroll;

				return ret;
		}
		// ------------------------------------------------------------------------------------------------------------
	}
}