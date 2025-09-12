using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;
using Xunit;

namespace Ivy.Ripgrep.IntegrationTests
{
    public class RipgrepClientIntegrationTests : IDisposable
    {
        private readonly string _testDir;
        private readonly RipgrepClient _client;

        public RipgrepClientIntegrationTests()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"ripgrep_test_{Guid.NewGuid()}");
            Directory.CreateDirectory(_testDir);
            _client = new RipgrepClient();
            
            SetupTestFiles();
        }

        private void SetupTestFiles()
        {
            File.WriteAllText(Path.Combine(_testDir, "test1.txt"), "Hello World\nThis is a test\nHello again");
            File.WriteAllText(Path.Combine(_testDir, "test2.cs"), "public class Test\n{\n    public void Hello() { }\n}");
            File.WriteAllText(Path.Combine(_testDir, "test3.md"), "# Hello\n\nThis is markdown\n\n## Hello Section");
        }

        [Fact]
        public async Task SearchAsync_FindsMatchesInTestFiles()
        {
            var options = new RipgrepOptions
            {
                Pattern = "Hello",
                WorkingDirectory = _testDir,
                Json = true
            };

            var matches = await _client.SearchBufferedAsync(options);

            matches.Should().NotBeEmpty();
            matches.Should().HaveCountGreaterThan(2);
            matches.All(m => m.LineText.Contains("Hello", StringComparison.OrdinalIgnoreCase)).Should().BeTrue();
        }

        [Fact]
        public async Task SearchAsync_WithGlobFilter_OnlySearchesSpecificFiles()
        {
            var options = new RipgrepOptions
            {
                Pattern = "Hello",
                WorkingDirectory = _testDir,
                IncludeGlobs = new[] { "*.cs" },
                Json = true
            };

            var matches = await _client.SearchBufferedAsync(options);

            matches.Should().NotBeEmpty();
            matches.All(m => m.FilePath.EndsWith(".cs")).Should().BeTrue();
        }

        [Fact]
        public async Task SearchAsync_CaseInsensitive_FindsMatches()
        {
            var options = new RipgrepOptions
            {
                Pattern = "hello",
                WorkingDirectory = _testDir,
                CaseInsensitive = true,
                Json = true
            };

            var matches = await _client.SearchBufferedAsync(options);

            matches.Should().NotBeEmpty();
            matches.Should().HaveCountGreaterThan(2);
        }

        [Fact]
        public async Task RunRawAsync_ExecutesSuccessfully()
        {
            var args = new[] { "--version" };

            var result = await _client.RunRawAsync(args);

            result.ExitCode.Should().Be(0);
            result.StandardOutput.Should().Contain("ripgrep");
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_testDir, true);
            }
            catch { }
        }
    }
}