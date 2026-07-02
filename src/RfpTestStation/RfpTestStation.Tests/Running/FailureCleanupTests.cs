using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;
using Xunit;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Tests.Running
{
    public sealed class FailureCleanupTests
    {
        [Fact]
        public async Task FailedStepSetsSequenceFailedAndSkipsRemainingMainSteps()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new StatusByNameExecutor(calls, "fail"));
            var sequence = Sequence(
                "MainSequence",
                main: new[] { Action("ok"), Action("fail"), Action("after failure") },
                cleanup: new[] { Action("cleanup") });
            var runner = new SequenceRunner(Document(sequence), registry, new NoDelayClock());

            var context = await runner.RunSequenceAsync("MainSequence");

            Assert.True(context.SequenceFailed);
            Assert.Equal(new[] { "ok", "fail", "cleanup" }, calls);
        }

        [Fact]
        public async Task CleanupRunsAfterStepError()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new ErrorByNameExecutor(calls, "error"));
            var sequence = Sequence(
                "MainSequence",
                main: new[] { Action("error") },
                cleanup: new[] { Action("cleanup") });
            var runner = new SequenceRunner(Document(sequence), registry, new NoDelayClock());

            var context = await runner.RunSequenceAsync("MainSequence");

            Assert.True(context.SequenceFailed);
            Assert.Equal(new[] { "error", "cleanup" }, calls);
        }

        private static SequenceDocument Document(params SequenceDefinition[] sequences)
        {
            var document = new SequenceDocument();
            foreach (var sequence in sequences)
            {
                document.Sequences.Add(sequence);
            }

            return document;
        }

        private static SequenceDefinition Sequence(
            string name,
            IEnumerable<StepDefinition>? setup = null,
            IEnumerable<StepDefinition>? main = null,
            IEnumerable<StepDefinition>? cleanup = null)
        {
            var sequence = new SequenceDefinition { Name = name };
            Add(sequence.SetupSteps, sequence.AllSteps, setup);
            Add(sequence.MainSteps, sequence.AllSteps, main);
            Add(sequence.CleanupSteps, sequence.AllSteps, cleanup);
            return sequence;
        }

        private static void Add(IList<StepDefinition> target, IList<StepDefinition> allSteps, IEnumerable<StepDefinition>? steps)
        {
            if (steps == null)
            {
                return;
            }

            foreach (var step in steps)
            {
                target.Add(step);
                allSteps.Add(step);
            }
        }

        private static StepDefinition Action(string name)
        {
            return new StepDefinition
            {
                Name = name,
                StepType = StepType.Action
            };
        }

        private sealed class StatusByNameExecutor : IStepExecutor
        {
            private readonly IList<string> _calls;
            private readonly string _failedStepName;

            public StatusByNameExecutor(IList<string> calls, string failedStepName)
            {
                _calls = calls;
                _failedStepName = failedStepName;
            }

            public Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
            {
                _calls.Add(context.Step.Name);
                return Task.FromResult(new StepResult
                {
                    StepName = context.Step.Name,
                    Status = context.Step.Name == _failedStepName ? StepStatus.Failed : StepStatus.Passed
                });
            }
        }

        private sealed class ErrorByNameExecutor : IStepExecutor
        {
            private readonly IList<string> _calls;
            private readonly string _errorStepName;

            public ErrorByNameExecutor(IList<string> calls, string errorStepName)
            {
                _calls = calls;
                _errorStepName = errorStepName;
            }

            public Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
            {
                _calls.Add(context.Step.Name);
                return Task.FromResult(new StepResult
                {
                    StepName = context.Step.Name,
                    Status = context.Step.Name == _errorStepName ? StepStatus.Error : StepStatus.Passed
                });
            }
        }

        private sealed class NoDelayClock : IClock
        {
            public System.DateTimeOffset UtcNow { get; } = System.DateTimeOffset.Parse("2026-06-30T00:00:00Z");

            public Task DelayAsync(System.TimeSpan delay, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
