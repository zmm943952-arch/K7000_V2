using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;
using Xunit;

namespace RfpTestStation.Tests.Workflow
{
    public sealed class FunctionalTestWorkflowTests
    {
        [Fact]
        public async Task WorkflowRepresentsProductionStagesWithoutTestStandStepInternals()
        {
            var items = new[]
            {
                Required("fixture.prepare", "FixturePrepare", TestItemKind.FixturePrepare, "MainSequence: fixture setup"),
                Required("flash.mcu.simple", "FlashMcuSimple", TestItemKind.Flash, "MainSequence: MCU简易"),
                Required("flash.tcon", "FlashTcon", TestItemKind.Flash, "MainSequence: TCON"),
                Required("flash.tddi", "FlashTddi", TestItemKind.Flash, "MainSequence: TDDI"),
                Required("flash.mcu.shipping", "FlashMcuShipping", TestItemKind.Flash, "MainSequence: MCU出货"),
                Required("safety.wait", "SafetyWait", TestItemKind.SafetyCheck, "MainSequence: X2气缸到位"),
                Required("fct.measurement", "FctMeasurement", TestItemKind.Measurement, "MainSequence: FCT"),
                Required("result.output", "ResultOutput", TestItemKind.ResultOutput, "MainSequence: OK/NG output"),
                Required("cleanup", "Cleanup", TestItemKind.Cleanup, "MainSequence: Cleanup")
            };
            var workflow = new FunctionalTestWorkflow(items, PassAll);

            var result = await workflow.RunAsync(new WorkflowRunContext { SerialNumber = "SN001" });

            Assert.True(result.Passed);
            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(items.Select(x => x.Name), result.Results.Select(x => x.ItemName));
            Assert.All(result.Results, x => Assert.False(string.IsNullOrWhiteSpace(x.SourceReference)));
        }

        [Fact]
        public async Task RequiredItemFailureMarksWorkflowFailedAndRunsCleanup()
        {
            var calls = new List<string>();
            var items = new[]
            {
                Required("fixture.prepare", "FixturePrepare", TestItemKind.FixturePrepare),
                Required("flash.mcu.simple", "FlashMcuSimple", TestItemKind.Flash),
                Required("fct.measurement", "FctMeasurement", TestItemKind.Measurement),
                Required("cleanup", "Cleanup", TestItemKind.Cleanup)
            };
            var workflow = new FunctionalTestWorkflow(items, (item, context, token) =>
            {
                calls.Add(item.Name);
                return Task.FromResult(item.Name == "FlashMcuSimple"
                    ? TestItemResult.Failed(item, "flash failed")
                    : TestItemResult.Passed(item));
            });

            var result = await workflow.RunAsync(new WorkflowRunContext { SerialNumber = "SN002" });

            Assert.False(result.Passed);
            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(new[] { "FixturePrepare", "FlashMcuSimple", "Cleanup" }, calls);
            Assert.DoesNotContain(result.Results, x => x.ItemName == "FctMeasurement");
        }

        [Fact]
        public async Task RequiredItemFailureRunsResultOutputBeforeCleanup()
        {
            var calls = new List<string>();
            var resultOutputSawFailure = false;
            var items = new[]
            {
                Required("fixture.prepare", "FixturePrepare", TestItemKind.FixturePrepare),
                Required("flash.mcu.simple", "FlashMcuSimple", TestItemKind.Flash),
                Required("fct.measurement", "FctMeasurement", TestItemKind.Measurement),
                Required("result.output", "ResultOutput", TestItemKind.ResultOutput),
                Required("cleanup", "Cleanup", TestItemKind.Cleanup)
            };
            var workflow = new FunctionalTestWorkflow(items, (item, context, token) =>
            {
                calls.Add(item.Name);
                if (item.Kind == TestItemKind.ResultOutput)
                {
                    resultOutputSawFailure = context.Values.ContainsKey("RunHasBlockingFailure")
                        && Equals(context.Values["RunHasBlockingFailure"], true);
                }

                return Task.FromResult(item.Name == "FlashMcuSimple"
                    ? TestItemResult.Failed(item, "flash failed")
                    : TestItemResult.Passed(item));
            });

            var result = await workflow.RunAsync(new WorkflowRunContext { SerialNumber = "SN002-NG" });

            Assert.False(result.Passed);
            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(new[] { "FixturePrepare", "FlashMcuSimple", "ResultOutput", "Cleanup" }, calls);
            Assert.True(resultOutputSawFailure);
            Assert.DoesNotContain(result.Results, x => x.ItemName == "FctMeasurement");
        }


        [Fact]
        public async Task OptionalItemFailureIsRecordedAndDoesNotStopRequiredItems()
        {
            var calls = new List<string>();
            var items = new[]
            {
                Required("fixture.prepare", "FixturePrepare", TestItemKind.FixturePrepare),
                Optional("measurement.optional", "OptionalMeasurement", TestItemKind.Measurement),
                Required("result.output", "ResultOutput", TestItemKind.ResultOutput),
                Required("cleanup", "Cleanup", TestItemKind.Cleanup)
            };
            var workflow = new FunctionalTestWorkflow(items, (item, context, token) =>
            {
                calls.Add(item.Name);
                return Task.FromResult(item.Name == "OptionalMeasurement"
                    ? TestItemResult.Failed(item, "optional check failed")
                    : TestItemResult.Passed(item));
            });

            var result = await workflow.RunAsync(new WorkflowRunContext { SerialNumber = "SN003" });

            Assert.True(result.Passed);
            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(new[] { "FixturePrepare", "OptionalMeasurement", "ResultOutput", "Cleanup" }, calls);
            Assert.Contains(result.Results, x => x.ItemName == "OptionalMeasurement" && x.Status == StepStatus.Failed);
        }

        [Fact]
        public async Task RequiredItemFailureWithStopOnFailureFalseContinuesNormalItemsAndMarksWorkflowFailed()
        {
            var calls = new List<string>();
            var items = new[]
            {
                Required("fixture.prepare", "FixturePrepare", TestItemKind.FixturePrepare),
                Required("limit.first", "FirstLimit", TestItemKind.LimitCheck, stopOnFailure: false),
                Required("limit.second", "SecondLimit", TestItemKind.LimitCheck),
                Required("cleanup", "Cleanup", TestItemKind.Cleanup)
            };
            var workflow = new FunctionalTestWorkflow(items, (item, context, token) =>
            {
                calls.Add(item.Name);
                return Task.FromResult(item.Name == "FirstLimit"
                    ? TestItemResult.Failed(item, "limit failed")
                    : TestItemResult.Passed(item));
            });

            var result = await workflow.RunAsync(new WorkflowRunContext { SerialNumber = "SN004" });

            Assert.False(result.Passed);
            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(new[] { "FixturePrepare", "FirstLimit", "SecondLimit", "Cleanup" }, calls);
        }

        [Fact]
        public async Task WorkflowRaisesItemStartedAndCompletedCallbacksAroundEachExecutedItem()
        {
            var events = new List<string>();
            var items = new[]
            {
                Required("fixture.prepare", "FixturePrepare", TestItemKind.FixturePrepare),
                Required("cleanup", "Cleanup", TestItemKind.Cleanup)
            };
            var workflow = new FunctionalTestWorkflow(items, (item, context, token) =>
            {
                events.Add("execute:" + item.Name);
                return Task.FromResult(TestItemResult.Passed(item));
            })
            {
                ItemStarted = item => events.Add("start:" + item.Name),
                ItemCompleted = result => events.Add("complete:" + result.ItemName)
            };

            await workflow.RunAsync(new WorkflowRunContext { SerialNumber = "SN-CALLBACK" });

            Assert.Equal(
                new[]
                {
                    "start:FixturePrepare",
                    "execute:FixturePrepare",
                    "complete:FixturePrepare",
                    "start:Cleanup",
                    "execute:Cleanup",
                    "complete:Cleanup"
                },
                events);
        }

        private static Task<TestItemResult> PassAll(TestItem item, WorkflowRunContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(TestItemResult.Passed(item));
        }

        private static TestItem Required(string id, string name, TestItemKind kind, string sourceReference = "", bool stopOnFailure = true)
        {
            return new TestItem(id, name, kind)
            {
                SourceReference = sourceReference,
                IsRequired = true,
                StopOnFailure = stopOnFailure,
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        private static TestItem Optional(string id, string name, TestItemKind kind)
        {
            return new TestItem(id, name, kind)
            {
                IsRequired = false,
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}
