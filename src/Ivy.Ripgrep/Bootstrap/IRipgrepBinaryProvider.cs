using System.Threading;
using System.Threading.Tasks;

namespace Ivy.Ripgrep.Bootstrap
{
    /// <summary>
    /// Interface for providing ripgrep binaries.
    /// </summary>
    public interface IRipgrepBinaryProvider
    {
        /// <summary>
        /// Gets the path to the ripgrep binary.
        /// </summary>
        Task<string> GetBinaryPathAsync(string? version = null, CancellationToken ct = default);
    }
}