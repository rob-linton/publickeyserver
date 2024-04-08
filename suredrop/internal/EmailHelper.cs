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
using Amazon.SimpleEmail;


namespace publickeyserver
{
	public class EmailHelper
	{
		// ------------------------------------------------------------------------------------------------------------
		public EmailHelper()
		{

		}
		// ------------------------------------------------------------------------------------------------------------
		public static async Task SendEmail(string sender, string recipient, string subject, string body)
		{
			// send an email
			// https://docs.aws.amazon.com/ses/latest/DeveloperGuide/send-using-sdk-net.html


			// Create an Amazon SES client
			using (var client = new AmazonSimpleEmailServiceClient(GLOBALS.s3key, GLOBALS.s3secret, RegionEndpoint.GetBySystemName(GLOBALS.sesendpoint)))
			{
				var sendRequest = new Amazon.SimpleEmail.Model.SendEmailRequest
				{
					Source = sender,
					Destination = new Amazon.SimpleEmail.Model.Destination
					{
						ToAddresses =
						new List<string> { recipient }
					},
					Message = new Amazon.SimpleEmail.Model.Message
					{
						Subject = new Amazon.SimpleEmail.Model.Content(subject),
						Body = new Amazon.SimpleEmail.Model.Body
						{
							Html = new Amazon.SimpleEmail.Model.Content
							{
								Charset = "UTF-8",
								Data = body
							}
						}
					}
				};
				try
				{
					Console.WriteLine("Sending email using Amazon SES...");
					var response = await client.SendEmailAsync(sendRequest);
					Console.WriteLine("The email was sent successfully.");
				}
				catch (Exception ex)
				{
					Console.WriteLine("The email was not sent.");
					Console.WriteLine("Error message: " + ex.Message);
				}
			}
		}
		// ------------------------------------------------------------------------------------------------------------
	}
}