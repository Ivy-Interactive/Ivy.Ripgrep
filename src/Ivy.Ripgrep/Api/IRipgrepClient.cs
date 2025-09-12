using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ivy.Ripgrep.Api
{
    /// <summary>
    /// Client interface for interacting with ripgrep.
    /// </summary>
    public interface IRipgrepClient
    {
        /// <summary>
        /// Performs a streaming search with ripgrep.
        /// </summary>
        IAsyncEnumerable<RipgrepMatch> SearchAsync(RipgrepOptions options, CancellationToken ct = default);

        /// <summary>
        /// Performs a buffered search with ripgrep.
        /// </summary>
        Task<IReadOnlyList<RipgrepMatch>> SearchBufferedAsync(RipgrepOptions options, CancellationToken ct = default);

        /// <summary>
        /// Runs ripgrep with raw arguments.
        /// </summary>
        Task<RipgrepProcessResult> RunRawAsync(IEnumerable<string> args, string? cwd = null, CancellationToken ct = default);
    }
}