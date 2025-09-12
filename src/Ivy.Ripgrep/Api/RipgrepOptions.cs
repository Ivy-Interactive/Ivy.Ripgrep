using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ivy.Ripgrep.Api
{
    /// <summary>
    /// Options for configuring ripgrep searches.
    /// </summary>
    public sealed class RipgrepOptions
    {
        /// <summary>
        /// The search pattern (regular expression).
        /// </summary>
        public string Pattern { get; init; } = "";

        /// <summary>
        /// Working directory for the search.
        /// </summary>
        public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();

        /// <summary>
        /// Glob patterns to include in search.
        /// </summary>
        public IEnumerable<string> IncludeGlobs { get; init; } = Enumerable.Empty<string>();

        /// <summary>
        /// Glob patterns to exclude from search.
        /// </summary>
        public IEnumerable<string> ExcludeGlobs { get; init; } = Enumerable.Empty<string>();

        /// <summary>
        /// Case insensitive search.
        /// </summary>
        public bool CaseInsensitive { get; init; }

        /// <summary>
        /// Smart case search (case insensitive unless pattern contains uppercase).
        /// </summary>
        public bool SmartCase { get; init; } = true;

        /// <summary>
        /// Match whole words only.
        /// </summary>
        public bool WordRegexp { get; init; }

        /// <summary>
        /// Enable multiline mode.
        /// </summary>
        public bool Multiline { get; init; }

        /// <summary>
        /// Handle Windows CRLF line endings.
        /// </summary>
        public bool MultilineCrLf { get; init; }

        /// <summary>
        /// Maximum directory depth to search.
        /// </summary>
        public int? MaxDepth { get; init; }

        /// <summary>
        /// Number of context lines to show before match.
        /// </summary>
        public int? ContextBefore { get; init; }

        /// <summary>
        /// Number of context lines to show after match.
        /// </summary>
        public int? ContextAfter { get; init; }

        /// <summary>
        /// Follow symbolic links.
        /// </summary>
        public bool FollowSymlinks { get; init; }

        /// <summary>
        /// Respect .gitignore files.
        /// </summary>
        public bool RespectGitIgnore { get; init; } = true;

        /// <summary>
        /// Search hidden files.
        /// </summary>
        public bool Hidden { get; init; }

        /// <summary>
        /// Unrestricted search (ignore all ignore files).
        /// </summary>
        public bool Unrestricted { get; init; }

        /// <summary>
        /// Number of threads to use.
        /// </summary>
        public int? Threads { get; init; }

        /// <summary>
        /// Maximum column width for output.
        /// </summary>
        public int? MaxColumns { get; init; }

        /// <summary>
        /// Maximum file size in MB to search.
        /// </summary>
        public int? MaxFilesizeMB { get; init; }

        /// <summary>
        /// Output in JSON format.
        /// </summary>
        public bool Json { get; init; } = true;

        /// <summary>
        /// Include line numbers in output.
        /// </summary>
        public bool LineNumber { get; init; } = true;

        /// <summary>
        /// Include column numbers in output.
        /// </summary>
        public bool Column { get; init; } = true;

        /// <summary>
        /// Additional raw arguments to pass to ripgrep.
        /// </summary>
        public IEnumerable<string> AdditionalArguments { get; init; } = Enumerable.Empty<string>();

        /// <summary>
        /// Override the ripgrep binary path.
        /// </summary>
        public string? OverrideRipgrepPath { get; init; }

        /// <summary>
        /// Required ripgrep version.
        /// </summary>
        public string? RequiredRipgrepVersion { get; init; }
    }
}