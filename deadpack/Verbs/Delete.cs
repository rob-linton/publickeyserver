#pragma warning disable 1998
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using CommandLine;
using Microsoft.VisualBasic;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;

namespace deadrop.Verbs;

[Verb("delete", HelpText = "Delete an alias.")]
public class DeleteOptions : Options
{
  	[Option('a', "alias", Required = true, HelpText = "Alias to be deleted")]
	public required string Alias { get; set; }	

}
class Delete 
{
	public static async Task<int> Execute(DeleteOptions opts, IProgress<StatusUpdate> progress = null)
	{
		try
		{
			
			Misc.LogHeader();

			Misc.LogLine($"Deleting  {opts.Alias}");
			Misc.LogLine($"");

			if (String.IsNullOrEmpty(Globals.Password))
			Globals.Password = Misc.GetPassword();

			string aliasOrEmail = opts.Alias;
			// if it is an email then swap it out for an alias

			string alias = aliasOrEmail;
			CertResult cert = await EmailHelper.GetAliasOrEmailFromServer(alias, false);
			List<string> names  = BouncyCastleHelper.GetAltNames(cert?.Certificate);
			string email = "";

			foreach (string name in names)
			{
				if (name.Contains("@"))
				{
					email = name;
				}
				else
				{
					alias = name;
				}
			}

			Misc.LogLine($"Deleting  {alias}");

			// remove the domain from the alias
			string shortAlias = Misc.GetAliasFromAlias(alias);
			
			// sign it with the sender
			string privateKeyPem = Storage.GetPrivateKey($"{alias}.rsa", Globals.Password);
			AsymmetricCipherKeyPair privateKey = BouncyCastleHelper.ReadKeyPairFromPemString(privateKeyPem);
			string serverDomain = Misc.GetDomain(alias);
			string domain = Misc.GetDomainFromAlias(alias);
			long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			byte[] data = $"{alias}{unixTimestamp.ToString()}{domain}".ToBytes();
			byte[] signature = BouncyCastleHelper.SignData(data, privateKey.Private);
			string base64Signature = Convert.ToBase64String(signature);

			string result = "";
			if (string.IsNullOrEmpty(email))
			{
				// delete the alias from the server
				result = await HttpHelper.Delete($"https://{serverDomain}/{shortAlias}?timestamp={unixTimestamp}&signature={base64Signature}");
			}
			else
			{
				result = await HttpHelper.Delete($"https://{serverDomain}/email/{email}/{shortAlias}?timestamp={unixTimestamp}&signature={base64Signature}");
			}

			// remove the alias from the local store
			Storage.DeletePrivateKey($"{alias}");

			return 0;
		}
		catch (Exception ex)
		{
			try
			{
				progress?.Report(new StatusUpdate { Status = ex.Message });
				await System.Threading.Tasks.Task.Delay(1); // DO NOT REMOVE-REQUIRED FOR UX
			}
			catch { }

			Misc.LogError("Unable to validate alias", ex.Message);
			return 1;
		}
		finally
		{
			Storage.DeletePrivateKey($"{opts.Alias}");
		}
	}
}