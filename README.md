# Ivy.Ripgrep

A .NET wrapper for [ripgrep](https://github.com/BurntSushi/ripgrep) with automatic binary provisioning. This library provides a clean, async API for using ripgrep in .NET applications, with automatic per-platform binary provisioning similar to how vscode-ripgrep works in Node.js.

## Features

- **Cross-platform**: Windows, macOS, Linux (x64 & ARM64)
- **Auto-provisioning**: Automatically downloads and caches the correct ripgrep binary for your platform
- **Streaming API**: `IAsyncEnumerable<Result>` for memory-efficient searches
- **Buffered API**: `Task<SearchResult[]>` for convenience
- **Rich options**: Maps to ripgrep flags with first-class .NET ergonomics
- **JSON parsing**: Uses ripgrep's JSON output mode for robust parsing
- **SHA-256 checksum verification**: Verifies downloaded binaries for security (enabled by default)
- **Integrity manifests**: Records download details for auditing

## Installation

```bash
dotnet add package Ivy.Ripgrep
```

## Quick Start

```csharp
using Ivy.Ripgrep;

var client = new RipgrepClient();
var options = new RipgrepOptions
{
    Pattern = "TODO",
    WorkingDirectory = @"C:\src\project",
    IncludeGlobs = new[] { "**/*.cs" },
    Json = true
};

// Streaming search
await foreach (var match in client.SearchAsync(options))
{
    Console.WriteLine($"{match.FilePath}:{match.Line}:{match.Column} {match.LineText}");
}

// Buffered search
var allMatches = await client.SearchBufferedAsync(options);
```

## Configuration

### Environment Variables

- `IVY_RIPGREP_VERSION`: Override the default ripgrep version (e.g., "14.1.1")
- `IVY_RIPGREP_SOURCE`: Override the base URL for downloading binaries
- `GITHUB_TOKEN`: Optional GitHub token for authenticated downloads (higher rate limits)

### Cache Locations

- Windows: `%LOCALAPPDATA%\Ivy.Ripgrep\bin\<version>\<rid>\`
- macOS: `~/Library/Caches/Ivy.Ripgrep/bin/<version>/<rid>/`
- Linux: `~/.cache/Ivy.Ripgrep/bin/<version>/<rid>/`

## Advanced Usage

### Custom Binary Provider

```csharp
public class CustomBinaryProvider : IRipgrepBinaryProvider
{
    public async Task<string> GetBinaryPathAsync(string? version = null, CancellationToken ct = default)
    {
        // Custom logic to provide ripgrep binary
        return "/path/to/rg";
    }
}

var client = new RipgrepClient(new CustomBinaryProvider());
```

### Raw Command Execution

```csharp
var result = await client.RunRawAsync(new[] { "--version" });
Console.WriteLine(result.StandardOutput);
```

## Supported Platforms

| Platform | Architecture | Runtime Identifier |
|----------|-------------|-------------------|
| Windows | x64 | x86_64-pc-windows-msvc |
| Windows | x86 | i686-pc-windows-msvc |
| Windows | ARM64 | aarch64-pc-windows-msvc |
| macOS | x64 | x86_64-apple-darwin |
| macOS | ARM64 | aarch64-apple-darwin |
| Linux | x64 | x86_64-unknown-linux-musl |
| Linux | x86 | i686-unknown-linux-musl |
| Linux | ARM | arm-unknown-linux-gnueabihf |
| Linux | ARM64 | aarch64-unknown-linux-musl |

## Security

Security is a top priority. The library includes:

- **SHA-256 checksum verification** for all downloaded binaries (enabled by default)
- **HTTPS-only downloads** from official GitHub releases
- **Integrity manifests** that record download details and verification status
- **Version pinning** to prevent unexpected updates

For detailed security information, see [SECURITY.md](SECURITY.md).

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.

## Third Party Notices

This project uses ripgrep, which is dual-licensed under MIT/Unlicense. See [ThirdPartyNotices.txt](src/Ivy.Ripgrep/Properties/ThirdPartyNotices.txt) for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Acknowledgments

- [ripgrep](https://github.com/BurntSushi/ripgrep) by Andrew Gallant
- [vscode-ripgrep](https://github.com/microsoft/vscode-ripgrep) for inspiration