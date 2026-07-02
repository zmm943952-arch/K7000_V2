using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;

namespace RfpTestStation.Adapters.Flashing
{
    public sealed class ProcessRunner : IExternalProcessRunner
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(10);
        private readonly IProcessTerminator _processTerminator;

        public ProcessRunner()
            : this(new WindowsProcessTerminator())
        {
        }

        public ProcessRunner(IProcessTerminator processTerminator)
        {
            _processTerminator = processTerminator ?? throw new ArgumentNullException(nameof(processTerminator));
        }

        public async Task<ExternalProcessResult> RunAsync(ExternalProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            if (startInfo == null)
            {
                throw new ArgumentNullException(nameof(startInfo));
            }

            var processStartInfo = BuildProcessStartInfo(startInfo);
            using (var process = new Process { StartInfo = processStartInfo, EnableRaisingEvents = true })
            {
                process.Start();
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                var timeout = startInfo.Timeout ?? DefaultTimeout;
                var timeoutMilliseconds = ToTimeoutMilliseconds(timeout);

                using (cancellationToken.Register(() => TryTerminateTree(process)))
                {
                    var exited = await Task.Run(() => process.WaitForExit(timeoutMilliseconds)).ConfigureAwait(false);
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return new ExternalProcessResult
                        {
                            ExitCode = -1,
                            StandardOutput = await stdoutTask.ConfigureAwait(false),
                            StandardError = await stderrTask.ConfigureAwait(false),
                            Cancelled = true
                        };
                    }

                    if (!exited)
                    {
                        TryTerminateTree(process);
                        return new ExternalProcessResult
                        {
                            ExitCode = -1,
                            StandardOutput = await stdoutTask.ConfigureAwait(false),
                            StandardError = await stderrTask.ConfigureAwait(false),
                            TimedOut = true
                        };
                    }
                }

                process.WaitForExit();
                return new ExternalProcessResult
                {
                    ExitCode = process.ExitCode,
                    StandardOutput = await stdoutTask.ConfigureAwait(false),
                    StandardError = await stderrTask.ConfigureAwait(false)
                };
            }
        }

        private static ProcessStartInfo BuildProcessStartInfo(ExternalProcessStartInfo startInfo)
        {
            var extension = Path.GetExtension(startInfo.FileName);
            if (string.Equals(extension, ".bat", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".cmd", StringComparison.OrdinalIgnoreCase))
            {
                return CreateStartInfo(
                    "cmd.exe",
                    "/c " + Quote(startInfo.FileName) + AppendArguments(startInfo.Arguments),
                    startInfo.WorkingDirectory);
            }

            if (string.Equals(extension, ".ps1", StringComparison.OrdinalIgnoreCase))
            {
                return CreateStartInfo(
                    "powershell.exe",
                    "-NoProfile -ExecutionPolicy Bypass -File " + Quote(startInfo.FileName) + AppendArguments(startInfo.Arguments),
                    startInfo.WorkingDirectory);
            }

            return CreateStartInfo(startInfo.FileName, startInfo.Arguments, startInfo.WorkingDirectory);
        }

        private static ProcessStartInfo CreateStartInfo(string fileName, string arguments, string workingDirectory)
        {
            return new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments ?? string.Empty,
                WorkingDirectory = workingDirectory ?? string.Empty,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
        }

        private static string AppendArguments(string arguments)
        {
            return string.IsNullOrWhiteSpace(arguments) ? string.Empty : " " + arguments;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static int ToTimeoutMilliseconds(TimeSpan timeout)
        {
            if (timeout <= TimeSpan.Zero)
            {
                return (int)DefaultTimeout.TotalMilliseconds;
            }

            return timeout.TotalMilliseconds > int.MaxValue ? int.MaxValue : (int)timeout.TotalMilliseconds;
        }

        private void TryTerminateTree(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    _processTerminator.TerminateTree(process);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
