﻿using System;

namespace publickeyserver
{
    

    static class GLOBALS
    {
		public static string version = "1.0.0";
        public static string s3key = "";
        public static string s3secret = "";
        public static string s3endpoint = "";
		 public static string sesendpoint = "";
        public static string s3bucket = "";
		public static string s3packages = "";
        public static string origin = "";
        public static string password = "";
		public static string identityFrom = "";

		public static string MaxBucketSize = "100000000"; // 100Mb
		public static string MaxBucketFiles = "100";
		public static string MaxPackageSize = "100000000"; //100Mb

		public static bool Anonymous = true;

		public static string AllowedIdentityDomains = "*";

		// Certificate storage directory
		public static string CertificateDirectory = "certificates";

        public static int status_certs_enrolled = 0;
        public static int status_certs_served = 0;
    }

    public class Help
	{
        public static string cacerts = "https://github.com/rob-linton/publickeyserver/wiki/Ca-Certs";
        public static string cert = "https://github.com/rob-linton/publickeyserver/wiki/Cert";
        public static string simpleenroll = "https://github.com/rob-linton/publickeyserver/wiki/Simple-Enroll";
        public static string serverkeygen = "https://github.com/rob-linton/publickeyserver/wiki/Server-Key-Gen";
    }

    public class Defines
	{
        public static int keyStrength = 2048;
        public static int caKeyStrength = 4096;
    }


}

