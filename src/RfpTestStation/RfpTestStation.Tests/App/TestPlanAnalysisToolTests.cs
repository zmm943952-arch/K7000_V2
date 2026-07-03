using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
            var reportPath = Path.Combine(Path.GetTempPath(), "RfpTestStation_TestPlanAnalysis_" + Guid.NewGuid().ToString("N") + ".md");

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -MarkdownPath \"" + reportPath + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("TESTPLAN ANALYSIS", result.Output);
                Assert.Contains("Plan: RFP 7000 V2", result.Output);
                Assert.Contains("Timeout total:", result.Output);
                Assert.Contains("KIND SUMMARY", result.Output);
                Assert.Contains("POWER-ON REUSE", result.Output);
                Assert.Contains("SETTLE TIME", result.Output);
                Assert.Contains("I2C REUSE", result.Output);
                Assert.Contains("OPTIMIZATION SUGGESTIONS", result.Output);

                var report = File.ReadAllText(reportPath, Encoding.UTF8);
                Assert.Contains("# Testplan Optimization Report", report);
                Assert.Contains("## Summary", report);
                Assert.Contains("## Kind Summary", report);
                Assert.Contains("## Optimization Priority Review", report);
                Assert.Contains("\u53ef\u7acb\u5373\u6539", report);
                Assert.Contains("\u9700\u786c\u4ef6\u786e\u8ba4", report);
                Assert.Contains("\u6682\u4e0d\u6539", report);
                Assert.Contains("## Optimization Suggestions", report);
            }
            finally
            {
                if (File.Exists(reportPath))
                {
                    File.Delete(reportPath);
                }
            }
        }

        [Fact]
        public void FlashTimeoutReviewScriptGeneratesActionableMarkdown()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "generate-flash-timeout-review.ps1");
            var reportPath = Path.Combine(Path.GetTempPath(), "RfpTestStation_FlashTimeoutReview_" + Guid.NewGuid().ToString("N") + ".md");

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -OutputPath \"" + reportPath + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("FLASH TIMEOUT REVIEW", result.Output);

                var report = File.ReadAllText(reportPath, Encoding.UTF8);
                Assert.Contains("# Flash Timeout Review", report);
                Assert.Contains("flash.mcu.simple", report);
                Assert.Contains("flash.tcon", report);
                Assert.Contains("flash.tddi", report);
                Assert.Contains("flash.mcu.shipping", report);
                Assert.Contains("Actual Duration", report);
                Assert.Contains("Hardware Confirmed", report);
            }
            finally
            {
                if (File.Exists(reportPath))
                {
                    File.Delete(reportPath);
                }
            }
        }

        [Fact]
        public void SettleTimeReviewScriptGeneratesActionableMarkdown()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "generate-settle-time-review.ps1");
            var reportPath = Path.Combine(Path.GetTempPath(), "RfpTestStation_SettleTimeReview_" + Guid.NewGuid().ToString("N") + ".md");

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -OutputPath \"" + reportPath + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("SETTLE TIME REVIEW", result.Output);

                var report = File.ReadAllText(reportPath, Encoding.UTF8);
                Assert.Contains("# Settle Time Review", report);
                Assert.Contains("Explicit settleMs total", report);
                Assert.Contains("Scope", report);
                Assert.Contains("Hardware Confirmed", report);
            }
            finally
            {
                if (File.Exists(reportPath))
                {
                    File.Delete(reportPath);
                }
            }
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
