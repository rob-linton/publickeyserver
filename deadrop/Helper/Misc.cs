using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using deadrop.Verbs;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Crypto;

namespace deadrop;

public class Misc
{
	public static string GetAliasFromAlias(string aliasAndDomain)
	{
		return aliasAndDomain.Split('.')[0];
	}

	public static string GetDomainFromAlias(string aliasAndDomain)
	{
		string[] bits = aliasAndDomain.Split('.');
		StringBuilder sb = new StringBuilder();
		for (int i = 1; i < bits.Length; i++)
		{
			sb.Append(bits[i]);
			if (i < bits.Length - 1)
			{
				sb.Append('.');
			}
		}
		return sb.ToString();
	}

	public static string GetDomain(Options opts, string alias)
	{
		if (opts.Domain != null && opts.Domain.Length > 0)
		{
			return opts.Domain;
		}
		else
		{	if (String.IsNullOrEmpty(alias))
			{
				return "publickeyserver.org";
			}
			else
			{
				return GetDomainFromAlias(alias);
			}
		}
		
	}

	public static async Task<Org.BouncyCastle.X509.X509Certificate> GetCertificate(Options opts, string alias)
	{

		string domain = Misc.GetDomain(opts, alias);

		// now get the "from" alias	
		if (opts.Verbose > 0)
			Console.WriteLine($"GET: https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

		var result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}");

		var c = JsonSerializer.Deserialize<CertResult>(result) ?? throw new Exception("Could not deserialize cert result");
		var certificate = c.Certificate ?? throw new Exception("Could not get certificate from cert result");

		return BouncyCastleHelper.ReadCertificateFromPemString(certificate);
	}
}