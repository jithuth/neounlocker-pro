# Bootloader Files

Place bootloader files here according to the model mapping in `appsettings.json`.

## Expected Files

The following bootloader files should be placed in this directory:

- `E5573.bin` - For Huawei E5573 model
- `E8372.bin` - For Huawei E8372 model
- `E5577.bin` - For Huawei E5577 model
- `E3372.bin` - For Huawei E3372 model

## File Format

Bootloader files should be in binary format (`.bin`) and must match the model exactly.

## Security Notice

**IMPORTANT**: Bootloader files are NOT included in the repository for security and licensing reasons. You must obtain these files separately from authorized sources.

## Configuration

The mapping between router models and bootloader files is configured in `appsettings.json`:

```json
"Bootloaders": {
  "Directory": "./Resources/Bootloaders",
  "ModelMapping": {
    "E5573": "E5573.bin",
    "E8372": "E8372.bin",
    "E5577": "E5577.bin",
    "E3372": "E3372.bin"
  }
}
```

You can add more models by updating this configuration and placing the corresponding bootloader files in this directory.
