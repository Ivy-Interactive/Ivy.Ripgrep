using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ivy.Ripgrep.Api;
using Ivy.Ripgrep.Bootstrap;
using Ivy.Ripgrep.Execution;
using Ivy.Ripgrep.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ivy.Ripgrep
{
    /// <summary>
    /// Main client for interacting with ripgrep.
    /// </summary>
    public class RipgrepClient : IRipgrepClient
    {
        private readonly IRipgrepBinaryProvider _binaryProvider;
        private readonly RipgrepRunner _runner;
        private readonly ILogger<RipgrepClient> _logger;

        /// <summary>
        /// Creates a new RipgrepClient instance.
        /// </summary>
        public RipgrepClient() : this(null, null)
        {
        }

        /// <summary>
        /// Creates a new RipgrepClient instance with custom providers.
        /// </summary>
        public RipgrepClient(IRipgrepBinaryProvider? binaryProvider = null, ILogger<RipgrepClient>? logger = null)
        {
            _logger = logger ?? NullLogger<RipgrepClient>.Instance;
            _binaryProvider = binaryProvider ?? new RipgrepDownloader(null, null, verifyChecksums: true);
            _runner = new RipgrepRunner();
        }

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<RipgrepMatch> SearchAsync(
            RipgrepOptions options,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var binaryPath = options.OverrideRipgrepPath ?? 
                await _binaryProvider.GetBinaryPathAsync(options.RequiredRipgrepVersion, ct);

            var args = _runner.BuildArguments(options);
            var process = await _runner.StartProcessAsync(binaryPath, args, options.WorkingDirectory, ct);

            using (process)
            {
                if (options.Json)
                {
                    await foreach (var match in RipgrepJsonParser.ParseJsonStreamAsync(process.StandardOutput, ct))
                    {
                        yield return match;
                    }
                }
                else
                {
                    await foreach (var match in ParseTextOutputAsync(process.StandardOutput, ct))
                    {
                        yield return match;
                    }
                }

                await Task.Run(() => process.WaitForExit(), ct);
                
                if (process.ExitCode != 0 && process.ExitCode != 1)
                {
                    var error = await process.StandardError.ReadToEndAsync();
                    _logger.LogWarning("Ripgrep exited with code {Code}: {Error}", process.ExitCode, error);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<RipgrepMatch>> SearchBufferedAsync(
            RipgrepOptions options,
            CancellationToken ct = default)
        {
            var matches = new List<RipgrepMatch>();

            try
            {
                await foreach (var match in SearchAsync(options, ct))
                {
                    matches.Add(match);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                _logger.LogWarning("SearchBufferedAsync cancelled after collecting {Count} partial results", matches.Count);
                // Return whatever partial results were collected before cancellation
            }

            return matches;
        }

        /// <inheritdoc/>
        public async Task<RipgrepProcessResult> RunRawAsync(
            IEnumerable<string> args,
            string? cwd = null,
            CancellationToken ct = default)
        {
            var binaryPath = await _binaryProvider.GetBinaryPathAsync(null, ct);
            var stopwatch = Stopwatch.StartNew();

            var process = await _runner.StartProcessAsync(binaryPath, args, cwd, ct);

            using (process)
            {
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                await Task.Run(() => process.WaitForExit(), ct);
                stopwatch.Stop();

                return new RipgrepProcessResult
                {
                    StandardOutput = await outputTask,
                    StandardError = await errorTask,
                    ExitCode = process.ExitCode,
                    ExecutionTime = stopwatch.Elapsed
                };
            }
        }

        private async IAsyncEnumerable<RipgrepMatch> ParseTextOutputAsync(
            StreamReader reader,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(':', 4);
                if (parts.Length >= 3)
                {
                    if (int.TryParse(parts[1], out var lineNum) && int.TryParse(parts[2], out var column))
                    {
                        var text = parts.Length > 3 ? parts[3] : "";
                        yield return new RipgrepMatch(
                            parts[0],
                            lineNum,
                            column,
                            text,
                            new List<(int start, int end)>());
                    }
                }
            }
        }
    }
}