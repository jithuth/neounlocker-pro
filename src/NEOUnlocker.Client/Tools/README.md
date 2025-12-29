# Fastboot Tool

Place `fastboot.exe` in this directory.

## Requirements

- **Windows**: Download `fastboot.exe` from Android SDK Platform Tools
- **Source**: https://developer.android.com/studio/releases/platform-tools

## Installation

1. Download Android SDK Platform Tools
2. Extract `fastboot.exe` from the zip file
3. Place `fastboot.exe` in this directory

## Verification

After placing `fastboot.exe`, you can verify it works by running:

```cmd
fastboot --version
```

## Configuration

The fastboot executable path is configured in `appsettings.json`:

```json
"Fastboot": {
  "ExecutablePath": "./Tools/fastboot.exe",
  "DetectionTimeoutSeconds": 60,
  "FlashTimeoutSeconds": 300
}
```

## Security Notice

**IMPORTANT**: `fastboot.exe` is NOT included in the repository. You must download it from official sources (Android SDK Platform Tools).
