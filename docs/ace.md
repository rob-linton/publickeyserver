# Anonymous Certificate Enrollment (ACE) Protocol

## Abstract

This document describes the Anonymous Certificate Enrollment (ACE) protocol, a simplified adaptation of the Enrollment over Secure Transport (EST) protocol defined in [RFC 7030](https://tools.ietf.org/html/rfc7030). EST describes a simple, yet functional, certificate management protocol targeting Public Key Infrastructure (PKI) clients that need to acquire client certificates and associated Certification Authority (CA) certificates.

The ACE protocol extends EST's foundation to provide automated certificate enrollment while preserving client anonymity. Unlike traditional PKI systems that require identity verification, ACE enables secure end-to-end encrypted communication using anonymous aliases, ensuring that private keys never leave the client device.

## 1. Introduction

### 1.1 Relationship to EST (RFC 7030)

EST profiles certificate enrollment for clients using Certificate Management over CMS (CMC) messages over a secure transport. The ACE protocol adopts EST's RESTful approach and transport security model while introducing anonymity as a core design principle.

Key similarities to EST:
- Use of HTTPS for secure transport
- RESTful API design patterns
- Certificate lifecycle management capabilities
- Support for client-generated key pairs

Key differences from EST:
- Removal of identity-based authentication requirements
- Simplified enrollment process without CMC complexity
- Anonymous alias-based certificate binding
- Streamlined API with fewer configuration options

### 1.2 Design Rationale

The ACE protocol addresses specific use cases where:
1. **Privacy is paramount** - Users require secure communication without revealing identity
2. **Simplicity matters** - Reduced complexity compared to full EST implementation
3. **Automation is essential** - No manual identity verification steps
4. **Security is maintained** - Strong cryptographic binding between certificates and aliases

## 2. Technical Specifications

### 2.1 Cryptographic Standards

The ACE protocol employs industry-standard cryptographic algorithms:

**RSA Key Pairs**
- Algorithm: RSA-2048
- Justification: Provides 112-bit security strength per NIST SP 800-57, adequate for medium-term use while maintaining compatibility with legacy systems

**Certificate Signatures**
- Algorithm: SHA512withRSA
- Justification: SHA-512 provides strong collision resistance; RSA signatures ensure broad compatibility

**Certificate Format**
- Standard: X.509 v3 certificates per RFC 5280
- Extensions: Custom OIDs for anonymous metadata storage

### 2.2 Alias Generation

Aliases follow a three-word hyphenated format (e.g., `missions-locks-sox.publickeyserver.org`):
- **Uniqueness**: Random selection from large word pools prevents collisions
- **Memorability**: Human-readable format aids in practical use
- **Anonymity**: No correlation to user identity or attributes

## 3. Protocol Operations

### 3.1 Transport Security

Following EST's model, EST specifies how to transfer messages securely via HTTP over TLS (HTTPS). ACE requires:
- Minimum TLS version: 1.2
- Server authentication via valid TLS certificate
- No client authentication requirement (preserving anonymity)

### 3.2 API Endpoints

The ACE protocol implements a subset of EST functionality plus anonymous-specific extensions:

#### 3.2.1 GET /cacerts

Returns the certificate chain for the ACE Certificate Authority.

**Purpose**: Allows clients to establish trust in the CA before enrollment
**Format**: `application/json` containing PEM-encoded certificate chain
**EST Equivalent**: `/cacerts` endpoint per RFC 7030 Section 4.1

#### 3.2.2 GET /simpleenroll

Quick enrollment method (not recommended for production use).

**Security Consideration**: Server-side key generation violates the principle that private keys should never leave the client device. Provided only for testing/development.

#### 3.2.3 POST /simpleenroll

Primary enrollment method for certificate issuance.

**Request Body**:
```json
{
  "key": "[public key in PEM format (RSA2048)]",
  "data": "[BASE64ENCODED DATA]"
}
```

**Process**:
1. Client generates RSA-2048 key pair locally
2. Client submits public key with optional metadata
3. Server validates request format
4. Server generates unique alias
5. Server issues certificate binding alias to public key

**Response**: `application/json` containing issued certificate in PEM format

#### 3.2.4 Certificate Retrieval Endpoints

ACE extends EST with flexible certificate retrieval:

1. `GET /cert?alias={alias}` - Query parameter format
2. `GET /cert/{alias}` - RESTful path parameter
3. `GET https://{alias}.publickeyserver.org` - Subdomain-based retrieval

**Justification**: Multiple retrieval methods accommodate different client capabilities and network configurations.

#### 3.2.5 GET /serverkeygen

Test endpoint for server-generated key pairs.

**Warning**: For development/testing only. Production systems must generate keys client-side.

## 4. Security Considerations

### 4.1 Anonymous Authentication Challenges

Unlike EST which relies on authentication of the EST client, ACE must prevent abuse without identity:

**Rate Limiting**: IP-based and temporal restrictions prevent mass enrollment
**Proof of Work**: Optional computational challenges for enrollment requests
**Revocation**: Alias-based revocation without revealing identity

### 4.2 Privacy Protection

- **No Identity Storage**: Server maintains no mapping between aliases and real identities
- **Metadata Sanitization**: Optional data fields are base64-encoded to prevent information leakage
- **Access Logs**: Minimal logging to prevent correlation attacks

### 4.3 Certificate Lifecycle

**Validity Period**: Recommended 90-365 days
**Renewal**: New enrollment required (no identity-based renewal)
**Revocation**: Standard CRL/OCSP with alias as identifier

## 5. Implementation Guidelines

### 5.1 Client Requirements

1. **Secure Key Storage**: Private keys must be generated and stored securely on client device
2. **TLS Validation**: Proper validation of server TLS certificate
3. **Alias Management**: Secure storage and retrieval of assigned aliases

### 5.2 Server Requirements

1. **Scalability**: Support for high-volume anonymous enrollments
2. **Audit Trail**: Privacy-preserving audit logs for security monitoring
3. **CA Integration**: Secure interface to issuing Certificate Authority

### 5.3 Interoperability

While ACE is not directly compatible with EST clients, it follows similar design patterns:
- RESTful API structure
- HTTPS transport
- Standard X.509 certificates
- JSON message formatting

## 6. Privacy and Compliance

### 6.1 Data Minimization

ACE implements privacy by design:
- No personally identifiable information (PII) collected
- Anonymous aliases prevent user tracking
- Optional metadata under user control

### 6.2 Regulatory Compliance

The protocol design considers:
- GDPR Article 25: Data protection by design and default
- No special category data processing
- User right to anonymity preserved

## 7. Operational Considerations

### 7.1 Deployment Models

1. **Public Service**: Open enrollment for anonymous communication
2. **Private Network**: Organization-specific anonymous certificates
3. **Hybrid**: Authenticated enrollment with anonymous usage

### 7.2 Monitoring and Maintenance

- **Health Checks**: Regular validation of CA chain and service availability
- **Performance Metrics**: Enrollment rate, certificate issuance time
- **Security Monitoring**: Anomaly detection without compromising anonymity

## 8. Future Considerations

### 8.1 Post-Quantum Readiness

Future versions may incorporate:
- Quantum-resistant algorithms (e.g., CRYSTALS-Dilithium)
- Hybrid classical/post-quantum certificates
- Algorithm agility for smooth transitions

### 8.2 Enhanced Privacy Features

Potential enhancements:
- Zero-knowledge proofs for attribute verification
- Blind signatures for enhanced anonymity
- Decentralized trust models

## References

- [RFC 7030] Pritikin, M., Ed., Yee, P., Ed., and D. Harkins, Ed., "Enrollment over Secure Transport", RFC 7030, October 2013.
- [RFC 5280] Cooper, D., et al., "Internet X.509 Public Key Infrastructure Certificate and Certificate Revocation List (CRL) Profile", RFC 5280, May 2008.
- [RFC 8555] Barnes, R., et al., "Automatic Certificate Management Environment (ACME)", RFC 8555, March 2019.
- [NIST SP 800-57] NIST, "Recommendation for Key Management, Part 1: General", NIST Special Publication 800-57 Part 1 Revision 5, May 2020.

## Appendix A: Implementation Status

The ACE protocol has been implemented and is operational at https://publickeyserver.org. The reference implementation is available at https://github.com/rob-linton/publickeyserver.

## Appendix B: Acknowledgments

The ACE protocol builds upon the foundational work of EST (RFC 7030) while addressing the specific needs of anonymous certificate enrollment. We acknowledge the EST authors and the broader IETF community for establishing the standards that make this work possible.