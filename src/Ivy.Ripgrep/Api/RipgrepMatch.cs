using System.Collections.Generic;

namespace Ivy.Ripgrep.Api
{
    /// <summary>
    /// Represents a match found by ripgrep.
    /// </summary>
    public sealed record RipgrepMatch(
        string FilePath,
        int Line,
        int Column,
        string LineText,
        IReadOnlyList<(int start, int end)> Spans);
}