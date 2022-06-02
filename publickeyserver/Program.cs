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

namespace publickeyserver
{
    static class GLOBALS
    {
        public static string s3key = "";
        public static string s3secret = "";
        public static string s3endpoint = "";
        public static string s3bucket = "";
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                DotEnv.Load(options: new DotEnvOptions(trimValues: true));
                var envVars = DotEnv.Read();

                GLOBALS.s3key = envVars["s3key"];
                GLOBALS.s3secret = envVars["s3secret"];
                GLOBALS.s3endpoint = envVars["s3endpoint"];
                GLOBALS.s3bucket = envVars["s3bucket"];
            }
            else
            {
                GLOBALS.s3key = args[0];
                GLOBALS.s3secret = args[1];
                GLOBALS.s3endpoint = args[2];
                GLOBALS.s3bucket = args[3];
            }


            // Initialise SeriLog
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File(
                    "C:\\shared\\log\\log_restapi.txt",
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [Thread: {ThreadId}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(2)) // a full disk flush will be performed every 2 seconds
                .CreateLogger();



            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
             .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://*:5001/");
                });
    }
}
