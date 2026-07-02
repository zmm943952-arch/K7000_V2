using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running.Executors
{
    public sealed class SequenceCallStepExecutor : IStepExecutor
    {
        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            var targetSequenceName = GetTargetSequenceName(context.Step);
            if (string.IsNullOrWhiteSpace(targetSequenceName))
            {
                return new StepResult
                {
                    StepName = context.Step.Name,
                    Status = StepStatus.Error,
                    Message = "Sequence call target is empty."
                };
            }

            if (context.Step.IsNewThread)
            {
                return new StepResult
                {
                    StepName = context.Step.Name,
                    Status = StepStatus.Passed,
                    Message = "New Thread sequence call queued: " + targetSequenceName
                };
            }

            await context.Runner.RunSequenceAsync(targetSequenceName, context.ExecutionContext, cancellationToken).ConfigureAwait(false);

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = context.ExecutionContext.SequenceFailed ? StepStatus.Failed : StepStatus.Passed
            };
        }

        private static string GetTargetSequenceName(StepDefinition step)
        {
            var raw = step.ConditionExpression ?? step.DescriptionRaw ?? step.ModuleCall?.RawText ?? string.Empty;
            raw = raw.Trim();

            const string prefix = "Sequence Call,";
            if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(prefix.Length).Trim();
            }

            var lineBreak = raw.IndexOfAny(new[] { '\r', '\n' });
            if (lineBreak >= 0)
            {
                raw = raw.Substring(0, lineBreak).Trim();
            }

            const string callPrefix = "Call ";
            if (raw.StartsWith(callPrefix, StringComparison.OrdinalIgnoreCase))
            {
                raw = raw.Substring(callPrefix.Length).Trim();
            }

            var argumentStart = raw.IndexOf('(');
            if (argumentStart >= 0)
            {
                raw = raw.Substring(0, argumentStart).Trim();
            }

            var currentFileMarker = raw.IndexOf(" in <Current File>", StringComparison.OrdinalIgnoreCase);
            if (currentFileMarker >= 0)
            {
                raw = raw.Substring(0, currentFileMarker).Trim();
            }

            return raw;
        }
    }
}
