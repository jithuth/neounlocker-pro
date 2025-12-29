# NEOUnlocker Pro

**Production-Ready Secure Firmware Flash System**

A Windows desktop application (.NET 8, WPF) with ASP.NET Core backend for securely flashing devices using native tools while protecting confidential firmware.

## 🔒 Security-First Architecture

NEOUnlocker Pro implements defense-in-depth security measures to ensure firmware remains confidential and cannot be reused:

- ✅ **Zero Plaintext Firmware on Client**: Firmware decrypted only in memory during flash
- ✅ **One-Time Sessions**: Cryptographically secured, HWID-bound, 15-minute expiry
- ✅ **Memory-Only Decryption**: Sensitive data zeroed immediately after use
- ✅ **Secure Key Management**: RSA-2048 with Windows DPAPI, AES-256-GCM encryption
- ✅ **Session Burning**: Automatic deactivation after use/failure

## 📋 Project Status

🚧 **Initial Setup** - Implementation in progress

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────┐
│     Windows Desktop Client (.NET 8)      │
│  ┌────────────────────────────────────┐ │
│  │ • RSA Key Management (DPAPI)      │ │
│  │ • HWID Generation & Binding       │ │
│  │ • Memory-Only Firmware Decrypt    │ │
│  │ • Secure Native Tool Execution    │ │
│  └────────────────────────────────────┘ │
└─────────────────────────────────────────┘
              ▲ │ HTTPS
              │ ▼
┌─────────────────────────────────────────┐
│   ASP.NET Core Backend (.NET 8)         │
│  ┌────────────────────────────────────┐ │
│  │ • One-Time Session Management     │ │
│  │ • Encrypted Firmware Storage      │ │
│  │ • Session Key Wrapping (RSA)      │ │
│  │ • Streaming Re-encryption         │ │
│  └────────────────────────────────────┘ │
└─────────────────────────────────────────┘
```

## 🛠️ Technology Stack

- **Backend**: ASP.NET Core 8.0
- **Client**: .NET 8 WPF
- **Encryption**: AES-256-GCM, RSA-2048-OAEP-SHA256
- **Key Storage**: Windows DPAPI (client), Azure Key Vault (server)
- **Native Tools**: bln.exe, fastboot.exe

## 📁 Project Structure

```
neounlocker-pro/
├── src/
│   ├── NEOUnlocker.Server/       # ASP.NET Core REST API
│   └── NEOUnlocker.Client/       # Windows WPF Desktop App
├── docs/                          # Documentation
├── tools/                         # Native flash tools
└── tests/                         # Unit & integration tests
```

## 🔐 Security Guarantees

### What We Protect Against
- ❌ Firmware extraction and reuse
- ❌ Man-in-the-middle attacks
- ❌ Session replay attacks
- ❌ Unauthorized device flashing
- ❌ Firmware reverse engineering

### How We Protect
- ✅ End-to-end encryption with ephemeral session keys
- ✅ Hardware-bound sessions (HWID validation)
- ✅ Cryptographic memory zeroing
- ✅ Tool integrity validation (SHA256)
- ✅ 3-pass secure file deletion
- ✅ Time-limited sessions with automatic expiry

## 🚀 Quick Start

### Prerequisites
- .NET 8 SDK
- Windows 10/11 (for client)
- Visual Studio 2022 or JetBrains Rider

### Server Setup
```bash
cd src/NEOUnlocker.Server
dotnet restore
dotnet run
```

### Client Setup
```bash
cd src/NEOUnlocker.Client
dotnet restore
dotnet run
```

## 📖 Documentation

- [Architecture Guide](docs/architecture.md) *(coming soon)*
- [Security Model](docs/security.md) *(coming soon)*
- [API Documentation](docs/api.md) *(coming soon)*
- [Deployment Guide](docs/deployment.md) *(coming soon)*

## 🤝 Contributing

This is a security-critical project. All contributions must:
1. Pass security review
2. Include comprehensive tests
3. Follow coding standards
4. Maintain zero-trust principles

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## ⚠️ Security Disclosure

Found a security vulnerability? Please email: security@neounlocker.com

**Do NOT create public issues for security vulnerabilities.**

## 🎯 Roadmap

- [x] Architecture design
- [ ] Server implementation
- [ ] Client implementation
- [ ] Security hardening
- [ ] Comprehensive testing
- [ ] Production deployment
- [ ] Documentation completion

---

**Built with security, performance, and reliability in mind.** 🛡️