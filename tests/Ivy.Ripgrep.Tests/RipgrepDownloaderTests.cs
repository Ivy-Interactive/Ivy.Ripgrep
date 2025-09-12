using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Ivy.Ripgrep.Bootstrap;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class RipgrepDownloaderTests : IDisposable
    {
        private readonly string _testCacheDir;

        public RipgrepDownloaderTests()
        {
            _testCacheDir = Path.Combine(Path.GetTempPath(), $"ripgrep_test_cache_{Guid.NewGuid()}");
        }

        [Fact]
        public async Task GetBinaryPathAsync_WithCachedBinary_ReturnsCachedPath()
        {
            // Arrange
            var version = "14.1.1";
            var target = PlatformDetector.GetRipgrepTarget();
            var binaryName = PlatformDetector.GetBinaryName();
            var expectedPath = Path.Combine(_testCacheDir, version, target, binaryName);
            
            Directory.CreateDirectory(Path.GetDirectoryName(expectedPath)!);
            File.WriteAllText(expectedPath, "dummy binary content");

            Environment.SetEnvironmentVariable("IVY_RIPGREP_CACHE_DIR", _testCacheDir);
            
            var downloader = new TestableRipgrepDownloader(_testCacheDir, new HttpClient());

            // Act
            var result = await downloader.GetBinaryPathAsync(version);

            // Assert
            result.Should().Be(expectedPath);
        }

        [Fact]
        public async Task VerifyChecksumAsync_WithMatchingChecksum_Succeeds()
        {
            // Arrange
            var testFile = Path.Combine(_testCacheDir, "test.zip");
            Directory.CreateDirectory(_testCacheDir);
            await File.WriteAllTextAsync(testFile, "test content");
            
            // Calculate expected hash for "test content"
            var expectedHash = "6AE8A75555209FD6C44157C0AED8016E763FF435A19CF186F76863140143FF72";
            
            var checksumFileContent = $"{expectedHash.ToLower()}  ripgrep-14.1.1-x86_64-pc-windows-msvc.zip\n";
            
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(checksumFileContent)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var downloader = new TestableRipgrepDownloader(_testCacheDir, httpClient);

            // Act & Assert
            var act = async () => await downloader.TestVerifyChecksum("14.1.1", "ripgrep-14.1.1-x86_64-pc-windows-msvc.zip", testFile);
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task VerifyChecksumAsync_WithMismatchedChecksum_ThrowsException()
        {
            // Arrange
            var testFile = Path.Combine(_testCacheDir, "test.zip");
            Directory.CreateDirectory(_testCacheDir);
            await File.WriteAllTextAsync(testFile, "test content");
            
            var wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";
            var checksumFileContent = $"{wrongHash}  ripgrep-14.1.1-x86_64-pc-windows-msvc.zip\n";
            
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(checksumFileContent)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var downloader = new TestableRipgrepDownloader(_testCacheDir, httpClient);

            // Act & Assert
            var act = async () => await downloader.TestVerifyChecksum("14.1.1", "ripgrep-14.1.1-x86_64-pc-windows-msvc.zip", testFile);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Checksum verification failed*");
        }

        [Fact]
        public async Task VerifyChecksumAsync_WithMissingChecksumInFile_ThrowsException()
        {
            // Arrange
            var testFile = Path.Combine(_testCacheDir, "test.zip");
            Directory.CreateDirectory(_testCacheDir);
            File.WriteAllText(testFile, "test content");
            
            var checksumFileContent = "abc123  some-other-file.zip\n";
            
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(checksumFileContent)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var downloader = new TestableRipgrepDownloader(_testCacheDir, httpClient);

            // Act & Assert
            var act = async () => await downloader.TestVerifyChecksum("14.1.1", "ripgrep-14.1.1-x86_64-pc-windows-msvc.zip", testFile);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Could not find checksum*");
        }

        [Fact]
        public void Constructor_WithVerifyChecksumsDisabled_DoesNotVerify()
        {
            // Arrange & Act
            var downloader = new TestableRipgrepDownloader(_testCacheDir, verifyChecksums: false);

            // Assert
            downloader.VerifyChecksumsEnabled.Should().BeFalse();
        }

        [Fact]
        public void Constructor_WithVerifyChecksumsEnabled_VerifiesByDefault()
        {
            // Arrange & Act
            var downloader = new TestableRipgrepDownloader(_testCacheDir, verifyChecksums: true);

            // Assert
            downloader.VerifyChecksumsEnabled.Should().BeTrue();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("IVY_RIPGREP_CACHE_DIR", null);
            try
            {
                if (Directory.Exists(_testCacheDir))
                {
                    Directory.Delete(_testCacheDir, true);
                }
            }
            catch { }
        }

        // Testable subclass to expose protected methods
        private class TestableRipgrepDownloader : RipgrepDownloader
        {
            private static string? _overrideCacheDir;

            static TestableRipgrepDownloader()
            {
                _overrideCacheDir = null;
            }

            public TestableRipgrepDownloader(string cacheDir, HttpClient? httpClient = null, bool verifyChecksums = true)
                : base(SetCacheDir(cacheDir, httpClient), NullLogger<RipgrepDownloader>.Instance, verifyChecksums)
            {
            }

            private static HttpClient? SetCacheDir(string cacheDir, HttpClient? httpClient)
            {
                _overrideCacheDir = cacheDir;
                return httpClient;
            }

            public bool VerifyChecksumsEnabled => base._verifyChecksums;

            public async Task TestVerifyChecksum(string version, string archiveName, string archivePath)
            {
                await base.VerifyChecksumAsync(version, archiveName, archivePath, CancellationToken.None);
            }

            protected override string GetCacheDirectory()
            {
                return _overrideCacheDir ?? Path.GetTempPath();
            }
        }
    }
}