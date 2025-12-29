# NEOUnlocker Pro - User Guide

## Quick Start Guide

This guide will help you unlock your Huawei router in three simple steps.

---

## Before You Begin

### What You Need

1. **Software**
   - NEOUnlocker Pro application
   - .NET 8 Runtime (if not using self-contained version)
   - Fastboot.exe (Android SDK Platform Tools)
   - Bootloader file for your router model

2. **Hardware**
   - Huawei router (E5573, E8372, E5577, E3372, or compatible)
   - USB cable (data cable, not charge-only)
   - Windows PC (Windows 10/11)

3. **Drivers**
   - Huawei USB drivers installed
   - Fastboot drivers installed

### Important Notes

⚠️ **Warning**: 
- Unlocking may void your warranty
- Backup your data before proceeding
- Ensure battery is >50% charged
- Only unlock devices you own

---

## Installation

### 1. Install Dependencies

#### Download Fastboot
1. Download [Android SDK Platform Tools](https://developer.android.com/studio/releases/platform-tools)
2. Extract `fastboot.exe`
3. Place in `Tools/` folder of NEOUnlocker Pro

#### Get Bootloader Files
1. Obtain bootloader file for your specific router model
2. Place in `Resources/Bootloaders/` folder
3. Update `appsettings.json` if needed:
   ```json
   "Bootloaders": {
     "ModelMapping": {
       "YourModel": "YourBootloader.bin"
     }
   }
   ```

#### Install Drivers
1. Install Huawei USB drivers (usually included with router software)
2. Install Android Fastboot drivers (from Android SDK Platform Tools)

### 2. Launch Application

Double-click `NEOUnlocker.Client.exe` to start the application.

---

## Step-by-Step Instructions

### Step 1: Read Router Properties

**Purpose**: Connect to your router and read device information

1. **Connect Your Router**
   - Plug router into PC via USB
   - Wait for Windows to recognize the device
   - Router should be in normal mode (not fastboot)

2. **Scan for Ports**
   - Application will auto-scan on startup
   - Or click "Scan Ports" button
   - Available COM ports will be listed

3. **Connect**
   - Select the correct COM port from dropdown
   - Typically named "3G PC UI Interface" or similar
   - Click "Connect" button
   - Status will show "Connected" in green

4. **Read Properties**
   - Click "Read Properties" button
   - Application will send AT commands to router
   - Watch the log panel for progress
   - Properties will appear in text boxes:
     - Manufacturer (e.g., "HUAWEI")
     - Model (e.g., "E5573")
     - IMEI (15-digit number)
     - Firmware Version
     - Lock Status

5. **Verify Information**
   - Check that model matches your router
   - Ensure IMEI is displayed correctly
   - If information is incomplete, disconnect and try again

**Troubleshooting**:
- ❌ **No COM ports found**: Install USB drivers
- ❌ **Connection fails**: Try different USB port or cable
- ❌ **No response to AT commands**: Router may not be in correct mode
- ❌ **Wrong information**: Try disconnecting and reconnecting

---

### Step 2: Switch to Fastboot Mode

**Purpose**: Prepare router for bootloader flashing

1. **Read Instructions**
   - Application will display instructions
   - You must manually switch router to fastboot mode

2. **Switch Router to Fastboot Mode**
   
   **Method varies by model**:
   
   **E5573 / E5577**:
   - Turn off router
   - Press and hold power button
   - Plug in USB cable while holding power
   - Release after 5 seconds
   - LED should turn solid (not blinking)

   **E8372 / E3372**:
   - May require physical button combination
   - Or use ADB/AT command: `AT^FASTBOOT`
   - Consult your router manual

3. **Detect Fastboot Device**
   - Once router is in fastboot mode
   - Click "Detect Fastboot Device"
   - Application will scan for device (up to 60 seconds)
   - Countdown timer will show remaining time

4. **Wait for Detection**
   - Status will show "Detected" in green when found
   - Device serial number will be displayed
   - If timeout occurs, retry step 2

**Troubleshooting**:
- ❌ **Device not detected**: Ensure router is in fastboot mode (LED solid)
- ❌ **Timeout**: Try different USB port or reinstall fastboot drivers
- ❌ **Wrong mode**: Router LED should be solid, not blinking
- ❌ **Fastboot.exe not found**: Check Tools/ folder

---

### Step 3: Unlock Device

**Purpose**: Flash bootloader and unlock the router

1. **Verify Bootloader**
   - Application will check for bootloader file
   - File must match your router model
   - If not found, operation will fail with error

2. **Start Unlock Process**
   - Click "Start Unlock" button
   - **Do NOT disconnect** router during this process
   - Progress bar will show status

3. **Monitor Progress**
   
   **Stages**:
   - "Preparing" - Validating bootloader file
   - "Flashing" - Transferring bootloader to device
   - "Unlocking" - Executing OEM unlock command
   - "Rebooting" - Restarting device
   - "Complete" - Success!

4. **Watch Log Panel**
   - Real-time progress messages
   - Any errors will be displayed in red
   - Success messages in green

5. **Wait for Completion**
   - Router will reboot automatically
   - LED will blink during reboot
   - Process takes 1-3 minutes typically

6. **Success!**
   - Message box will confirm success
   - Router is now unlocked
   - You can disconnect USB cable

**Troubleshooting**:
- ❌ **Bootloader not found**: Check Resources/Bootloaders/ folder
- ❌ **Flash fails**: Ensure battery >50%, try again
- ❌ **Unlock fails**: Some routers may already be unlocked
- ❌ **Device disconnects**: Use better USB cable, don't move router

---

## Understanding the Log Panel

The log panel shows real-time progress with color coding:

- **Black**: Informational messages
- **Green**: Success messages  
- **Orange**: Warnings (non-critical)
- **Red**: Errors (requires action)

Each message includes a timestamp `[HH:MM:SS]`.

---

## Common Issues & Solutions

### Port Connection Issues

**Problem**: Can't find COM port
- **Solution**: Install Huawei USB drivers, restart PC

**Problem**: Port listed but connection fails
- **Solution**: Try different port, check cable

**Problem**: "AT" command timeout
- **Solution**: Router not in correct mode, replug USB

### Fastboot Detection Issues

**Problem**: Fastboot device not detected
- **Solution**: Verify router in fastboot mode (LED solid)

**Problem**: "Fastboot not found" error
- **Solution**: Place fastboot.exe in Tools/ folder

**Problem**: Detection timeout
- **Solution**: Reinstall fastboot drivers, try manual detection

### Unlock Process Issues

**Problem**: Bootloader file not found
- **Solution**: Add correct .bin file to Resources/Bootloaders/

**Problem**: Flash operation fails
- **Solution**: Ensure battery charged, stable connection

**Problem**: Permission denied
- **Solution**: Run as Administrator

**Problem**: Device reboots before completion
- **Solution**: Better USB cable, direct PC port (not hub)

---

## After Unlocking

### Verify Unlock Status

1. Reconnect router in normal mode
2. Repeat Step 1 (Read Properties)
3. Check "Lock Status" field
4. Should show "Unlocked"

### What's Next?

Your router is now unlocked! You can:
- Install custom firmware
- Change SIM locks
- Modify network settings
- Use with any carrier

**Note**: Always backup before making changes.

---

## Safety Tips

✅ **DO**:
- Backup data before unlocking
- Use official bootloader files
- Keep battery charged >50%
- Use quality USB cables
- Read instructions carefully

❌ **DON'T**:
- Disconnect during flashing
- Use charge-only USB cables
- Unlock with low battery
- Use untrusted bootloader files
- Interrupt the process

---

## Getting Help

### Check Logs
- Review log panel for error messages
- Logs saved to `Logs/neounlocker.log`

### Common Error Codes
- `ERROR: Timeout` - Increase timeout in config
- `Port not available` - Close other applications using port
- `Device not found` - Check drivers and connections
- `File not found` - Check file paths in appsettings.json

### Support Resources
- GitHub Issues: [Report a bug](https://github.com/jithuth/neounlocker-pro/issues)
- Documentation: Check README.md
- Community Forums: XDA Developers, GSM Forums

---

## Legal & Warranty

### Disclaimer
- This software is provided "as is"
- No warranty of any kind
- Use at your own risk
- Authors not responsible for damage

### Legal Considerations
- Only unlock devices you own
- Check local laws and regulations
- May void manufacturer warranty
- May violate carrier terms of service

### Privacy
- Application does not collect or transmit data
- All operations are local
- No internet connection required (except for downloads)

---

## Advanced Configuration

### Edit appsettings.json

```json
{
  "SerialPort": {
    "BaudRate": 115200,        // Usually 115200
    "ReadTimeout": 5000,        // Increase if slow response
    "WriteTimeout": 5000
  },
  "Fastboot": {
    "DetectionTimeoutSeconds": 60,   // Max wait time
    "FlashTimeoutSeconds": 300       // Max flash time
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"  // Debug for more details
    }
  }
}
```

### Add Custom Bootloader Mapping

```json
"Bootloaders": {
  "ModelMapping": {
    "YourModel": "YourBootloader.bin"
  }
}
```

---

## Tips for Success

1. **Preparation**: Have everything ready before starting
2. **Patience**: Don't rush, follow each step carefully
3. **Stable Connection**: Use direct PC USB port, not hub
4. **Good Cable**: Use data-capable USB cable
5. **Full Battery**: Charge device >50% minimum
6. **Read Logs**: Monitor progress in log panel
7. **Backup**: Always backup before major changes

---

## Supported Devices

### Tested Models
- Huawei E5573
- Huawei E8372
- Huawei E5577
- Huawei E3372

### Potentially Compatible
- Other Huawei routers with fastboot support
- May require specific bootloader files

### Not Supported
- Non-Huawei devices
- Routers without fastboot mode
- Devices without AT command support

---

## Version Information

**Application**: NEOUnlocker Pro v1.0
**Framework**: .NET 8
**Platform**: Windows 10/11
**License**: MIT

---

**Good luck unlocking your router!** 🔓

For more information, see:
- README.md - Project overview
- ROUTER_UNLOCK_IMPLEMENTATION.md - Technical details
- appsettings.json - Configuration reference
