using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NEOUnlocker.Client.Services;

/// <summary>
/// Implementation of native tool executor with security measures.
/// </summary>
public class NativeToolExecutor : INativeToolExecutor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<NativeToolExecutor> _logger;
    private readonly string _toolsPath;
    private readonly bool _validateIntegrity;
    private readonly int _overwritePasses;

    public NativeToolExecutor(IConfiguration configuration, ILogger<NativeToolExecutor> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        _toolsPath = configuration["ToolSettings:ToolsPath"] ?? "./tools";
        _validateIntegrity = configuration.GetValue<bool>("ToolSettings:ValidateToolIntegrity", true);
        _overwritePasses = configuration.GetValue<int>("SecuritySettings:SecureFileOverwritePasses", 3);
    }

    /// <inheritdoc/>
    public async Task<bool> ExecuteToolAsync(
        string toolName,
        string arguments,
        Dictionary<string, Stream> firmwareStreams,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var toolPath = Path.Combine(_toolsPath, toolName);
        
        if (!File.Exists(toolPath))
        {
            _logger.LogError("Tool not found: {ToolPath}", toolPath);
            progress?.Report($"Error: Tool not found: {toolName}");
            return false;
        }

        // Validate tool integrity
        if (_validateIntegrity && !ValidateToolIntegrity(toolPath))
        {
            _logger.LogError("Tool integrity validation failed: {ToolPath}", toolPath);
            progress?.Report($"Error: Tool integrity validation failed: {toolName}");
            return false;
        }

        _logger.LogInformation("Executing tool: {ToolName} with args: {Arguments}", toolName, arguments);
        progress?.Report($"Starting {toolName}...");

        // Create temporary files for firmware (only when stdin not supported)
        var tempFiles = new List<string>();
        
        try
        {
            // Write firmware to secure temp files
            foreach (var kvp in firmwareStreams)
            {
                var tempFile = await WriteToSecureTempFileAsync(kvp.Key, kvp.Value, cancellationToken);
                tempFiles.Add(tempFile);
                
                // Replace placeholders in arguments
                arguments = arguments.Replace($"{{{kvp.Key}}}", tempFile);
            }

            // Execute the tool
            var startInfo = new ProcessStartInfo
            {
                FileName = toolPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("Tool output: {Output}", e.Data);
                    progress?.Report(e.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogWarning("Tool error: {Error}", e.Data);
                    progress?.Report($"Error: {e.Data}");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            var exitCode = process.ExitCode;
            _logger.LogInformation("Tool exited with code: {ExitCode}", exitCode);
            
            if (exitCode == 0)
            {
                progress?.Report($"{toolName} completed successfully");
                return true;
            }
            else
            {
                progress?.Report($"{toolName} failed with exit code {exitCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute tool: {ToolName}", toolName);
            progress?.Report($"Error executing {toolName}: {ex.Message}");
            return false;
        }
        finally
        {
            // Securely delete all temp files
            foreach (var tempFile in tempFiles)
            {
                await SecureDeleteFileAsync(tempFile);
            }
        }
    }

    private bool ValidateToolIntegrity(string toolPath)
    {
        try
        {
            // In production, this would validate against known SHA256 hashes
            // For now, just verify the file exists and is readable
            using var fs = File.OpenRead(toolPath);
            var hash = SHA256.HashData(fs);
            var hashString = Convert.ToHexString(hash);
            
            _logger.LogInformation("Tool {Tool} SHA256: {Hash}", Path.GetFileName(toolPath), hashString);
            
            // TODO: Compare against known good hashes from configuration
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate tool integrity: {ToolPath}", toolPath);
            return false;
        }
    }

    private async Task<string> WriteToSecureTempFileAsync(
        string fileName,
        Stream stream,
        CancellationToken cancellationToken)
    {
        // Create temp file with exclusive access
        var tempDir = Path.Combine(Path.GetTempPath(), "NEOUnlocker");
        Directory.CreateDirectory(tempDir);
        
        var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid()}_{fileName}");
        
        _logger.LogInformation("Writing firmware to temp file: {TempFile}", tempFile);
        
        // Write with exclusive access (FileShare.None)
        using (var fs = new FileStream(
            tempFile,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }
        
        // Set file attributes to hidden and temporary
        File.SetAttributes(tempFile, FileAttributes.Hidden | FileAttributes.Temporary);
        
        return tempFile;
    }

    private async Task SecureDeleteFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            _logger.LogInformation("Securely deleting temp file: {FilePath}", filePath);
            
            // Get file size
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;
            
            // 3-pass overwrite with random data
            var random = RandomNumberGenerator.Create();
            var buffer = new byte[Math.Min(fileSize, 1024 * 1024)]; // 1MB buffer
            
            for (int pass = 0; pass < _overwritePasses; pass++)
            {
                using var fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Write,
                    FileShare.None);
                
                fs.Seek(0, SeekOrigin.Begin);
                
                long remaining = fileSize;
                while (remaining > 0)
                {
                    var toWrite = (int)Math.Min(remaining, buffer.Length);
                    random.GetBytes(buffer, 0, toWrite);
                    await fs.WriteAsync(buffer, 0, toWrite);
                    remaining -= toWrite;
                }
                
                await fs.FlushAsync();
            }
            
            // Zero the buffer
            CryptographicOperations.ZeroMemory(buffer);
            
            // Finally delete the file
            File.Delete(filePath);
            
            _logger.LogInformation("Securely deleted temp file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to securely delete file: {FilePath}", filePath);
            
            // Attempt regular delete as fallback
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
