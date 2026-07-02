using System;
using System.IO;
using System.Linq;
using RfpTestStation.Core.TestPlans;
using RfpTestStation.Core.Workflow;
using Xunit;

namespace RfpTestStation.Tests.TestPlans
{
    public sealed class TestPlanRepositoryTests
    {
        [Fact]
        public void LoadReadsStableProjectTestPlan()
        {
            var plan = TestPlanRepository.Load(ProjectTestPlanPath());

            Assert.Equal("RFP 7000 V2", plan.Name);
            Assert.False(string.IsNullOrWhiteSpace(plan.Version));
            Assert.Equal("RFP7000V2", plan.Product);
            Assert.NotEmpty(plan.Items);
            Assert.Contains(plan.Items, x => x.Id == "flash.mcu.simple" && x.Kind == TestItemKind.Flash);
            AssertFunctionalGroup(plan, "fct.hvac-position.group", 6);
            AssertFunctionalGroup(plan, "fct.hvac-sw.group", 5);
            AssertFunctionalGroup(plan, "fct.hvac-ind.group", 6);
            AssertFunctionalGroup(plan, "fct.button.group", 7);
            AssertFunctionalGroup(plan, "fct.swpack-pwm.group", 4);
            AssertFunctionalGroup(plan, "fct.hvac-bklt.group", 5);
            Assert.Contains(plan.Items, x => x.Id == "cleanup.fixture" && x.Kind == TestItemKind.Cleanup);

            var fixturePositionIndex = plan.Items.Select((x, i) => new { Item = x, Index = i }).Single(x => x.Item.Id == "safety.fixture-position").Index;
            var firstFlashIndex = plan.Items.Select((x, i) => new { Item = x, Index = i }).First(x => x.Item.Kind == TestItemKind.Flash).Index;
            Assert.True(fixturePositionIndex < firstFlashIndex);
        }

        [Fact]
        public void LoadRejectsMissingRequiredItemId()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".testplan.json");
            File.WriteAllText(path, @"{
  ""name"": ""Invalid"",
  ""version"": ""1.0"",
  ""product"": ""UnitTest"",
  ""items"": [
    { ""name"": ""No Id"", ""kind"": ""Flash"" }
  ]
}");

            try
            {
                Assert.Throws<TestPlanValidationException>(() => TestPlanRepository.Load(path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SavePersistsEditedPlanAndPreservesParameters()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".testplan.json");
            File.WriteAllText(path, @"{
  ""name"": ""Editable"",
  ""version"": ""1.0"",
  ""product"": ""UnitTest"",
  ""items"": [
    {
      ""id"": ""limit.voltage"",
      ""name"": ""Voltage Limit"",
      ""kind"": ""LimitCheck"",
      ""enabled"": true,
      ""required"": true,
      ""stopOnFailure"": true,
      ""timeoutSeconds"": 30,
      ""parameters"": {
        ""adapter"": ""DaqVoltage"",
        ""low"": 1.0,
        ""high"": 3.3,
        ""unit"": ""V"",
        ""unknown"": ""keep""
      }
    }
  ]
}");

            try
            {
                var plan = TestPlanRepository.Load(path);
                var item = plan.Items.Single();
                plan.Name = "Edited";
                item.Name = "Edited Voltage Limit";
                item.IsEnabled = false;
                item.StopOnFailure = false;
                item.TimeoutSeconds = 45;
                item.Parameters["low"] = 1.2;
                item.Parameters["high"] = 3.5;

                TestPlanRepository.Save(plan, path);

                var reloaded = TestPlanRepository.Load(path);
                var reloadedItem = reloaded.Items.Single();
                Assert.Equal("Edited", reloaded.Name);
                Assert.Equal("Edited Voltage Limit", reloadedItem.Name);
                Assert.False(reloadedItem.IsEnabled);
                Assert.False(reloadedItem.StopOnFailure);
                Assert.Equal(45, reloadedItem.TimeoutSeconds);
                Assert.Equal(1.2, (double)reloadedItem.Parameters["low"]!);
                Assert.Equal(3.5, (double)reloadedItem.Parameters["high"]!);
                Assert.Equal("keep", (string)reloadedItem.Parameters["unknown"]!);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void WorkflowFactoryPreservesOrderAndExecutionSettings()
        {
            var plan = TestPlanRepository.Load(ProjectTestPlanPath());

            var items = TestPlanWorkflowFactory.CreateItems(plan).ToList();

            Assert.Equal(plan.Items.Select(x => x.Id), items.Select(x => x.Id));
            Assert.Contains(items, x => x.Id == "flash.mcu.simple"
                && x.IsRequired
                && x.Timeout == TimeSpan.FromSeconds(600)
                && x.SourceReference.Contains("Rfp7000V2.testplan.json"));
            Assert.Equal(TestItemKind.Cleanup, items.Last().Kind);
        }

        [Fact]
        public void WorkflowFactorySkipsDisabledItemsAndPreservesStopPolicy()
        {
            var plan = new TestPlanDefinition
            {
                Name = "Factory Test",
                Version = "1.0",
                Product = "UnitTest"
            };
            plan.Items.Add(new TestPlanItemDefinition
            {
                Id = "skip",
                Name = "Skip",
                Kind = TestItemKind.Measurement,
                IsEnabled = false,
                IsRequired = true,
                StopOnFailure = true,
                TimeoutSeconds = 10
            });
            plan.Items.Add(new TestPlanItemDefinition
            {
                Id = "run",
                Name = "Run",
                Kind = TestItemKind.Measurement,
                IsEnabled = true,
                IsRequired = true,
                StopOnFailure = false,
                TimeoutSeconds = 20
            });

            var items = TestPlanWorkflowFactory.CreateItems(plan).ToList();

            var item = Assert.Single(items);
            Assert.Equal("run", item.Id);
            Assert.False(item.StopOnFailure);
        }

        [Fact]
        public void SaveRejectsDisabledItemWithoutId()
        {
            var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".testplan.json");
            var plan = new TestPlanDefinition
            {
                Name = "Invalid",
                Version = "1.0",
                Product = "UnitTest"
            };
            plan.Items.Add(new TestPlanItemDefinition
            {
                Name = "Missing Id",
                Kind = TestItemKind.Flash,
                IsEnabled = false,
                TimeoutSeconds = 30
            });

            try
            {
                Assert.Throws<TestPlanValidationException>(() => TestPlanRepository.Save(plan, path));
            }
            finally
            {
                File.Delete(path);
            }
        }

        private static string ProjectTestPlanPath()
        {
            return Path.Combine(TestPaths.RepoRoot(), "src", "RfpTestStation", "Rfp7000V2.testplan.json");
        }

        private static void AssertFunctionalGroup(TestPlanDefinition plan, string id, int childCount)
        {
            var item = plan.Items.Single(x => x.Id == id);
            Assert.Equal(TestItemKind.FunctionalCheck, item.Kind);
            Assert.Equal("I2cFunctionalGroup", (string)item.Parameters["template"]!);
            Assert.Equal(childCount, item.Parameters["items"]!.Count());
        }
    }
}
