# Anonymous Certificate Enrollment protocol (ACE)

This protocol is loosley based on the Enrolment over Secure Transport (EST) (RFC7030) protocol.   
https://tools.ietf.org/html/rfc7030  

Abstract  
--------

RFC7030 (EST) provides a secure method for creating, retrieving and verifying certificates.  

The ACE protocol is designed (as is EST) to provide an automated method for creating, retrieving and verifying certificates. However this protocol removes all requirement for identification, other than the verification that a certificate is bound to an anonymous alias.  

This protocol has been designed to facilitate an end-to-end encrypted communication such that the private key associated with an alias never leaves the client device and that the device is associated with an anonymous alias and nothing more.  

The assumption being that to communicate securely and anonymously using an end-to-end encryption protocol that can be securly verified all one needs to know is the `alias` of the destination individual.  

Implementation
--------------

Thw ACE protocol uses the same REST API mechanism as EST and provides the following endpoints:  
  
`GET /cacerts`  
`POST /simpleenroll`  
  
Because the EST protocol does not provide a mechanism for the retrieval of public keys, the extra endpoint has been implemented:  

`GET /cert/` `alias`  

And for testing and convinence the following endpoint has been provided to allow certs to be issued by the server that include a private key:  

`POST /serverkeygen`  


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
  "key"     : "public key in PEM format"  *mandatory*
  "0"       : "optional information 0" *optional*  
  "1"       : "optional information 1" *optional*
  ..
  "65534"   : "optional information 65534" *optional*
}
```

```
Return format:  
  application/x-pkcs12  
```










