using System;
namespace publickeyserver
{
    public static class Extensions
    {
        // convert a secure string into a normal plain text string
        public static String FromBytes(this byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        // convert a plain text string into a secure string
        public static byte[] ToBytes(this String str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static String ToPlainString(this System.Security.SecureString secureStr)
        {
            String plainStr = new System.Net.NetworkCredential(string.Empty, secureStr).Password;
            return plainStr;
        }

        // convert a plain text string into a secure string
        public static System.Security.SecureString ToSecureString(this String plainStr)
        {
            var secStr = new System.Security.SecureString(); secStr.Clear();
            foreach (char c in plainStr.ToCharArray())
            {
                secStr.AppendChar(c);
            }
            return secStr;
        }


    }
}
