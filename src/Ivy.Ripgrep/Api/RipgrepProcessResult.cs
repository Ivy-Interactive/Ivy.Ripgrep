using System;

namespace Ivy.Ripgrep.Api
{
    /// <summary>
    /// Result from running ripgrep process.
    /// </summary>
    public sealed class RipgrepProcessResult
    {
        /// <summary>
        /// Standard output from the process.
        /// </summary>
        public string StandardOutput { get; init; } = "";

        /// <summary>
        /// Standard error from the process.
        /// </summary>
        public string StandardError { get; init; } = "";

        /// <summary>
        /// Exit code from the process.
        /// </summary>
        public int ExitCode { get; init; }

        /// <summary>
        /// Execution time.
        /// </summary>
        public TimeSpan ExecutionTime { get; init; }
    }
}