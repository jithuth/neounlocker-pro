# NEOUnlocker Pro

**Production-Ready Router Unlock Desktop Application**

A Windows desktop application (.NET 8, Windows Forms) for unlocking Huawei routers through a secure three-step process using AT commands and fastboot operations.

## 🔓 Overview

NEOUnlocker Pro is a professional tool designed to unlock Huawei routers by:
1. Reading router properties via AT commands (3G PC UI Interface)
2. Switching device to fastboot mode (Huawei Download Port)
3. Flashing bootloader and executing unlock commands

## 📋 Project Status

🚀 **Core Implementation Complete** - Windows Forms application with full three-step unlock workflow

## 🏗️ Architecture Overview

```
┌─────────────────────────────────────────┐
│   Windows Desktop Application (.NET 8)  │
│                                          │
│  ┌────────────────────────────────────┐ │
│  │     Three-Step Unlock Process     │ │
│  │                                    │ │
│  │  Step 1: Read Router Properties   │ │
│  │  • Serial Port (AT Commands)      │ │
│  │  • IMEI, Model, Firmware, Status  │ │
│  │                                    │ │
│  │  Step 2: Switch to Fastboot       │ │
│  │  • Disconnect serial port         │ │
│  │  • Detect fastboot device         │ │
│  │  • Verify connection              │ │
│  │                                    │ │
│  │  Step 3: Unlock Device            │ │
│  │  • Flash bootloader               │ │
│  │  • Execute unlock command         │ │
│  │  • Reboot device                  │ │
│  └────────────────────────────────────┘ │
└─────────────────────────────────────────┘
            ↕ Serial Port (AT)
            ↕ Fastboot Protocol
┌─────────────────────────────────────────┐
│         Huawei Router Device             │
│  • 3G PC UI Interface (AT Commands)     │
│  • Huawei Download Port (Fastboot)      │
└─────────────────────────────────────────┘
```

## 🛠️ Technology Stack

- **Framework**: .NET 8 Windows Forms
- **Serial Communication**: System.IO.Ports
- **Device Detection**: System.Management (WMI)
- **Fastboot**: Native fastboot.exe integration
- **Architecture**: Service-based with dependency injection
- **Async/Await**: Non-blocking UI operations

## ✨ Features

### Step 1: Read Router Properties
- ✅ COM port scanning and auto-detection
- ✅ Huawei device identification via WMI
- ✅ AT command communication
- ✅ Retrieve: Manufacturer, Model, IMEI, Firmware, Lock Status
- ✅ Store properties in memory for Step 3

### Step 2: Switch to Fastboot Mode
- ✅ Safe serial port disconnection
- ✅ Clear user instructions for mode switching
- ✅ Automatic fastboot device detection
- ✅ Device serial number verification
- ✅ Timeout handling with countdown
- ✅ Manual retry option

### Step 3: Unlock Device
- ✅ Model-based bootloader selection
- ✅ Progress tracking with percentage
- ✅ Fastboot flashing operations
- ✅ OEM unlock command execution
- ✅ Automatic device reboot
- ✅ Error handling and retry logic

### User Interface
- ✅ Modern Windows Forms design
- ✅ Step-by-step wizard flow
- ✅ Real-time progress tracking
- ✅ Color-coded log panel
- ✅ Status bar with elapsed time
- ✅ Responsive async operations
- ✅ Professional error handling

## 📁 Project Structure

```
neounlocker-pro/
├── src/
│   ├── NEOUnlocker.Client/         # Windows Forms Application
│   │   ├── Forms/
│   │   │   ├── MainForm.cs         # Main UI form
│   │   │   └── MainForm.Designer.cs
│   │   ├── Services/
│   │   │   ├── SerialPortService.cs      # AT command communication
│   │   │   ├── FastbootService.cs        # Fastboot operations
│   │   │   └── RouterService.cs          # Three-step orchestration
│   │   ├── Models/
│   │   │   ├── RouterInfo.cs             # Router properties
│   │   │   ├── UnlockProgress.cs         # Progress tracking
│   │   │   └── UnlockResult.cs           # Result model
│   │   ├── Helpers/
│   │   │   ├── ATCommandHelper.cs        # AT command utilities
│   │   │   └── PortDetectionHelper.cs    # COM port detection
│   │   ├── Resources/
│   │   │   └── Bootloaders/              # Bootloader files (not included)
│   │   ├── Tools/
│   │   │   └── fastboot.exe              # Fastboot tool (not included)
│   │   └── appsettings.json
│   └── NEOUnlocker.Server/         # ASP.NET Core Backend (separate project)
└── docs/                            # Documentation
```

## 🚀 Quick Start

### Prerequisites
- **Windows 10/11** (64-bit)
- **.NET 8 SDK** or Runtime
- **Fastboot.exe** (Android SDK Platform Tools)
- **Bootloader files** for your router model
- **USB Drivers** for Huawei devices

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/jithuth/neounlocker-pro.git
   cd neounlocker-pro
   ```

2. **Install fastboot.exe**
   - Download [Android SDK Platform Tools](https://developer.android.com/studio/releases/platform-tools)
   - Extract `fastboot.exe` to `src/NEOUnlocker.Client/Tools/`

3. **Add bootloader files**
   - Place bootloader files in `src/NEOUnlocker.Client/Resources/Bootloaders/`
   - Update `appsettings.json` with model mappings

4. **Build and run**
   ```bash
   cd src/NEOUnlocker.Client
   dotnet restore
   dotnet run
   ```

   Or open `NEOUnlocker.sln` in Visual Studio 2022 and press F5.

## 🔧 Configuration

Edit `src/NEOUnlocker.Client/appsettings.json`:

```json
{
  "SerialPort": {
    "BaudRate": 115200,
    "DataBits": 8,
    "Parity": "None",
    "StopBits": "One",
    "ReadTimeout": 5000,
    "WriteTimeout": 5000
  },
  "Fastboot": {
    "ExecutablePath": "./Tools/fastboot.exe",
    "DetectionTimeoutSeconds": 60,
    "FlashTimeoutSeconds": 300
  },
  "Bootloaders": {
    "Directory": "./Resources/Bootloaders",
    "ModelMapping": {
      "E5573": "E5573.bin",
      "E8372": "E8372.bin"
    }
  }
}
```

## 📖 Usage

### Step-by-Step Guide

1. **Launch Application**
   - Start NEOUnlocker Pro
   - Application will auto-scan for COM ports

2. **Step 1: Read Router Properties**
   - Connect router via USB (3G PC UI Interface mode)
   - Select COM port from dropdown
   - Click "Connect"
   - Click "Read Properties"
   - Verify: Model, IMEI, Firmware are displayed

3. **Step 2: Switch to Fastboot Mode**
   - Follow on-screen instructions
   - Switch device to Fastboot mode (Huawei Download Port)
   - Click "Detect Fastboot Device"
   - Wait for device detection (up to 60 seconds)
   - Device serial will be displayed when detected

4. **Step 3: Unlock Device**
   - Verify correct bootloader file is mapped for your model
   - Click "Start Unlock"
   - Monitor progress in the log panel
   - Wait for completion message
   - Device will reboot automatically

### AT Commands Used

The application uses standard AT commands:
- `AT+CGMI` - Get manufacturer
- `AT+CGMM` - Get model
- `AT+CGSN` - Get IMEI/serial number
- `AT+CGMR` - Get firmware version
- `AT^CARDLOCK?` - Get lock status
- `ATI` - Get device information

### Fastboot Commands

The application executes:
- `fastboot devices` - Detect connected device
- `fastboot flash bootloader <file>` - Flash bootloader
- `fastboot oem unlock` - Unlock device
- `fastboot reboot` - Reboot device

## ⚠️ Important Notes

### Security & Legal
- **Bootloader files are NOT included** in this repository for security and licensing reasons
- Only unlock devices you own or have authorization to unlock
- Unlocking may void warranty and violate terms of service
- Use at your own risk

### Requirements
- **Drivers**: Install Huawei USB drivers before use
- **Bootloaders**: Must be obtained separately for each model
- **Administrator**: May require admin privileges for some operations
- **Backup**: Backup device data before unlocking

### Supported Devices
- Huawei E5573
- Huawei E8372
- Huawei E5577
- Huawei E3372
- Other Huawei routers (with appropriate bootloader files)

## 🐛 Troubleshooting

### COM Port Issues
- Ensure USB drivers are installed
- Try different USB ports
- Check Device Manager for port number
- Restart device and try again

### Fastboot Not Detected
- Verify fastboot.exe is in Tools directory
- Check device is in fastboot mode (not normal mode)
- Try `fastboot devices` in command prompt
- Install Huawei fastboot drivers

### Bootloader Flash Fails
- Verify bootloader file matches device model exactly
- Check file is not corrupted
- Ensure sufficient battery (>50%)
- Try different USB cable/port

## 📄 License

MIT License - See [LICENSE](LICENSE) file for details

## ⚠️ Disclaimer

This software is provided "as is" without warranty of any kind. The authors are not responsible for any damage or data loss that may occur from using this software. Always backup your data and understand the risks before proceeding.

## 🎯 Roadmap

- [x] Core three-step unlock process
- [x] Windows Forms UI
- [x] Serial port communication (AT commands)
- [x] Fastboot integration
- [x] Progress tracking and logging
- [ ] Multi-language support
- [ ] Device compatibility checker
- [ ] Automatic bootloader download
- [ ] Advanced diagnostics mode
- [ ] Batch unlock support

---

**Built for educational and research purposes.** 🔓