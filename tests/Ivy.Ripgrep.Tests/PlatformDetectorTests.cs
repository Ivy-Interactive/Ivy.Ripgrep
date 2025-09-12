using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Ivy.Ripgrep.Bootstrap;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class PlatformDetectorTests
    {
        [Fact]
        public void GetRipgrepTarget_ReturnsValidTarget()
        {
            var target = PlatformDetector.GetRipgrepTarget();
            
            target.Should().NotBeNullOrEmpty();
            target.Should().Contain("-");
        }

        [Fact]
        public void GetBinaryName_ReturnsCorrectName()
        {
            var name = PlatformDetector.GetBinaryName();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                name.Should().Be("rg.exe");
            }
            else
            {
                name.Should().Be("rg");
            }
        }

        [Fact]
        public void GetArchiveExtension_ReturnsCorrectExtension()
        {
            var ext = PlatformDetector.GetArchiveExtension();
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ext.Should().Be(".zip");
            }
            else
            {
                ext.Should().Be(".tar.gz");
            }
        }
    }
}