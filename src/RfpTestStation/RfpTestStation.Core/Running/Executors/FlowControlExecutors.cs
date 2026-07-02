using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running.Executors
{
    public sealed class IfStepExecutor : IStepExecutor
    {
        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            if (EvaluateCondition(context))
            {
                await context.Runner.ExecuteStepListAsync(context.Step.Children, context.ExecutionContext, stopOnFailure: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return Result(context, context.ExecutionContext.SequenceFailed ? StepStatus.Failed : StepStatus.Passed);
        }

        private static bool EvaluateCondition(StepExecutionContext context)
        {
            var expression = context.Step.ConditionExpression ?? context.Step.DescriptionRaw ?? string.Empty;
            return !string.IsNullOrWhiteSpace(expression)
                && context.ExpressionEvaluator.EvaluateBoolean(expression, context.ExecutionContext);
        }

        private static StepResult Result(StepExecutionContext context, StepStatus status)
        {
            return new StepResult { StepName = context.Step.Name, Status = status };
        }
    }

    public sealed class ElseStepExecutor : IStepExecutor
    {
        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            await context.Runner.ExecuteStepListAsync(context.Step.Children, context.ExecutionContext, stopOnFailure: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new StepResult
            {
                StepName = context.Step.Name,
                Status = context.ExecutionContext.SequenceFailed ? StepStatus.Failed : StepStatus.Passed
            };
        }
    }

    public sealed class WhileStepExecutor : IStepExecutor
    {
        private const int MaxIterations = 10000;

        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            var expression = context.Step.ConditionExpression ?? context.Step.DescriptionRaw ?? string.Empty;
            if (string.IsNullOrWhiteSpace(expression))
            {
                return new StepResult { StepName = context.Step.Name, Status = StepStatus.Passed };
            }

            var iterations = 0;
            while (context.ExpressionEvaluator.EvaluateBoolean(expression, context.ExecutionContext))
            {
                cancellationToken.ThrowIfCancellationRequested();
                iterations++;
                if (iterations > MaxIterations)
                {
                    throw new InvalidOperationException("While loop exceeded " + MaxIterations + " iterations.");
                }

                var keepGoing = await context.Runner.ExecuteStepListAsync(context.Step.Children, context.ExecutionContext, stopOnFailure: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (!keepGoing)
                {
                    break;
                }
            }

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = context.ExecutionContext.SequenceFailed ? StepStatus.Failed : StepStatus.Passed
            };
        }
    }

    public sealed class ForStepExecutor : IStepExecutor
    {
        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            await context.Runner.ExecuteStepListAsync(context.Step.Children, context.ExecutionContext, stopOnFailure: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            return new StepResult
            {
                StepName = context.Step.Name,
                Status = context.ExecutionContext.SequenceFailed ? StepStatus.Failed : StepStatus.Passed
            };
        }
    }
}
