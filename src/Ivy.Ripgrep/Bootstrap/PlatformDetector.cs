using System;
using System.Runtime.InteropServices;

namespace Ivy.Ripgrep.Bootstrap
{
    public static class PlatformDetector
    {
        public static string GetRipgrepTarget()
        {
            var os = GetOperatingSystem();
            var arch = GetArchitecture();

            return (os, arch) switch
            {
                (OS.Windows, Arch.X64) => "x86_64-pc-windows-msvc",
                (OS.Windows, Arch.X86) => "i686-pc-windows-msvc",
                (OS.Windows, Arch.Arm64) => "aarch64-pc-windows-msvc",
                
                (OS.MacOS, Arch.X64) => "x86_64-apple-darwin",
                (OS.MacOS, Arch.Arm64) => "aarch64-apple-darwin",
                
                (OS.Linux, Arch.X64) => "x86_64-unknown-linux-musl",
                (OS.Linux, Arch.X86) => "i686-unknown-linux-musl",
                (OS.Linux, Arch.Arm) => "arm-unknown-linux-gnueabihf",
                (OS.Linux, Arch.Arm64) => "aarch64-unknown-linux-musl",
                
                _ => throw new PlatformNotSupportedException($"Unsupported platform: {os} {arch}")
            };
        }

        public static string GetBinaryName()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "rg.exe" : "rg";
        }

        public static string GetArchiveExtension()
        {
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".zip" : ".tar.gz";
        }

        private static OS GetOperatingSystem()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return OS.Windows;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return OS.MacOS;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return OS.Linux;
            
            throw new PlatformNotSupportedException("Unknown operating system");
        }

        private static Arch GetArchitecture()
        {
            return RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X64 => Arch.X64,
                Architecture.X86 => Arch.X86,
                Architecture.Arm => Arch.Arm,
                Architecture.Arm64 => Arch.Arm64,
                _ => throw new PlatformNotSupportedException($"Unknown architecture: {RuntimeInformation.ProcessArchitecture}")
            };
        }

        private enum OS
        {
            Windows,
            MacOS,
            Linux
        }

        private enum Arch
        {
            X64,
            X86,
            Arm,
            Arm64
        }
    }
}