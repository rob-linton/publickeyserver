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
                DotEnv.Load(options: new DotEnvOptions(trimValues: true));
                var envVars = DotEnv.Read();

                GLOBALS.s3key = envVars["S3KEY"];
                GLOBALS.s3secret = envVars["S3SECRET"];
                GLOBALS.s3endpoint = envVars["S3ENDPOINT"];
				GLOBALS.sesendpoint = envVars["SESENDPOINT"];
                GLOBALS.s3bucket = envVars["S3BUCKET"];
                GLOBALS.origin = envVars["ORIGIN"];
				GLOBALS.identityFrom = envVars["EMAILFROM"];

				// optional
				if (envVars.ContainsKey("MAX_BUCKET_SIZE"))
					GLOBALS.MaxBucketSize = envVars["MAX_BUCKET_SIZE"];
				if (envVars.ContainsKey("MAX_BUCKET_FILES"))
					GLOBALS.MaxBucketFiles = envVars["MAX_BUCKET_FILES"];
				if (envVars.ContainsKey("MAX_PACKAGE_SIZE"))
					GLOBALS.MaxPackageSize = envVars["MAX_PACKAGE_SIZE"];
				if (envVars.ContainsKey("ANONYMOUS"))
					GLOBALS.Anonymous = Convert.ToBoolean(envVars["ANONYMOUS"]);
				if (envVars.ContainsKey("ALLOWED_EMAIL_DOMAINS"))
					GLOBALS.AllowedIdentityDomains = envVars["ALLOWED_EMAIL_DOMAINS"];

                try
                {
                    GLOBALS.password = envVars["PASSWORD"];
                }
                catch { }

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
                    if (!File.Exists($"subcacert.{GLOBALS.origin}.pem"))
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
                            byte[] cacertBytes = BouncyCastleHelper.DecryptWithKey(File.ReadAllBytes($"cacert.{GLOBALS.origin}.pem"), password1.ToBytes(), GLOBALS.origin.ToBytes());

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
                if (!File.Exists($"subcacert.{GLOBALS.origin}.pem"))
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
                    webBuilder.UseUrls("http://0.0.0.0:5001/");
                });
    }
}
