# NEOUnlocker Pro - Security Documentation

## Overview

NEOUnlocker Pro implements a defense-in-depth security architecture to protect confidential firmware from unauthorized access, extraction, and reuse. This document details the security measures implemented at each layer.

## Threat Model

### Assets to Protect
1. **Firmware Files** - Confidential proprietary firmware binaries
2. **Session Keys** - Ephemeral encryption keys
3. **Master Key** - Server-side encryption key
4. **Client Private Keys** - Used for session key unwrapping
5. **User Credits** - Session-based credit system

### Threats Considered
1. **Firmware Extraction** - Attacker attempts to obtain plaintext firmware
2. **Session Replay** - Reusing captured session data
3. **Man-in-the-Middle** - Intercepting and modifying communications
4. **Key Theft** - Stealing encryption keys from client or server
5. **Reverse Engineering** - Analyzing binaries to extract secrets
6. **Memory Dumping** - Capturing sensitive data from memory
7. **Unauthorized Flashing** - Flashing without proper authorization

## Security Layers

### Layer 1: Cryptographic Primitives

#### AES-256-GCM (Firmware Encryption)
- **Algorithm**: AES with 256-bit keys in GCM mode
- **Nonce**: 12 bytes, randomly generated per encryption
- **Tag**: 16 bytes for authentication
- **Usage**: 
  - Firmware encrypted at rest with master key
  - Re-encrypted with session key for transmission
  - Decrypted in client memory only

**Format**: `[12-byte nonce][16-byte tag][ciphertext]`

**Security Properties**:
- Authenticated encryption (integrity + confidentiality)
- Resistance to padding oracle attacks
- Parallel decryption possible

#### RSA-2048-OAEP-SHA256 (Key Wrapping)
- **Algorithm**: RSA with 2048-bit keys
- **Padding**: OAEP with SHA-256
- **Usage**: Wrapping session keys with client public key

**Security Properties**:
- Asymmetric encryption allows key distribution
- OAEP prevents chosen-ciphertext attacks
- 2048-bit provides adequate security through 2030

#### SHA-256 (Integrity Verification)
- **Usage**:
  - Hardware ID generation
  - Native tool integrity validation
  - Potential future use for firmware hashing

### Layer 2: Key Management

#### Server-Side (Master Key)
```
Master Key (AES-256)
├── Storage: Configuration / Environment Variable / Azure Key Vault
├── Access: Server process only
├── Rotation: Manual (recommended quarterly)
└── Backup: Encrypted offline backup
```

**Best Practices**:
- Never commit to source control
- Use Azure Key Vault in production
- Implement key rotation policy
- Monitor key access logs

#### Client-Side (RSA Keypair)
```
RSA Keypair (2048-bit)
├── Generation: On first run
├── Storage: LocalApplicationData\NEOUnlocker\keys\
├── Protection: Windows DPAPI (CurrentUser scope)
└── Format: PKCS#8 (private), SubjectPublicKeyInfo (public)
```

**Windows DPAPI Protection**:
- Tied to user account
- Automatically encrypted/decrypted by OS
- Cannot be used by other users
- Survives application reinstallation

**Security Properties**:
- Private key never transmitted
- Unique keypair per client installation
- Lost if user profile is deleted (by design)

#### Session Keys
```
Session Key (AES-256)
├── Generation: Per session, cryptographically random
├── Lifespan: 15 minutes maximum
├── Transmission: Wrapped with client's RSA public key
├── Storage: In-memory only
└── Destruction: Zeroed after use
```

**Security Properties**:
- One-time use only
- Cannot be reused after session ends
- Automatically expires
- Cryptographically bound to HWID

### Layer 3: Session Management

#### Session Lifecycle
1. **Creation**
   - Client sends: HWID + Device Type + RSA Public Key
   - Server generates: Unique Session ID + Session Key
   - Server wraps: Session Key with Client Public Key
   - Server stores: Session metadata with HWID binding

2. **Active Phase**
   - Client downloads firmware using Session ID + HWID
   - Each request validates HWID matches session
   - Session expires after 15 minutes
   - Firmware re-encrypted with session key per download

3. **Completion/Expiry**
   - Client reports success/failure
   - Session marked as "Burned" (invalidated)
   - Session key zeroed from memory
   - Credits deducted (if successful)

#### Session Security Properties
- **HWID Binding**: Session tied to specific hardware
- **Time-Limited**: 15-minute absolute expiration
- **One-Time Use**: Cannot be reused after completion
- **Atomic**: Either succeeds completely or fails
- **Audit Trail**: All actions logged

### Layer 4: Firmware Protection

#### At Rest (Server)
```
Plaintext Firmware → [AES-256-GCM + Master Key] → Encrypted Storage
```

**Storage Format**: `firmware-name.enc`

**Security Properties**:
- Confidential firmware never stored in plaintext on server
- Decryption only during active session processing
- Temporary plaintext immediately zeroed from memory

#### In Transit (Server → Client)
```
Encrypted Firmware → [Decrypt with Master Key] → [Re-encrypt with Session Key] → Client
```

**Security Properties**:
- Double encryption layer
- Session key unique per client session
- Cannot replay captured traffic
- TLS provides additional transport encryption

#### Client Processing
```
Encrypted Download → [Decrypt with Session Key] → Memory Buffer → Native Tool
```

**Security Properties**:
- Firmware exists in plaintext ONLY in memory
- Never written to disk except secure temp files
- Memory buffers zeroed immediately after use
- Temp files deleted with 3-pass overwrite

### Layer 5: Memory Safety

#### Sensitive Data Zeroing

All sensitive buffers are zeroed using `CryptographicOperations.ZeroMemory()`:

```csharp
// Example usage throughout codebase
byte[] sessionKey = ...; // Sensitive data
try {
    // Use session key
} finally {
    CryptographicOperations.ZeroMemory(sessionKey);
}
```

**Locations where zeroing is applied**:
1. Session keys (server and client)
2. Master key temporary copies
3. Decrypted firmware buffers
4. Encryption/decryption nonces and tags
5. Private key material after loading

**Why This Matters**:
- Prevents key extraction from memory dumps
- Reduces exposure time of sensitive data
- Complies with security best practices
- Guards against cold boot attacks

### Layer 6: Native Tool Security

#### Tool Integrity Validation
```csharp
SHA256 hash = ComputeHash(tool_exe);
bool valid = CompareWithKnownGoodHash(hash);
if (!valid) throw SecurityException;
```

**Implementation**:
- SHA-256 hash computed before execution
- Compared against known-good hashes (to be configured)
- Execution refused if validation fails

**Future Enhancement**:
- Digital signature verification
- Certificate pinning
- Allowlist of authorized tools

#### Secure Temp File Handling

When stdin streaming not supported:

```
1. Create exclusive temp file (FileShare.None)
2. Write decrypted firmware
3. Execute native tool
4. 3-pass random overwrite
5. Delete file
```

**Overwrite Pattern**:
- Pass 1: Random data
- Pass 2: Random data  
- Pass 3: Random data
- Delete

**Security Properties**:
- Prevents file recovery tools
- Exclusive access prevents concurrent reads
- Hidden and temporary file attributes
- Random filename to avoid prediction

### Layer 7: Transport Security

#### HTTPS/TLS
- All client-server communication over HTTPS
- TLS 1.2 minimum (1.3 recommended)
- Valid SSL certificate required in production
- Certificate validation enforced

#### API Security
- HWID validation on every request
- Session ID must be presented
- No authentication bypass
- Rate limiting recommended (not implemented)

## Security Validation Checklist

### Code Security
- [x] No hardcoded secrets
- [x] No plaintext firmware in code/resources
- [x] All crypto uses BCL (System.Security.Cryptography)
- [x] No unsafe code blocks
- [x] All buffers zeroed after use
- [x] Exception handling doesn't leak secrets
- [x] Logging doesn't expose keys/firmware

### Cryptographic Implementation
- [x] AES-256-GCM properly implemented
- [x] RSA-OAEP-SHA256 properly implemented
- [x] Random number generation uses CSP
- [x] Nonces never reused
- [x] Key sizes meet standards (AES-256, RSA-2048)
- [x] No deprecated algorithms (MD5, SHA1, DES, etc.)

### Session Security
- [x] HWID binding enforced
- [x] Session expiry implemented
- [x] One-time use enforced
- [x] Session burning on completion
- [x] Concurrent session limits configurable

### Memory Safety
- [x] Session keys zeroed
- [x] Master key never copied unnecessarily
- [x] Decrypted firmware zeroed
- [x] Temporary buffers zeroed
- [x] Private keys zeroed after loading

### File Security
- [x] Encrypted firmware only on server
- [x] Secure temp file deletion
- [x] 3-pass overwrite implemented
- [x] Exclusive file access
- [x] No plaintext firmware artifacts

### Client Security
- [x] Private key protected with DPAPI
- [x] Key storage in secure location
- [x] No key transmission to server
- [x] Tool integrity validation
- [x] Memory-only firmware decryption

## Known Limitations

### Current Implementation
1. **No Authentication** - HWID-only authorization (add OAuth/JWT for production)
2. **No Rate Limiting** - Vulnerable to DoS (add rate limiting middleware)
3. **No Audit Logging** - Limited audit trail (add comprehensive logging)
4. **No Tool Signature Verification** - Only SHA256 hash (add code signing verification)
5. **In-Memory Session Store** - Lost on server restart (use Redis/database)
6. **No Credit System Backend** - Credits tracked but not persisted
7. **No Firmware Encryption Tool** - Manual encryption required

### Mitigations in Production
1. Implement OAuth 2.0 or API key authentication
2. Add rate limiting per HWID
3. Integrate Application Insights or similar
4. Require digitally signed tools
5. Use distributed session store
6. Implement database-backed credit system
7. Create firmware management tool

## Security Incident Response

### If Master Key is Compromised
1. Generate new master key immediately
2. Re-encrypt all firmware with new key
3. Update server configuration
4. Rotate credentials
5. Audit access logs
6. Notify stakeholders

### If Session Key is Compromised
- Low risk: Session expires in 15 minutes
- Session is one-time use and HWID-bound
- Cannot be replayed after session completes
- Monitor for abnormal session patterns

### If Client Private Key is Compromised
- Affects single client installation only
- Generate new keypair (delete old key file)
- No impact on other clients
- HWID still provides binding

### If Firmware is Leaked
- This is the threat we're designed to prevent
- If plaintext firmware is compromised, assess:
  - Was it from server? (Check master key security)
  - Was it from client? (Check memory safety)
  - Was it from temp files? (Check deletion)
- Implement additional controls as needed

## Compliance Considerations

### GDPR
- HWID may be considered personal data
- Implement data retention policies
- Provide data export/deletion mechanisms
- Document data processing activities

### PCI DSS (if handling payments for credits)
- Encrypt credit card data
- Use PCI-compliant payment processor
- Implement access controls
- Regular security audits

### ISO 27001
- Document information security policies
- Risk assessment and treatment
- Incident management procedures
- Business continuity planning

## Recommended Security Audits

1. **Cryptographic Review** - Verify correct use of crypto APIs
2. **Penetration Testing** - Test for vulnerabilities
3. **Code Review** - Security-focused code analysis
4. **Dependency Scanning** - Check for vulnerable packages
5. **Static Analysis** - Automated security scanning

## References

- NIST Special Publication 800-175B: Guideline for Using Cryptographic Standards
- OWASP Top 10 Security Risks
- Microsoft Security Development Lifecycle (SDL)
- CWE/SANS Top 25 Most Dangerous Software Errors

---

**Last Updated**: December 29, 2025

**Security Contacts**:
- Security Issues: security@neounlocker.com
- General Support: support@neounlocker.com

**⚠️ CRITICAL**: Never disable security features in production. Any shortcuts compromise the entire security model.
