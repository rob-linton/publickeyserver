# Deadpack Manual  

```  
=========================================================================================================================================================
DEADPACK v1.0 (deadrop.org)
[D]eadrop's [E]ncrypted [A]rchive and [D]istribution [PACK]age (DEADPACK)
Copyright Rob Linton, 2023
Post Quantum Cryptography (PQC) using the Crystal Kyber and Dilithium algorithms
Acknowledgement to the BouncyCastle C# Crypto library
==========================================================================================================================================================
```  


Deadpack is a command line way to send post quantum end-to-end encrypted files to a person without having to know their identity. If you choose however, you can link your alias to your email address to make it easier for people to find and send files to you.  

The executable deadpack relies on its companion web service deadrop.org, and there are no restrictions or limits provided on creating and storing aliases.  
There are however limits to how many deadpacks can be sent and received via deadrop.org. (Mainly because I'm not made of money and the Dead Drop service is hosted at my own cost)  
If this is a problem you can host your own instance of deadrop.org on your own domain. Deadpack will happily use your public key server instead. Instructions on how to do this *TBA*.  

This site is a thought experiment by Rob Linton, a Developer based in Melbourne, Australia, on how to send an anonymous end-to-end encrypted files, and uses Post Quantum Cryptography, specifically Crystal-Kyber.  
Acknowledgement to the BouncyCastle C# Crypto library.
Use this program at your own risk. The source for this site is available at https://github/rob-linton  

## Quick Start Guide

1. Download the app from https://github.com/rob-linton/deadrop/releases , supported platforms MacOS, Windows & Linux.   

2. Check that the executable hash is correct.  
(You can also download the source and compile it directly from https://github.com/rob-linton/deadrop)  

3. To create an alias linked with your email address run the following command:

	```
	deadpack create -e [optional your email address]
	```
	`Then enter a pass phrase associated with this alias.`  

	An alias will be created for you which looks similar to this:
	`missouri-precise-samsung.deadrop.org`  

4. To receive a deadpack file and automatically download it when it is ready run the following command:

	```
	deadpack receive -i 60
	```

	This will check if a file is available once every minute and the download it when it is

5. Once the file is downloaded, you can unpack the deadpack by running the following command:

	```
	deadpack unpack -f [name of deadpack file to unpack]
	```

6. To create a deadpack:

	```
	deadpack pack -f alias-to-sendfrom.deadrop.org -a alias-to-sendto.deadrop.org -i word.docx -o word.deadpack 
	```

7. And finally to send it

	```
	deadpack send -i word.deadpack 
	```


## Command Line Options

The following are options that are common to all commands  

**-v -verbose [0-3]**  

Set the output verbosity level for deadpack.  
1 = Show verification checkboxes, useful to see exactly what checks are being undertaken.  
2 = Information on steps being undertaken  
3 = Http traffic tracing  

**-p -passphrase [passphrase]**  

The passphrase used when the private keys were created using the `create` command. If not supplied the user is prompted. Not preferred as bash and cmd history will reveal your passphase easily.    

**-d -domain [domain]**  

Used to force deadpack to use another domain other than deadrop. Allow the use of a custom Public Key Server managed by the user. Instructions for running your own deadrop server can be found `here`.  


## deadpack create  

Creates a alias. This is your identity. Creates private keys and stores your public keys up on the deadrop server.  
An alias looks like the following:  

`missouri-precise-samsung.deadrop.org`  

An alias comprises of 3 random words drawn from a possible dictionary of 1000 words, which gives a total of 1000 x 1000 x 1000 alias combinations, or 1,000,000,000 (1 billion)
The words are followed by the dead drop domain. By default it is `deadrop.org`, but as users can host their own dead drop servers, it may be another domain such as `publickeyserver.org`

You can create as many aliases as you like. This is how people address files to you unless you have optionally associated it with your email address.  
If you associate multiple aliases with your email address, they can use your email address instead.

**-e -email [email address]**  
Associate an optional email address with an anonymous alias.  

**-t -token [email verification token]**  
Required if you would like to associate an email with your alias. To get an email verification token use `deadpack verify -e [your email address]` and one will be sent to your email inbox.   

### Example
```bash
deadpack create  
```
Create an anonymous alias. You will be prompted to enter a pass phrase associated with this alias.  


```bash
deadpack create -e [your email address]  
```
Create an alias linked with your email address. You will be prompted to enter a pass phrase associated with this alias.  
  

## deadpack verify

Creates an email verification token. Not required unless you would like to associate your alias with your email address. The token is sent to your email.  

**-e -email [your email address]**  

### Example  
```bash
deadpack verify -e [your email address]  
```  
  

## deadpack pack  

Create an encrypted deadpack that can only be unpacked by someone holding the Post Quantum Crystal-Kyber and RSA private keys.


**-i -input [File or file wildcard]**  

File or files to be included in the deadpack. Wildcards are ok.  

**-s -subdirectories**  

Traverse subdirectories  

**-a -aliases [space separated list of aliases/emails to encrypt this deadpack for]**

A list of aliases to encrypt this deadpack for. If the alias does not exist this command will error. You can use an email address instead of an alias if you like. If they currently do not have an alias associated with that email they will get instructions on how to do so. The create command will then wait for them to complete this step before proceeding.

*nb Source and destination aliases must be from the same domain*

**-o -output [Name of output deadpack]**  

Output deadpack name  

**-f -from [The from aliases]**  

The alias to be used as the sending alias address

### Examples

```bash
deadpack pack -f missouri-precise-samsung.deadrop.org -a closure-brook-grades.deadrop.org mighty-dimensions-trends.deadrop.org -i data/* -s -o my.deadpack
```  

Create a deadpack for closure-brook-grades.deadrop.org & mighty-dimensions-trends.deadrop.org, using all of the files in the data subdirectory, include subdirectories.

```bash
deadpack pack -f missouri-precise-samsung.deadrop.org -a rob@some.email.com -i word.docx -o word.deadpack
```  

Create a deadpack for the users rob@some.email.com, if they don't have an alias then wait until they create one, checking once a minute until they do. Then pack the file word.docx.  
  

## deadpack send

Send a deadpack to an alias using Dead Drop.  

Please note there is no requirement to use Dead Drop to send a deadpack to someone, once created a deadpack can be sent using any traditional means such as email etc.
Dead Drop currently limits 2 pending deadpacks per alias, which means until they receive them they cannot receive any others.

**-i -input [The deadpack to send]**  

The name of the deadpack to send.  
The is no need to specify the recipients as that is already encoded into the deadpack at the pack stage.

### Example

```bash
deadpack send -i my.deadpack
```

  

## deadpack receive

Download any pending deadpacks from the Dead drop server.

**-a -alias [alias to receive for]**

Alias to use when checking for deadpacks on the Dead Drop server. If no alias is specified, deadpack will iterate through your aliases one at a time and check for waiting deadpacks.  
Also if no alias is specified -force will for set to true.  

**-f -force**

Download all deadpacks without prompting

**-s -seconds [seconds]**  

Check for deadpacks every x seconds and don't exit

### Examples

```bash
deadpack receive -s 60
```  

Check for any incoming deadpacks from the Dead Drop server from any of my aliases and download them automatically, checking every 60 seconds.

```bash
deadpack receive -a missouri-precise-samsung.deadrop.org
```  

Check for any deadpacks that have been sent to my alias and interactively choose to download them.
  

## deadpack unpack

Unpack a deadpack and extract the files.  

**-i -input [input deadpack]**  

Input deadpack file

**-o -output [output directory, default is deadpack name]**  

Output directory to extract the files to

**-a -alias [alias to use when attempting to unpack the deadpack]**  

If no alias is specified unpack will iterate through all of your laiases one at a time and attempt to unpack the deadpack file with each one in turn.

### Example

```bash
deadpack unpack -i my.deadpack
```

Unpack the deadpack by providing the name of the deadpack file to unpack. No alias was specified, so it will iterate through all of your aliases until one matches
  

## deadpack list

List all of your aliases


### Example

```bash
deadpack list
```
  

## deadpack certify

Certify that an alias is valid

**-a -alias [alias to certify]**

### Examples

```bash
deadpack certify -a missouri-precise-samsung.deadrop.org
```

Certify that the alias is valid.

```bash
deadpack certify -a rob@some.email.com
```

Certify that the email address has a Dead Drop alias associated with it

