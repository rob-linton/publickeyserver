# Deadpack Manual  

```  
=========================================================================================================================================================
DEADPACK v1.0 (suredrop.org)
[D]eadrop's [E]ncrypted [A]rchive and [D]istribution [PACK]age (DEADPACK)
Copyright Rob Linton, 2023
Post Quantum Cryptography (PQC) using the Crystal Kyber and Dilithium algorithms
Acknowledgement to the BouncyCastle C# Crypto library
==========================================================================================================================================================
```  


Deadpack is a command line way to send post quantum end-to-end encrypted files to a person without having to know their identity. If you choose however, you can link your alias to your email address to make it easier for people to find and send files to you.  

The executable surepack relies on its companion web service suredrop.org, and there are no restrictions or limits provided on creating and storing aliases.  
There are however limits to how many surepacks can be sent and received via suredrop.org. (Mainly because I'm not made of money and the Dead Drop service is hosted at my own cost)  
If this is a problem you can host your own instance of suredrop.org on your own domain. Deadpack will happily use your public key server instead. Instructions on how to do this *TBA*.  

This site is a thought experiment by Rob Linton, a Developer based in Melbourne, Australia, on how to send an anonymous end-to-end encrypted files, and uses Post Quantum Cryptography, specifically Crystal-Kyber.  
Acknowledgement to the BouncyCastle C# Crypto library.
Use this program at your own risk. The source for this site is available at https://github/rob-linton  

## Quick Start Guide

1. Download the app from https://github.com/rob-linton/suredrop/releases , supported platforms MacOS, Windows & Linux.   

2. Check that the executable hash is correct.  
(You can also download the source and compile it directly from https://github.com/rob-linton/suredrop)  

3. To create an alias linked with your email address run the following command:

	```
	surepack create -e [optional your email address]
	```
	`Then enter a pass phrase associated with this alias.`  

	An alias will be created for you which looks similar to this:
	`missouri-precise-samsung.suredrop.org`  

4. To receive a surepack file and automatically download it when it is ready run the following command:

	```
	surepack receive -i 60
	```

	This will check if a file is available once every minute and the download it when it is

5. Once the file is downloaded, you can unpack the surepack by running the following command:

	```
	surepack unpack -f [name of surepack file to unpack]
	```

6. To create a surepack:

	```
	surepack pack -f alias-to-sendfrom.suredrop.org -a alias-to-sendto.suredrop.org -i word.docx -o word.surepack 
	```

7. And finally to send it

	```
	surepack send -i word.surepack 
	```


## Command Line Options

The following are options that are common to all commands  

**-v -verbose [0-3]**  

Set the output verbosity level for surepack.  
1 = Show verification checkboxes, useful to see exactly what checks are being undertaken.  
2 = Information on steps being undertaken  
3 = Http traffic tracing  

**-p -passphrase [passphrase]**  

The passphrase used when the private keys were created using the `create` command. If not supplied the user is prompted. Not preferred as bash and cmd history will reveal your passphase easily.    

**-d -domain [domain]**  

Used to force surepack to use another domain other than suredrop. Allow the use of a custom Public Key Server managed by the user. Instructions for running your own suredrop server can be found `here`.  


## surepack create  

Creates a alias. This is your identity. Creates private keys and stores your public keys up on the suredrop server.  
An alias looks like the following:  

`missouri-precise-samsung.suredrop.org`  

An alias comprises of 3 random words drawn from a possible dictionary of 1000 words, which gives a total of 1000 x 1000 x 1000 alias combinations, or 1,000,000,000 (1 billion)
The words are followed by the dead drop domain. By default it is `suredrop.org`, but as users can host their own dead drop servers, it may be another domain such as `publickeyserver.org`

You can create as many aliases as you like. This is how people address files to you unless you have optionally associated it with your email address.  
If you associate multiple aliases with your email address, they can use your email address instead.

**-e -email [email address]**  
Associate an optional email address with an anonymous alias.  

**-t -token [email verification token]**  
Required if you would like to associate an email with your alias. To get an email verification token use `surepack verify -e [your email address]` and one will be sent to your email inbox.   

### Example
```bash
surepack create  
```
Create an anonymous alias. You will be prompted to enter a pass phrase associated with this alias.  


```bash
surepack create -e [your email address]  
```
Create an alias linked with your email address. You will be prompted to enter a pass phrase associated with this alias.  
  

## surepack verify

Creates an email verification token. Not required unless you would like to associate your alias with your email address. The token is sent to your email.  

**-e -email [your email address]**  

### Example  
```bash
surepack verify -e [your email address]  
```  
  

## surepack pack  

Create an encrypted surepack that can only be unpacked by someone holding the Post Quantum Crystal-Kyber and RSA private keys.


**-i -input [File or file wildcard]**  

File or files to be included in the surepack. Wildcards are ok.  

**-s -subdirectories**  

Traverse subdirectories  

**-a -aliases [space separated list of aliases/emails to encrypt this surepack for]**

A list of aliases to encrypt this surepack for. If the alias does not exist this command will error. You can use an email address instead of an alias if you like. If they currently do not have an alias associated with that email they will get instructions on how to do so. The create command will then wait for them to complete this step before proceeding.

*nb Source and destination aliases must be from the same domain*

**-o -output [Name of output surepack]**  

Output surepack name  

**-f -from [The from aliases]**  

The alias to be used as the sending alias address

### Examples

```bash
surepack pack -f missouri-precise-samsung.suredrop.org -a closure-brook-grades.suredrop.org mighty-dimensions-trends.suredrop.org -i data/* -s -o my.surepack
```  

Create a surepack for closure-brook-grades.suredrop.org & mighty-dimensions-trends.suredrop.org, using all of the files in the data subdirectory, include subdirectories.

```bash
surepack pack -f missouri-precise-samsung.suredrop.org -a rob@some.email.com -i word.docx -o word.surepack
```  

Create a surepack for the users rob@some.email.com, if they don't have an alias then wait until they create one, checking once a minute until they do. Then pack the file word.docx.  
  

## surepack send

Send a surepack to an alias using Dead Drop.  

Please note there is no requirement to use Dead Drop to send a surepack to someone, once created a surepack can be sent using any traditional means such as email etc.
Dead Drop currently limits 2 pending surepacks per alias, which means until they receive them they cannot receive any others.

**-i -input [The surepack to send]**  

The name of the surepack to send.  
The is no need to specify the recipients as that is already encoded into the surepack at the pack stage.

### Example

```bash
surepack send -i my.surepack
```

  

## surepack receive

Download any pending surepacks from the Dead drop server.

**-a -alias [alias to receive for]**

Alias to use when checking for surepacks on the Dead Drop server. If no alias is specified, surepack will iterate through your aliases one at a time and check for waiting surepacks.  
Also if no alias is specified -force will for set to true.  

**-f -force**

Download all surepacks without prompting

**-s -seconds [seconds]**  

Check for surepacks every x seconds and don't exit

### Examples

```bash
surepack receive -s 60
```  

Check for any incoming surepacks from the Dead Drop server from any of my aliases and download them automatically, checking every 60 seconds.

```bash
surepack receive -a missouri-precise-samsung.suredrop.org
```  

Check for any surepacks that have been sent to my alias and interactively choose to download them.
  

## surepack unpack

Unpack a surepack and extract the files.  

**-i -input [input surepack]**  

Input surepack file

**-o -output [output directory, default is surepack name]**  

Output directory to extract the files to

**-a -alias [alias to use when attempting to unpack the surepack]**  

If no alias is specified unpack will iterate through all of your laiases one at a time and attempt to unpack the surepack file with each one in turn.

### Example

```bash
surepack unpack -i my.surepack
```

Unpack the surepack by providing the name of the surepack file to unpack. No alias was specified, so it will iterate through all of your aliases until one matches
  

## surepack list

List all of your aliases


### Example

```bash
surepack list
```
  

## surepack certify

Certify that an alias is valid

**-a -alias [alias to certify]**

### Examples

```bash
surepack certify -a missouri-precise-samsung.suredrop.org
```

Certify that the alias is valid.

```bash
surepack certify -a rob@some.email.com
```

Certify that the email address has a Dead Drop alias associated with it

