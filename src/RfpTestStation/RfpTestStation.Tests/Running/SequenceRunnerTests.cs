using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;
using Xunit;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Tests.Running
{
    public sealed class SequenceRunnerTests
    {
        [Fact]
        public async Task RunSequenceExecutesSetupMainAndCleanupInOrder()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var runner = new SequenceRunner(Document(Sequence(
                "MainSequence",
                setup: new[] { Action("setup") },
                main: new[] { Action("main") },
                cleanup: new[] { Action("cleanup") })), registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(new[] { "setup", "main", "cleanup" }, calls);
        }

        [Fact]
        public async Task RunSequenceRecordsSkippedStepsWithoutExecutingExecutor()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var skipped = Action("skip");
            skipped.RunMode = RunMode.Skip;
            var runner = new SequenceRunner(Document(Sequence("MainSequence", main: new[] { skipped })), registry, new FakeClock());

            var context = await runner.RunSequenceAsync("MainSequence");

            Assert.Empty(calls);
            Assert.Equal(StepStatus.Skipped, context.StepResults.Single().Status);
        }

        [Fact]
        public async Task WaitStepUsesClockDelayFromTimeIntervalExpression()
        {
            var clock = new FakeClock();
            var runner = new SequenceRunner(Document(Sequence(
                "MainSequence",
                main: new[] { Step("wait", StepType.Wait, "TimeInterval(1)") })), StepExecutorRegistry.CreateDefault(), clock);

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(TimeSpan.FromSeconds(1), clock.Delays.Single());
        }

        [Fact]
        public async Task WaitStepTreatsThreadWaitAsAlreadyHandled()
        {
            var runner = new SequenceRunner(Document(Sequence(
                "MainSequence",
                main: new[] { Step("wait thread", StepType.Wait, "Thread(Stop_Seq_Period (Main))") })), StepExecutorRegistry.CreateDefault(), new FakeClock());

            var context = await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(StepStatus.Passed, context.StepResults.Single().Status);
        }

        [Theory]
        [InlineData(true, 1)]
        [InlineData(false, 0)]
        public async Task IfStepRunsChildrenOnlyWhenConditionIsTrue(bool enabled, int expectedCalls)
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var ifStep = Step("if", StepType.If, "Locals.Enabled");
            ifStep.Children.Add(Action("inside"));
            var context = new StationExecutionContext();
            context.Locals["Enabled"] = enabled;
            var runner = new SequenceRunner(Document(Sequence("MainSequence", main: new[] { ifStep })), registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence", context);

            Assert.Equal(expectedCalls, calls.Count);
        }

        [Fact]
        public async Task RunSequenceInitializesFileGlobalsFromDocument()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var ifStep = Step("if", StepType.If, "FileGlobals.StopStatus==False");
            ifStep.Children.Add(Action("inside"));
            var document = Document(Sequence("MainSequence", main: new[] { ifStep }));
            document.FileGlobals["StopStatus"] = false;
            var runner = new SequenceRunner(document, registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(new[] { "inside" }, calls);
        }

        [Fact]
        public async Task RunSequenceInitializesLocalsFromSequence()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var ifStep = Step("if", StepType.If, "Locals.X1气缸原点==False");
            ifStep.Children.Add(Action("inside"));
            var sequence = Sequence("MainSequence", main: new[] { ifStep });
            sequence.Locals["X1气缸原点"] = false;
            var runner = new SequenceRunner(Document(sequence), registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(new[] { "inside" }, calls);
        }

        [Fact]
        public async Task WhileStepRepeatsUntilConditionBecomesFalse()
        {
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new DelegateExecutor((step, context) =>
            {
                var count = (int)context.Locals["Count"];
                context.Locals["Count"] = count + 1;
                if (count + 1 >= 3)
                {
                    context.Locals["KeepGoing"] = false;
                }

                return StepStatus.Passed;
            }));
            var whileStep = Step("while", StepType.While, "Locals.KeepGoing");
            whileStep.Children.Add(Action("increment"));
            var context = new StationExecutionContext();
            context.Locals["Count"] = 0;
            context.Locals["KeepGoing"] = true;
            var runner = new SequenceRunner(Document(Sequence("MainSequence", main: new[] { whileStep })), registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence", context);

            Assert.Equal(3, context.Locals["Count"]);
        }

        [Fact]
        public async Task SequenceCallRunsTargetSequence()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var document = Document(
                Sequence("MainSequence", main: new[] { Step("call sub", StepType.SequenceCall, "SubSeq") }),
                Sequence("SubSeq", main: new[] { Action("sub action") }));
            var runner = new SequenceRunner(document, registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(new[] { "sub action" }, calls);
        }

        [Fact]
        public async Task SequenceCallParsesTestStandCallExpression()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var document = Document(
                Sequence("MainSequence", main: new[]
                {
                    Step("Stop_Seq_Period", StepType.SequenceCall, "Call Stop_Seq_Period(Locals.ModbusClientRef, False) in <Current File>")
                }),
                Sequence("Stop_Seq_Period", main: new[] { Action("stop monitor") }));
            var runner = new SequenceRunner(document, registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(new[] { "stop monitor" }, calls);
        }

        [Fact]
        public async Task NewThreadSequenceCallDoesNotBlockMainSequence()
        {
            var calls = new List<string>();
            var registry = new StepExecutorRegistry();
            registry.Register(StepType.Action, new RecordingExecutor(calls));
            var call = Step("call monitor", StepType.SequenceCall, "Call Monitor() in <Current File>");
            call.IsNewThread = true;
            var document = Document(
                Sequence("MainSequence", main: new[] { call, Action("after monitor") }),
                Sequence("Monitor", main: new[] { Action("monitor loop") }));
            var runner = new SequenceRunner(document, registry, new FakeClock());

            await runner.RunSequenceAsync("MainSequence");

            Assert.Equal(new[] { "after monitor" }, calls);
        }

        [Fact]
        public async Task NumericLimitStepFailsWhenValueIsOutsideLimits()
        {
            var numericLimit = Step("numeric limit", StepType.NumericLimitTest, "Locals.Value");
            numericLimit.Limits.Add(new LimitDefinition { Low = 1, High = 10, Unit = "V" });
            var context = new StationExecutionContext();
            context.Locals["Value"] = 11;
            var runner = new SequenceRunner(Document(Sequence("MainSequence", main: new[] { numericLimit })), StepExecutorRegistry.CreateDefault(), new FakeClock());

            await runner.RunSequenceAsync("MainSequence", context);

            var result = context.StepResults.Single();
            Assert.True(context.SequenceFailed);
            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(11.0, result.Value);
            Assert.Equal(1, result.LowLimit);
            Assert.Equal(10, result.HighLimit);
            Assert.Equal("V", result.Unit);
        }

        [Fact]
        public async Task PassFailStepFailsWhenBooleanExpressionIsFalse()
        {
            var passFail = Step("pass fail", StepType.PassFailTest, "Locals.Ok");
            var context = new StationExecutionContext();
            context.Locals["Ok"] = false;
            var runner = new SequenceRunner(Document(Sequence("MainSequence", main: new[] { passFail })), StepExecutorRegistry.CreateDefault(), new FakeClock());

            await runner.RunSequenceAsync("MainSequence", context);

            Assert.True(context.SequenceFailed);
            Assert.Equal(StepStatus.Failed, context.StepResults.Single().Status);
        }

        [Fact]
        public async Task StringValueStepUsesStepResultStringAssignmentRightHandSide()
        {
            var step = Step("string value", StepType.StringValueTest, null);
            step.ConditionExpression = "Step.Result.String";
            step.PreExpression = "Step.Result.String=Locals.Path,";
            var context = new StationExecutionContext();
            context.Locals["Path"] = "mock.log";
            var runner = new SequenceRunner(Document(Sequence("MainSequence", main: new[] { step })), StepExecutorRegistry.CreateDefault(), new FakeClock());

            await runner.RunSequenceAsync("MainSequence", context);

            var result = context.StepResults.Single();
            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("mock.log", result.Value);
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
            return Step(name, StepType.Action, null);
        }

        private static StepDefinition Step(string name, StepType type, string? expression)
        {
            return new StepDefinition
            {
                Name = name,
                StepType = type,
                DescriptionRaw = expression,
                ConditionExpression = expression
            };
        }

        private sealed class RecordingExecutor : IStepExecutor
        {
            private readonly IList<string> _calls;

            public RecordingExecutor(IList<string> calls)
            {
                _calls = calls;
            }

            public Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
            {
                _calls.Add(context.Step.Name);
                return Task.FromResult(new StepResult
                {
                    StepName = context.Step.Name,
                    Status = StepStatus.Passed
                });
            }
        }

        private sealed class DelegateExecutor : IStepExecutor
        {
            private readonly Func<StepDefinition, StationExecutionContext, StepStatus> _execute;

            public DelegateExecutor(Func<StepDefinition, StationExecutionContext, StepStatus> execute)
            {
                _execute = execute;
            }

            public Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
            {
                return Task.FromResult(new StepResult
                {
                    StepName = context.Step.Name,
                    Status = _execute(context.Step, context.ExecutionContext)
                });
            }
        }

        private sealed class FakeClock : IClock
        {
            public IList<TimeSpan> Delays { get; } = new List<TimeSpan>();

            public DateTimeOffset UtcNow { get; private set; } = DateTimeOffset.Parse("2026-06-30T00:00:00Z");

            public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
            {
                Delays.Add(delay);
                UtcNow = UtcNow.Add(delay);
                return Task.CompletedTask;
            }
        }
    }
}
