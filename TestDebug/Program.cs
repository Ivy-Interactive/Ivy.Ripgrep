using System;
using System.Threading.Tasks;
using Ivy.Ripgrep;
using Ivy.Ripgrep.Api;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // Create logger factory
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        
        var logger = loggerFactory.CreateLogger<Ivy.Ripgrep.RipgrepClient>();
        
        try
        {
            Console.WriteLine("Creating RipgrepClient...");
            var client = new RipgrepClient(null, logger);
            
            var options = new RipgrepOptions
            {
                Pattern = "test",
                WorkingDirectory = Environment.CurrentDirectory,
                Json = true
            };
            
            Console.WriteLine("Starting search...");
            await foreach (var match in client.SearchAsync(options))
            {
                Console.WriteLine($"Found: {match.FilePath}:{match.Line}");
                break; // Just test if it starts
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}