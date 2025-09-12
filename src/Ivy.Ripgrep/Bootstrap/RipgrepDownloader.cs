using System;
using System.IO;
using System.IO.Compression;
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
            var apiUrl = $"https://api.github.com/repos/{GitHubRepo}/releases/tags/v{version}";
            
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("User-Agent", "Ivy.Ripgrep");
            
            var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", $"token {token}");
            }

            var response = await _httpClient.SendAsync(request, ct);
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
            
            // Download the checksums file
            var checksumUrl = $"https://github.com/{GitHubRepo}/releases/download/v{version}/ripgrep-{version}-SHA256SUM";
            var checksumResponse = await _httpClient.GetStringAsync(checksumUrl);
            
            // Parse checksums file to find our archive's expected hash
            string? expectedHash = null;
            foreach (var line in checksumResponse.Split('\n'))
            {
                if (line.Contains(archiveName))
                {
                    var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        expectedHash = parts[0].ToUpperInvariant();
                        break;
                    }
                }
            }
            
            if (string.IsNullOrEmpty(expectedHash))
            {
                throw new InvalidOperationException($"Could not find checksum for {archiveName} in checksums file");
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
                await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, outputDir, true));
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
                var info = new FileInfo(filePath);
                info.IsReadOnly = false;
            }
        }

        protected virtual string GetCacheDirectory()
        {
            var baseDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                    ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Caches")
                    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), ".cache");

            return Path.Combine(baseDir, "Ivy.Ripgrep", "bin");
        }
    }
}