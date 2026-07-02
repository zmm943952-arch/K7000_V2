using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running.Executors
{
    public sealed class WaitStepExecutor : IStepExecutor
    {
        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            var expression = context.Step.ConditionExpression ?? context.Step.DescriptionRaw ?? string.Empty;
            if (expression.TrimStart().StartsWith("Thread(", StringComparison.OrdinalIgnoreCase))
            {
                return new StepResult
                {
                    StepName = context.Step.Name,
                    Status = StepStatus.Passed,
                    Message = "Thread wait treated as already handled."
                };
            }

            var seconds = string.IsNullOrWhiteSpace(expression)
                ? 0
                : context.ExpressionEvaluator.EvaluateNumber(expression, context.ExecutionContext);

            await context.Clock.DelayAsync(TimeSpan.FromSeconds(seconds), cancellationToken).ConfigureAwait(false);

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = StepStatus.Passed
            };
        }
    }
}
