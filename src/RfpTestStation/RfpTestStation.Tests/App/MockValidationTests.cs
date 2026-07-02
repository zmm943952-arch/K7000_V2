using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RfpTestStation.App.ViewModels;
using Xunit;

namespace RfpTestStation.Tests.App
{
    public sealed class MockValidationTests
    {
        [Fact]
        public async Task MockValidationRunsFullPassingFlowAndWritesProductionCsv()
        {
            var serialNumber = NewSerialNumber("PASS");
            var reportDirectory = Path.Combine(TestPaths.RepoRoot(), "Reports");

            try
            {
                var viewModel = await RunMockAsync(serialNumber, "None");

                Assert.Equal("Pass", viewModel.OverallStatus);
                Assert.Empty(viewModel.FailureResults);
                Assert.Contains(viewModel.Results, x => x.StepName == "Fixture Prepare" && x.Status == "Passed");
                Assert.Contains(viewModel.Results, x => x.StepName == "Result Output" && x.Status == "Passed");

                var csv = ReadSingleReport(reportDirectory, serialNumber, "Passed", "csv");
                Assert.Contains("[SN]," + serialNumber, csv);
                Assert.Contains("[Result],Passed", csv);
                Assert.Contains("Step,Status,Measurement,Expected Value,Units,Low Limit,High Limit,Comparison Type,Target,Sent,Reply,Reason,StartTime,EndTime", csv);
                Assert.Contains("Fixture Prepare,Passed", csv);
                Assert.Contains("Result Output,Passed", csv);
            }
            finally
            {
                DeleteReports(reportDirectory, serialNumber);
            }
        }

        [Fact]
        public async Task MockValidationRunsFailureScenariosAndAuditsCsv()
        {
            var cases = new[]
            {
                new ScenarioCase(
                    "TCON flash failed",
                    "TCON Flash",
                    "Error",
                    "Mock TCON flash failed",
                    "ExitCode=0",
                    "ProcessExit",
                    "Runtime/Flash/RedCase_Auto/Debug/FlashUpdate_Run.bat",
                    "ExitCode=4; StdErr=Firmware file not found"),
                new ScenarioCase(
                    "I2C debug no response",
                    "I2C Debug Mode Check",
                    "Error",
                    "Mock I2C no response",
                    "DebugMode=True",
                    "I2cResponse",
                    "EnterAndCheckDebugMode(0x12)",
                    "NoResponse"),
                new ScenarioCase(
                    "DAQ voltage low",
                    "AC Input 3 High Limit",
                    "Failed",
                    "Mock DAQ voltage outside limits",
                    "3.135..3.465",
                    "Range",
                    "ReadVoltage(3)",
                    "2.900V")
            };
            var reportDirectory = Path.Combine(TestPaths.RepoRoot(), "Reports");

            foreach (var scenario in cases)
            {
                var serialNumber = NewSerialNumber("FAIL");
                try
                {
                    var viewModel = await RunMockAsync(serialNumber, scenario.Name);

                    Assert.Equal("Fail", viewModel.OverallStatus);
                    Assert.Contains(scenario.ReasonFragment, viewModel.OverallStatusText);
                    var failure = Assert.Single(viewModel.FailureResults, x => x.StepName == scenario.StepName);
                    Assert.Equal(scenario.Status, failure.Status);
                    Assert.Contains(scenario.ReasonFragment, failure.Message);
                    Assert.Equal(scenario.ExpectedValue, failure.ExpectedValue);
                    Assert.Equal(scenario.CompareType, failure.CompareType);

                    var csv = ReadSingleReport(reportDirectory, serialNumber, "Failed", "csv");
                    Assert.Contains("[SN]," + serialNumber, csv);
                    Assert.Contains("[Result],Failed", csv);
                    Assert.Contains(scenario.StepName + "," + scenario.Status, csv);
                    Assert.Contains(scenario.ExpectedValue, csv);
                    Assert.Contains(scenario.CompareType, csv);
                    Assert.Contains(scenario.SentFragment, csv);
                    Assert.Contains(scenario.ReplyFragment, csv);
                    Assert.Contains(scenario.ReasonFragment, csv);
                }
                finally
                {
                    DeleteReports(reportDirectory, serialNumber);
                }
            }
        }

        private static async Task<MainViewModel> RunMockAsync(string serialNumber, string scenarioName)
        {
            var viewModel = new MainViewModel
            {
                SerialNumber = serialNumber,
                ExecutionMode = "Mock",
                SelectedMockScenarioName = scenarioName
            };

            await viewModel.StartRunAsync();
            return viewModel;
        }

        private static string NewSerialNumber(string prefix)
        {
            return "UT" + prefix + Guid.NewGuid().ToString("N");
        }

        private static string ReadSingleReport(string reportDirectory, string serialNumber, string result, string extension)
        {
            var path = Assert.Single(Directory.GetFiles(reportDirectory, serialNumber + "_*_" + result + "." + extension));
            return File.ReadAllText(path);
        }

        private static void DeleteReports(string reportDirectory, string serialNumber)
        {
            if (!Directory.Exists(reportDirectory))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(reportDirectory, serialNumber + "_*.*"))
            {
                File.Delete(path);
            }
        }

        private sealed class ScenarioCase
        {
            public ScenarioCase(
                string name,
                string stepName,
                string status,
                string reasonFragment,
                string expectedValue,
                string compareType,
                string sentFragment,
                string replyFragment)
            {
                Name = name;
                StepName = stepName;
                Status = status;
                ReasonFragment = reasonFragment;
                ExpectedValue = expectedValue;
                CompareType = compareType;
                SentFragment = sentFragment;
                ReplyFragment = replyFragment;
            }

            public string Name { get; }

            public string StepName { get; }

            public string Status { get; }

            public string ReasonFragment { get; }

            public string ExpectedValue { get; }

            public string CompareType { get; }

            public string SentFragment { get; }

            public string ReplyFragment { get; }
        }
    }
}
