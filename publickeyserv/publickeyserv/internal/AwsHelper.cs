using System;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;

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
    }
}
