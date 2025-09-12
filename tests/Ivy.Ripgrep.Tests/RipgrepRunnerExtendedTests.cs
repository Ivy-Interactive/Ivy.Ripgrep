using System.Linq;
using FluentAssertions;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;
using Ivy.Ripgrep.Execution;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class RipgrepRunnerExtendedTests
    {
        private readonly RipgrepRunner _runner = new();

        [Fact]
        public void BuildArguments_WithSmartCase_AddsSmartCaseFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                SmartCase = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-S");
        }

        [Fact]
        public void BuildArguments_WithWordRegexp_AddsWordFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                WordRegexp = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-w");
        }

        [Fact]
        public void BuildArguments_WithMultiline_AddsMultilineFlags()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                Multiline = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-U");
            args.Should().Contain("--multiline");
        }

        [Fact]
        public void BuildArguments_WithMultilineCrLf_AddsCrLfFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                MultilineCrLf = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--crlf");
        }

        [Fact]
        public void BuildArguments_WithFollowSymlinks_AddsFollowFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                FollowSymlinks = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-L");
        }

        [Fact]
        public void BuildArguments_WithNoRespectGitIgnore_AddsUnrestrictedFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                RespectGitIgnore = false
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-u");
        }

        [Fact]
        public void BuildArguments_WithHidden_AddsHiddenFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                Hidden = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--hidden");
        }

        [Fact]
        public void BuildArguments_WithUnrestricted_AddsTripleUnrestrictedFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                Unrestricted = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-uuu");
        }

        [Fact]
        public void BuildArguments_WithThreads_AddsThreadsFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                Threads = 4
            };

            var args = _runner.BuildArguments(options);

            var threadIndex = args.IndexOf("-j");
            threadIndex.Should().BeGreaterThanOrEqualTo(0);
            args[threadIndex + 1].Should().Be("4");
        }

        [Fact]
        public void BuildArguments_WithMaxColumns_AddsMaxColumnsFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                MaxColumns = 150
            };

            var args = _runner.BuildArguments(options);

            var columnIndex = args.IndexOf("--max-columns");
            columnIndex.Should().BeGreaterThanOrEqualTo(0);
            args[columnIndex + 1].Should().Be("150");
        }

        [Fact]
        public void BuildArguments_WithMaxFilesize_AddsMaxFilesizeFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                MaxFilesizeMB = 10
            };

            var args = _runner.BuildArguments(options);

            var sizeIndex = args.IndexOf("--max-filesize");
            sizeIndex.Should().BeGreaterThanOrEqualTo(0);
            args[sizeIndex + 1].Should().Be("10M");
        }

        [Fact]
        public void BuildArguments_WithLineNumberNotJson_AddsLineNumberFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                LineNumber = true,
                Json = false
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-n");
        }

        [Fact]
        public void BuildArguments_WithColumnNotJson_AddsColumnFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                Column = true,
                Json = false
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--column");
        }

        [Fact]
        public void BuildArguments_AlwaysEndsWithCurrentDirectory()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test"
            };

            var args = _runner.BuildArguments(options);

            args.Should().NotBeEmpty();
            args.Last().Should().Be(".");
        }

        [Fact]
        public void BuildArguments_ComplexOptions_BuildsCorrectly()
        {
            var options = new RipgrepOptions
            {
                Pattern = "TODO",
                Json = true,
                CaseInsensitive = true,
                IncludeGlobs = new[] { "*.cs", "*.js" },
                ExcludeGlobs = new[] { "node_modules", "bin" },
                MaxDepth = 5,
                Hidden = true,
                Threads = 8
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--json");
            args.Should().Contain("-i");
            args.Should().Contain("*.cs");
            args.Should().Contain("*.js");
            args.Should().Contain("!node_modules");
            args.Should().Contain("!bin");
            args.Should().Contain("--hidden");
            args.Should().Contain("TODO");
            args.Should().Contain(".");
        }
    }
}