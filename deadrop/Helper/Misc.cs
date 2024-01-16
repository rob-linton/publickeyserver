using System.Text;
using System.Text.Json;
using deadrop.Verbs;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.X509;

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

	public static async Task<X509Certificate> GetCertificate(Options opts, string alias)
	{

		string domain = Misc.GetDomain(opts, alias);

		// now get the "from" alias	
		var result = await HttpHelper.Get($"https://{domain}/cert/{Misc.GetAliasFromAlias(alias)}", opts);

		var c = JsonSerializer.Deserialize<CertResult>(result) ?? throw new Exception("Could not deserialize cert result");
		var certificate = c.Certificate ?? throw new Exception("Could not get certificate from cert result");

		return BouncyCastleHelper.ReadCertificateFromPemString(certificate);
	}

	public static void LogLine(Options opts, string message)
	{
		if (opts.Verbose > 0)
			Console.WriteLine(message);
	}

	public static void LogLine1(Options opts, string message)
	{
		if (opts.Verbose > 1)
		{
			Console.WriteLine("--------------------------------------------------------------------------------");
			Console.WriteLine(message);
			Console.WriteLine("--------------------------------------------------------------------------------");
		}
	}

	public static void LogLine(string message)
	{
		Console.WriteLine(message);
	}

	public static void LogError(Options opts, string message, string details = "")
	{
		Console.WriteLine($"\n*** ERROR: {message} ***\n");

		if (opts.Verbose > 0 && !String.IsNullOrEmpty(details))
			Console.Write($"*** ERROR: {details} ***\n");
	}

	public static void LogChar(Options opts, string message)
	{
		if (opts.Verbose > 0)
			Console.Write(message);
	}

	public static void LogChar(string message)
	{
		Console.Write(message);
	}

	public static void LogHeader()
	{
		LogArt();

		LogLine("================================================================================");
		LogLine("DEADPACK v1.0 (deadrop.org)");
		LogLine("Deadrop's Encrypted Archive and Distribution PACKage");
		LogLine("Copyright Rob Linton, 2023");
		LogLine("================================================================================\n");
	}

	public static void LogArt()
	{
		string art = 
@"
################################################################################
##########################  Dead Drop (deadrop.org)   ##########################
################################################################################
###############################                   ##############################
###########################                           ##########################
#########################                               ########################
########################                                 #######################
#######################*                                 *######################
#######################                                   ######################
#######################/                                 /######################
########################,    *####&\         /&####*    ,#######################
#########################   ##########     ##########   ########################
########################   ###########     ###########   #######################
#######################      #######/       \#######      ######################
#######################&              #####              &######################
##############################       #######       (############################
###############################                   ##############################
################,    ##########                   ##########    ,###############
##############&        &############|#|#|#|#|############&        &#############
#############               &#######################&               ############
##############&                   (###########(                   &#############
###########################/                         /##########################
#################################               ################################
################.*%&####&(             *#/             (&####&%*.(##############
#############                   .###############.                   ############
##############*           ,###########################,           *#############
################      ,###################################,      ###############
################################################################################
################### Anonymous End to End Encrypted File Drops ##################
################################################################################
";
		LogLine(art);
	}
}