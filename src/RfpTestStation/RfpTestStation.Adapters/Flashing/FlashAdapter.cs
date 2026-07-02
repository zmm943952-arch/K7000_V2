using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Adapters.TestPlans;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Flashing
{
    public sealed class FlashAdapter : IFlashAdapter, ITestPlanFlashAdapter
    {
        private readonly FlashScriptMap _scriptMap;
        private readonly IExternalProcessRunner _processRunner;
        private readonly TimeSpan _timeout;

        public FlashAdapter(FlashScriptMap scriptMap, IExternalProcessRunner processRunner, TimeSpan timeout)
        {
            _scriptMap = scriptMap ?? throw new ArgumentNullException(nameof(scriptMap));
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
            _timeout = timeout;
        }

        public string AdapterName
        {
            get { return "Flash"; }
        }

        public async Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var script = _scriptMap.Resolve(step);
            return await ExecuteScriptAsync(step.Name, script, _timeout, cancellationToken).ConfigureAwait(false);
        }

        public async Task<StepResult> ExecuteAsync(TestItem item, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            var script = _scriptMap.Resolve(item);
            var timeout = item.Timeout > TimeSpan.Zero ? item.Timeout : _timeout;
            return await ExecuteScriptAsync(item.Name, script, timeout, cancellationToken).ConfigureAwait(false);
        }

        private async Task<StepResult> ExecuteScriptAsync(
            string stepName,
            FlashScriptDefinition script,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            if (!File.Exists(script.ScriptPath))
            {
                return new StepResult
                {
                    StepName = stepName,
                    Status = StepStatus.Error,
                    Message = "Flash script does not exist: " + script.ScriptPath,
                    Value = -1,
                    ExpectedValue = "ExitCode=0",
                    CompareType = "ProcessExit",
                    Target = script.ScriptPath,
                    Sent = FormatCommand(script),
                    Reply = "Script file missing"
                };
            }

            var processResult = await _processRunner.RunAsync(
                new ExternalProcessStartInfo
                {
                    FileName = script.ScriptPath,
                    Arguments = script.Arguments,
                    WorkingDirectory = script.WorkingDirectory,
                    Timeout = timeout
                },
                cancellationToken).ConfigureAwait(false);

            return new StepResult
            {
                StepName = stepName,
                Status = MapStatus(processResult),
                Message = FormatMessage(script, processResult),
                Value = processResult.ExitCode,
                ExpectedValue = "ExitCode=0",
                CompareType = "ProcessExit",
                Target = script.ScriptPath,
                Sent = FormatCommand(script),
                Reply = FormatReply(processResult),
                ExternalLogPath = ExtractExternalLogPath(processResult)
            };
        }

        private static StepStatus MapStatus(ExternalProcessResult result)
        {
            if (result.Cancelled)
            {
                return StepStatus.Stopped;
            }

            if (result.TimedOut)
            {
                return StepStatus.Error;
            }

            var combinedText = (result.StandardOutput ?? string.Empty) + Environment.NewLine + (result.StandardError ?? string.Empty);
            if (result.ExitCode != 0 || ContainsFailToken(combinedText))
            {
                return StepStatus.Failed;
            }

            return StepStatus.Passed;
        }

        private static bool ContainsFailToken(string text)
        {
            return text.IndexOf("FAIL", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("ERROR", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatMessage(FlashScriptDefinition script, ExternalProcessResult result)
        {
            return "FlashKind=" + script.FlashKind
                + "; Script=" + script.ScriptPath
                + "; Arguments=" + script.Arguments
                + "; WorkingDirectory=" + script.WorkingDirectory
                + "; ExitCode=" + result.ExitCode
                + "; TimedOut=" + result.TimedOut
                + "; Cancelled=" + result.Cancelled
                + "; StdOut=" + (result.StandardOutput ?? string.Empty)
                + "; StdErr=" + (result.StandardError ?? string.Empty);
        }

        private static string FormatCommand(FlashScriptDefinition script)
        {
            return script.ScriptPath
                + (string.IsNullOrWhiteSpace(script.Arguments) ? string.Empty : " " + script.Arguments)
                + "; WorkingDirectory=" + script.WorkingDirectory;
        }

        private static string FormatReply(ExternalProcessResult result)
        {
            return "ExitCode=" + result.ExitCode
                + "; TimedOut=" + result.TimedOut
                + "; Cancelled=" + result.Cancelled
                + "; StdOut=" + (result.StandardOutput ?? string.Empty)
                + "; StdErr=" + (result.StandardError ?? string.Empty);
        }

        private static string? ExtractExternalLogPath(ExternalProcessResult result)
        {
            var text = (result.StandardOutput ?? string.Empty) + Environment.NewLine + (result.StandardError ?? string.Empty);
            foreach (var rawLine in text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var line = rawLine.Trim();
                var markerIndex = line.IndexOf("Log saved to:", StringComparison.OrdinalIgnoreCase);
                if (markerIndex < 0)
                {
                    continue;
                }

                var path = line.Substring(markerIndex + "Log saved to:".Length).Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }

            return null;
        }
    }
}
