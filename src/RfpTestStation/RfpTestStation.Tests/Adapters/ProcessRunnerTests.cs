using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Adapters.Flashing;
using RfpTestStation.Core.Abstractions;
using Xunit;

namespace RfpTestStation.Tests.Adapters
{
    public sealed class ProcessRunnerTests
    {
        [Fact]
        public async Task RunAsyncExecutesCmdScriptWithArgumentsAndCapturesOutput()
        {
            var workingDirectory = CreateTempDirectory();
            var scriptPath = Path.Combine(workingDirectory, "echo_args.cmd");
            File.WriteAllText(
                scriptPath,
                "@echo off\r\n"
                + "echo args=%*\r\n"
                + "exit /b 0\r\n");

            try
            {
                var result = await new ProcessRunner().RunAsync(
                    new ExternalProcessStartInfo
                    {
                        FileName = scriptPath,
                        Arguments = "alpha beta",
                        WorkingDirectory = workingDirectory,
                        Timeout = TimeSpan.FromSeconds(5)
                    },
                    CancellationToken.None);

                Assert.Equal(0, result.ExitCode);
                Assert.False(result.TimedOut);
                Assert.Contains("args=alpha beta", result.StandardOutput);
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public async Task RunAsyncExecutesPowerShellScriptWithArgumentsAndCapturesOutput()
        {
            var workingDirectory = CreateTempDirectory();
            var scriptPath = Path.Combine(workingDirectory, "echo_args.ps1");
            File.WriteAllText(
                scriptPath,
                "Write-Output ('args=' + ($args -join ','))\r\n"
                + "exit 0\r\n");

            try
            {
                var result = await new ProcessRunner().RunAsync(
                    new ExternalProcessStartInfo
                    {
                        FileName = scriptPath,
                        Arguments = "alpha beta",
                        WorkingDirectory = workingDirectory,
                        Timeout = TimeSpan.FromSeconds(5)
                    },
                    CancellationToken.None);

                Assert.Equal(0, result.ExitCode);
                Assert.False(result.TimedOut);
                Assert.Contains("args=alpha,beta", result.StandardOutput);
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public async Task RunAsyncReturnsTimedOutResultWhenProcessExceedsTimeout()
        {
            var workingDirectory = CreateTempDirectory();
            var scriptPath = Path.Combine(workingDirectory, "sleep.ps1");
            File.WriteAllText(scriptPath, "Start-Sleep -Seconds 5\r\n");

            try
            {
                var result = await new ProcessRunner().RunAsync(
                    new ExternalProcessStartInfo
                    {
                        FileName = scriptPath,
                        WorkingDirectory = workingDirectory,
                        Timeout = TimeSpan.FromMilliseconds(250)
                    },
                    CancellationToken.None);

                Assert.True(result.TimedOut);
                Assert.Equal(-1, result.ExitCode);
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        [Fact]
        public async Task RunAsyncTerminatesProcessTreeWhenCancelled()
        {
            var workingDirectory = CreateTempDirectory();
            var scriptPath = Path.Combine(workingDirectory, "sleep.ps1");
            File.WriteAllText(scriptPath, "Start-Sleep -Seconds 5\r\n");
            var terminator = new RecordingProcessTerminator();
            var cancellation = new CancellationTokenSource();

            try
            {
                var runTask = new ProcessRunner(terminator).RunAsync(
                    new ExternalProcessStartInfo
                    {
                        FileName = scriptPath,
                        WorkingDirectory = workingDirectory,
                        Timeout = TimeSpan.FromSeconds(5)
                    },
                    cancellation.Token);

                await Task.Delay(250);
                cancellation.Cancel();
                var result = await runTask;

                Assert.True(result.Cancelled);
                Assert.True(terminator.TerminatedTree);
            }
            finally
            {
                Directory.Delete(workingDirectory, true);
            }
        }

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "RfpTestStation_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private sealed class RecordingProcessTerminator : IProcessTerminator
        {
            public bool TerminatedTree { get; private set; }

            public void TerminateTree(System.Diagnostics.Process process)
            {
                TerminatedTree = true;
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
        }
    }
}
