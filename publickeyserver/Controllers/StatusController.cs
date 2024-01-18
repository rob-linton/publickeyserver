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
	public class StatusController : ControllerBase
	{
		private readonly ILogger<Controller> _logger;
		private const int ChunkSize = 1024 * 1024; // 1MB chunk size

		public StatusController(ILogger<Controller> logger)
		{
			_logger = logger;
		}
		
		// ------------------------------------------------------------------------------------------------------------
		[Produces("application/json")]
		[HttpGet("{recipient}")]
		public async Task<IActionResult> Status(string recipient, string timestamp, string signature)
		{
			try
			{
				signature = signature.Replace(" ", "+");
				
				string host = Request.Host.Host + ":" + Request.Host.Port;
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