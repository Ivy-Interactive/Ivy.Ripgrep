using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using Ivy.Ripgrep.Api;

namespace Ivy.Ripgrep.Json
{
    public static class RipgrepJsonParser
    {
        public static async IAsyncEnumerable<RipgrepMatch> ParseJsonStreamAsync(
            StreamReader reader,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            string? currentFile = null;
            
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                RipgrepJsonEvent? evt;
                try
                {
                    evt = JsonSerializer.Deserialize<RipgrepJsonEvent>(line);
                }
                catch (JsonException)
                {
                    continue;
                }

                if (evt is { Type: "begin", Data.Path.Text: not null })
                {
                    currentFile = evt.Data.Path.Text;
                }
                else if (evt?.Type == "match" && currentFile != null && evt.Data != null)
                {
                    var match = ConvertToMatch(currentFile, evt.Data);
                    if (match != null)
                        yield return match;
                }
            }
        }

        private static RipgrepMatch? ConvertToMatch(string filePath, RipgrepJsonData data)
        {
            if (data.LineNumber == null || data.Lines?.Text == null)
                return null;

            var spans = new List<(int start, int end)>();
            if (data.Submatches != null)
            {
                spans.AddRange(data.Submatches.Select(s => (s.Start, s.End)));
            }

            var column = spans.Count > 0 ? spans[0].start + 1 : 1;

            return new RipgrepMatch(
                filePath,
                data.LineNumber.Value,
                column,
                data.Lines.Text,
                spans);
        }
    }
}