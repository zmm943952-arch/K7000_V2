using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Expressions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Core.Running
{
    public sealed class SequenceRunner
    {
        private readonly SequenceDocument _document;
        private readonly StepExecutorRegistry _registry;
        private readonly IClock _clock;
        private readonly TestStandExpressionEvaluator _expressionEvaluator;
        private readonly IStationAdapterRegistry _adapterRegistry;

        public SequenceRunner(SequenceDocument document)
            : this(document, StepExecutorRegistry.CreateDefault(), new SystemClock(), NullStationAdapterRegistry.Instance)
        {
        }

        public SequenceRunner(SequenceDocument document, StepExecutorRegistry registry, IClock clock)
            : this(document, registry, clock, NullStationAdapterRegistry.Instance)
        {
        }

        public SequenceRunner(SequenceDocument document, StepExecutorRegistry registry, IClock clock, IStationAdapterRegistry adapterRegistry)
        {
            _document = document;
            _registry = registry;
            _clock = clock;
            _expressionEvaluator = new TestStandExpressionEvaluator();
            _adapterRegistry = adapterRegistry;
        }

        public Action<StepResult>? StepCompleted { get; set; }

        public async Task<StationExecutionContext> RunSequenceAsync(
            string sequenceName,
            StationExecutionContext? context = null,
            CancellationToken cancellationToken = default)
        {
            var executionContext = context ?? new StationExecutionContext();
            var sequence = _document.GetSequence(sequenceName);
            InitializeFileGlobals(executionContext);
            InitializeLocals(sequence, executionContext);

            try
            {
                var setupPassed = await ExecuteStepListAsync(sequence.SetupSteps, executionContext, stopOnFailure: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (setupPassed && !executionContext.SequenceFailed && !executionContext.StopRequested)
                {
                    await ExecuteStepListAsync(sequence.MainSteps, executionContext, stopOnFailure: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                executionContext.StopRequested = true;
            }
            finally
            {
                await ExecuteStepListAsync(sequence.CleanupSteps, executionContext, stopOnFailure: false, cancellationToken: CancellationToken.None).ConfigureAwait(false);
            }

            return executionContext;
        }

        private void InitializeFileGlobals(StationExecutionContext executionContext)
        {
            foreach (var item in _document.FileGlobals)
            {
                if (!executionContext.FileGlobals.ContainsKey(item.Key))
                {
                    executionContext.FileGlobals[item.Key] = item.Value;
                }
            }
        }

        private static void InitializeLocals(SequenceDefinition sequence, StationExecutionContext executionContext)
        {
            foreach (var item in sequence.Locals)
            {
                if (!executionContext.Locals.ContainsKey(item.Key))
                {
                    executionContext.Locals[item.Key] = item.Value;
                }
            }
        }

        internal async Task<bool> ExecuteStepListAsync(
            IEnumerable<StepDefinition> steps,
            StationExecutionContext executionContext,
            bool stopOnFailure,
            CancellationToken cancellationToken)
        {
            foreach (var step in steps)
            {
                if (executionContext.StopRequested)
                {
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();

                var result = await ExecuteStepAsync(step, executionContext, cancellationToken).ConfigureAwait(false);
                executionContext.StepResults.Add(result);
                StepCompleted?.Invoke(result);

                if (IsFailureStatus(result.Status))
                {
                    if (result.Status == StepStatus.Failed || result.Status == StepStatus.Error)
                    {
                        executionContext.SequenceFailed = true;
                    }

                    if (stopOnFailure)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<StepResult> ExecuteStepAsync(
            StepDefinition step,
            StationExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            var startTime = _clock.UtcNow;

            if (step.RunMode == RunMode.Skip)
            {
                return CreateResult(step, StepStatus.Skipped, startTime, _clock.UtcNow, null, null);
            }

            try
            {
                var stepContext = new StepExecutionContext(step, executionContext, this, _clock, _expressionEvaluator, _adapterRegistry);
                var executor = _registry.Resolve(step);
                var result = await executor.ExecuteAsync(stepContext, cancellationToken).ConfigureAwait(false);
                result.StepName = string.IsNullOrWhiteSpace(result.StepName) ? step.Name : result.StepName;
                if (result.StartTime == default)
                {
                    result.StartTime = startTime;
                }

                if (result.EndTime == default)
                {
                    result.EndTime = _clock.UtcNow;
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                executionContext.StopRequested = true;
                return CreateResult(step, StepStatus.Stopped, startTime, _clock.UtcNow, null, null);
            }
            catch (Exception ex)
            {
                return CreateResult(step, StepStatus.Error, startTime, _clock.UtcNow, ex.Message, ex);
            }
        }

        private static StepResult CreateResult(
            StepDefinition step,
            StepStatus status,
            DateTimeOffset startTime,
            DateTimeOffset endTime,
            string? message,
            Exception? error)
        {
            return new StepResult
            {
                StepName = step.Name,
                Status = status,
                StartTime = startTime,
                EndTime = endTime,
                Message = message,
                Error = error
            };
        }

        private static bool IsFailureStatus(StepStatus status)
        {
            return status == StepStatus.Failed
                || status == StepStatus.Error
                || status == StepStatus.Terminated
                || status == StepStatus.Stopped;
        }
    }
}
