using System;
using System.IO;
using System.Linq;
using RfpTestStation.Core.MockScenarios;
using RfpTestStation.Core.TestPlans;
using RfpTestStation.Core.Workflow;
using Xunit;

namespace RfpTestStation.Tests.TestPlans
{
    public sealed class MockScenarioRepositoryTests
    {
        [Fact]
        public void LoadAvailableReadsScenarioDisplayNamesFromRuntimeDirectory()
        {
            using (var temp = new TempDirectory())
            {
                File.WriteAllText(Path.Combine(temp.Path, "daq-low.mockscenario.json"), @"
                {
                  ""name"": ""DAQ voltage low"",
                  ""description"": ""Force AC input 3 below low limit"",
                  ""items"": {
                    ""fct.ac-input.3.hi"": {
                      ""status"": ""Failed"",
                      ""reason"": ""Mock DAQ voltage outside limits"",
                      ""value"": 2.9
                    }
                  }
                }");
                var repository = new MockScenarioRepository(temp.Path);

                var scenarios = repository.LoadAvailable().ToList();

                var scenario = Assert.Single(scenarios);
                Assert.Equal("DAQ voltage low", scenario.Name);
                Assert.EndsWith("daq-low.mockscenario.json", scenario.Path, StringComparison.OrdinalIgnoreCase);
            }
        }

        [Fact]
        public void ApplyScenarioInjectsMockResultByTestItemIdWithoutRemovingExistingParameters()
        {
            using (var temp = new TempDirectory())
            {
                var path = Path.Combine(temp.Path, "daq-low.mockscenario.json");
                File.WriteAllText(path, @"
                {
                  ""name"": ""DAQ voltage low"",
                  ""items"": {
                    ""fct.ac-input.3.hi"": {
                      ""status"": ""Failed"",
                      ""reason"": ""Mock DAQ voltage outside limits"",
                      ""value"": 2.9,
                      ""unit"": ""V""
                    }
                  }
                }");
                var plan = new TestPlanDefinition();
                plan.Items.Add(new TestPlanItemDefinition
                {
                    Id = "fct.ac-input.3.hi",
                    Name = "AC Input 3 High Limit",
                    Kind = TestItemKind.LimitCheck,
                    TimeoutSeconds = 30,
                    Parameters =
                    {
                        ["adapter"] = "DaqVoltage",
                        ["channel"] = 3
                    }
                });
                var item = TestPlanWorkflowFactory.CreateItems(plan).Single();
                var repository = new MockScenarioRepository(temp.Path);

                var appliedCount = repository.Apply(path, new[] { item });

                Assert.Equal(1, appliedCount);
                Assert.Equal("DaqVoltage", (string)item.Parameters["adapter"]!);
                Assert.Equal("Failed", (string)item.Parameters["mock"]!["status"]!);
                Assert.Equal("Mock DAQ voltage outside limits", (string)item.Parameters["mock"]!["reason"]!);
                Assert.Equal(2.9, (double)item.Parameters["mock"]!["value"]!);
                Assert.Equal("V", (string)item.Parameters["mock"]!["unit"]!);
            }
        }

        private sealed class TempDirectory : IDisposable
        {
            public TempDirectory()
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RfpTestStation_MockScenarios_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Path);
            }

            public string Path { get; }

            public void Dispose()
            {
                if (Directory.Exists(Path))
                {
                    Directory.Delete(Path, true);
                }
            }
        }
    }
}
