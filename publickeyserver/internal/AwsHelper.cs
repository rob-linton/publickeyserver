using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Serilog;
using System.IO;

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
			try
			{
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
			catch (AmazonS3Exception e)
			{
				Log.Error("HTTP reponse not 200 in AwsHelper.Exists: {a}, {b}", response.HttpStatusCode, e.Message);
				throw;
			}
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
	}
}
