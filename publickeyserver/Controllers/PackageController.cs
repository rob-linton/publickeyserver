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
		private const int ChunkSize = 1024 * 1024; // 1MB chunk size

		public PackageController(ILogger<Controller> logger)
		{
			_logger = logger;
		}

		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpPost("{sender, senderSignature, recipient, recipientSignature}")]
		public async Task<IActionResult> UploadFile(string sender, string senderSignature, string recipient, string recipientSignature)
		{
			// create a unique package name
			string package = Guid.NewGuid().ToString();

			try
			{
				// make sure the sender and recipient are part of the global domain
				if (!sender.EndsWith(GLOBALS.origin))
					return BadRequest($"Sender {sender} is not part of the global domain {GLOBALS.origin}");
				
				if (!recipient.EndsWith(GLOBALS.origin))
					return BadRequest($"Recipient {recipient} is not part of the global domain {GLOBALS.origin}");

				// validate the sender
				(bool senderValid, byte[] senderRootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(GLOBALS.origin, sender);
				if (!senderValid)
					return BadRequest($"Alias {sender} is not valid");

				// validate the recipient
				(bool recipientValid, byte[] recipientRootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(GLOBALS.origin, recipient);
				if (!recipientValid)
					return BadRequest($"Alias {recipient} is not valid");

				// now validate that they share the root certificate
				if (!senderRootFingerprint.SequenceEqual(recipientRootFingerprint))
				{
					return BadRequest($"Aliases do not share the same root certificate {sender} -> {recipient}");
				}

				// get the public key of the sender
				var toX509 = await Misc.GetCertificate(sender);
				AsymmetricKeyParameter publicKey;
				if (toX509 != null)
				{
					publicKey = toX509.GetPublicKey();
				}
				else
				{
					return BadRequest($"Could not get certificate for alias {sender}");
				}
				
				// now validate that the signature is valid of the sender
				if (!BouncyCastleHelper.VerifySignature(sender.ToBytes(), senderSignature.ToBytes(), publicKey))
				{
					return BadRequest($"Signature is not valid for sender {sender}");
				}

				// now validate that the signature is valid of the recipient
				if (!BouncyCastleHelper.VerifySignature(recipient.ToBytes(), recipientSignature.ToBytes(), publicKey))
				{
					return BadRequest($"Signature is not valid for recipient {recipient}");
				}

				//
				// so if we have got here the sender and recipient are valid
				// now we need to save the file
				//

				// set the key
				string key = $"{package}/{filename}";

				using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
				{
					var initiateRequest = new InitiateMultipartUploadRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = filename
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
							Key = filename,
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
						Key = filename,
						UploadId = initResponse.UploadId,
						PartETags = uploadResponses.Select(r => new PartETag(r.PartNumber, r.ETag)).ToList()
					};

					await _s3Client.CompleteMultipartUploadAsync(completeRequest);

					return Ok($"File {filename} uploaded successfully in {partNumber - 1} parts");
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
		[HttpGet("{filename}")]
		public async Task<IActionResult> DownloadFile(string filename)
		{
			if (string.IsNullOrEmpty(filename))
				return BadRequest("Filename is not provided.");
		}
		// ------------------------------------------------------------------------------------------------------------
	}
}