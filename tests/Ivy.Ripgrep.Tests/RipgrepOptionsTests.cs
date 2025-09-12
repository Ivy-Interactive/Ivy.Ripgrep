using System.Linq;
using FluentAssertions;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class RipgrepOptionsTests
    {
        [Fact]
        public void Constructor_SetsDefaultValues()
        {
            var options = new RipgrepOptions();

            options.Pattern.Should().BeEmpty();
            options.WorkingDirectory.Should().NotBeNullOrEmpty();
            options.IncludeGlobs.Should().BeEmpty();
            options.ExcludeGlobs.Should().BeEmpty();
            options.CaseInsensitive.Should().BeFalse();
            options.SmartCase.Should().BeTrue();
            options.WordRegexp.Should().BeFalse();
            options.Multiline.Should().BeFalse();
            options.MultilineCrLf.Should().BeFalse();
            options.MaxDepth.Should().BeNull();
            options.ContextBefore.Should().BeNull();
            options.ContextAfter.Should().BeNull();
            options.FollowSymlinks.Should().BeFalse();
            options.RespectGitIgnore.Should().BeTrue();
            options.Hidden.Should().BeFalse();
            options.Unrestricted.Should().BeFalse();
            options.Threads.Should().BeNull();
            options.MaxColumns.Should().BeNull();
            options.MaxFilesizeMB.Should().BeNull();
            options.Json.Should().BeTrue();
            options.LineNumber.Should().BeTrue();
            options.Column.Should().BeTrue();
            options.AdditionalArguments.Should().BeEmpty();
            options.OverrideRipgrepPath.Should().BeNull();
            options.RequiredRipgrepVersion.Should().BeNull();
        }

        [Fact]
        public void InitProperties_CanBeSet()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                WorkingDirectory = "/custom/path",
                IncludeGlobs = new[] { "*.cs", "*.txt" },
                ExcludeGlobs = new[] { "*.log" },
                CaseInsensitive = true,
                SmartCase = false,
                WordRegexp = true,
                Multiline = true,
                MultilineCrLf = true,
                MaxDepth = 5,
                ContextBefore = 2,
                ContextAfter = 3,
                FollowSymlinks = true,
                RespectGitIgnore = false,
                Hidden = true,
                Unrestricted = true,
                Threads = 4,
                MaxColumns = 150,
                MaxFilesizeMB = 10,
                Json = false,
                LineNumber = false,
                Column = false,
                AdditionalArguments = new[] { "--stats" },
                OverrideRipgrepPath = "/usr/bin/rg",
                RequiredRipgrepVersion = "14.0.0"
            };

            options.Pattern.Should().Be("test");
            options.WorkingDirectory.Should().Be("/custom/path");
            options.IncludeGlobs.Should().ContainInOrder("*.cs", "*.txt");
            options.ExcludeGlobs.Should().ContainInOrder("*.log");
            options.CaseInsensitive.Should().BeTrue();
            options.SmartCase.Should().BeFalse();
            options.WordRegexp.Should().BeTrue();
            options.Multiline.Should().BeTrue();
            options.MultilineCrLf.Should().BeTrue();
            options.MaxDepth.Should().Be(5);
            options.ContextBefore.Should().Be(2);
            options.ContextAfter.Should().Be(3);
            options.FollowSymlinks.Should().BeTrue();
            options.RespectGitIgnore.Should().BeFalse();
            options.Hidden.Should().BeTrue();
            options.Unrestricted.Should().BeTrue();
            options.Threads.Should().Be(4);
            options.MaxColumns.Should().Be(150);
            options.MaxFilesizeMB.Should().Be(10);
            options.Json.Should().BeFalse();
            options.LineNumber.Should().BeFalse();
            options.Column.Should().BeFalse();
            options.AdditionalArguments.Should().ContainInOrder("--stats");
            options.OverrideRipgrepPath.Should().Be("/usr/bin/rg");
            options.RequiredRipgrepVersion.Should().Be("14.0.0");
        }

        [Theory]
        [InlineData("")]
        [InlineData("simple")]
        [InlineData("test.*pattern")]
        [InlineData("^start")]
        [InlineData("end$")]
        [InlineData("[a-z]+")]
        public void Pattern_AcceptsVariousRegexPatterns(string pattern)
        {
            var options = new RipgrepOptions { Pattern = pattern };
            options.Pattern.Should().Be(pattern);
        }

        [Fact]
        public void IncludeGlobs_CanBeEmptyOrMultiple()
        {
            var options1 = new RipgrepOptions { IncludeGlobs = Enumerable.Empty<string>() };
            options1.IncludeGlobs.Should().BeEmpty();

            var options2 = new RipgrepOptions { IncludeGlobs = new[] { "*.cs" } };
            options2.IncludeGlobs.Should().HaveCount(1);

            var options3 = new RipgrepOptions { IncludeGlobs = new[] { "*.cs", "*.vb", "*.fs" } };
            options3.IncludeGlobs.Should().HaveCount(3);
        }

        [Fact]
        public void ExcludeGlobs_CanBeEmptyOrMultiple()
        {
            var options1 = new RipgrepOptions { ExcludeGlobs = Enumerable.Empty<string>() };
            options1.ExcludeGlobs.Should().BeEmpty();

            var options2 = new RipgrepOptions { ExcludeGlobs = new[] { "node_modules" } };
            options2.ExcludeGlobs.Should().HaveCount(1);

            var options3 = new RipgrepOptions { ExcludeGlobs = new[] { "*.log", "*.tmp", "*.bak" } };
            options3.ExcludeGlobs.Should().HaveCount(3);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(100)]
        public void MaxDepth_AcceptsPositiveValues(int depth)
        {
            var options = new RipgrepOptions { MaxDepth = depth };
            options.MaxDepth.Should().Be(depth);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 2)]
        [InlineData(5, 5)]
        [InlineData(10, 3)]
        public void ContextLines_CanBeSetIndependently(int before, int after)
        {
            var options = new RipgrepOptions
            {
                ContextBefore = before,
                ContextAfter = after
            };

            options.ContextBefore.Should().Be(before);
            options.ContextAfter.Should().Be(after);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(8)]
        [InlineData(16)]
        public void Threads_AcceptsValidThreadCounts(int threads)
        {
            var options = new RipgrepOptions { Threads = threads };
            options.Threads.Should().Be(threads);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1024)]
        public void MaxFilesizeMB_AcceptsValidSizes(int sizeMB)
        {
            var options = new RipgrepOptions { MaxFilesizeMB = sizeMB };
            options.MaxFilesizeMB.Should().Be(sizeMB);
        }
    }
}