using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using dotenv.net;
using System.IO;

namespace publickeyserver
{ 
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine($"{GLOBALS.origin} {GLOBALS.version}");
            if (args.Length < 5)
            {
                // Try to load from .env file first
                DotEnv.Load(options: new DotEnvOptions(trimValues: true));
                var envVars = DotEnv.Read();

                // Check if we have environment variables from system or .env file
                // Prefer system environment variables over .env file
                GLOBALS.s3key = Environment.GetEnvironmentVariable("S3KEY") ?? (envVars.ContainsKey("S3KEY") ? envVars["S3KEY"] : "") ?? "";
                GLOBALS.s3secret = Environment.GetEnvironmentVariable("S3SECRET") ?? (envVars.ContainsKey("S3SECRET") ? envVars["S3SECRET"] : "") ?? "";
                GLOBALS.s3endpoint = Environment.GetEnvironmentVariable("S3ENDPOINT") ?? (envVars.ContainsKey("S3ENDPOINT") ? envVars["S3ENDPOINT"] : "") ?? "";
                GLOBALS.sesendpoint = Environment.GetEnvironmentVariable("SESENDPOINT") ?? (envVars.ContainsKey("SESENDPOINT") ? envVars["SESENDPOINT"] : "") ?? "";
                GLOBALS.s3bucket = Environment.GetEnvironmentVariable("S3BUCKET") ?? (envVars.ContainsKey("S3BUCKET") ? envVars["S3BUCKET"] : "") ?? "";
                GLOBALS.origin = Environment.GetEnvironmentVariable("ORIGIN") ?? (envVars.ContainsKey("ORIGIN") ? envVars["ORIGIN"] : "") ?? "";
                GLOBALS.identityFrom = Environment.GetEnvironmentVariable("EMAILFROM") ?? (envVars.ContainsKey("EMAILFROM") ? envVars["EMAILFROM"] : "") ?? "";

                // optional parameters
                var maxBucketSize = Environment.GetEnvironmentVariable("MAX_BUCKET_SIZE") ?? (envVars.ContainsKey("MAX_BUCKET_SIZE") ? envVars["MAX_BUCKET_SIZE"] : null);
                if (!string.IsNullOrEmpty(maxBucketSize))
                    GLOBALS.MaxBucketSize = maxBucketSize;

                var maxBucketFiles = Environment.GetEnvironmentVariable("MAX_BUCKET_FILES") ?? (envVars.ContainsKey("MAX_BUCKET_FILES") ? envVars["MAX_BUCKET_FILES"] : null);
                if (!string.IsNullOrEmpty(maxBucketFiles))
                    GLOBALS.MaxBucketFiles = maxBucketFiles;

                var maxPackageSize = Environment.GetEnvironmentVariable("MAX_PACKAGE_SIZE") ?? (envVars.ContainsKey("MAX_PACKAGE_SIZE") ? envVars["MAX_PACKAGE_SIZE"] : null);
                if (!string.IsNullOrEmpty(maxPackageSize))
                    GLOBALS.MaxPackageSize = maxPackageSize;

                var anonymous = Environment.GetEnvironmentVariable("ANONYMOUS") ?? (envVars.ContainsKey("ANONYMOUS") ? envVars["ANONYMOUS"] : null);
                if (!string.IsNullOrEmpty(anonymous))
                    GLOBALS.Anonymous = Convert.ToBoolean(anonymous);

                var allowedDomains = Environment.GetEnvironmentVariable("ALLOWED_EMAIL_DOMAINS") ?? (envVars.ContainsKey("ALLOWED_EMAIL_DOMAINS") ? envVars["ALLOWED_EMAIL_DOMAINS"] : null);
                if (!string.IsNullOrEmpty(allowedDomains))
                    GLOBALS.AllowedIdentityDomains = allowedDomains;

                // password
                try
                {
                    GLOBALS.password = Environment.GetEnvironmentVariable("PASSWORD") ?? (envVars.ContainsKey("PASSWORD") ? envVars["PASSWORD"] : "") ?? "";
                }
                catch { }

                // Validate required parameters
                if (string.IsNullOrEmpty(GLOBALS.s3key) || string.IsNullOrEmpty(GLOBALS.s3secret) || 
                    string.IsNullOrEmpty(GLOBALS.s3endpoint) || string.IsNullOrEmpty(GLOBALS.s3bucket) || 
                    string.IsNullOrEmpty(GLOBALS.origin))
                {
                    Console.WriteLine("Error: Missing required configuration parameters.");
                    Console.WriteLine("Required: S3KEY, S3SECRET, S3ENDPOINT, S3BUCKET, ORIGIN");
                    Environment.Exit(1);
                }

                Console.WriteLine("------------------------------------------------------------------------------------------------------------");
                Console.WriteLine("Reading configuration from env...");
                Console.WriteLine("AWS region      : " + GLOBALS.s3endpoint);
                Console.WriteLine("AWS bucket      : " + GLOBALS.s3bucket);
                Console.WriteLine("AWS key         : " + GLOBALS.s3key);
                Console.WriteLine("AWS secret      : " + GLOBALS.s3secret);
                Console.WriteLine("Origin          : " + GLOBALS.origin);
				Console.WriteLine("AWS SES region  : " + GLOBALS.sesendpoint);
                Console.WriteLine("------------------------------------------------------------------------------------------------------------");
            }
            else
            {
                GLOBALS.s3key = args[0];
                GLOBALS.s3secret = args[1];
                GLOBALS.s3endpoint = args[2];
                GLOBALS.s3bucket = args[3];
                GLOBALS.origin = args[4];

                try
                {
                    GLOBALS.password = args[5];
                }
                catch { }

                Console.WriteLine("------------------------------------------------------------------------------------------------------------");
                Console.WriteLine("Reading configuration from command line...");
                Console.WriteLine("AWS region  : " + GLOBALS.s3endpoint);
                Console.WriteLine("AWS bucket  : " + GLOBALS.s3bucket);
                Console.WriteLine("AWS key     : " + GLOBALS.s3key);
                Console.WriteLine("AWS secret  : " + GLOBALS.s3secret);
                Console.WriteLine("Origin      : " + GLOBALS.origin);
                Console.WriteLine("------------------------------------------------------------------------------------------------------------");
            }

            // if we don't have a password by this point then ask for it
            if (String.IsNullOrEmpty(GLOBALS.password))
			{
                
                //Console.WriteLine("Please enter the password:");
                string password1 = Misc.getPasswordFromConsole("Please enter the password: ");
                //string password1 = Console.ReadLine();

                // test the validity of the password
                try
				{
                    // Ensure certificate directory exists
                    System.IO.Directory.CreateDirectory(GLOBALS.CertificateDirectory);
                    
                    if (!File.Exists(Path.Combine(GLOBALS.CertificateDirectory, $"subcacert.{GLOBALS.origin}.pem")))
					{
                        Console.WriteLine("The Certificate Authority Sub Cert does not exist.");
                        //Console.WriteLine("Please enter the password again to confirm creation:");
                        //string password2 = Console.ReadLine();
                        string password2 = Misc.getPasswordFromConsole("Please enter the password again to confirm creation: ");

                        if (password1 == password2)
						{
                            //BouncyCastleHelper.CreateEncryptedCA(GLOBALS.origin, password1);
                            BouncyCastleHelper.CheckCAandCreate(GLOBALS.origin, password1);
                            GLOBALS.password = password1;
                            Console.WriteLine("CA created successfully.");
                        }
                        else
						{
                            Console.WriteLine("Passwords do not match, exiting...");
                            Environment.Exit(0);
                        }
                    }
                    else
					{
                        try
						{
                            byte[] cacertBytes = BouncyCastleHelper.DecryptWithKey(File.ReadAllBytes(Path.Combine(GLOBALS.CertificateDirectory, $"cacert.{GLOBALS.origin}.pem")), password1.ToBytes(), GLOBALS.origin.ToBytes());

                            // if we get here we successfully decrypted it, so accept the password
                            GLOBALS.password = password1;
						}
                        catch
						{
                            Console.WriteLine("Passwords do not match, exiting...");
                            Environment.Exit(0);
                        }
					}
				}
				catch
                {
                    Console.WriteLine("Exiting...");
                    Environment.Exit(0);
                }
			}
            else
			{
                // Ensure certificate directory exists
                System.IO.Directory.CreateDirectory(GLOBALS.CertificateDirectory);
                
                if (!File.Exists(Path.Combine(GLOBALS.CertificateDirectory, $"subcacert.{GLOBALS.origin}.pem")))
                {
                    BouncyCastleHelper.CheckCAandCreate(GLOBALS.origin, GLOBALS.password);
                }
            }

            // Initialise SeriLog
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    "log_" + GLOBALS.origin + ".log",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [Thread: {ThreadId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(2)) // a full disk flush will be performed every 2 seconds
                .CreateLogger();

           

            // start the web server
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
             .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://0.0.0.0:5000/");
                });
    }
}
