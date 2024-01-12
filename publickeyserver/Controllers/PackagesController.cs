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


namespace publickeyserver
{
	[ApiController]
	[Route("api/[controller]")]
	public class PackagesController : ControllerBase
	{
		private readonly ILogger<Controller> _logger;
		private const int ChunkSize = 1024 * 1024; // 1MB chunk size

		public PackagesController(ILogger<Controller> logger)
		{
			_logger = logger;
		}
		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpGet("{recipient}/list")]
		public async Task<IActionResult> ListPackages(string recipient, string timestamp, string signature)
		{
			try
			{
				string host = Request.Host.Host;
				string key = $"packages/{recipient}";

				string result = await PackageHelper.ValidateRecipient(recipient, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var request = new ListObjectsV2Request
					{
						BucketName = GLOBALS.s3bucket,
						Prefix = key
					};

					var response = await _s3Client.ListObjectsV2Async(request);

					var files = new List<object>();
					foreach (var obj in response.S3Objects)
					{
						files.Add(new
						{
							FileName = obj.Key,
							Size = obj.Size,
							LastModified = obj.LastModified
						});
					}

					return Ok(files);
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
		}
		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpGet("{recipient}/status")]
		public async Task<IActionResult> StatusPackages(string recipient, string timestamp, string signature)
		{
			try
			{
				string host = Request.Host.Host;
				string key = $"packages/{recipient}";

				string result = await PackageHelper.ValidateRecipient(recipient, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				(int count, long totalSize) = await PackageHelper.GetPackagesStatus(key);

				return Ok(new
				{
					Count = count,
					TotalSize = totalSize
				});
				
			}
			catch (AmazonS3Exception e)
			{
				return BadRequest($"Error encountered on server. Message:'{e.Message}'");
			}
			catch (System.Exception e)
			{
				return BadRequest($"Unknown error encountered on server. Message:'{e.Message}'");
			}
		}
		// ------------------------------------------------------------------------------------------------------------
	}
}