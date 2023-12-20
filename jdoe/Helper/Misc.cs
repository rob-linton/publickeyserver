using System.Text;
using jdoe.Verbs;
using Org.BouncyCastle.Asn1.Cmp;

namespace jdoe;

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
}