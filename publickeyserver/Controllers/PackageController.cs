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
	public class PackageController : ControllerBase
	{
		private readonly ILogger<Controller> _logger;
		private const int ChunkSize = 1024 * 1024; // 1MB chunk size

		public PackageController(ILogger<Controller> logger)
		{
			_logger = logger;
		}

		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpPost("{recipient}")]
		public async Task<IActionResult> UploadPackage(string sender, string recipient, string timestamp, string signature)
		{
			try
			{
				// signature is of the following:
				// sender + recipient + timestamp + origin
				// host is bare eg. publickeyserver.org

				// get the url host header
				string host = Request.Host.Host;

				// create a unique package name
				string packageName = Guid.NewGuid().ToString();
				byte[] packageHash = BouncyCastleHelper.GetHashOfString(packageName);
				string package = BouncyCastleHelper.ConvertHashToString(packageHash);

				string key = $"packages/{recipient}/{package}";

				string result = await PackageHelper.ValidateSenderAndRecipient(sender, recipient, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				//
				// so if we have got here the sender and recipient are valid
				// now we need to save the file
				//

				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var initiateRequest = new InitiateMultipartUploadRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key
					};

					var initResponse = await _s3Client.InitiateMultipartUploadAsync(initiateRequest);

					var partNumber = 1;
					var uploadResponses = new List<UploadPartResponse>();

					byte[] buffer = new byte[ChunkSize];
					int bytesRead;
					while ((bytesRead = await Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
					{
						var memStream = new MemoryStream(buffer, 0, bytesRead);
						var uploadRequest = new UploadPartRequest
						{
							BucketName = GLOBALS.s3bucket,
							Key = key,
							UploadId = initResponse.UploadId,
							PartNumber = partNumber++,
							PartSize = bytesRead,
							InputStream = memStream
						};

						var uploadResponse = await _s3Client.UploadPartAsync(uploadRequest);
						uploadResponses.Add(uploadResponse);
					}

					var completeRequest = new CompleteMultipartUploadRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key,
						UploadId = initResponse.UploadId,
						PartETags = uploadResponses.Select(r => new PartETag(r.PartNumber, r.ETag)).ToList()
					};

					await _s3Client.CompleteMultipartUploadAsync(completeRequest);

					return Ok($"File {key} uploaded successfully in {partNumber - 1} parts");
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
		[HttpGet("{recipient}/{package}")]
		public async Task<IActionResult> DownloadPackage(string package, string recipient, string timestamp, string signature)
		{
			try
			{
				// get the url host header
				string host = Request.Host.Host;
				string key = $"packages/{recipient}/{package}";

				string result = await PackageHelper.ValidateRecipient(recipient, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var request = new GetObjectRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key
					};

					using var response = await _s3Client.GetObjectAsync(request);
					Response.ContentType = response.Headers.ContentType;
					Response.ContentLength = response.Headers.ContentLength;
					Response.Headers.Append("Content-Disposition", $"attachment; filename={package}");

					await using var responseStream = response.ResponseStream;
					var buffer = new byte[ChunkSize];
					int bytesRead;
					while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
					{
						await Response.Body.WriteAsync(buffer, 0, bytesRead);
						await Response.Body.FlushAsync();
					}

					return new FileStreamResult(Response.Body, response.Headers.ContentType);
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