using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace publickeyserv
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
            if (args.Length != 3)
            {
                Console.WriteLine("usage: [s3key] [s3secret] [s3endpoint] [s3bucket]");
                return;
            }
            else
            {
                GLOBALS.s3key = args[0];
                GLOBALS.s3secret = args[1];
                GLOBALS.s3endpoint = args[2];
                GLOBALS.s3bucket = args[3];
            }


            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
