# NEOUnlocker Pro - Developer Guide

## Prerequisites

### Development Environment
- .NET 8 SDK or later
- Windows 10/11 (for client development and testing)
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

### For Production Deployment
- Windows Server 2019+ or Azure App Service (for server)
- Windows 10/11 (for client)
- SSL certificate for HTTPS
- Azure Key Vault or similar for secure key storage (recommended)

## Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/jithuth/neounlocker-pro.git
cd neounlocker-pro
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Build the Solution

```bash
dotnet build
```

## Server Setup

### Configuration

Edit `src/NEOUnlocker.Server/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "FirmwareSettings": {
    "StoragePath": "./FirmwareStorage",
    "MasterEncryptionKeyBase64": ""  // Leave empty for dev, set for production
  },
  "SessionSettings": {
    "SessionExpiryMinutes": 15,
    "MaxConcurrentSessions": 100
  }
}
```

### Generate Master Encryption Key (Production)

```bash
# Generate a 32-byte (256-bit) key and encode as Base64
openssl rand -base64 32
```

Copy the output to `MasterEncryptionKeyBase64` in appsettings.json or store in environment variable:

```bash
export FirmwareSettings__MasterEncryptionKeyBase64="<your-base64-key>"
```

### Prepare Encrypted Firmware

Before running the server, you need to encrypt your firmware files and place them in the storage directory:

1. Create the firmware storage directory:
   ```bash
   mkdir -p src/NEOUnlocker.Server/FirmwareStorage
   ```

2. Encrypt your firmware files using AES-256-GCM with your master key:
   - Format: `[12-byte nonce][16-byte tag][encrypted data]`
   - Save as: `<firmware-name>.enc` (e.g., `system.bin.enc`)

3. Place encrypted files in the `FirmwareStorage` directory

**Note:** A firmware encryption utility should be developed for production use.

### Run the Server

#### Development Mode

```bash
cd src/NEOUnlocker.Server
dotnet run
```

Server will start on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

#### Production Mode

```bash
cd src/NEOUnlocker.Server
dotnet publish -c Release -o ./publish
cd publish
dotnet NEOUnlocker.Server.dll
```

## Client Setup

### Configuration

Edit `src/NEOUnlocker.Client/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ServerSettings": {
    "BaseUrl": "https://localhost:5001",
    "ApiPath": "/api/flash"
  },
  "ToolSettings": {
    "ToolsPath": "./tools",
    "ValidateToolIntegrity": true
  },
  "SecuritySettings": {
    "RSAKeySize": 2048,
    "SecureFileOverwritePasses": 3
  }
}
```

### Prepare Native Tools

1. Create the tools directory:
   ```bash
   mkdir -p src/NEOUnlocker.Client/bin/Debug/net8.0-windows/tools
   ```

2. Place the native flash tools in the directory:
   - `bln.exe` (for MTK devices)
   - `fastboot.exe` (for Qualcomm devices)
   - Any required DLL dependencies

### Run the Client

#### Development Mode

```bash
cd src/NEOUnlocker.Client
dotnet run
```

**Note:** Client must be run on Windows due to WPF and DPAPI requirements.

#### Production Mode

```bash
cd src/NEOUnlocker.Client
dotnet publish -c Release -o ./publish -r win-x64 --self-contained
```

The published application will be in `./publish` directory.

## API Documentation

### Endpoints

#### 1. Create Flash Session

**POST** `/api/flash/sessions`

Request:
```json
{
  "hwid": "ABC123...",
  "deviceType": "MTK6580",
  "clientPublicKeyPem": "-----BEGIN PUBLIC KEY-----\n..."
}
```

Response:
```json
{
  "sessionId": "xyz...",
  "wrappedSessionKeyBase64": "...",
  "expiresAt": "2025-12-29T16:00:00Z",
  "status": "Active",
  "firmwareFiles": ["system.bin", "usbloader-5577.bin"],
  "creditCost": 1
}
```

#### 2. Get Session Details

**GET** `/api/flash/sessions/{sessionId}?hwid={hwid}`

Response: Same as create session response

#### 3. Download Firmware

**GET** `/api/flash/sessions/{sessionId}/firmware/{fileName}?hwid={hwid}`

Response: Binary stream of re-encrypted firmware

#### 4. Complete Session

**POST** `/api/flash/sessions/{sessionId}/complete`

Request:
```json
{
  "hwid": "ABC123...",
  "success": true,
  "errorMessage": null
}
```

Response:
```json
{
  "success": true,
  "message": "Session completed successfully",
  "creditsDeducted": true
}
```

## Security Architecture

### Key Management

1. **Master Key (Server)**
   - AES-256 key for encrypting firmware at rest
   - Stored securely (Azure Key Vault recommended)
   - Never leaves the server

2. **Session Key**
   - Generated per session (AES-256)
   - Wrapped with client's RSA public key
   - Used to re-encrypt firmware for transmission

3. **Client Keypair**
   - RSA-2048 keypair generated on first run
   - Private key encrypted with Windows DPAPI
   - Stored in user's LocalApplicationData

### Data Flow

1. Client generates RSA keypair (first run)
2. Client requests session with HWID and public key
3. Server generates session key and wraps it with client's public key
4. Client unwraps session key with private key
5. Client downloads firmware encrypted with session key
6. Client decrypts firmware in memory only
7. Client streams to native tool via secure temp files
8. Temp files securely deleted (3-pass overwrite)
9. Session burned after completion

### Memory Safety

All sensitive data is zeroed using `CryptographicOperations.ZeroMemory()`:
- Session keys
- Decrypted firmware
- Temporary buffers
- Private keys (after use)

## Supported Devices

### MTK6580
- Tool: `bln.exe`
- Firmware: `system.bin`, `usbloader-5577.bin`

### Qualcomm8937
- Tool: `fastboot.exe`
- Firmware: `system.bin`, `boot.img`

## Troubleshooting

### Server Issues

**Error: Master key not configured**
- Solution: Set `FirmwareSettings:MasterEncryptionKeyBase64` in configuration
- For development: Leave empty to use random key (not secure)

**Error: Firmware file not found**
- Solution: Ensure encrypted firmware files exist in `FirmwareStorage` directory
- Files must be named: `<firmware-name>.enc`

### Client Issues

**Error: Failed to load HWID**
- Solution: Ensure WMI service is running
- Check system permissions for hardware access

**Error: Tool not found**
- Solution: Place native tools in `tools` directory
- Verify tools are executable

**Error: Failed to connect to server**
- Solution: Check server is running
- Verify `ServerSettings:BaseUrl` in client configuration
- Check firewall settings

## Development Notes

### Building on Linux

The client project uses WPF which is Windows-only. To build on Linux for CI:
- The `EnableWindowsTargeting` property is set to `true` in the client csproj
- The build will succeed but the executable can only run on Windows

### Testing

Currently, no automated tests are included. Manual testing steps:

1. Start the server
2. Verify API endpoints respond correctly
3. Start the client
4. Verify HWID is generated
5. Test flash operation with mock devices

### Adding New Device Types

1. Update `DeviceFirmwareMap` in `FirmwareService.cs`
2. Update `DeviceToolMap` in `FlashClient.cs`
3. Ensure encrypted firmware files exist
4. Add device type to client UI ComboBox

## Production Deployment

### Server Deployment Checklist

- [ ] Set strong master encryption key
- [ ] Enable HTTPS with valid certificate
- [ ] Configure proper logging (Application Insights, etc.)
- [ ] Set up monitoring and alerts
- [ ] Implement rate limiting
- [ ] Add authentication/authorization
- [ ] Backup encrypted firmware regularly
- [ ] Set up automatic session cleanup
- [ ] Configure CORS properly
- [ ] Enable firewall rules

### Client Deployment Checklist

- [ ] Code sign the executable
- [ ] Include all native tools and dependencies
- [ ] Configure correct server URL
- [ ] Test on clean Windows installation
- [ ] Create installer (MSI/MSIX)
- [ ] Include user documentation
- [ ] Test with various Windows versions
- [ ] Verify antivirus doesn't flag tools

## License

MIT License - See [LICENSE](LICENSE) file for details

## Support

For issues and questions:
- GitHub Issues: https://github.com/jithuth/neounlocker-pro/issues
- Security Issues: security@neounlocker.com (DO NOT create public issues)

---

**⚠️ Security Warning:** This system handles confidential firmware. Always follow security best practices and never disable security features in production.
