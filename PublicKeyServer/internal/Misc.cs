using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;

namespace publickeyserver
{
	public class Misc
	{
		// ---------------------------------------------------------------------
		public Misc()
		{
		}
		// ---------------------------------------------------------------------
		public static dynamic GetRequestBodyDynamic(dynamic Request)
		{
			try
			{
				byte[] buf;
				using (var ms = new MemoryStream())
				{
					Task atask = Request.Body.CopyToAsync(ms);
					atask.Wait();

					buf = ms.ToArray();
				}
				string s_buf = Encoding.UTF8.GetString(buf);
				dynamic d_buf = JsonConvert.DeserializeObject(s_buf) ?? getEmptyDynamic();

				return d_buf;
			}
			catch
			{
				return getEmptyDynamic();
			}
		}
		// ---------------------------------------------------------------------
		public static byte[] GetRequestBody(dynamic Request)
		{
			try
			{
				byte[] buf;
				using (var ms = new MemoryStream())
				{
					Task atask = Request.Body.CopyToAsync(ms);
					atask.Wait();

					buf = ms.ToArray();
				}

				return buf;
			}
			catch
			{
				return getEmptyDynamic();
			}
		}
		// ---------------------------------------------------------------------
		public static IActionResult err(HttpResponse Response, string msg)
		{
			return err(Response, msg, "");
		}
		// ---------------------------------------------------------------------
		public static IActionResult err(HttpResponse Response, string msg, string helpURL)
		{
			Response.StatusCode = StatusCodes.Status400BadRequest;
			Dictionary<string, string> error = new Dictionary<string, string>();

			error["error"] = msg;

			if (!String.IsNullOrEmpty(helpURL))
				error["help"] = helpURL;

			return new JsonResult(error);
		}
		// ---------------------------------------------------------------------
		public static dynamic getEmptyDynamic()
		{
			return JsonConvert.DeserializeObject("{}") ?? new { };
		}
		// ---------------------------------------------------------------------
		public static string getPasswordFromConsole(String displayMessage)
		{
			SecureString pass = new SecureString();
			Console.WriteLine(displayMessage);
			ConsoleKeyInfo key;

			do
			{
				key = Console.ReadKey(true);

				// Backspace Should Not Work
				if (!char.IsControl(key.KeyChar))
				{
					pass.AppendChar(key.KeyChar);
					Console.Write("*");
				}
				else
				{
					if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
					{
						pass.RemoveAt(pass.Length - 1);
						Console.Write("\b \b");
					}
				}
			}
			// Stops Receving Keys Once Enter is Pressed
			while (key.Key != ConsoleKey.Enter);

			Console.WriteLine("");
			return pass.ToPlainString();
		}
		// ---------------------------------------------------------------------
		public static string GetAliasFromAlias(string aliasAndDomain)
		{
			return aliasAndDomain.Split('.')[0];
		}
		// ---------------------------------------------------------------------
		public static string GetRandomString(int length)
		{
			const string valid = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
			StringBuilder res = new StringBuilder();
			Random rnd = new Random();
			while (0 < length--)
			{
				res.Append(valid[rnd.Next(valid.Length)]);
			}
			return res.ToString();
		}

		// ---------------------------------------------------------------------
		internal static bool IsAllowedIdentity(string identity)
		{
			if (String.IsNullOrEmpty(identity))
				return false;

			string allowedIdentityDomains = GLOBALS.AllowedIdentityDomains;

			if (allowedIdentityDomains == "*")
				return true;
			
			string[] allowedDomains = allowedIdentityDomains.Split(',');
			
			foreach (string domain in allowedDomains)
			{
				// check if the domain is a substring of the identity
				if (identity.Contains(domain.Trim()))
					return true;
			}	

			return false;
		}
		// ---------------------------------------------------------------------
		// is anontymous allowed
		internal static bool IsAnonymousAllowed()
		{
			return GLOBALS.Anonymous;
		}
		// ---------------------------------------------------------------------
		public static List<string> GenerateSignature(byte[] data)
		{
			if (data == null || data.Length == 0)
			{
				throw new ArgumentException("Data cannot be null or empty");
			}

			List<int> signature = new List<int>();
			int sum = 0;

			// Loop through the byte array, adding up the values
			for (int i = 0; i < data.Length; i++)
			{
				sum += data[i];

				// Every few bytes, calculate a signature number and reset the sum
				if ((i + 1) % (data.Length / 10) == 0)
				{
					signature.Add(sum % 1000 + 1); // Ensure the number is between 1 and 1000
					sum = 0;
				}
			}

			// If the signature has less than 10 numbers, fill in the rest
			while (signature.Count < 10)
			{
				sum += sum; // Arbitrary operation to change the sum
				signature.Add(sum % 1000 + 1);
			}
			List<string> sig = new List<string>();
			Words words = new Words();
			foreach (int i in signature)
			{
				sig.Add(words.GetWord(i));
			}
			
			return sig;
		}
		// ---------------------------------------------------------------------
	}
}
