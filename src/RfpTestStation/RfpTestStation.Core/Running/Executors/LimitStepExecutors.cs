using System;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Linq;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running.Executors
{
    public sealed class LimitStepExecutors : IStepExecutor
    {
        public async Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            if (ModuleStepClassifier.HasCallableModule(context.Step))
            {
                return await context.AdapterRegistry
                    .ResolveActionAdapter(context.Step)
                    .ExecuteAsync(context.Step, context.ExecutionContext, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (context.Step.StepType == StepType.NumericLimitTest || context.Step.StepType == StepType.MultipleNumericLimitTest)
            {
                return ExecuteNumericLimit(context);
            }

            if (context.Step.StepType == StepType.StringValueTest)
            {
                return ExecuteStringValue(context);
            }

            if (context.Step.StepType == StepType.PassFailTest)
            {
                return ExecutePassFail(context);
            }

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = StepStatus.Passed
            };
        }

        private static StepResult ExecuteNumericLimit(StepExecutionContext context)
        {
            var expression = SelectValueExpression(context, "Step.Result.Numeric", "0");
            var value = context.ExpressionEvaluator.EvaluateNumber(expression, context.ExecutionContext);
            var limit = context.Step.Limits.FirstOrDefault();
            var passed = true;

            if (limit?.Low != null && value < limit.Low.Value)
            {
                passed = false;
            }

            if (limit?.High != null && value > limit.High.Value)
            {
                passed = false;
            }

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = passed ? StepStatus.Passed : StepStatus.Failed,
                Value = value,
                LowLimit = limit?.Low,
                HighLimit = limit?.High,
                Unit = limit?.Unit
            };
        }

        private static StepResult ExecuteStringValue(StepExecutionContext context)
        {
            var expression = SelectValueExpression(context, "Step.Result.String", "\"\"");
            var value = context.ExpressionEvaluator.Evaluate(expression, context.ExecutionContext).Value;
            var actual = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            var expected = context.Step.Limits.FirstOrDefault()?.ExpectedString;
            var passed = expected == null || string.Equals(actual, expected, StringComparison.Ordinal);

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = passed ? StepStatus.Passed : StepStatus.Failed,
                Value = actual,
                Message = expected == null ? null : "Expected: " + expected
            };
        }

        private static StepResult ExecutePassFail(StepExecutionContext context)
        {
            var expression = SelectValueExpression(context, "Step.Result.PassFail", string.Empty);
            var passed = string.IsNullOrWhiteSpace(expression)
                || context.ExpressionEvaluator.EvaluateBoolean(expression, context.ExecutionContext);

            return new StepResult
            {
                StepName = context.Step.Name,
                Status = passed ? StepStatus.Passed : StepStatus.Failed,
                Value = passed
            };
        }

        private static string SelectValueExpression(StepExecutionContext context, string resultProperty, string defaultExpression)
        {
            var preAssignment = ExtractResultAssignmentRightHandSide(context.Step.PreExpression ?? string.Empty, resultProperty);
            if (!string.IsNullOrWhiteSpace(preAssignment))
            {
                return preAssignment!;
            }

            var expression = context.Step.ConditionExpression ?? context.Step.PreExpression ?? context.Step.DescriptionRaw ?? defaultExpression;
            var resultAssignment = ExtractResultAssignmentRightHandSide(expression, resultProperty);
            return string.IsNullOrWhiteSpace(resultAssignment) ? expression : resultAssignment!;
        }

        private static string? ExtractResultAssignmentRightHandSide(string expression, string resultProperty)
        {
            var text = expression.Trim();
            if (!text.StartsWith(resultProperty, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var equals = text.IndexOf('=');
            if (equals < 0)
            {
                return null;
            }

            var rightHandSide = text.Substring(equals + 1).Trim();
            while (rightHandSide.EndsWith(",", StringComparison.Ordinal))
            {
                rightHandSide = rightHandSide.Substring(0, rightHandSide.Length - 1).Trim();
            }

            return rightHandSide;
        }
    }
}
