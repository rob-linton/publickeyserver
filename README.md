## Public Key Server

This project implements the Anonymous Certificate Enrolment protocol [ACE](https://github.com/rob-linton/publickeyserver/blob/main/ACE/ace.md) which is loosley based on the Enrolment over Secure Transport [EST](https://tools.ietf.org/html/rfc7030) (RFC7030) protocol.

The Public Key Server Project provides a simple and opinionated method for an anonymous individual to obtain a certificate associated with an anonymous alias and have it validated by a third party.  This project provides a simple yet functional anonymous certificate management protocol which allows unidentified individuals to know with certainty that they have identified the public key associated with an alias. 

The underlying principle of this protocol is to allow an individual to obtain a certificate which is bound to an anonymous alias, in this case an automatically and randomly generated 3 word phrase, an example of which could be `crow mandate current`, and then allow a third party to validate this certificate, and therefore access the alias's public key.

This project makes no assumptions regarding the use of such certificates after they have been issued.

Security
--------

While this project is simple and opinionated, some effort has been made to ensure server security including CA certificate storage.

CA certificate storage is within an offcloud certified HSM, server security makes use of standard Amazon AWS standards, with the source code publically available here in Github.

This source code runs in production at https://publickeyserver.org.




