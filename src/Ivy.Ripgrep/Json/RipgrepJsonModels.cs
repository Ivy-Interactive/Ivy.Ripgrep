using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Ivy.Ripgrep.Json
{
    internal class RipgrepJsonEvent
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("data")]
        public RipgrepJsonData? Data { get; set; }
    }

    internal class RipgrepJsonData
    {
        [JsonPropertyName("path")]
        public RipgrepJsonPath? Path { get; set; }

        [JsonPropertyName("lines")]
        public RipgrepJsonLines? Lines { get; set; }

        [JsonPropertyName("line_number")]
        public int? LineNumber { get; set; }

        [JsonPropertyName("absolute_offset")]
        public long? AbsoluteOffset { get; set; }

        [JsonPropertyName("submatches")]
        public List<RipgrepJsonSubmatch>? Submatches { get; set; }

        [JsonPropertyName("stats")]
        public RipgrepJsonStats? Stats { get; set; }
    }

    internal class RipgrepJsonPath
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    internal class RipgrepJsonLines
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    internal class RipgrepJsonSubmatch
    {
        [JsonPropertyName("match")]
        public RipgrepJsonMatchInfo? Match { get; set; }

        [JsonPropertyName("start")]
        public int Start { get; set; }

        [JsonPropertyName("end")]
        public int End { get; set; }
    }

    internal class RipgrepJsonMatchInfo
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    internal class RipgrepJsonStats
    {
        [JsonPropertyName("elapsed")]
        public RipgrepJsonDuration? Elapsed { get; set; }

        [JsonPropertyName("searches")]
        public long Searches { get; set; }

        [JsonPropertyName("searches_with_match")]
        public long SearchesWithMatch { get; set; }

        [JsonPropertyName("bytes_searched")]
        public long BytesSearched { get; set; }
    }

    internal class RipgrepJsonDuration
    {
        [JsonPropertyName("secs")]
        public long Secs { get; set; }

        [JsonPropertyName("nanos")]
        public long Nanos { get; set; }
    }
}