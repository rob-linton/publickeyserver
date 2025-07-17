# PublicKeyServer Documentation

Comprehensive documentation for the PublicKeyServer and SurePack secure file transfer system

[Overview](#overview) | [Getting Started](#getting-started) | [Technical Docs](#technical-docs) | [Resources](#resources) | [GitHub](https://github.com/rob-linton/publickeyserver)

## What is PublicKeyServer?

PublicKeyServer is a next-generation secure file transfer platform that implements the **Anonymous Certificate Enrollment (ACE)** protocol. It provides end-to-end encrypted file sharing with optional anonymous identity capabilities, designed as a modern, user-friendly alternative to PGP.

> **Key Innovation:** PublicKeyServer combines military-grade encryption with consumer-grade simplicity. Users get memorable three-word aliases (like `happy-cloud-tree.publickeyserver.org`) instead of complex key fingerprints, while benefiting from post-quantum cryptography that protects against future threats.

### Core Components

- **SureDrop Server:** REST API server providing certificate management and encrypted file relay
- **SurePack Client:** Cross-platform CLI and GUI for creating, sending, and receiving encrypted packages
- **ACE Protocol:** Anonymous certificate system based on EST (RFC7030) principles
- **Hybrid Encryption:** AES-256-GCM + RSA-2048 + Kyber1024 (quantum-resistant)

### Key Features

End-to-End Encryption | Post-Quantum Ready | Anonymous Option | No Key Management | Perfect Forward Secrecy | Zero-Knowledge Server | Integrated Delivery | Digital Signatures

## Getting Started

### 🚀 Simple Step-by-Step Guide
A beginner-friendly walkthrough of how the certificate system works, explained with simple analogies and clear examples.
- [simple-analysis.html](simple-analysis.html) • 15KB

### 📖 SurePack User Manual
Complete command reference and usage guide for SurePack, including examples for packing, sending, and receiving files.
- [HELP.html](HELP.html) • 24KB

## Technical Documentation

### 🔍 Deep Technical Analysis
Comprehensive technical overview of the entire system architecture, security model, and implementation details.
- [README.html](README.html) • 18KB

### 🔐 Certificate System Analysis
Detailed technical breakdown of how the certificate creation, enrollment, and verification processes work.
- [analysis.html](analysis.html) • 25KB

### 📄 ACE Protocol Specification
The Anonymous Certificate Enrollment protocol specification, detailing the REST API and certificate format.
- [ace.html](ace.html) • 11KB

## Additional Resources

### 💼 Business Case & Pitch
Executive summary and business justification for SurePack, including market analysis and competitive advantages.
- [PITCH.html](PITCH.html) • 39KB

### Quick Links

- **Live Demo:** [publickeyserver.org](https://publickeyserver.org)
- **Downloads:** Available for Windows, macOS, and Linux at the main site
- **Source Code:** [github.com/rob-linton/publickeyserver](https://github.com/rob-linton/publickeyserver)
- **API Reference:** See the ACE Protocol documentation for REST API details

## Understanding the Technology Stack

### Cryptography

- **Classical Encryption:** RSA-2048 for key exchange
- **Symmetric Encryption:** AES-256-GCM for file content
- **Post-Quantum KEM:** CRYSTALS-Kyber1024 (NIST approved)
- **Digital Signatures:** SHA-512 with RSA + CRYSTALS-Dilithium5
- **Key Derivation:** Argon2id for password-based encryption

### Implementation

- **Server:** ASP.NET Core REST API
- **Client:** .NET 8 (cross-platform)
- **Storage:** Amazon S3 for scalability
- **Crypto Library:** Bouncy Castle
- **GUI Framework:** Terminal.Gui

---

PublicKeyServer Documentation • [GitHub](https://github.com/rob-linton/publickeyserver)

© 2024 PublicKeyServer Project