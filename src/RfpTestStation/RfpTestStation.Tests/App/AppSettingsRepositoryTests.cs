using System;
using System.IO;
using RfpTestStation.Core;
using RfpTestStation.Core.Configuration;
using Xunit;

namespace RfpTestStation.Tests.App
{
    public sealed class AppSettingsRepositoryTests
    {
        [Fact]
        public void LoadOrDefaultReturnsStationDefaultsWhenSettingsFileDoesNotExist()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var stationPaths = new StationPaths(repoRoot);
                var repository = new AppSettingsRepository(stationPaths.AppSettingsPath, repoRoot);

                var settings = repository.LoadOrDefault(stationPaths);

                Assert.Equal("Operator", settings.CurrentUser);
                Assert.Equal("K7000", settings.ProductName);
                Assert.Equal("中文", settings.SelectedLanguage);
                Assert.Equal("Mock", settings.ExecutionMode);
                Assert.Equal("Runtime/TestPlans/Rfp7000V2.testplan.json", settings.TestPlanPath);
                Assert.Equal("Runtime/Config/Config.json", settings.ConfigJsonPath);
                Assert.Equal("None", settings.MockScenarioName);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveAndLoadPreservesEditableStartupSettings()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var stationPaths = new StationPaths(repoRoot);
                var repository = new AppSettingsRepository(stationPaths.AppSettingsPath, repoRoot);
                repository.Save(new AppSettings
                {
                    CurrentUser = "Engineer",
                    ProductName = "K7000-ALT",
                    SelectedLanguage = "English",
                    ExecutionMode = "Hardware",
                    TestPlanPath = "Runtime/TestPlans/Alt.testplan.json",
                    ConfigJsonPath = "Runtime/Config/AltConfig.json",
                    MockScenarioName = "DAQ voltage low"
                });

                var loaded = repository.LoadOrDefault(stationPaths);

                Assert.Equal("Engineer", loaded.CurrentUser);
                Assert.Equal("K7000-ALT", loaded.ProductName);
                Assert.Equal("English", loaded.SelectedLanguage);
                Assert.Equal("Hardware", loaded.ExecutionMode);
                Assert.Equal("Runtime/TestPlans/Alt.testplan.json", loaded.TestPlanPath);
                Assert.Equal("Runtime/Config/AltConfig.json", loaded.ConfigJsonPath);
                Assert.Equal("DAQ voltage low", loaded.MockScenarioName);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void LoadOrDefaultReplacesLegacyDefaultTestPlanPath()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var stationPaths = new StationPaths(repoRoot);
                var repository = new AppSettingsRepository(stationPaths.AppSettingsPath, repoRoot);
                repository.Save(new AppSettings
                {
                    CurrentUser = "Operator",
                    ProductName = "K7000",
                    SelectedLanguage = "中文",
                    ExecutionMode = "Mock",
                    TestPlanPath = "Project/TestPlans/Rfp7000V2.testplan.json",
                    ConfigJsonPath = "Project/Config.json"
                });

                var loaded = repository.LoadOrDefault(stationPaths);

                Assert.Equal("Runtime/TestPlans/Rfp7000V2.testplan.json", loaded.TestPlanPath);
                Assert.Equal("Runtime/Config/Config.json", loaded.ConfigJsonPath);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        private static string CreateTempRepoRoot()
        {
            var repoRoot = Path.Combine(Path.GetTempPath(), "RfpTestStation_AppSettings_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Runtime", "Config"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Runtime", "TestPlans"));
            return repoRoot;
        }
    }
}
