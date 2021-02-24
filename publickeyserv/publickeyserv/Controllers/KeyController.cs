using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace publickeyserv.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KeyController : ControllerBase
    {
        
        private readonly ILogger<Controller> _logger;

        public KeyController(ILogger<Controller> logger)
        {
            _logger = logger;
        }

        // ---------------------------------------------------------------------
        [HttpGet]
        public PublicKey Get(string alias)
        {
            PublicKey cert = new PublicKey();

            cert.name = "test";
            cert.api = "https://google.com";
            cert.alias = "trevor freddy gotcha";
            cert.api = "";

            return cert;
            
        }
        // ---------------------------------------------------------------------
        [HttpPost]
        public async Task<Stream> Post([FromBody] CreateKey createkey)
        {

            string key = createkey.key;
            string name = createkey.name;
            string api = createkey.api;

            string alias = "";
            while (true)
            {
                // Generate a random first name
                var randomizerFirstName = RandomizerFactory.GetRandomizer(new FieldOptionsTextWords { Min = 3, Max = 3 });
                alias = randomizerFirstName.Generate();

                // check if exists in s3
                using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
                {
                   var tExists = AwsHelper.Exists(client, alias);
                    await tExists;

                    if (!tExists.Result)
                        break;
                    
                };
            }

            byte[] byteArray = Encoding.ASCII.GetBytes(alias);
            MemoryStream stream = new MemoryStream(byteArray);

            return stream;

            //
            // create the certificate
            //

            //
            // save the certificate and the api details
            //
            using (var client = new AmazonS3Client(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.s3endpoint)))
            {
                using (var newMemoryStream = new MemoryStream())
                {
                    IFormFile file; // ** placeholder
                    file.CopyTo(newMemoryStream);

                    var uploadRequest = new TransferUtilityUploadRequest
                    {
                        InputStream = newMemoryStream,
                        Key = file.FileName,
                        BucketName = "yourBucketName",
                        CannedACL = S3CannedACL.PublicRead
                    };

                    var fileTransferUtility = new TransferUtility(client);
                    fileTransferUtility.UploadAsync(uploadRequest);
                }

            };



            PublicKey cert = new PublicKey();

            cert.name = "test";
            cert.api = "https://google.com";
            cert.alias = "trevor freddy gotcha";
            cert.api = "";

            //return cert;

        }
        // ---------------------------------------------------------------------
    }
}

/*
 * "pubkey":"[base64 public key]",
    "api":"[api of app],
    "appname":"[name of app]",
*/