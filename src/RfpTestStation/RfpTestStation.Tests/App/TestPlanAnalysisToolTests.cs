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
                Assert.Contains("docs/validation/flash-timeout-review.md", report);
                Assert.Contains("docs/validation/settle-time-review.md", report);
                Assert.Contains("docs/validation/i2c-reuse-review.md", report);
                Assert.Contains("docs/validation/stop-policy-review.md", report);
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

        [Fact]
        public void I2cReuseReviewScriptGeneratesActionableMarkdown()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "generate-i2c-reuse-review.ps1");
            var reportPath = Path.Combine(Path.GetTempPath(), "RfpTestStation_I2cReuseReview_" + Guid.NewGuid().ToString("N") + ".md");

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -OutputPath \"" + reportPath + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("I2C REUSE REVIEW", result.Output);

                var report = File.ReadAllText(reportPath, Encoding.UTF8);
                Assert.Contains("# I2C Reuse Review", report);
                Assert.Contains("Repeated signature count", report);
                Assert.Contains("Hardware Confirmed", report);
                Assert.Contains("Merge Candidate", report);
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
        public void StopOnFailureReviewScriptGeneratesActionableMarkdown()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "generate-stop-policy-review.ps1");
            var reportPath = Path.Combine(Path.GetTempPath(), "RfpTestStation_StopPolicyReview_" + Guid.NewGuid().ToString("N") + ".md");

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -OutputPath \"" + reportPath + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("STOP POLICY REVIEW", result.Output);

                var report = File.ReadAllText(reportPath, Encoding.UTF8);
                Assert.Contains("# Stop Policy Review", report);
                Assert.Contains("result.output", report);
                Assert.Contains("cleanup.fixture", report);
                Assert.Contains("Allowed To Continue", report);
                Assert.Contains("Reason", report);
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
        public void HardwareConfirmationChecklistScriptGeneratesActionableMarkdown()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "generate-hardware-confirmation-checklist.ps1");
            var reportPath = Path.Combine(Path.GetTempPath(), "RfpTestStation_HardwareChecklist_" + Guid.NewGuid().ToString("N") + ".md");

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -OutputPath \"" + reportPath + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("HARDWARE CONFIRMATION CHECKLIST", result.Output);

                var report = File.ReadAllText(reportPath, Encoding.UTF8);
                Assert.Contains("# Testplan Hardware Confirmation Checklist", report);
                Assert.Contains("No-Hardware Completed", report);
                Assert.Contains("Hardware Required", report);
                Assert.Contains("Flash Timeout", report);
                Assert.Contains("Settle Time", report);
                Assert.Contains("I2C Reuse", report);
                Assert.Contains("Stop Policy", report);
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
        public void OptimizationAuditScriptRegeneratesReportsAndRunsMockValidation()
        {
            var repoRoot = TestPaths.RepoRoot();
            var scriptPath = Path.Combine(repoRoot, "Tools", "run-testplan-optimization-audit.ps1");
            var outputDirectory = Path.Combine(Path.GetTempPath(), "RfpTestStation_OptimizationAudit_" + Guid.NewGuid().ToString("N"));

            try
            {
                var result = RunPowerShell(
                    repoRoot,
                    "-NoProfile -ExecutionPolicy Bypass -File \"" + scriptPath + "\" -OutputDirectory \"" + outputDirectory + "\"");

                Assert.Equal(0, result.ExitCode);
                Assert.Contains("TESTPLAN OPTIMIZATION AUDIT", result.Output);
                Assert.Contains("TESTPLAN ANALYSIS", result.Output);
                Assert.Contains("FLASH TIMEOUT REVIEW", result.Output);
                Assert.Contains("SETTLE TIME REVIEW", result.Output);
                Assert.Contains("I2C REUSE REVIEW", result.Output);
                Assert.Contains("STOP POLICY REVIEW", result.Output);
                Assert.Contains("HARDWARE CONFIRMATION CHECKLIST", result.Output);
                Assert.Contains("Mock validation completed.", result.Output);

                Assert.True(File.Exists(Path.Combine(outputDirectory, "testplan-optimization-report.md")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "flash-timeout-review.md")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "settle-time-review.md")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "i2c-reuse-review.md")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "stop-policy-review.md")));
                Assert.True(File.Exists(Path.Combine(outputDirectory, "testplan-hardware-confirmation-checklist.md")));
            }
            finally
            {
                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
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
