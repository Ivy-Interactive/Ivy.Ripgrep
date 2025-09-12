using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;
using Ivy.Ripgrep.Bootstrap;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class RipgrepClientTests
    {
        [Fact]
        public async Task SearchAsync_WithValidOptions_CallsBinaryProvider()
        {
            // Arrange
            var mockBinaryProvider = new Mock<IRipgrepBinaryProvider>();
            mockBinaryProvider
                .Setup(x => x.GetBinaryPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("rg.exe");

            var client = new RipgrepClient(mockBinaryProvider.Object);
            var options = new RipgrepOptions { Pattern = "test", Json = false };

            // Act
            try
            {
                await client.SearchAsync(options).ToListAsync();
            }
            catch
            {
                // Expected to fail since we're not providing a real binary
            }

            // Assert
            mockBinaryProvider.Verify(x => x.GetBinaryPathAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_WithOverridePath_DoesNotCallBinaryProvider()
        {
            // Arrange
            var mockBinaryProvider = new Mock<IRipgrepBinaryProvider>();
            var client = new RipgrepClient(mockBinaryProvider.Object);
            var options = new RipgrepOptions 
            { 
                Pattern = "test",
                OverrideRipgrepPath = "/custom/path/rg"
            };

            // Act
            try
            {
                await client.SearchAsync(options).ToListAsync();
            }
            catch
            {
                // Expected to fail since we're not providing a real binary
            }

            // Assert
            mockBinaryProvider.Verify(
                x => x.GetBinaryPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), 
                Times.Never);
        }

        [Fact]
        public async Task SearchAsync_WithRequiredVersion_PassesVersionToBinaryProvider()
        {
            // Arrange
            var mockBinaryProvider = new Mock<IRipgrepBinaryProvider>();
            mockBinaryProvider
                .Setup(x => x.GetBinaryPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("rg.exe");

            var client = new RipgrepClient(mockBinaryProvider.Object);
            var options = new RipgrepOptions 
            { 
                Pattern = "test",
                RequiredRipgrepVersion = "14.0.0"
            };

            // Act
            try
            {
                await client.SearchAsync(options).ToListAsync();
            }
            catch
            {
                // Expected to fail since we're not providing a real binary
            }

            // Assert
            mockBinaryProvider.Verify(
                x => x.GetBinaryPathAsync("14.0.0", It.IsAny<CancellationToken>()), 
                Times.Once);
        }

        [Fact]
        public async Task SearchBufferedAsync_ReturnsAllMatches()
        {
            // Arrange
            var mockBinaryProvider = new Mock<IRipgrepBinaryProvider>();
            mockBinaryProvider
                .Setup(x => x.GetBinaryPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("rg.exe");

            var client = new TestableRipgrepClient(mockBinaryProvider.Object);
            client.SetTestMatches(new[]
            {
                new RipgrepMatch("file1.txt", 1, 1, "Line 1", new List<(int, int)>()),
                new RipgrepMatch("file2.txt", 2, 5, "Line 2", new List<(int, int)>())
            });

            var options = new RipgrepOptions { Pattern = "test" };

            // Act
            var results = await client.SearchBufferedAsync(options);

            // Assert
            results.Should().HaveCount(2);
            results[0].FilePath.Should().Be("file1.txt");
            results[1].FilePath.Should().Be("file2.txt");
        }

        [Fact]
        public async Task RunRawAsync_WithValidArgs_CallsBinaryProvider()
        {
            // Arrange
            var mockBinaryProvider = new Mock<IRipgrepBinaryProvider>();
            mockBinaryProvider
                .Setup(x => x.GetBinaryPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("rg.exe");

            var client = new RipgrepClient(mockBinaryProvider.Object);
            var args = new[] { "--version" };

            // Act
            try
            {
                await client.RunRawAsync(args);
            }
            catch
            {
                // Expected to fail since we're not providing a real binary
            }

            // Assert
            mockBinaryProvider.Verify(x => x.GetBinaryPathAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SearchAsync_WithCancellation_StopsProperly()
        {
            // Arrange
            var mockBinaryProvider = new Mock<IRipgrepBinaryProvider>();
            mockBinaryProvider
                .Setup(x => x.GetBinaryPathAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("rg.exe");

            var client = new TestableRipgrepClient(mockBinaryProvider.Object);
            client.SetTestDelay(TimeSpan.FromSeconds(10)); // Long delay to ensure cancellation happens
            
            var cts = new CancellationTokenSource();
            var options = new RipgrepOptions { Pattern = "test" };

            // Act
            var searchTask = client.SearchAsync(options, cts.Token).ToListAsync();
            cts.CancelAfter(100); // Cancel after 100ms

            // Assert
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await searchTask);
        }

        // Testable subclass for mocking behavior
        private class TestableRipgrepClient : RipgrepClient
        {
            private IEnumerable<RipgrepMatch> _testMatches = Enumerable.Empty<RipgrepMatch>();
            private TimeSpan _testDelay = TimeSpan.Zero;

            public TestableRipgrepClient(IRipgrepBinaryProvider binaryProvider)
                : base(binaryProvider, NullLogger<RipgrepClient>.Instance)
            {
            }

            public void SetTestMatches(IEnumerable<RipgrepMatch> matches)
            {
                _testMatches = matches;
            }

            public void SetTestDelay(TimeSpan delay)
            {
                _testDelay = delay;
            }

            public override async IAsyncEnumerable<RipgrepMatch> SearchAsync(
                RipgrepOptions options,
                [EnumeratorCancellation] CancellationToken ct = default)
            {
                if (_testDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_testDelay, ct);
                }

                foreach (var match in _testMatches)
                {
                    ct.ThrowIfCancellationRequested();
                    yield return match;
                }
            }
        }
    }
}