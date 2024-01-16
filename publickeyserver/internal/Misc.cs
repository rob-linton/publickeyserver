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
				dynamic d_buf = JsonConvert.DeserializeObject(s_buf);

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
			return JsonConvert.DeserializeObject("{}");
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
	}
}
