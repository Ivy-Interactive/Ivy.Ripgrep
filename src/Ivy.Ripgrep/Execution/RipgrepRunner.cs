using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ivy.Ripgrep.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ivy.Ripgrep.Execution
{
    public class RipgrepRunner
    {
        private readonly ILogger<RipgrepRunner> _logger;

        public RipgrepRunner(ILogger<RipgrepRunner>? logger = null)
        {
            _logger = logger ?? NullLogger<RipgrepRunner>.Instance;
        }

        public Task<Process> StartProcessAsync(
            string binaryPath,
            IEnumerable<string> arguments,
            string? workingDirectory = null,
            CancellationToken ct = default)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = binaryPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            var enumerable = arguments as string[] ?? arguments.ToArray();
            foreach (var arg in enumerable)
            {
                startInfo.ArgumentList.Add(arg);
            }

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            _logger.LogDebug("Starting ripgrep: {Path} {Args}", binaryPath, string.Join(" ", enumerable));

            var process = new Process { StartInfo = startInfo };
            
            if (!process.Start())
            {
                throw new InvalidOperationException("Failed to start ripgrep process");
            }

            ct.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                    // ignored
                }
            });

            return Task.FromResult(process);
        }

        public List<string> BuildArguments(RipgrepOptions options)
        {
            var args = new List<string>();

            if (options.Json)
            {
                args.Add("--json");
            }

            if (options.CaseInsensitive)
            {
                args.Add("-i");
            }

            if (options.SmartCase)
            {
                args.Add("-S");
            }

            if (options.WordRegexp)
            {
                args.Add("-w");
            }

            if (options.Multiline)
            {
                args.Add("-U");
                args.Add("--multiline");
            }

            if (options.MultilineCrLf)
            {
                args.Add("--crlf");
            }

            foreach (var glob in options.IncludeGlobs)
            {
                args.Add("-g");
                args.Add(glob);
            }

            foreach (var glob in options.ExcludeGlobs)
            {
                args.Add("-g");
                args.Add($"!{glob}");
            }

            if (options.MaxDepth.HasValue)
            {
                args.Add("--max-depth");
                args.Add(options.MaxDepth.Value.ToString());
            }

            if (options.ContextBefore.HasValue)
            {
                args.Add("-B");
                args.Add(options.ContextBefore.Value.ToString());
            }

            if (options.ContextAfter.HasValue)
            {
                args.Add("-A");
                args.Add(options.ContextAfter.Value.ToString());
            }

            if (options.FollowSymlinks)
            {
                args.Add("-L");
            }

            if (!options.RespectGitIgnore)
            {
                args.Add("-u");
            }

            if (options.Hidden)
            {
                args.Add("--hidden");
            }

            if (options.Unrestricted)
            {
                args.Add("-uuu");
            }

            if (options.Threads.HasValue)
            {
                args.Add("-j");
                args.Add(options.Threads.Value.ToString());
            }

            if (options.MaxColumns.HasValue)
            {
                args.Add("--max-columns");
                args.Add(options.MaxColumns.Value.ToString());
            }

            if (options.MaxFilesizeMB.HasValue)
            {
                args.Add("--max-filesize");
                args.Add($"{options.MaxFilesizeMB.Value}M");
            }

            if (options is { LineNumber: true, Json: false })
            {
                args.Add("-n");
            }

            if (options is { Column: true, Json: false })
            {
                args.Add("--column");
            }

            args.AddRange(options.AdditionalArguments);

            if (!string.IsNullOrEmpty(options.Pattern))
            {
                args.Add(options.Pattern);
            }

            args.Add(".");

            return args;
        }
    }
}