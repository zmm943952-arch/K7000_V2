using System;
using System.Diagnostics;
using System.IO;
using Xunit;

namespace RfpTestStation.Tests.App
{
    public sealed class TestPlanAnalysisToolTests
    {
        [Fact]
        public void AnalyzeTestPlanScriptReportsTimingAndOptimizationSignals()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "analyze-testplan.ps1");
            var result = RunPowerShell(repoRoot, "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\"");

            Assert.Equal(0, result.ExitCode);
            Assert.Contains("TESTPLAN ANALYSIS", result.Output);
            Assert.Contains("Plan: RFP 7000 V2", result.Output);
            Assert.Contains("Timeout total:", result.Output);
            Assert.Contains("KIND SUMMARY", result.Output);
            Assert.Contains("POWER-ON REUSE", result.Output);
            Assert.Contains("SETTLE TIME", result.Output);
            Assert.Contains("I2C REUSE", result.Output);
            Assert.Contains("OPTIMIZATION SUGGESTIONS", result.Output);
        }

        private static ProcessResult RunPowerShell(string workingDirectory, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start PowerShell.");
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();
                return new ProcessResult(process.ExitCode, output + error);
            }
        }

        private sealed class ProcessResult
        {
            public ProcessResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output;
            }

            public int ExitCode { get; }

            public string Output { get; }
        }
    }
}
