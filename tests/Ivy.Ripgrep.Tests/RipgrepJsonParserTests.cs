using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Ivy.Ripgrep.Json;
using Xunit;

namespace Ivy.Ripgrep.Tests
{
    public class RipgrepJsonParserTests
    {
        [Fact]
        public async Task ParseJsonStreamAsync_WithValidMatchEvent_ParsesCorrectly()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Hello World""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""Hello""},""start"":0,""end"":5}]}}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""},""binary_offset"":null,""stats"":{""elapsed"":{""secs"":0,""nanos"":1000000},""searches"":1,""searches_with_match"":1,""bytes_searched"":100}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            var match = matches[0];
            match.FilePath.Should().Be("test.txt");
            match.Line.Should().Be(1);
            match.LineText.Should().Be("Hello World");
            match.Spans.Should().HaveCount(1);
            match.Spans[0].start.Should().Be(0);
            match.Spans[0].end.Should().Be(5);
            match.Column.Should().Be(1); // Column is 1-indexed
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithMultipleMatches_ParsesAll()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""file1.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""file1.txt""},""lines"":{""text"":""First match""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""First""},""start"":0,""end"":5}]}}
{""type"":""match"",""data"":{""path"":{""text"":""file1.txt""},""lines"":{""text"":""Second match""},""line_number"":5,""absolute_offset"":50,""submatches"":[{""match"":{""text"":""Second""},""start"":0,""end"":6}]}}
{""type"":""end"",""data"":{""path"":{""text"":""file1.txt""}}}
{""type"":""begin"",""data"":{""path"":{""text"":""file2.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""file2.txt""},""lines"":{""text"":""Third match""},""line_number"":10,""absolute_offset"":100,""submatches"":[{""match"":{""text"":""Third""},""start"":0,""end"":5}]}}
{""type"":""end"",""data"":{""path"":{""text"":""file2.txt""}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(3);
            
            matches[0].FilePath.Should().Be("file1.txt");
            matches[0].Line.Should().Be(1);
            matches[0].LineText.Should().Be("First match");
            
            matches[1].FilePath.Should().Be("file1.txt");
            matches[1].Line.Should().Be(5);
            matches[1].LineText.Should().Be("Second match");
            
            matches[2].FilePath.Should().Be("file2.txt");
            matches[2].Line.Should().Be(10);
            matches[2].LineText.Should().Be("Third match");
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithMultipleSubmatches_ParsesAllSpans()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Hello World Hello""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""Hello""},""start"":0,""end"":5},{""match"":{""text"":""Hello""},""start"":12,""end"":17}]}}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            var match = matches[0];
            match.Spans.Should().HaveCount(2);
            match.Spans[0].Should().Be((0, 5));
            match.Spans[1].Should().Be((12, 17));
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithNoSubmatches_HandlesGracefully()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""No highlights""},""line_number"":1,""absolute_offset"":0,""submatches"":[]}}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            matches[0].Spans.Should().BeEmpty();
            matches[0].Column.Should().Be(1); // Default column when no spans
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithContextLines_IgnoresContextEvents()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""context"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Context before""},""line_number"":1,""absolute_offset"":0,""submatches"":[]}}
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Actual match""},""line_number"":2,""absolute_offset"":15,""submatches"":[{""match"":{""text"":""match""},""start"":7,""end"":12}]}}
{""type"":""context"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Context after""},""line_number"":3,""absolute_offset"":30,""submatches"":[]}}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            matches[0].LineText.Should().Be("Actual match");
            matches[0].Line.Should().Be(2);
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithEmptyStream_ReturnsEmpty()
        {
            var json = "";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().BeEmpty();
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithInvalidJson_SkipsInvalidLines()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
INVALID JSON LINE
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Valid match""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""Valid""},""start"":0,""end"":5}]}}
{BROKEN JSON}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            matches[0].LineText.Should().Be("Valid match");
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithSummaryEvent_IgnoresSummary()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Match""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""Match""},""start"":0,""end"":5}]}}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""summary"",""data"":{""stats"":{""elapsed"":{""secs"":0,""nanos"":1000000},""searches"":1,""searches_with_match"":1,""bytes_searched"":100}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            matches[0].LineText.Should().Be("Match");
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithSpecialCharacters_HandlesCorrectly()
        {
            var json = @"
{""type"":""begin"",""data"":{""path"":{""text"":""test.txt""}}}
{""type"":""match"",""data"":{""path"":{""text"":""test.txt""},""lines"":{""text"":""Line with \""quotes\"" and \ttabs\nand newlines""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""quotes""},""start"":11,""end"":17}]}}
{""type"":""end"",""data"":{""path"":{""text"":""test.txt""}}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            matches.Should().HaveCount(1);
            matches[0].LineText.Should().Contain("\"quotes\"");
            matches[0].LineText.Should().Contain("\t");
        }

        [Fact]
        public async Task ParseJsonStreamAsync_WithMissingPathInBegin_HandlesGracefully()
        {
            var json = @"
{""type"":""match"",""data"":{""path"":{""text"":""orphan.txt""},""lines"":{""text"":""Orphan match""},""line_number"":1,""absolute_offset"":0,""submatches"":[{""match"":{""text"":""Orphan""},""start"":0,""end"":6}]}}
";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            using var reader = new StreamReader(stream);

            var matches = await RipgrepJsonParser.ParseJsonStreamAsync(reader).ToListAsync();

            // Should not parse orphan matches without a begin event
            matches.Should().BeEmpty();
        }
    }
}