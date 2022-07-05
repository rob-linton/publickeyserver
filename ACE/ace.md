# Anonymous Certificate Enrollment protocol (ACE)

This protocol is loosley based on the Enrolment over Secure Transport [EST](https://tools.ietf.org/html/rfc7030) (RFC7030) protocol.   
https://tools.ietf.org/html/rfc7030  

Abstract  
--------

RFC7030 (EST) provides a secure method for creating, retrieving and verifying certificates.  

The ACE protocol is a simple, opinionated distilled version of EST, developed for the very specific purpose of creating, providing and validating anonymous certificates that may be used for encryption without identity.

The ACE protocol is designed (as is EST) to provide an automated method for creating, retrieving and verifying certificates. However this protocol removes all requirement for identification, other than the verification that a certificate is bound to an anonymous alias.  

This protocol has been designed to facilitate an end-to-end encrypted communication such that the private key associated with an alias never leaves the client device and that the device is associated with an anonymous alias and nothing more.  

The assumption being, that to communicate securely and anonymously using an end-to-end encryption protocol that can be securly verified by all parties, all one needs to know is the `alias` of the destination individual.  

Detail
------

The ACE protocol is designed to be as simple as possible with very little (if any) customisation options.

RSA key pairs are RSA2048.  

X.509 certificates are signed using the following algorithm:

`SHA512WITHRSA`  

Alias's are automatically assigned and are a random group of three words.

eg:
  marlin spike coil

Optional extra information may be baked into an X.509 certificate. The base OID is as follows:

`iso.org.dod.internet.private.enterprise.publickeyserver (1.3.6.1.4.1.57055.2)`

Optional information may be ascii with the following characters allowed:

```
[a..z|A..Z|[0..9]:|/|;|.|?|&|#|!|$|%|^|*|(|)|[|]|{|}|<|>|,|_|-|+|=|@|~|
```  

Optional information may also be provided base64 encoded, no assumption is made as to the data format provided.


Authentication
--------------

No authentication will be used to access a server providing the ACE protocol.


Implementation
--------------

Thw ACE protocol uses the same REST API mechanism as EST and provides the following endpoints:  
  
`GET /cacerts`  
`POST /simpleenroll`  
  
Because the EST protocol does not provide a mechanism for the retrieval of public keys, the extra endpoint has been implemented:  

`GET /cert/` `alias`  


GET /cacerts  
------------
Returns a PKCS#12 certificate of the CA root signing authority for this server. 
```
Return format:  
  application/x-pkcs12  
```

POST /simpleenroll    
------------------
Creates a certificate and returns it in PKCS#12 format  

```
POST BODY  

Post format:  
  application/json    


{  
  "key",     : "public key in PEM format (RSA2048)"  *mandatory*
  "servers" :
    [
        "0.0.0.0",   
        ..,
        "0.0.0.0"
    ],
   "data" : 
    {
        "1" : "optional information 1",
        ..,
        "65534" : "optional information 65534"
    }
}
```

Where:

servers:

An optional list of server IP addresses hosting instances of the publicdataserver.
(default of omitted is to user publicdataserver.org)

data:

An optional list of arbitary data.

Allowed chracters.
```
[a..z|A..Z|[0..9]:|/|;|.|?|&|#|!|$|%|^|*|(|)|[|]|{|}|<|>|,|_|-|+|=|@|~|
```

Each optional data piece is has no limit to it's data length

 
```
Return format:  
  application/x-pkcs12  
  
  with embedded OID's:
  OID 1.3.6.1.4.1.57055.0 = "optional servers"
    eg. OID 1.3.6.1.4.1.57055.0.1 = "0.0.0.0"
    eg. OID 1.3.6.1.4.1.57055.0.2 = "0.0.0.0"

  OID 1.3.6.1.4.1.57055.1 = "optional data"
    eg. OID 1.3.6.1.4.1.57055.1.1 = "data 1"
    eg. OID 1.3.6.1.4.1.57055.1.2 = "data 2"
  
```


Official listing:
http://oid-info.com/cgi-bin/display?oid=1.3.6.1.4.1.57055&a=display


POST GET /cert/{alias}
----------------------
Returns the associated certificate with the alias in PKCS#12 format.  

```
alias = generated alias
```  

```
Return format:  
  application/x-pkcs12  
```  









