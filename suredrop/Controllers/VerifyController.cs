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
using suredrop;


namespace publickeyserver
{
	[ApiController]
	[Route("[controller]")]
	public class VerifyController : ControllerBase
	{
		private readonly ILogger<Controller> _logger;
		private const int ChunkSize = 1024 * 1024; // 1MB chunk size

		public VerifyController(ILogger<Controller> logger)
		{
			_logger = logger;
		}
		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpPost("email/{identity}")]
		public async Task<IActionResult> Verify(string identity, bool intro = false)
		{
			try
			{

				// check if the identity is valid
				if (Misc.IsAllowedIdentity(identity) == false)
					return Misc.err(Response, "Invalid identity address", Help.simpleenroll);

				// need to generate the token and identity to the identity address
				// generate a token
				string tokenFile = $"{GLOBALS.origin}/tokens/{identity}.token";
				
				bool generateToken = false;
				try
				{
					byte[] raw;
					using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
					{
						raw = await AwsHelper.Get(client, tokenFile);
					};
					var t = Encoding.UTF8.GetString(raw ?? throw new Exception("Token file not found"));
					IdentityToken? identityTokenFile = JsonConvert.DeserializeObject<IdentityToken>(t);
					long timestamp = Convert.ToInt64(identityTokenFile!.Timestamp);

					// get the current timestamp
					long unixTimestampFile = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

					// check if it is older than 1 hour, if it is then allow it to be resent
					// else return the response that token exists
					if (unixTimestampFile - timestamp > 3600)
					{
						generateToken = true;

						// delete the old token
						using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
						{
							await AwsHelper.Delete(client, tokenFile);
						};
					}
				}
				catch 
				{ 
					generateToken = true; 
				}

				// send a response that the token has already been sent
				if (generateToken == false)
					return Misc.err(Response, "Identity verification code already sent to identity address, please wait 1 hour between verification requests to the same identity", Help.simpleenroll);
			
				// no token file, so generate one
				string tokenFileContentsNew = Misc.GetRandomString(8);
				IdentityToken identityToken = new IdentityToken();
				identityToken.Identity = identity;
				identityToken.Token = tokenFileContentsNew;

				// get a unix timestamp in utc
				long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
				identityToken.Timestamp = unixTimestamp.ToString();

				string dataNew = JsonConvert.SerializeObject(identityToken);

				byte[] tokenFileContentsNewBytes = Encoding.UTF8.GetBytes(dataNew);

				using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					await AwsHelper.Put(client, tokenFile, tokenFileContentsNewBytes);
				};

				// now send the identity
				string identityBody = $"Your identity verification code is {tokenFileContentsNew}";
				string identitySubject = $"Your identity verification code for {GLOBALS.origin}";
				string identityFrom = GLOBALS.identityFrom;
				string identityTo = identity;

				// send the identity
				await EmailHelper.SendEmail(identityFrom, identityTo, identitySubject, identityBody);

				return Ok("Verification code sent to identity address");
					
			}
			catch (AmazonS3Exception e)
			{
				return BadRequest($"Error encountered on server. Message:'{e.Message}'");
			}
			catch (Exception e)
			{
				return BadRequest($"Unknown error encountered on server. Message:'{e.Message}'");
			}
		}
		// ------------------------------------------------------------------------------------------------------------
	}
}