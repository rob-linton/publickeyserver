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
using deadrop;


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

			ret["application"] = "deadrop";
			ret["version"] = GLOBALS.version;
			ret["origin"] = GLOBALS.origin;
			ret["uptime"] = Math.Round(runtime.TotalSeconds);
			ret["certs-served"] = GLOBALS.status_certs_served;
			ret["certs-enrolled"] = GLOBALS.status_certs_enrolled;

			ret["max-bucket-files"] = Convert.ToInt64(GLOBALS.MaxBucketFiles);
			ret["max-bucket-size"] = Convert.ToInt64(GLOBALS.MaxBucketSize);
			ret["max-package-size"] = Convert.ToInt64(GLOBALS.MaxPackageSize);


			// return email allowed
			ret["anonymous"] = GLOBALS.Anonymous.ToString();
			//ret["allowed_email_domains"] = GLOBALS.AllowedEmailDomains;

			// lets check to see if we have a cacert
			Org.BouncyCastle.X509.X509Certificate ca;
			string certPEM = "";

			try
			{
				byte[] cacertBytes = System.IO.File.ReadAllBytes($"subcacert.{GLOBALS.origin}.pem");

				byte[] cacertDecrypted = BouncyCastleHelper.DecryptWithKey(cacertBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());

				certPEM = cacertDecrypted.FromBytes();


				// test to make sure we can read it
				ca = (Org.BouncyCastle.X509.X509Certificate)BouncyCastleHelper.fromPEM(certPEM);

				// get the signature
				byte[] fingerprint = await BouncyCastleHelper.GetCaRootFingerprint(GLOBALS.origin);
				List<string> sig = Misc.GenerateSignature(fingerprint);

				// serialise sig to a json string
				ret["root-ca-signature"] = sig;

				ret["status"] = "OK";

			}
			catch
			{
				ret["status"] = "NOT OK";
			}


			Response.StatusCode = StatusCodes.Status200OK;
			

			return new JsonResult(ret);
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// returns the CA cert in x.509
		// will create one if one does not already exist
		// both the cacert and the RSA2048 keys are encrypted with AES-GCM on disk with the provided password
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("cacerts")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> CaCerts()
		{

			// lets check to see if we have a cacert
			Org.BouncyCastle.X509.X509Certificate ca;
			string certPEM = "";
			string subcertPEM = "";

			try
			{
				byte[] cacertBytes = System.IO.File.ReadAllBytes($"cacert.{GLOBALS.origin}.pem");
				byte[] cacertDecrypted = BouncyCastleHelper.DecryptWithKey(cacertBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());
				certPEM = cacertDecrypted.FromBytes();

				byte[] subcacertBytes = System.IO.File.ReadAllBytes($"subcacert.{GLOBALS.origin}.pem");
				byte[] subcacertDecrypted = BouncyCastleHelper.DecryptWithKey(subcacertBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());
				subcertPEM = subcacertDecrypted.FromBytes();

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

			cacerts.Add(subcertPEM);
			cacerts.Add(certPEM);
			ret["origin"] = GLOBALS.origin;
			ret["cacerts"] = cacerts;
			ret["help"] = Help.cacerts;

			return new JsonResult(ret);
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// get a cert from the cert store using the alias
		// in this case an S3 bucket
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("cert/{alias}")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> Cert(string alias)
		{
			try
			{
				alias = alias + "." + GLOBALS.origin;

				byte[] raw;
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, $"cert/{alias}.pem");
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
		// get a cert from the cert store using the email
		// in this case an S3 bucket
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("email/{email}")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> CertUsingEmail(string email)
		{
			try
			{
				List<S3File> aliasList;
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					aliasList = await AwsHelper.List(client, $"email/{email}");
				};

				// loop through the list and get the newest s3file object
				long oldest = 0;
				string key = "";
				foreach (var s3file in aliasList)
				{
					string name = s3file.Name;
					long timestamp = s3file.Timestamp;

					if (timestamp > oldest)
					{
						oldest = timestamp;
						key = name;
					}
				}

				string[] parts = key.Split("/");
				string lastKey = parts[parts.Length - 1];
				lastKey = lastKey.Replace(".pem", "");
				string alias = lastKey;

				byte[] raw;
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, $"email/{email}/{alias}.pem");
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
		// get a cert from the cert store using the alias
		// in this case an S3 bucket
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("{*aliasIn}")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> CertWildcard(string aliasIn)
		{
			try
			{
				string alias = "";

				// get the host string
				HostString host = Request.Host;

				// remove the origin
				string[] bits = host.ToString().Replace(GLOBALS.origin, "").Split('.');

				// take the host name if it was passed in at the front
				string shortAlias = "";
				if (bits.Length == 2)
				{
					alias = bits[0] + "." + GLOBALS.origin;
					shortAlias = bits[0];
				}
				else if (aliasIn != "index.html")
				{
					// else take the end of the url
					alias = aliasIn + "." + GLOBALS.origin;
				}

				// return the index.html page if the host was empty and the url was empty
				if (aliasIn == "index.html" && alias == "")
				{
					string index = System.IO.File.ReadAllText("wwwroot/index.html");
					index = index.Replace("{CERTIFICATE}", "");
					return new ContentResult()
					{
						Content = index,
						ContentType = "text/html",
					};

				}

				byte[] raw;
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					raw = await AwsHelper.Get(client, $"cert/{alias}.pem");
				};

				string cert = raw.FromBytes();

				//
				// we now return the info page
				//

				List<string> certificate = new List<string>();
				certificate.Add("<br>");
				certificate.Add("<br>");
				certificate.Add($"Alias: {alias}");
				certificate.Add("<br>");

				// convert the cert to an x.509 bouncy castle
				Org.BouncyCastle.X509.X509Certificate x509 = (Org.BouncyCastle.X509.X509Certificate)BouncyCastleHelper.fromPEM(cert);

				// get the subject
				string subject = x509.SubjectDN.ToString();
				certificate.Add($"Subject: {subject}");

				// get the issuer
				string issuer = x509.IssuerDN.ToString();
				certificate.Add($"Issuer: {issuer}");

				// get the expiry
				string expiry = x509.NotAfter.ToString("dd-MMM-yyyy hh.mmtt");
				certificate.Add($"Not After: {expiry}");

				// not before
				string notbefore = x509.NotBefore.ToString("dd-MMM-yyyy hh.mmtt");
				certificate.Add($"Not Before: {notbefore}");

				// get the serial number
				string serial = x509.SerialNumber.ToString();
				certificate.Add($"Serial: {serial}");

				// get the subject alternative names
				List<string> san = BouncyCastleHelper.GetAltNames(cert);
				foreach (var s in san)
				{
					certificate.Add($"Subject Alternative Name: {s.ToString()}");
				}

				bool recipientValid = false;
				byte[] recipientRootFingerprint = new byte[32];
				try
				{
					(recipientValid, recipientRootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(GLOBALS.origin, alias);
				}
				catch { }

				List<string> sig = Misc.GenerateSignature(recipientRootFingerprint);


				// create a string from the list
				string certInfo = "";
				foreach (string line in certificate)
				{
					certInfo += line + "<br>";
				}

				certInfo = certInfo + $"<br><a href=\"https://{host}/cert/{shortAlias}\">Full Certificate</a>";

				certInfo = certInfo + $"<br><br>Certificate Valid: {recipientValid}<br>";

				certInfo = certInfo + $"<br><br>Root Signature:<br>----------------<br>";
				// add the signature
				foreach (string line in sig)
				{
					certInfo += line + "<br>";
				}
				
				string index2 = System.IO.File.ReadAllText("wwwroot/index.html");
				index2 = index2.Replace("{CERTIFICATE}", certInfo);
				return new ContentResult()
				{
					Content = index2,
					ContentType = "text/html",
				};


				/*
				Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["certificate"] = cert;
				ret["help"] = Help.cert;

				GLOBALS.status_certs_served++;
				return new JsonResult(ret);
				*/
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
		//
		// takes:
		// {
		//   "key" : "RSA2048 Public key in PEM format",
		//   "data": "Arbitary data in JSON format"
		// }
		//
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/json")]
		[HttpPost]
		public async Task<IActionResult> SimpleEnroll([FromServices] IWords words)
		{
			try
			{
				// we use our own custom code to get the body instead of [FromBody]
				// as this is brittle and doesn't allow line feeds etc in the key
				dynamic createkey = Misc.GetRequestBodyDynamic(Request);

				//
				// get the user public key
				//
				AsymmetricKeyParameter? publickeyRequestor = null;
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
				string dataBase64 = createkey.data;
				
				// un base64 the data
				byte[] bytes = Convert.FromBase64String(dataBase64);
				string data = Encoding.UTF8.GetString(bytes);

				// convert to a dictionary
				CustomExtensionData emailTokenData = JsonConvert.DeserializeObject<CustomExtensionData>(data) ?? new CustomExtensionData();

				string email = emailTokenData.Email ?? "";
				string token = emailTokenData.Token ?? "";

				// check if this email is allowed
				if (GLOBALS.Anonymous == false && String.IsNullOrEmpty(email))
				{
					return Misc.err(Response, "Anonymous aliases not allowed on this server", Help.simpleenroll);
				}
				if (GLOBALS.Anonymous == false)
				{
					if (Misc.IsAllowedEmail(email) == false)
						return Misc.err(Response, "Invalid email address", Help.simpleenroll);
				}

				if (email.Length > 0 && token.Length > 0)
				{
					// validate the token
					string tokenFile = $"tokens/{email}.token";
					string tokenFileContents = "";
					try
					{
						byte[] raw;
						using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
						{
							raw = await AwsHelper.Get(client, tokenFile);
						};
						EmailToken emailTokenFile = JsonConvert.DeserializeObject<EmailToken>(Encoding.UTF8.GetString(raw) ?? "") ?? new EmailToken();
						
						long timestamp = Convert.ToInt64(emailTokenFile.Timestamp);

						// get the current timestamp
						long unixTimestampFile = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

						// check if it is older than 1 hour, if it is then delete it
						// else return the response that token exists
						if (unixTimestampFile - timestamp > 3600)
						{
							// delete the old token
							using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
							{
								await AwsHelper.Delete(client, tokenFile);
							};
						}
						else
						{
							tokenFileContents = emailTokenFile.Token ?? "";
						}
					}
					catch
					{
						return Misc.err(Response, "Invalid token", Help.simpleenroll);
					}

					if (tokenFileContents != token)
					{
						return Misc.err(Response, "Invalid token", Help.simpleenroll);
					}
					else
					{
						// delete the token file and continue *success*
						using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
						{
							await AwsHelper.Delete(client, tokenFile);
						};
					}
				}
				else if (email.Length > 0 && token.Length == 0)
				{
						return Misc.err(Response, "Email verification code must be provided when associating an email address", Help.simpleenroll);
				}
				else if (email.Length == 0 && token.Length > 0)
				{
					return Misc.err(Response, "Email address required", Help.simpleenroll);
				}

				// create the cert
				var ret = await KeyHelper.CreateKey(publickeyRequestor, words, dataBase64, email);

				GLOBALS.status_certs_enrolled++;
				return Ok(ret);

			}
			catch (Exception e)
			{
				Log.Error(e, "Unable to create cert");
				return Misc.err(Response, "Unable to create cert", Help.simpleenroll);
			}

		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// returns a new x.509 certificate with the common name set to a random
		// set of three words and signed by the root CA
		// not secure, essentially used for testing
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("simpleenroll")]
		[Produces("application/json")]
		[HttpGet]
		public async Task<IActionResult> SimpleEnrollGet([FromServices] IWords words)
		{

			try
			{
				AsymmetricCipherKeyPair privatekeyACP = ControllerHelper.GenerateKey();
				AsymmetricKeyParameter publickeyRequestor = privatekeyACP.Public;

				// get a three word alias
				string alias = await ControllerHelper.GenerateAlias(GLOBALS.origin, GLOBALS.s3endpoint, GLOBALS.s3key, GLOBALS.s3secret, words);

				//
				// get the CA private key
				//
				byte[] cakeysBytes = System.IO.File.ReadAllBytes($"subcakeys.{GLOBALS.origin}.pem");
				byte[] cakeysDecrypted = BouncyCastleHelper.DecryptWithKey(cakeysBytes, GLOBALS.password.ToBytes(), GLOBALS.origin.ToBytes());
				string cakeysPEM = cakeysDecrypted.FromBytes();
				AsymmetricCipherKeyPair cakeys = (AsymmetricCipherKeyPair)BouncyCastleHelper.fromPEM(cakeysPEM);

				//
				// now create the certificate
				//
				Log.Information("Creating Certificate - " + alias);
				Org.BouncyCastle.X509.X509Certificate cert = BouncyCastleHelper.CreateCertificateBasedOnCertificateAuthorityPrivateKey(alias, "", "", GLOBALS.origin, cakeys.Private, publickeyRequestor);

				// convert to PEM
				string certPEM = BouncyCastleHelper.toPEM(cert);

				Log.Information(certPEM);

				//
				// save the certificate in S3
				//
				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					await AwsHelper.Put(client, $"cert/{alias}.pem", certPEM.ToBytes());
				}


				Response.StatusCode = StatusCodes.Status200OK;
				Dictionary<string, string> ret = new Dictionary<string, string>();

				ret["alias"] = alias;
				ret["origin"] = GLOBALS.origin;
				ret["publickey"] = BouncyCastleHelper.toPEM(publickeyRequestor);
				ret["privatekey"] = BouncyCastleHelper.toPEM(privatekeyACP);
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
			AsymmetricCipherKeyPair privatekeyACP = ControllerHelper.GenerateKey();

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
		// deletes a public key pair in PEM format on behalf of the user
		// -------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("{alias}")]
		[Produces("application/json")]
		[HttpDelete]
		public async Task<IActionResult> DeleteCert(string alias, string timestamp, string signature)
		{
			alias = alias + "." + GLOBALS.origin;

			string key = $"cert/{alias}.pem";
			try
			{
				// fix the signature
				signature = signature.Replace(" ", "+");

				// get the url host header
				string port = "";
				if (Request.Host.Port != null)
					port = ":" + Request.Host.Port;
				string host = Request.Host.Host + port;
				
				string result = await PackageHelper.ValidateRecipient(alias, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var request = new DeleteObjectRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key
					};

					var response = await _s3Client.DeleteObjectAsync(request);
					return Ok();
				}
			}
			catch (AmazonS3Exception e)
			{
				return BadRequest($"Error encountered on server. Message:'{e.Message}'");
			}
			catch (Exception e)
			{
				return BadRequest($"Unknown error encountered on server. Message:'{e.Message}'");
			}
			finally
			{
				Response.Body.Close();
			}
		}
		// ------------------------------------------------------------------------------------------------------------------------------------------------------
		// deletes a public key pair and email in PEM format on behalf of the user
		// -------------------------------------------------------------------------------------------------------------------------------------------------------
		[Route("email/{email}/{alias}")]
		[Produces("application/json")]
		[HttpDelete]
		public async Task<IActionResult> DeleteCertEmail(string alias, string email, string timestamp, string signature)
		{
			string longAlias = alias + "." + GLOBALS.origin;

			try
			{
				// fix the signature
				signature = signature.Replace(" ", "+");

				// get the url host header
				string port = "";
				if (Request.Host.Port != null)
					port = ":" + Request.Host.Port;
				string host = Request.Host.Host + port;
				
				string result = await PackageHelper.ValidateRecipient(longAlias, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				string key = $"email/{email}/{longAlias}.pem";
				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var request = new DeleteObjectRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key
					};

					var response = await _s3Client.DeleteObjectAsync(request);
				}

				key = $"cert/{longAlias}.pem";
				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var request = new DeleteObjectRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key
					};

					var response = await _s3Client.DeleteObjectAsync(request);
					
				}

				return Ok();

			}
			catch (AmazonS3Exception e)
			{
				return BadRequest($"Error encountered on server. Message:'{e.Message}'");
			}
			catch (Exception e)
			{
				return BadRequest($"Unknown error encountered on server. Message:'{e.Message}'");
			}
			finally
			{
				Response.Body.Close();
			}
		}
		// ---------------------------------------------------------------------------------------------------------------
	}
}
