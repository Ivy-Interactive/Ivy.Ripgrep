# Security Considerations

## Overview
Ivy.Ripgrep downloads and executes binary files from the internet. This document outlines the security measures in place and best practices for secure usage.

## Security Features

### 1. SHA-256 Checksum Verification ✅
- **What**: Every downloaded ripgrep binary is verified against its SHA-256 checksum
- **How**: Downloads the official checksum file from GitHub releases and compares it with the computed hash
- **Why**: Ensures the binary hasn't been corrupted or tampered with during download
- **Status**: Enabled by default

### 2. HTTPS-Only Downloads ✅
- All downloads use HTTPS exclusively
- Prevents man-in-the-middle attacks during download
- GitHub API and release assets are served over TLS

### 3. Official GitHub Releases ✅
- Downloads only from the official BurntSushi/ripgrep GitHub repository
- Uses GitHub's infrastructure and security measures
- Release artifacts are signed by GitHub Actions

### 4. Integrity Manifest ✅
- Creates an `integrity.json` file alongside each downloaded binary
- Records:
  - Version downloaded
  - Download timestamp
  - Checksum verification status
  - Source URL
- Allows auditing of installed binaries

### 5. Configurable Version Pinning ✅
- Default version is pinned in code (currently 14.1.1)
- Can be overridden via environment variable
- Prevents automatic updates to untested versions

## Trust Chain

```
GitHub Actions (BurntSushi/ripgrep)
    ↓ (builds and signs)
GitHub Releases 
    ↓ (HTTPS + checksums)
Ivy.Ripgrep Downloader
    ↓ (verifies checksum)
Local Cache
    ↓ (executes)
Your Application
```

## Security Best Practices

### For Library Users

1. **Pin Specific Versions**
   ```csharp
   var options = new RipgrepOptions
   {
       RequiredRipgrepVersion = "14.1.1" // Pin to tested version
   };
   ```

2. **Use Environment Variables for Version Control**
   ```bash
   export IVY_RIPGREP_VERSION=14.1.1
   ```

3. **Review Cached Binaries**
   - Windows: `%LOCALAPPDATA%\Ivy.Ripgrep\bin\`
   - macOS: `~/Library/Caches/Ivy.Ripgrep/bin/`
   - Linux: `~/.cache/Ivy.Ripgrep/bin/`
   - Check the `integrity.json` file in each version folder

4. **Use GitHub Token for Rate Limiting**
   ```bash
   export GITHUB_TOKEN=your_token_here
   ```
   This also provides authenticated API access for better security.

5. **Monitor for Security Updates**
   - Watch the ripgrep repository for security advisories
   - Update Ivy.Ripgrep when new versions are released

### For Enterprise Users

1. **Host Internal Mirror**
   ```csharp
   Environment.SetEnvironmentVariable("IVY_RIPGREP_SOURCE", "https://internal-mirror.company.com");
   ```

2. **Pre-Download and Verify Binaries**
   - Download ripgrep binaries during CI/CD
   - Verify checksums in controlled environment
   - Deploy pre-verified binaries with your application

3. **Implement Custom Binary Provider**
   ```csharp
   public class EnterpriseBinaryProvider : IRipgrepBinaryProvider
   {
       public async Task<string> GetBinaryPathAsync(string? version, CancellationToken ct)
       {
           // Return path to pre-approved binary
           return "/approved-binaries/ripgrep/rg.exe";
       }
   }
   ```

4. **Network Isolation**
   - Run in environments without internet access
   - Use `OverrideRipgrepPath` to specify local binary

## Potential Risks and Mitigations

### Risk: Supply Chain Attack
**Mitigation**: 
- Checksum verification ensures binary integrity
- Only downloads from official GitHub releases
- Version pinning prevents automatic updates

### Risk: GitHub Account Compromise
**Mitigation**:
- Monitor ripgrep repository for unusual activity
- Use version pinning to avoid automatic updates
- Consider hosting internal mirror for critical systems

### Risk: Network Interception
**Mitigation**:
- All downloads use HTTPS
- Checksum verification detects tampering
- Consider certificate pinning for high-security environments

### Risk: Local Cache Tampering
**Mitigation**:
- Integrity manifest records download details
- File system permissions should protect cache directory
- Consider implementing additional file integrity monitoring

## Verification Commands

### Manually Verify a Downloaded Binary
```bash
# On Linux/macOS
sha256sum /path/to/rg

# On Windows (PowerShell)
Get-FileHash -Path "C:\path\to\rg.exe" -Algorithm SHA256
```

### Check Integrity Manifest
```bash
cat ~/.cache/Ivy.Ripgrep/bin/14.1.1/x86_64-unknown-linux-musl/integrity.json
```

## Reporting Security Issues

If you discover a security vulnerability in Ivy.Ripgrep:

1. **Do NOT** create a public GitHub issue
2. Contact the maintainers privately
3. Provide detailed information about the vulnerability
4. Allow time for a fix before public disclosure

## Security Checklist

- [ ] Checksum verification is enabled (default)
- [ ] Using HTTPS for all downloads
- [ ] Version is pinned in production
- [ ] Cache directory has appropriate permissions
- [ ] Monitoring ripgrep for security updates
- [ ] Have incident response plan for compromised binaries

## Additional Resources

- [Ripgrep Security Policy](https://github.com/BurntSushi/ripgrep/security)
- [GitHub Security Best Practices](https://docs.github.com/en/code-security)
- [OWASP Dependency Check](https://owasp.org/www-project-dependency-check/)

## Summary

Ivy.Ripgrep implements multiple layers of security to ensure safe download and execution of ripgrep binaries. The most critical security feature is **SHA-256 checksum verification**, which is enabled by default and ensures that downloaded binaries match the official releases exactly.

For maximum security, enterprise users should consider hosting internal mirrors and using pre-verified binaries rather than downloading directly from GitHub.