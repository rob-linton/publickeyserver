using System.Text;

namespace jdoe;

public class Misc
{
	public static string getAliasFromAliasAndDomain(string aliasAndDomain)
	{
		return aliasAndDomain.Split('.')[0];
	}
}