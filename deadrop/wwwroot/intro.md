Your email verification code is {tokenFileContentsNew}


Someone wants to send you an encrypted file, if you don't want to accept it you can ignore this email.

{GLOBALS.origin} is an open source program for sending end-to-end encrypted files using Post Quantum Encryption (PQE) in combination with traditional RSA and AES-GCM.

The source for this framework is located at https://github.com/rob-linton/deadrop


Before they can send it to you you need to create a PQE & RSA public/private key pair so they can encrypt it with your public key before they send it.

To accept this file follow this quick start guide:

1. Download the app from https://github.com/rob-linton/deadrop/releases , supported platforms MacOS, Windows & Linux.

2. Check that the executable hash is correct.
(You can also download the source and compile it directly from https://github.com/rob-linton/deadrop)

3. To create an alias linked with this email address run the following command:

deadpack create -e [your email address]
Then enter a pass phrase associated with this alias

4. To receive the file and automatically download it when it is ready run the following command:

deadpack receive -i 60

This will check if the file is available once every minute and the download it when it is

5. Once the file is downloaded, you can unpack the deadpack by running the following command:

deadpack unpack -f [name of deadpack file to unpack]

Detailed manual can be found here https://github.com/rob-linton/deadrop



