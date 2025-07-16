# 📦 SurePack User Manual

## What is SurePack?

SurePack is a secure file transfer application that uses military-grade encryption with post-quantum cryptography. It allows you to send files securely and optionally anonymously to other users. Think of it as "PGP for 2025" - but much easier to use!

### Key Features
- 🔐 **End-to-End Encryption**: Only recipients can decrypt your files
- 🛡️ **Quantum-Resistant**: Protected against future quantum computers
- 🎭 **Anonymous Option**: No email or identity required
- 🚀 **Easy to Use**: Simple commands or GUI interface
- 📦 **Secure Packaging**: Files are encrypted, compressed, and signed

## Getting Started

### Installation

1. Download SurePack for your platform from [publickeyserver.org](https://publickeyserver.org)
2. Extract the archive to a folder on your computer
3. Add the folder to your system PATH (optional, for command-line usage)

### Quick Start - Two Simple Steps

**Step 1: Create Your Identity (Alias)**
```bash
surepack create
```

**Step 2: Send a File**
```bash
surepack pack -i myfile.pdf -a recipient-alias -f your-alias -o package.surepack
surepack send -i package.surepack
```

That's it! Let's dive into the details...

---

## 📝 Step 1: Creating Your Alias (Digital Identity)

An alias is your unique identity in the SurePack system. It's like a secure email address that you'll use to send and receive files.

### Command Line Method

```bash
surepack create
```

You'll be prompted to:
1. **Enter a password**: This protects your private keys (remember it!)
2. **Choose a domain** (optional): Press Enter to use the default `publickeyserver.org`

The system will then:
- Generate three types of encryption keys (RSA, Kyber, and Dilithium)
- Create a unique three-word alias (e.g., `happy-cloud-tree.publickeyserver.org`)
- Store your keys securely on your computer

**Example Output:**
```
Creating...
Domain: publickeyserver.org

Please enter passphrase: ********

- Requesting alias from: publickeyserver.org

[✓] Generated RSA key pair
[✓] Generated Quantum Kyber key pair  
[✓] Generated Quantum Dilithium key pair
[✓] Root certificate fingerprint saved

Alias happy-cloud-tree.publickeyserver.org created
```

### GUI Method

1. Run `surepack gui` or double-click the SurePack executable
2. Click the **"+ Alias"** button
3. Enter your password
4. Click **"Create Alias"**

### Optional: Associate an Email

If you want to be identifiable (not anonymous), you can associate an email:

```bash
surepack create -e yourname@example.com
```

You'll need to:
1. First, request a verification code:
   ```bash
   surepack verify -e yourname@example.com
   ```
   (A verification code will be sent to your email)
   
2. Then create your alias with the verification code:
   ```bash
   surepack create -e yourname@example.com -t 123456
   ```

---

## 📤 Step 2: Sending Files

Sending files is a two-part process: **Pack** (encrypt) and **Send** (upload).

### Part A: Pack Your Files

The `pack` command creates an encrypted `.surepack` file containing your documents.

**Basic Example:**
```bash
surepack pack -i document.pdf -a john-doe-smith.publickeyserver.org -f your-alias -o mypackage.surepack
```

**Parameters Explained:**
- `-i` : Input file(s) to pack
- `-a` : Recipient alias(es) - who can open the package
- `-f` : Your alias (sender)
- `-o` : Output filename for the package

**Advanced Examples:**

1. **Multiple Files (using wildcards):**
   ```bash
   surepack pack -i "*.pdf" -a recipient -f your-alias -o documents.surepack
   ```

2. **Multiple Recipients:**
   ```bash
   surepack pack -i file.doc -a alice,bob,charlie -f your-alias -o shared.surepack
   ```

3. **Include Subdirectories:**
   ```bash
   surepack pack -i "*" -r -a recipient -f your-alias -o everything.surepack
   ```

4. **Add a Message:**
   ```bash
   surepack pack -i report.xlsx -a boss -f your-alias -s "Q4 Report" -m "Here's the quarterly report" -o report.surepack
   ```

### Part B: Send the Package

Once packed, send the package to the recipient(s):

```bash
surepack send -i mypackage.surepack
```

The package will be uploaded to the PublicKeyServer server where recipients can download it.

### GUI Method

1. Run `surepack gui`
2. Click **"+ SurePack"** button
3. Select your files and recipients
4. Click **"Create"** then **"Pack to Outbox"**
5. Use **"Send/Receive"** menu to upload

---

## 📥 Receiving Files

Check for packages sent to you:

```bash
surepack receive -a your-alias
```

You'll see a list of waiting packages. Select which to download or press 'a' for all.

To automatically receive all packages:
```bash
surepack receive -a your-alias -f
```

---

## 📂 Unpacking Files

Once received, unpack the files:

```bash
surepack unpack -i package.surepack -o output-folder
```

If you have multiple aliases, it will automatically find the right one.

---

## 🔐 Security Notes

### Your Private Keys
- Stored in: `~/.local/share/surepack/aliases/` (Linux/Mac) or `%LOCALAPPDATA%\surepack\aliases\` (Windows)
- Protected by your password
- **Never share your private keys!**

### Password Security
- Choose a strong password - it protects all your keys
- You'll need it every time you use SurePack
- If forgotten, you'll need to create a new alias

### Verification
- Aliases are verified through the PublicKeyServer server
- Root certificate fingerprints ensure authenticity
- All packages are digitally signed

---

## 🎯 Common Use Cases

### Anonymous File Drop
```bash
# Create anonymous alias (no email)
surepack create

# Send files anonymously  
surepack pack -i whistleblow.pdf -a journalist -f your-alias -o anon.surepack
surepack send -i anon.surepack
```

### Secure Business Documents
```bash
# Create business alias with email
surepack create -e john@company.com

# Send contract with message
surepack pack -i contract.docx -a client@example.com -f john@company.com -s "Contract for Review" -m "Please sign and return" -o contract.surepack
surepack send -i contract.surepack
```

### Batch Processing
```bash
# Pack all PDFs in folder
surepack pack -i "reports/*.pdf" -a manager -f your-alias -o monthly-reports.surepack

# Send from outbox (all pending packages)
surepack send
```

---

## 🚀 Quick Command Reference

| Command | Description |
|---------|-------------|
| `surepack create` | Create a new alias |
| `surepack verify -e email` | Request email verification code |
| `surepack list` | Show your aliases |
| `surepack pack -i file -a recipient -f sender -o output` | Create encrypted package |
| `surepack send -i package` | Upload package to server |
| `surepack receive -a alias` | Download packages |
| `surepack unpack -i package -o folder` | Decrypt and extract files |
| `surepack gui` | Launch graphical interface |

---

## ❓ Troubleshooting

### "Aliases do not share the same root certificate"
- You can only send files between aliases from the same server
- Check the domain part of the aliases match

### "Could not find alias in package"
- You're trying to unpack a file not sent to you
- Check you're using the correct alias

### "Incorrect password"
- Enter the password you used when creating your alias
- Passwords are case-sensitive

---

## 📞 Support

- Website: [publickeyserver.org](https://publickeyserver.org)
- Documentation: [docs.publickeyserver.org](https://docs.publickeyserver.org)
- Issues: [github.com/publickeyserver/surepack/issues](https://github.com/publickeyserver/surepack/issues)

---

## 🎉 You're Ready!

You now know how to:
- ✅ Create a secure alias
- ✅ Pack and encrypt files
- ✅ Send files to others
- ✅ Receive and unpack files

Start with `surepack create` and enjoy secure, private file sharing! 