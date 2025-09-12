using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ivy.Ripgrep.Bootstrap
{
    public class RipgrepDownloader : IRipgrepBinaryProvider
    {
        private const string DefaultVersion = "14.1.1";
        private const string GitHubRepo = "BurntSushi/ripgrep";
        private readonly HttpClient _httpClient;
        private readonly ILogger<RipgrepDownloader> _logger;
        private readonly string _cacheDirectory;
        protected readonly bool _verifyChecksums;

        public RipgrepDownloader(HttpClient? httpClient = null, ILogger<RipgrepDownloader>? logger = null, bool verifyChecksums = true)
        {
            _httpClient = httpClient ?? new HttpClient();
            _logger = logger ?? NullLogger<RipgrepDownloader>.Instance;
            _cacheDirectory = GetCacheDirectory();
            _verifyChecksums = verifyChecksums;
        }

        public async Task<string> GetBinaryPathAsync(string? version = null, CancellationToken ct = default)
        {
            version ??= Environment.GetEnvironmentVariable("IVY_RIPGREP_VERSION") ?? DefaultVersion;
            
            var target = PlatformDetector.GetRipgrepTarget();
            var binaryName = PlatformDetector.GetBinaryName();
            var cacheDir = Path.Combine(_cacheDirectory, version, target);
            var binaryPath = Path.Combine(cacheDir, binaryName);

            if (File.Exists(binaryPath))
            {
                _logger.LogDebug("Using cached ripgrep binary at {Path}", binaryPath);
                return binaryPath;
            }

            _logger.LogInformation("Downloading ripgrep {Version} for {Target}", version, target);
            await DownloadAndExtractAsync(version, target, cacheDir, ct);
            
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SetExecutablePermission(binaryPath);
            }

            return binaryPath;
        }

        private async Task DownloadAndExtractAsync(string version, string target, string outputDir, CancellationToken ct)
        {
            Directory.CreateDirectory(outputDir);
            
            var archiveExt = PlatformDetector.GetArchiveExtension();
            var archiveName = $"ripgrep-{version}-{target}{archiveExt}";
            var archivePath = Path.Combine(Path.GetTempPath(), archiveName);

            try
            {
                var downloadUrl = await GetDownloadUrlAsync(version, archiveName, ct);
                await DownloadFileAsync(downloadUrl, archivePath, ct);
                
                if (_verifyChecksums)
                {
                    await VerifyChecksumAsync(version, archiveName, archivePath, ct);
                }
                
                await ExtractArchiveAsync(archivePath, outputDir, archiveExt);
                
                // Write integrity manifest
                var manifestPath = Path.Combine(outputDir, "integrity.json");
                var manifest = new
                {
                    version = version,
                    target = target,
                    downloadedAt = DateTime.UtcNow,
                    checksumVerified = _verifyChecksums,
                    source = downloadUrl
                };
                await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(manifest), ct);
            }
            finally
            {
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }
            }
        }

        private async Task<string> GetDownloadUrlAsync(string version, string assetName, CancellationToken ct)
        {
            var apiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/tags/{version}";
            
            _logger.LogDebug("Fetching release info from: {Url}", apiUrl);
            
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("User-Agent", "Ivy.Ripgrep");
            
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"token {token}");
            }

            var response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to fetch release info from {Url}. Status: {Status}. Content: {Content}", 
                    apiUrl, response.StatusCode, content);
            }
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            var assets = doc.RootElement.GetProperty("assets");
            foreach (var asset in assets.EnumerateArray())
            {
                if (asset.GetProperty("name").GetString() == assetName)
                {
                    return asset.GetProperty("browser_download_url").GetString()!;
                }
            }

            throw new InvalidOperationException($"Asset {assetName} not found in release {version}");
        }

        private async Task DownloadFileAsync(string url, string outputPath, CancellationToken ct)
        {
            _logger.LogDebug("Downloading from {Url} to {Path}", url, outputPath);
            
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            using var fileStream = File.Create(outputPath);
            await response.Content.CopyToAsync(fileStream);
        }

        protected virtual async Task VerifyChecksumAsync(string version, string archiveName, string archivePath, CancellationToken ct)
        {
            _logger.LogInformation("Verifying checksum for {Archive}", archiveName);
            
            // Download the individual checksum file for this archive
            var checksumUrl = $"https://github.com/{GitHubRepo}/releases/download/{version}/{archiveName}.sha256";
            string expectedHash;
            
            try
            {
                var checksumResponse = await _httpClient.GetStringAsync(checksumUrl);
                
                // Parse different checksum file formats
                // Format 1: Just the hash
                // Format 2: "hash  filename"
                // Format 3: Windows CertUtil output
                var lines = checksumResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length == 1)
                {
                    // Single line - either just hash or "hash  filename"
                    expectedHash = lines[0].Trim().Split(' ', '\t')[0].ToUpperInvariant();
                }
                else if (lines.Any(l => l.Contains("SHA256 hash of")))
                {
                    // Windows CertUtil format - hash is on the second line
                    var hashLine = lines.FirstOrDefault(l => !l.Contains("SHA256") && !l.Contains("CertUtil") && l.Trim().Length > 0);
                    expectedHash = hashLine?.Trim().ToUpperInvariant() ?? "";
                }
                else
                {
                    // Try to find a line that looks like a hash (64 hex chars)
                    var hashLine = lines.FirstOrDefault(l => System.Text.RegularExpressions.Regex.IsMatch(l.Trim(), "^[a-fA-F0-9]{64}"));
                    expectedHash = hashLine?.Trim().ToUpperInvariant() ?? checksumResponse.Trim().Split(' ', '\t')[0].ToUpperInvariant();
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Could not download checksum file from {Url}: {Message}. Skipping verification.", checksumUrl, ex.Message);
                return; // Skip verification if checksum file doesn't exist
            }
            
            if (string.IsNullOrEmpty(expectedHash))
            {
                _logger.LogWarning("Checksum file was empty for {Archive}. Skipping verification.", archiveName);
                return;
            }
            
            // Calculate actual hash
            using var sha256 = SHA256.Create();
            using var fileStream = File.OpenRead(archivePath);
            var hashBytes = await Task.Run(() => sha256.ComputeHash(fileStream), ct);
            var actualHash = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();
            
            // Verify
            if (actualHash != expectedHash)
            {
                _logger.LogError("Checksum verification failed. Expected: {Expected}, Actual: {Actual}", expectedHash, actualHash);
                throw new InvalidOperationException($"Checksum verification failed for {archiveName}. The file may be corrupted or tampered with.");
            }
            
            _logger.LogInformation("Checksum verified successfully for {Archive}", archiveName);
        }

        private async Task ExtractArchiveAsync(string archivePath, string outputDir, string extension)
        {
            if (extension == ".zip")
            {
                // Extract to a temp directory first
                var tempExtractDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempExtractDir);
                
                try
                {
                    await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, tempExtractDir, true));
                    
                    // Find the rg.exe file and move it to the output directory
                    var rgExePath = Directory.GetFiles(tempExtractDir, "rg.exe", SearchOption.AllDirectories).FirstOrDefault();
                    if (rgExePath != null)
                    {
                        var targetPath = Path.Combine(outputDir, "rg.exe");
                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }
                        File.Move(rgExePath, targetPath);
                    }
                    else
                    {
                        throw new FileNotFoundException("Could not find rg.exe in the extracted archive");
                    }
                }
                finally
                {
                    // Clean up temp directory
                    if (Directory.Exists(tempExtractDir))
                    {
                        Directory.Delete(tempExtractDir, true);
                    }
                }
            }
            else if (extension == ".tar.gz")
            {
                await ExtractTarGzAsync(archivePath, outputDir);
            }
            else
            {
                throw new NotSupportedException($"Archive format {extension} is not supported");
            }
        }

        private async Task ExtractTarGzAsync(string archivePath, string outputDir)
        {
            using var fileStream = File.OpenRead(archivePath);
            using var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress);
            using var memoryStream = new MemoryStream();
            await gzipStream.CopyToAsync(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);

            var buffer = new byte[512];
            while (memoryStream.Read(buffer, 0, 512) == 512)
            {
                var name = Encoding.ASCII.GetString(buffer, 0, 100).TrimEnd('\0');
                if (string.IsNullOrWhiteSpace(name))
                    break;

                var sizeStr = Encoding.ASCII.GetString(buffer, 124, 11).TrimEnd('\0');
                var size = Convert.ToInt64(sizeStr, 8);

                if (name.EndsWith("/"))
                {
                    Directory.CreateDirectory(Path.Combine(outputDir, name));
                }
                else if (size > 0)
                {
                    var filePath = Path.Combine(outputDir, name);
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                    await using var outputFile = File.Create(filePath);
                    var remaining = size;
                    var fileBuffer = new byte[4096];
                    while (remaining > 0)
                    {
                        var toRead = (int)Math.Min(remaining, fileBuffer.Length);
                        var read = memoryStream.Read(fileBuffer, 0, toRead);
                        if (read == 0)
                            break;
                        outputFile.Write(fileBuffer, 0, read);
                        remaining -= read;
                    }

                    var padding = (512 - (size % 512)) % 512;
                    if (padding > 0)
                        memoryStream.Seek(padding, SeekOrigin.Current);
                }
                else
                {
                    var padding = (512 - (size % 512)) % 512;
                    if (padding > 0)
                        memoryStream.Seek(padding, SeekOrigin.Current);
                }
            }
        }

        private void SetExecutablePermission(string filePath)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // On Unix-like systems, we need to set the executable bit using chmod
                // since netstandard2.1 doesn't have UnixFileMode
                try
                {
                    using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{filePath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true,
                        RedirectStandardOutput = true
                    });
                    
                    if (process != null)
                    {
                        process.WaitForExit();
                        if (process.ExitCode != 0)
                        {
                            var error = process.StandardError.ReadToEnd();
                            _logger.LogWarning("Failed to set executable permission on {FilePath}: {Error}", filePath, error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to set executable permission on {FilePath}", filePath);
                }
            }
        }

        protected virtual string GetCacheDirectory()
        {
            string baseDir;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (string.IsNullOrEmpty(home))
                    home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
                baseDir = Path.Combine(home, "Library", "Caches");
            }
            else // Linux and other Unix-like systems
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                if (string.IsNullOrEmpty(home))
                    home = Environment.GetEnvironmentVariable("HOME") ?? "/tmp";
                baseDir = Path.Combine(home, ".cache");
            }

            return Path.Combine(baseDir, "Ivy.Ripgrep", "bin");
        }
    }
}