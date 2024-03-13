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
	[Route("[controller]")]
	public class LookupController : ControllerBase
	{
		private readonly ILogger<Controller> _logger;
		private const int ChunkSize = 1024 * 1024; // 1MB chunk size

		public LookupController(ILogger<Controller> logger)
		{
			_logger = logger;
		}
		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpGet("{recipient}")]
		public async Task<IActionResult> List(string recipient)
		{
			try
			{
				string key = $"cert/{recipient}";

				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					// get a list of objects in the bucket
					var request = new ListObjectsV2Request
					{
						BucketName = GLOBALS.s3bucket,
						Prefix = key,
						MaxKeys = 10
					};

					ListResult files;
					ListObjectsV2Response response;
					do
					{
						response = await _s3Client.ListObjectsV2Async(request);

						// Process the response.
						List<ListFile> listFiles = new List<ListFile>();
						foreach (S3Object entry in response.S3Objects)
						{
							ListFile listFile = new ListFile () {
								Key = entry.Key,
								Size = entry.Size,
								LastModified = entry.LastModified
							};

							// do something with entry
							listFiles.Add(listFile);
						}
						files = new ListResult()
						{ 
							Files = listFiles,
							Count = listFiles.Count,
							Size = (long)listFiles.Sum(x => x.Size)
						};
						
						// If response is truncated, set the marker to get the next 
						// set of keys.
						if (response.IsTruncated)
						{
							request.ContinuationToken = response.NextContinuationToken;
						}
						else
						{
							request = null;
						}
					} while (request != null);

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
	}
}