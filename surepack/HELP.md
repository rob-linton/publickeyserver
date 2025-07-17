# SurePack Help Documentation

## Overview

SurePack is a secure file transfer application that uses post-quantum cryptography to protect your files. It allows you to send files securely and optionally anonymously to other users through encrypted packages.

## Quick Start Guide

### 1. Create Your Identity (Alias)
```bash
surepack create
```

### 2. Pack Your Files
```bash
surepack pack -i document.pdf -a recipient-alias -f your-alias -o package.surepack
```

### 3. Send the Package
```bash
surepack send -i package.surepack
```

### 4. Receive Packages
```bash
surepack receive -a your-alias
```

### 5. Unpack Received Files
```bash
surepack unpack -i package.surepack -o output-folder
```

---

## Complete Command Reference

### `surepack about`
Display version and about information for SurePack.

**Usage:**
```bash
surepack about
```

**Options:**
- `-v, --verbose` : Set output to verbose messages (default: 0)

---

### `surepack certify`
Verify the certificate chain and authenticity of an alias.

**Usage:**
```bash
surepack certify -a alice.publickeyserver.org
```

**Options:**
- `-a, --alias` : The alias to certify (required)
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password
- `-d, --domain` : Domain name

---

### `surepack create`
Create a new alias (digital identity) for sending and receiving encrypted files.

**Usage:**
```bash
# Create anonymous alias
surepack create

# Create alias with email
surepack create -e john@example.com -t 123456
```

**Options:**
- `-e, --email` : Optional email address to associate with alias
- `-t, --token` : Email validation token (obtained via verify command)
- `-d, --domain` : Domain name (default: publickeyserver.org)
- `-p, --passphrase` : Enter password
- `-v, --verbose` : Set output to verbose messages

**Example Output:**
```
Creating...
Domain: publickeyserver.org

Please enter passphrase: ********

[✓] Generated RSA key pair
[✓] Generated Quantum Kyber key pair  
[✓] Generated Quantum Dilithium key pair
[✓] Root certificate fingerprint saved

Alias happy-cloud-tree.publickeyserver.org created
```

---

### `surepack delete`
Delete an alias from both local storage and the server.

**Usage:**
```bash
surepack delete -a old-alias.publickeyserver.org
```

**Options:**
- `-a, --alias` : The alias to delete (required)
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password
- `-d, --domain` : Domain name

**Warning:** This action is permanent and cannot be undone.

---

### `surepack gui`
Launch the graphical user interface.

**Usage:**
```bash
# Launch GUI
surepack gui

# Open a surepack file in GUI
surepack gui -u package.surepack

# Create new package with file in GUI
surepack gui -i document.pdf
```

**Options:**
- `-u, --unpack` : Unpack surepack file in GUI
- `-i, --input` : Input file for packing in GUI
- `-v, --verbose` : Set output to verbose messages

---

### `surepack list`
List all your aliases and verify their certificates.

**Usage:**
```bash
surepack list
```

**Options:**
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password

**Example Output:**
```
alice-smith.publickeyserver.org
bob-jones.publickeyserver.org
charlie-work.company.com
```

---

### `surepack pack`
Create an encrypted package (.surepack file) from one or more files.

**Usage:**
```bash
# Pack single file
surepack pack -i document.pdf -a recipient -f sender -o package.surepack

# Pack multiple files with wildcard
surepack pack -i "*.txt" -a recipient -f sender -o documents.surepack

# Pack with multiple recipients
surepack pack -i file.doc -a alice,bob,charlie -f sender -o shared.surepack

# Pack with subdirectories
surepack pack -i "*" -r -a recipient -f sender -o everything.surepack

# Pack with message and subject
surepack pack -i report.xlsx -a boss -f employee -s "Q4 Report" -m "Please review" -o report.surepack
```

**Options:**
- `-i, --input` : Input file(s) to pack (wildcards supported) (required)
- `-a, --aliases` : Recipient aliases/emails (comma-delimited) (required)
- `-f, --from` : Your sender alias (required)
- `-o, --output` : Output package filename (required)
- `-r, --recurse` : Include subdirectories
- `-s, --subject` : Package subject line
- `-m, --message` : Optional message to include
- `-c, --compression` : Compression type [BROTLI, GZIP, NONE] (default: GZIP)
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password

**Examples:**

1. **Basic file packaging:**
   ```bash
   surepack pack -i invoice.pdf -a client.publickeyserver.org -f mycompany.publickeyserver.org -o invoice-package.surepack
   ```

2. **Pack all PDFs in a folder:**
   ```bash
   surepack pack -i "reports/*.pdf" -a manager -f employee -o monthly-reports.surepack
   ```

3. **Pack for multiple recipients with a message:**
   ```bash
   surepack pack -i contract.docx -a "alice,bob,legal@company.com" -f sender -s "Contract Review" -m "Please sign by Friday" -o contract.surepack
   ```

---

### `surepack receive`
Download encrypted packages sent to your alias from the server.

**Usage:**
```bash
# Interactive receive (choose which packages)
surepack receive -a your-alias

# Automatic receive all packages
surepack receive -a your-alias -f

# Continuous monitoring (check every 60 seconds)
surepack receive -a your-alias -s 60
```

**Options:**
- `-a, --alias` : Your alias to check for packages
- `-f, --force` : Download all packages without prompting
- `-s, --seconds` : Check for packages every X seconds (0 = once only)
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password

**Example Session:**
```
Receiving packages...
Alias: alice.publickeyserver.org

Found 3 packages:
1. package1.surepack (2.5 MB) from bob.publickeyserver.org
2. package2.surepack (512 KB) from charlie.publickeyserver.org
3. package3.surepack (10 MB) from david.publickeyserver.org

Select packages to download (1-3, 'a' for all, 'q' to quit): a

Downloading package1.surepack... Done
Downloading package2.surepack... Done
Downloading package3.surepack... Done
```

---

### `surepack send`
Upload encrypted packages to the server for recipients to download.

**Usage:**
```bash
# Send specific package
surepack send -i package.surepack

# Send all packages in outbox
surepack send
```

**Options:**
- `-i, --input` : Package file to send (optional, sends all outbox if omitted)
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password

**Examples:**

1. **Send a specific package:**
   ```bash
   surepack send -i invoice-package.surepack
   ```

2. **Send all pending packages from outbox:**
   ```bash
   surepack send
   ```

---

### `surepack unpack`
Decrypt and extract files from a .surepack package.

**Usage:**
```bash
# Unpack to specific directory
surepack unpack -i package.surepack -o extracted-files/

# Unpack using specific alias
surepack unpack -i package.surepack -o output/ -a your-alias
```

**Options:**
- `-i, --input` : Input .surepack file to unpack (required)
- `-o, --output` : Output directory for extracted files
- `-a, --alias` : Specific alias to use (auto-detects if omitted)
- `-v, --verbose` : Set output to verbose messages
- `-p, --passphrase` : Enter password

**Example Output:**
```
Unpacking surepack...
Input: package.surepack
Output directory: extracted-files/

Checking alias alice.publickeyserver.org...
[✓] Valid recipient
[✓] Certificate verified
[✓] Decrypting package...

Extracted files:
- document1.pdf (1.2 MB)
- document2.docx (456 KB)
- image.png (2.3 MB)

Package unpacked successfully to: extracted-files/
```

---

### `surepack verify`
Request email verification code to associate an email with an alias.

**Usage:**
```bash
surepack verify -e john@example.com
```

**Options:**
- `-e, --email` : Email address to verify (required)
- `-d, --domain` : Domain name
- `-v, --verbose` : Set output to verbose messages

**Note:** After receiving the verification code via email, use it when creating an alias:
```bash
surepack create -e john@example.com -t 123456
```

---

## Complete Workflow Examples

### Example 1: Anonymous File Transfer

**Sender (Alice):**
```bash
# Create anonymous alias
surepack create
# Output: Alias alice-tree-cloud.publickeyserver.org created

# Pack sensitive documents
surepack pack -i "whistleblower-docs/*" -a journalist.newspaper.com -f alice-tree-cloud.publickeyserver.org -o anonymous-leak.surepack

# Send the package
surepack send -i anonymous-leak.surepack
```

**Recipient (Journalist):**
```bash
# Check for new packages
surepack receive -a journalist.newspaper.com

# Unpack the received file
surepack unpack -i anonymous-leak.surepack -o secure-folder/
```

### Example 2: Business Document Exchange

**Setup:**
```bash
# Request email verification
surepack verify -e john@company.com
# Check email for verification code: 123456

# Create business alias with email
surepack create -e john@company.com -t 123456 -d company.com
```

**Sending Contract:**
```bash
# Pack contract with metadata
surepack pack -i "contract-v2.docx" -a "legal@client.com,ceo@client.com" -f john@company.com -s "Contract for Review" -m "Please review sections 3 and 5 carefully. Sign by EOD Friday." -o contract-package.surepack

# Send to server
surepack send -i contract-package.surepack
```

**Receiving Response:**
```bash
# Check for responses
surepack receive -a john@company.com -f

# Extract signed contract
surepack unpack -i signed-contract.surepack -o completed-contracts/
```

### Example 3: Batch Processing

```bash
# Pack all monthly reports
surepack pack -i "2024-reports/*.pdf" -a accounting@hq.com -f branch-office.company.com -s "Monthly Reports - January 2024" -o january-reports.surepack

# Pack all quarterly data with compression
surepack pack -i "Q4-data/*" -r -c BROTLI -a data-team@company.com -f analytics.company.com -o q4-data.surepack

# Send all packages in outbox
surepack send
```

---

## Global Options

These options are available for all commands:

- `-v, --verbose` : Verbosity level (0=quiet, 1=normal, 2=detailed, 3=debug)
- `-p, --passphrase` : Provide password via command line (not recommended for security)
- `-d, --domain` : Override default domain (publickeyserver.org)

---

## Security Best Practices

1. **Password Protection**: Always use a strong password when creating aliases
2. **Alias Verification**: Use `surepack certify` to verify recipients before sending sensitive data
3. **Anonymous Transfers**: For maximum anonymity, create aliases without email association
4. **Key Storage**: Private keys are stored in:
   - Windows: `%LOCALAPPDATA%\surepack\aliases\`
   - Linux/Mac: `~/.local/share/surepack/aliases/`

---

## Troubleshooting

### Common Issues

**"Incorrect password"**
- Ensure you're using the password set when creating the alias
- Passwords are case-sensitive

**"Could not find alias in package"**
- You're trying to unpack a file not sent to any of your aliases
- Use `surepack list` to see your available aliases

**"Aliases do not share the same root certificate"**
- Sender and recipient must be from compatible servers
- Check domain compatibility with administrator

---

## Need More Help?

- Run `surepack <command> --help` for detailed help on any command
- Visit [publickeyserver.org](https://publickeyserver.org) for documentation
- Report issues at [github.com/publickeyserver/surepack](https://github.com/publickeyserver/surepack) 