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
	public class PackageHelper
	{
		// ------------------------------------------------------------------------------------------------------------
		public PackageHelper()
		{
		
		}
		// ------------------------------------------------------------------------------------------------------------
		public async Task<(int count, long totalSize)> SumPackages(string recipient)
		{
			string key = $"{GLOBALS.origin}/packages/{recipient}";

			using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
			{
				var request = new ListObjectsV2Request
				{
					BucketName = GLOBALS.s3bucket,
					Prefix = key
				};

				long totalSize = 0;
				int fileCount = 0;

				ListObjectsV2Response response;
				do
				{
					response = await _s3Client.ListObjectsV2Async(request);
					foreach (var obj in response.S3Objects)
					{
						totalSize += obj.Size; // Accumulate the total size
						fileCount++; // Increment the file count
					}

					// Set the continuation token to continue the listing
					request.ContinuationToken = response.NextContinuationToken;
				} while (response.IsTruncated); // Continue while there are more objects

				return (fileCount, totalSize );
			}
		}
		// ------------------------------------------------------------------------------------------------------------
		public static async Task<string> ValidateRecipient(string recipient, string host, string signature, string timestamp)
		{
#if !DEBUG
			// make sure the host header matches the origin
			if (!host.EndsWith(GLOBALS.origin))
				return $"Host {host} is not part of the global domain {GLOBALS.origin}";

			if (!recipient.EndsWith(GLOBALS.origin))
				return $"Recipient {recipient} is not part of the global domain {GLOBALS.origin}";
#endif
			string internalSig = $"{recipient}{timestamp}{GLOBALS.origin}".ToLower();

			string domain = GLOBALS.origin;
#if DEBUG
			domain = host;
#endif

			// get the public key of the recipient
			var toX509 = await GetCertificate(domain, recipient);
			AsymmetricKeyParameter publicKey;
			if (toX509 != null)
			{
				publicKey = toX509.GetPublicKey();
			}
			else
			{
				return $"Could not get certificate for alias {recipient}";
			}

			// now validate that the signature is valid of the recipient
			if (!BouncyCastleHelper.VerifySignature(internalSig.ToBytes(), Convert.FromBase64String(signature), publicKey))
			{
				return $"Signature is not valid for request {signature}, format is recipient + timestamp(UTC seconds since epoch) + origin";
			}

			// validate the recipient
			(bool recipientValid, byte[] recipientRootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, recipient);
			if (!recipientValid)
				return $"Alias {recipient} is not valid";

			// verify threat the unix timestamp is within 10 minutes of now
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dt = dt.AddSeconds(Convert.ToDouble(timestamp));
			if (dt > DateTime.UtcNow.AddMinutes(10) || dt < DateTime.UtcNow.AddMinutes(-10))
			{
				return $"Timestamp must be within +/- 10 minutes of UTC";
			}

			// now make sure the recipient is valid
			return "";
			
		}
		// ------------------------------------------------------------------------------------------------------------
		public static async Task<string> ValidateSenderAndRecipient(string sender, string recipient, string host, string signature, string timestamp)
		{
#if !DEBUG
			// make sure the host header matches the origin
			if (!host.EndsWith(GLOBALS.origin))
				return $"Host {host} is not part of the global domain {GLOBALS.origin}";
#endif
			// signature is of the following:
			string internalSig = $"{sender}{recipient}{timestamp}{GLOBALS.origin}".ToLower();

			// make sure the sender and recipient are part of the global domain
			if (!sender.EndsWith(GLOBALS.origin))
				return $"Sender {sender} is not part of the global domain {GLOBALS.origin}";
			
			if (!recipient.EndsWith(GLOBALS.origin))
				return $"Recipient {recipient} is not part of the global domain {GLOBALS.origin}";

			// validate the sender
			string domain = GLOBALS.origin;
#if DEBUG
			domain = host;
#endif
			(bool senderValid, byte[] senderRootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, sender);
			if (!senderValid)
				return $"Alias {sender} is not valid";

			// validate the recipient
			(bool recipientValid, byte[] recipientRootFingerprint) = await BouncyCastleHelper.VerifyAliasAsync(domain, recipient);
			if (!recipientValid)
				return $"Alias {recipient} is not valid";

			// now validate that they share the root certificate
			if (!senderRootFingerprint.SequenceEqual(recipientRootFingerprint))
			{
				return $"Aliases do not share the same root certificate {sender} -> {recipient}";
			}

			// get the public key of the sender
			var toX509 = await PackageHelper.GetCertificate(domain, sender);
			AsymmetricKeyParameter publicKey;
			if (toX509 != null)
			{
				publicKey = toX509.GetPublicKey();
			}
			else
			{
				return $"Could not get certificate for alias {sender}";
			}
			try
			{
				// now validate that the signature is valid of the recipient
				if (!BouncyCastleHelper.VerifySignature(internalSig.ToBytes(), Convert.FromBase64String(signature), publicKey))
				{
					return $"Signature is not valid for request {signature}, format is sender + recipient + timestamp(UTC seconds since epoch) + origin";
				}
			}
			catch (Exception ex)
			{
				return $"Could not verify signature {ex.Message}";
			}

			// verify threat the unix timestamp is within 10 minutes of now
			DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
			dt = dt.AddSeconds(Convert.ToDouble(timestamp));
			if (dt > DateTime.UtcNow.AddMinutes(10) || dt < DateTime.UtcNow.AddMinutes(-10))
			{
				return $"Timestamp must be within +/- 10 minutes of UTC";
			}

			return "";
		}
		// ------------------------------------------------------------------------------------------------------------
		public static async Task<(int count, long totalSize)> GetPackagesStatus(string key)
		{
			using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
			{
				var request = new ListObjectsV2Request
				{
					BucketName = GLOBALS.s3bucket,
					Prefix = key
				};

				long totalSize = 0;
				int fileCount = 0;

				ListObjectsV2Response response;
				do
				{
					response = await _s3Client.ListObjectsV2Async(request);
					foreach (var obj in response.S3Objects)
					{
						totalSize += obj.Size; // Accumulate the total size
						fileCount++; // Increment the file count
					}

					// Set the continuation token to continue the listing
					request.ContinuationToken = response.NextContinuationToken;
				} while (response.IsTruncated); // Continue while there are more objects

				return (fileCount, totalSize);
			}
		}
		// ------------------------------------------------------------------------------------------------------------
		public static async Task<X509Certificate> GetCertificate(string domain, string alias)
		{

			// now get the "from" alias	
			string result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

			var c = System.Text.Json.JsonSerializer.Deserialize<CertResult>(result) ?? throw new Exception("Could not deserialize cert result");
			var certificate = c.Certificate ?? throw new Exception("Could not get certificate from cert result");

			return BouncyCastleHelper.ReadCertificateFromPemString(certificate);
		}
		// ------------------------------------------------------------------------------------------------------------
	}
}