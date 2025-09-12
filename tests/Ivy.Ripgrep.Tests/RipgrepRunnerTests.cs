using System.Linq;
using FluentAssertions;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;
using Ivy.Ripgrep.Execution;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class RipgrepRunnerTests
    {
        private readonly RipgrepRunner _runner = new();

        [Fact]
        public void BuildArguments_WithJsonOption_AddsJsonFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                Json = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--json");
            args.Should().Contain("test");
        }

        [Fact]
        public void BuildArguments_WithCaseInsensitive_AddsCaseFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                CaseInsensitive = true
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-i");
        }

        [Fact]
        public void BuildArguments_WithIncludeGlobs_AddsGlobFlags()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                IncludeGlobs = new[] { "*.cs", "*.txt" }
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-g");
            args.Should().Contain("*.cs");
            args.Should().Contain("*.txt");
        }

        [Fact]
        public void BuildArguments_WithExcludeGlobs_AddsNegatedGlobFlags()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                ExcludeGlobs = new[] { "*.log", "*.tmp" }
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-g");
            args.Should().Contain("!*.log");
            args.Should().Contain("!*.tmp");
        }

        [Fact]
        public void BuildArguments_WithMaxDepth_AddsMaxDepthFlag()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                MaxDepth = 3
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--max-depth");
            args.Should().Contain("3");
        }

        [Fact]
        public void BuildArguments_WithContextLines_AddsContextFlags()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                ContextBefore = 2,
                ContextAfter = 3
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("-B");
            args.Should().Contain("2");
            args.Should().Contain("-A");
            args.Should().Contain("3");
        }

        [Fact]
        public void BuildArguments_WithAdditionalArguments_AppendsThemCorrectly()
        {
            var options = new RipgrepOptions
            {
                Pattern = "test",
                AdditionalArguments = new[] { "--stats", "--no-heading" }
            };

            var args = _runner.BuildArguments(options);

            args.Should().Contain("--stats");
            args.Should().Contain("--no-heading");
        }
    }
}