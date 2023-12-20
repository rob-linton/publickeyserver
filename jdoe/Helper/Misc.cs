using System.Text;
using Org.BouncyCastle.Asn1.Cmp;

namespace jdoe;

public class Misc
{
	public static string GetAliasFromAliasAndDomain(string aliasAndDomain)
	{
		return aliasAndDomain.Split('.')[0];
	}

	public static string GetDomainFromAliasAndDomain(string aliasAndDomain)
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
}