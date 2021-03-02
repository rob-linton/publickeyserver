using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace publickeyserv
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
		// ------------------------------------------------------------------------------------------------------
		public static dynamic getEmptyDynamic()
		{
			return JsonConvert.DeserializeObject("{}");
		}
		// ---------------------------------------------------------------------
		// ---------------------------------------------------------------------

	}
}
