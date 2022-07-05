## Public Key Server

This project implements the Anonymous Certificate Enrolment protocol [ACE](https://github.com/rob-linton/publickeyserver/blob/main/ACE/ace.md) which is loosley based on the Enrolment over Secure Transport [EST](https://tools.ietf.org/html/rfc7030) (RFC7030) protocol.

The Public Key Server Project provides a simple and opinionated method for an anonymous individual to obtain a certificate associated with an anonymous alias and have it validated by a third party.  This project provides a simple yet functional anonymous certificate management protocol which allows unidentified individuals to know with certainty that they have identified the public key associated with an alias. 

The underlying principle of this protocol is to allow an individual to obtain a certificate which is bound to an anonymous alias, in this case an automatically and randomly generated 3 word phrase, an example of which could be `crow mandate current`, and then allow a third party to validate this certificate, and therefore access the alias's public key.

This project makes no assumptions regarding the use of such certificates after they have been issued.

Certificate Lifecycle
---------------------

It is expected that certificates will be short lived and disposable.  

Security
--------

While this project is simple and opinionated, some effort has been made to ensure server security including CA certificate storage.

CA certificate storage is within an offcloud certified HSM, server security makes use of standard Amazon AWS standards, with the source code publically available here in Github.

This source code runs in production at https://publickeyserver.org.




NOTE:


public HttpResponseMessage Get(string smsNumber, string code)
{
    RsaKeyPairGenerator r = new RsaKeyPairGenerator();
    r.Init(new Org.BouncyCastle.Crypto.KeyGenerationParameters(new Org.BouncyCastle.Security.SecureRandom(), 2048));

    AsymmetricCipherKeyPair keys = r.GenerateKeyPair();

    string publicKeyPath = Path.Combine(Path.GetTempPath(), "publicKey.key");

    if (File.Exists(publicKeyPath))
    {
        File.Delete(publicKeyPath);
    }

    using (TextWriter textWriter = new StreamWriter(publicKeyPath, false))
    {
        PemWriter pemWriter = new PemWriter(textWriter);
        pemWriter.WriteObject(keys.Public);
        pemWriter.Writer.Flush();
    }

    string certSubjectName = "UShadow_RSA";
    var certName = new X509Name("CN=" + certSubjectName);
    var serialNo = BigInteger.ProbablePrime(120, new Random());

    X509V3CertificateGenerator gen2 = new X509V3CertificateGenerator();
    gen2.SetSerialNumber(serialNo);
    gen2.SetSubjectDN(certName);
    gen2.SetIssuerDN(new X509Name(true, "CN=UShadow"));
    gen2.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(30, 0, 0, 0)));
    gen2.SetNotAfter(DateTime.Now.AddYears(2));
    gen2.SetSignatureAlgorithm("sha512WithRSA");

    gen2.SetPublicKey(keys.Public);

    Org.BouncyCastle.X509.X509Certificate newCert = gen2.Generate(keys.Private);

    Pkcs12Store store = new Pkcs12StoreBuilder().Build();

    X509CertificateEntry certEntry = new X509CertificateEntry(newCert);
    store.SetCertificateEntry(newCert.SubjectDN.ToString(), certEntry);

    AsymmetricKeyEntry keyEntry = new AsymmetricKeyEntry(keys.Private);
    store.SetKeyEntry(newCert.SubjectDN.ToString() + "_key", keyEntry, new X509CertificateEntry[] { certEntry });

    using (MemoryStream ms = new MemoryStream())
    {
        store.Save(ms, "Password".ToCharArray(), new SecureRandom());

        var resp = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(ms.ToArray())
        };

        resp.Content.Headers.Add("Content-Type", "application/x-pkcs12");
        return resp;
    }
}