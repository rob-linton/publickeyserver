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
	[Route("package")]
	public class PackageController : ControllerBase
	{
		private readonly ILogger<Controller> _logger;
		private const int ChunkSizeLimit = 10 * 1024 * 1024; // 10MB chunk size

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
				// fix the signature
				signature = signature.Replace(" ", "+");

				// signature is of the following:
				// sender + recipient + timestamp + origin
				// host is bare eg. publickeyserver.org

				// get the url host header
				string host = Request.Host.Host + ":" + Request.Host.Port;

				// create a unique package name
				string packageName = Guid.NewGuid().ToString();
				byte[] packageHash = BouncyCastleHelper.GetHashOfString(packageName);
				string package = BouncyCastleHelper.ConvertHashToString(packageHash);
				
				// total package size
				long packageSize = Request.ContentLength.Value;
				if (packageSize <= 0)
				{
					return BadRequest("Package size is zero or less");
				}

				string keyLocation = $"packages/{recipient}/";
				string keyFile = $"{package}";

				string result = await PackageHelper.ValidateSenderAndRecipient(sender, recipient, host, signature, timestamp);
				if (!String.IsNullOrEmpty(result))
					return BadRequest(result);

				// check the bucket status
				(int bucketCount, long bucketSize) = await PackageHelper.GetPackagesStatus(recipient);	

				if (packageSize > Convert.ToInt32(GLOBALS.MaxPackageSize))
				{
					await AwsHelper.MultipartUploadBlank(Request);
					return BadRequest($"Package size limit exceeded. Maximum size {GLOBALS.MaxPackageSize} bytes");
				}

				// check the max bucket size
				if (bucketSize + packageSize > Convert.ToInt32(GLOBALS.MaxBucketSize))
				{
					await AwsHelper.MultipartUploadBlank(Request);
					return BadRequest($"Bucket size limit exceeded. Maximum size is {GLOBALS.MaxBucketSize} bytes");
				}

				// check the max bucket count
				if (bucketCount > Convert.ToInt32(GLOBALS.MaxBucketFiles))
				{
					await AwsHelper.MultipartUploadBlank(Request);
					return BadRequest($"Bucket count limit exceeded. Maximum count is {GLOBALS.MaxBucketFiles}");
				}

				//
				// so if we have got here the sender and recipient are valid
				// now we need to save the file
				//

				// if size is larger than chunk size then use multipart upload
				if (packageSize > ChunkSizeLimit)
				{
					try
					{
						string resultMultipart = await AwsHelper.MultipartUpload(keyLocation, keyFile, Request, bucketSize);
						return Ok(resultMultipart);
					}
					catch (Exception ex)
					{
						return BadRequest($"Error uploading file: {ex.Message}");
					}
				}
				else
				{
					try
					{
						string resultSingle = await AwsHelper.SingleUpload(keyLocation, keyFile, Request);
						return Ok(resultSingle);
					}
					catch (Exception ex)
					{
						return BadRequest($"Error uploading file: {ex.Message}");
					}
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
				string host = Request.Host.Host + ":" + Request.Host.Port;
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
					var buffer = new byte[ChunkSizeLimit];
					int bytesRead;
					while ((bytesRead = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
					{
						await Response.Body.WriteAsync(buffer, 0, bytesRead);
						await Response.Body.FlushAsync();
					}

					Log.Information($"File {key} downloaded");
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