using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RfpTestStation.Adapters.Flashing;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;
using Xunit;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Tests.Adapters
{
    public sealed class FlashAdapterTests
    {
        [Theory]
        [InlineData("Test RFP_Flash_once.bat.vi", "Runtime\\Flash\\RFP_Auto\\Scripts\\Flash_once.bat")]
        [InlineData("Test RedCase_FlashUpdate_Run.bat.vi", "Runtime\\Flash\\RedCase_Auto\\Debug\\FlashUpdate_Run.bat")]
        [InlineData("Test TDDI_Flash_once.bat.vi", "Runtime\\Flash\\TDDI_Auto\\Test\\Test\\bin\\Debug\\flash_run.bat")]
        public void MapResolvesDownloadViToExistingScript(string viName, string expectedRelativePath)
        {
            var map = new FlashScriptMap(TestPaths.RepoRoot());

            var script = map.Resolve(StepForVi(viName));

            var expectedPath = Path.GetFullPath(Path.Combine(TestPaths.RepoRoot(), expectedRelativePath));
            Assert.Equal(expectedPath, script.ScriptPath);
            Assert.Equal(Path.GetDirectoryName(expectedPath), script.WorkingDirectory);
            Assert.True(File.Exists(script.ScriptPath));
        }

        [Fact]
        public async Task ExecuteRunsMappedScriptWithWorkingDirectory()
        {
            var runner = new FakeProcessRunner(new ExternalProcessResult
            {
                ExitCode = 0,
                StandardOutput = "programming complete PASS\r\n[INFO] Log saved to: C:\\Logs\\SN001_pass.log"
            });
            var adapter = new FlashAdapter(new FlashScriptMap(TestPaths.RepoRoot()), runner, TimeSpan.FromSeconds(30));

            var result = await adapter.ExecuteAsync(
                StepForVi("Test RFP_Flash_once.bat.vi"),
                new StationExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.EndsWith("Runtime\\Flash\\RFP_Auto\\Scripts\\Flash_once.bat", runner.LastStartInfo!.FileName);
            Assert.Equal(Path.GetDirectoryName(runner.LastStartInfo.FileName), runner.LastStartInfo.WorkingDirectory);
            Assert.Contains("ExitCode=0", result.Message);
            Assert.Contains("Flash_once.bat", result.Sent);
            Assert.Contains("programming complete PASS", result.Reply);
            Assert.Equal("C:\\Logs\\SN001_pass.log", result.ExternalLogPath);
        }

        [Fact]
        public async Task ExecuteMapsNonZeroExitCodeToFailure()
        {
            var runner = new FakeProcessRunner(new ExternalProcessResult
            {
                ExitCode = 2,
                StandardError = "flash failed"
            });
            var adapter = new FlashAdapter(new FlashScriptMap(TestPaths.RepoRoot()), runner, TimeSpan.FromSeconds(30));

            var result = await adapter.ExecuteAsync(
                StepForVi("Test RedCase_FlashUpdate_Run.bat.vi"),
                new StationExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Contains("ExitCode=2", result.Message);
            Assert.Contains("flash failed", result.Message);
            Assert.Contains("FlashUpdate_Run.bat", result.Sent);
            Assert.Contains("StdErr=flash failed", result.Reply);
        }

        [Fact]
        public async Task ExecuteTestPlanItemPassesArgumentsToExternalProcess()
        {
            var runner = new FakeProcessRunner(new ExternalProcessResult
            {
                ExitCode = 0,
                StandardOutput = "PASS"
            });
            var adapter = new FlashAdapter(new FlashScriptMap(TestPaths.RepoRoot()), runner, TimeSpan.FromSeconds(30));

            var result = await adapter.ExecuteAsync(
                FlashItem(@"{
  ""flashKind"": ""RfpMcuSimple"",
  ""script"": ""Runtime/Flash/RFP_Auto/Scripts/Flash_once.bat"",
  ""arguments"": ""--station K7000 --mode simple""
}"),
                new StationExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("--station K7000 --mode simple", runner.LastStartInfo!.Arguments);
            Assert.Contains("Arguments=--station K7000 --mode simple", result.Message);
        }

        [Fact]
        public async Task ExecuteTestPlanItemReturnsErrorWhenScriptDoesNotExist()
        {
            var runner = new FakeProcessRunner(new ExternalProcessResult
            {
                ExitCode = 0,
                StandardOutput = "PASS"
            });
            var adapter = new FlashAdapter(new FlashScriptMap(TestPaths.RepoRoot()), runner, TimeSpan.FromSeconds(30));

            var result = await adapter.ExecuteAsync(
                FlashItem(@"{
  ""flashKind"": ""Missing"",
  ""script"": ""Project/DoesNotExist/missing_flash.bat""
}"),
                new StationExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Error, result.Status);
            Assert.Contains("Flash script does not exist", result.Message);
            Assert.Null(runner.LastStartInfo);
        }

        private static StepDefinition StepForVi(string viName)
        {
            return new StepDefinition
            {
                Name = viName,
                DescriptionRaw = "Pass/Fail Test,  Support\\Download\\" + viName
            };
        }

        private static TestItem FlashItem(string parametersJson)
        {
            return new TestItem("flash.test", "Flash Test", TestItemKind.Flash)
            {
                Timeout = TimeSpan.FromSeconds(600),
                Parameters = JObject.Parse(parametersJson)
            };
        }

        private sealed class FakeProcessRunner : IExternalProcessRunner
        {
            private readonly ExternalProcessResult _result;

            public FakeProcessRunner(ExternalProcessResult result)
            {
                _result = result;
            }

            public ExternalProcessStartInfo? LastStartInfo { get; private set; }

            public Task<ExternalProcessResult> RunAsync(ExternalProcessStartInfo startInfo, CancellationToken cancellationToken)
            {
                LastStartInfo = startInfo;
                return Task.FromResult(_result);
            }
        }
    }
}
