using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Serilog;
using System.IO;
using Amazon;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;


namespace publickeyserver
{
	public class AwsHelper
	{
		public AwsHelper()
		{
		}
		// ---------------------------------------------------------------------
		public static async Task<bool> Exists(Amazon.S3.AmazonS3Client client, string key)
		{
			GetObjectMetadataResponse response = null;
			
			var request = new GetObjectMetadataRequest
			{
				BucketName = GLOBALS.s3bucket,
				Key = key
			};


			// If the object doesn't exist then a "NotFound" will be thrown
			response = await client.GetObjectMetadataAsync(request);
			if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
			{
				throw new Exception("Unable to find cert");
			}

			return true;
		}
		// ---------------------------------------------------------------------
		public static async Task Put(Amazon.S3.AmazonS3Client client, string key, byte[] body)
		{
			string safeKey = key.Replace(" ", "-");

			PutObjectResponse response = null;
			try
			{
				using (MemoryStream ms = new MemoryStream())
				{
					ms.Write(body, 0, body.Length);

					PutObjectRequest request = new PutObjectRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = safeKey,
						InputStream = ms
					};

					response = await client.PutObjectAsync(request);
					if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
					{
						throw new Exception("Unable to put cert");
					}
				}
			}
			catch (AmazonS3Exception e)
			{
				Log.Error("HTTP reponse not 200 in AwsHelper.Put: {a}, {b}", response.HttpStatusCode, e.Message);
				throw;
			}
		}
		// ---------------------------------------------------------------------
		public static async Task<byte[]> Get(Amazon.S3.AmazonS3Client client, string key)
		{
			string safeKey = key.Replace(" ", "-");

			GetObjectResponse response = null;
			try
			{
				GetObjectRequest request = new GetObjectRequest
				{
					BucketName = GLOBALS.s3bucket,
					Key = safeKey
				};

				response = await client.GetObjectAsync(request);
				if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
				{
					throw new Exception("Unable to get cert");
				}

				using (response.ResponseStream)
				{
					using (MemoryStream ms = new MemoryStream())
					{

						const int bufferSize = 65536; // 64K

						Byte[] buffer = new Byte[bufferSize];
						int bytesRead = response.ResponseStream.Read(buffer, 0, bufferSize);

						while (bytesRead > 0)
						{
							ms.Write(buffer, 0, bytesRead);
							bytesRead = response.ResponseStream.Read(buffer, 0, bufferSize);
						}

						ms.Position = 0;

						return ms.ToArray();
					}
				}
			}
			catch (AmazonS3Exception e)
			{
				Log.Error("HTTP reponse not 200 in AwsHelper.Get: {a}, {b}", response.HttpStatusCode, e.Message);
				throw;
			}
		}
		// ---------------------------------------------------------------------
		public static async Task<string> MultipartUpload(string keyLocation, string keyFile, HttpRequest Request, long bucketSize)
		{
			string key = keyLocation + keyFile;

			int ChunkSize = 1 * 1024 * 1024; // 1MB chunk size
			const int minChunkSize = 50 * 1024 * 1024; // 50 MB minimum chunk size


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

/*
				long totalBytesRead = 0;
				byte[] buffer = new byte[ChunkSize];
				int bytesRead;
				// only upload when the buffer has more than 5mb of data
				while ((bytesRead = await Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					
					totalBytesRead += bytesRead;
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
*/

				long totalBytesRead = 0;
				
				byte[] buffer = new byte[ChunkSize];
				byte[] accumulatedBuffer = new byte[minChunkSize * 2]; // Buffer to accumulate data
				int bytesRead;
				int accumulatedBytesRead = 0;

				while ((bytesRead = await Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					totalBytesRead += bytesRead;
					//Log.Information($"Adding {bytesRead} bytes to total {totalBytesRead}");

					// Copy data to accumulated buffer
					Array.Copy(buffer, 0, accumulatedBuffer, accumulatedBytesRead, bytesRead);
					accumulatedBytesRead += bytesRead;

					// Check if accumulated data is greater than 5MB
					if (accumulatedBytesRead >= minChunkSize)
					{
						var memStream = new MemoryStream(accumulatedBuffer, 0, accumulatedBytesRead);
						var uploadRequest = new UploadPartRequest
						{
							BucketName = GLOBALS.s3bucket,
							Key = key,
							UploadId = initResponse.UploadId,
							PartNumber = partNumber++,
							PartSize = accumulatedBytesRead,
							InputStream = memStream
						};

						var uploadResponse = await _s3Client.UploadPartAsync(uploadRequest);
						uploadResponses.Add(uploadResponse);

						// Reset the accumulated buffer
						accumulatedBytesRead = 0;
					}
				}

				// Handle the last chunk if it's less than 5MB and not empty
				if (accumulatedBytesRead > 0)
				{
					var memStream = new MemoryStream(accumulatedBuffer, 0, accumulatedBytesRead);
					var uploadRequest = new UploadPartRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key,
						UploadId = initResponse.UploadId,
						PartNumber = partNumber++,
						PartSize = accumulatedBytesRead,
						InputStream = memStream
					};

					var uploadResponse = await _s3Client.UploadPartAsync(uploadRequest);
					uploadResponses.Add(uploadResponse);
				}

				// check the max bucket size again now we have a correct length
				if (bucketSize + totalBytesRead > Convert.ToInt32(GLOBALS.MaxBucketSize))
				{
					await _s3Client.AbortMultipartUploadAsync(new AbortMultipartUploadRequest
					{
						BucketName = GLOBALS.s3bucket,
						Key = key,
						UploadId = initResponse.UploadId
					});
					throw new Exception($"*** Error: Content length variance detected. Bucket size limit exceeded. Maximum size is {GLOBALS.MaxBucketSize} bytes");
				}

				var completeRequest = new CompleteMultipartUploadRequest
				{
					BucketName = GLOBALS.s3bucket,
					Key = key,
					UploadId = initResponse.UploadId,
					PartETags = uploadResponses.Select(r => new PartETag(r.PartNumber, r.ETag)).ToList()
				};

				await _s3Client.CompleteMultipartUploadAsync(completeRequest);

				Log.Information($"File {key} uploaded successfully in {partNumber - 1} parts");
				return $"{keyFile} uploaded successfully in {partNumber - 1} parts";
			}
		}
		// ------------------------------------------------------------------------------------
		public static async Task<string> MultipartUploadBlank(HttpRequest Request)
		{
			int ChunkSize = 50 * 1024 * 1024; // 50MB chunk size

			{
				long totalBytesRead = 0;
				byte[] buffer = new byte[ChunkSize];
				int bytesRead;
				while ((bytesRead = await Request.Body.ReadAsync(buffer, 0, buffer.Length)) > 0)
				{
					totalBytesRead += bytesRead;
				}
				return $"";
			}
		}
		// ------------------------------------------------------------------------------------
		public static async Task<string> SingleUpload(string keyLocation, string keyFile, HttpRequest Request)
		{
			string key = keyLocation + keyFile;
			using (var _s3Client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
			{
				PutObjectResponse response = null;
				try
				{
					using (MemoryStream ms = new MemoryStream())
					{
						await Request.Body.CopyToAsync(ms);

						PutObjectRequest request = new PutObjectRequest
						{
							BucketName = GLOBALS.s3bucket,
							Key = key,
							InputStream = ms
						};

						response = await _s3Client.PutObjectAsync(request);
						if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
						{
							throw new Exception("Unable to put file into S3");
						}
					}
				}
				catch (AmazonS3Exception e)
				{
					Log.Error("HTTP reponse not 200 in AwsHelper.Put: {a}, {b}", response.HttpStatusCode, e.Message);
					throw;
				}
			}

			return $"{keyFile} uploaded successfully";
		}
	}
}
