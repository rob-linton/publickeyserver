using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Serilog;

namespace publickeyserv
{
	public class AwsHelper
	{
		public AwsHelper()
		{
		}
		// ---------------------------------------------------------------------
		public static async Task<bool> Exists(Amazon.S3.AmazonS3Client client, string key)
		{

			try
			{
				var request = new GetObjectMetadataRequest
				{
					BucketName = GLOBALS.s3bucket,
					Key = key
				};


				// If the object doesn't exist then a "NotFound" will be thrown
				await client.GetObjectMetadataAsync(request);
				return true;
			}
			catch (AmazonS3Exception e)
			{
				if (string.Equals(e.ErrorCode, "NoSuchBucket"))
				{
					//bucketExists = false;
					return false;
				}
				else if (string.Equals(e.ErrorCode, "NotFound"))
				{
					return false;
				}
				throw;
			}
		}
		// ---------------------------------------------------------------------
		public static async Task<bool> Put(Amazon.S3.AmazonS3Client client, string key, string body)
		{

			try
			{

				PutObjectRequest request = new PutObjectRequest
				{
					BucketName = GLOBALS.s3bucket,
					Key = key,
					ContentBody = body
				};

				PutObjectResponse response = await client.PutObjectAsync(request);
				return true;
			}
			catch (Exception e)
			{
				Log.Error("Error putting object into S3", e);
				return false;
			}

		}
		// ---------------------------------------------------------------------
	}
}
