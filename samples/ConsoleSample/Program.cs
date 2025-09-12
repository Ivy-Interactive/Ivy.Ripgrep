using System;
using System.Threading.Tasks;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;

namespace ConsoleSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: ConsoleSample <pattern> [directory]");
                Console.WriteLine("Example: ConsoleSample TODO");
                Console.WriteLine("Example: ConsoleSample \"class.*Test\" C:\\src\\project");
                return;
            }

            var pattern = args[0];
            var directory = args.Length > 1 ? args[1] : Environment.CurrentDirectory;

            Console.WriteLine($"Searching for '{pattern}' in {directory}...\n");

            var client = new RipgrepClient();
            var options = new RipgrepOptions
            {
                Pattern = pattern,
                WorkingDirectory = directory,
                IncludeGlobs = ["**/*.cs", "**/*.txt", "**/*.md"],
                Json = true,
                CaseInsensitive = false,
                SmartCase = true
            };

            try
            {
                var matchCount = 0;
                await foreach (var match in client.SearchAsync(options))
                {
                    Console.WriteLine($"{match.FilePath}:{match.Line}:{match.Column}");
                    Console.WriteLine($"  {match.LineText.Trim()}");
                    Console.WriteLine();
                    matchCount++;

                    if (matchCount >= 20)
                    {
                        Console.WriteLine("... (showing first 20 matches)");
                        break;
                    }
                }

                Console.WriteLine(matchCount == 0 ? "No matches found." : $"\nFound {matchCount} match(es).");
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}