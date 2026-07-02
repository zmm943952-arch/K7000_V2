using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RfpTestStation.App.ViewModels;
using RfpTestStation.Core;
using RfpTestStation.Core.Configuration;
using RfpTestStation.Core.Model;
using Xunit;

namespace RfpTestStation.Tests.App
{
    public sealed class MainViewModelTests
    {
        [Fact]
        public void StartsInMockIdleState()
        {
            var viewModel = new MainViewModel();

            Assert.Equal("Mock", viewModel.ExecutionMode);
            Assert.Equal("Idle", viewModel.OverallStatus);
            Assert.Equal("中文", viewModel.SelectedLanguage);
            Assert.Equal("自动化测试系统", viewModel.TitleText);
            Assert.Equal("待机", viewModel.OverallStatusText);
            Assert.Equal("Operator", viewModel.CurrentUser);
            Assert.Equal("K7000", viewModel.ProductName);
            Assert.Equal("RFP 7000 V2", viewModel.TestPlanName);
            Assert.Equal("Config.json", viewModel.ConfigName);
            Assert.Equal("Runtime/Config/Config.json", viewModel.ConfigJsonPath);
            Assert.Equal("当前模式: Mock", viewModel.ExecutionModeStatusText);
            Assert.False(viewModel.IsHardwareMode);
            Assert.Equal(0, viewModel.GoodCount);
            Assert.Equal(0, viewModel.FailCount);
            Assert.Equal(0, viewModel.ErrorCount);
            Assert.Equal(0, viewModel.TotalCount);
            Assert.Equal("0.00%", viewModel.YieldRateText);
            Assert.Equal(0.0, viewModel.ProgressPercent);
            Assert.False(viewModel.IsRunning);
            Assert.DoesNotContain(viewModel.PlaceholderItems, x => x.Contains("PowerControl.vi"));
            Assert.Contains(viewModel.PlaceholderItems, x => x.Contains("ReadSN.vi"));
        }

        [Fact]
        public void HardwareModeShowsStrongRunPageWarning()
        {
            var viewModel = new MainViewModel
            {
                ExecutionMode = "Hardware"
            };

            Assert.Equal("当前模式: Hardware", viewModel.ExecutionModeStatusText);
            Assert.True(viewModel.IsHardwareMode);
            Assert.Contains("真实控制", viewModel.HardwareModeWarningText);
        }

        [Fact]
        public void StartsWithAutomaticHardwareSelfCheckResults()
        {
            var viewModel = new MainViewModel
            {
                ExecutionMode = "Hardware"
            };

            Assert.NotEmpty(viewModel.HardwareSelfCheckItems);
            Assert.Contains(viewModel.HardwareSelfCheckItems, x => x.Name.Contains("运行模式") && x.Status == "警告");
            Assert.Contains(viewModel.HardwareSelfCheckItems, x => x.Name.Contains("TDDI") && x.Status == "通过");
            Assert.Contains(viewModel.Logs, x => x.Contains("Hardware self-check completed"));
        }

        [Fact]
        public void RunPageDoesNotExposeManualHardwareSelfCheckControls()
        {
            var xaml = File.ReadAllText(Path.Combine(
                TestPaths.RepoRoot(),
                "src",
                "RfpTestStation",
                "RfpTestStation.App",
                "MainWindow.xaml"));

            Assert.DoesNotContain("RunHardwareSelfCheckCommand", xaml);
            Assert.DoesNotContain("RunHardwareSelfCheckButtonText", xaml);
            Assert.DoesNotContain("HardwareSelfCheckTitleText", xaml);
        }

        [Theory]
        [InlineData(StepStatus.Passed, "#166534", "#ECFDF3")]
        [InlineData(StepStatus.Failed, "#991B1B", "#FEF2F2")]
        [InlineData(StepStatus.Error, "#9A3412", "#FFF7ED")]
        [InlineData(StepStatus.Skipped, "#475467", "#F2F4F7")]
        public void StepResultViewModelMapsStatusToHighContrastColors(StepStatus status, string foreground, string background)
        {
            var result = StepResultViewModel.FromResult(new StepResult
            {
                StepName = "Step",
                Status = status
            });

            Assert.Equal(foreground, result.StatusForeground);
            Assert.Equal(background, result.StatusBackground);
        }

        [Fact]
        public void StepResultViewModelHighlightsRunningStepRow()
        {
            var result = StepResultViewModel.Pending("step-1", "Burn TDDI");

            result.MarkRunning();

            Assert.Equal("Running", result.Status);
            Assert.Equal("#075985", result.StatusForeground);
            Assert.Equal("#EFF8FF", result.StatusBackground);
            Assert.Equal("#F0F9FF", result.RowBackground);
            Assert.True(result.IsCurrent);
            Assert.False(result.IsCompleted);
        }

        [Fact]
        public void StepResultViewModelClearsRunningHighlightWhenCompleted()
        {
            var result = StepResultViewModel.Pending("step-1", "Burn TDDI");
            result.MarkRunning();

            result.MarkCompleted(new StepResult
            {
                StepName = "Burn TDDI",
                Status = StepStatus.Passed,
                Message = "OK"
            });

            Assert.Equal("Passed", result.Status);
            Assert.Equal("#FFFFFF", result.RowBackground);
            Assert.False(result.IsCurrent);
            Assert.True(result.IsCompleted);
        }

        [Theory]
        [InlineData(StepStatus.Passed, false)]
        [InlineData(StepStatus.Skipped, false)]
        [InlineData(StepStatus.Failed, true)]
        [InlineData(StepStatus.Error, true)]
        [InlineData(StepStatus.Stopped, true)]
        [InlineData(StepStatus.Terminated, true)]
        public void StepResultViewModelMarksOnlyFailureStatusesForFailureDetail(StepStatus status, bool expectedFailure)
        {
            var result = StepResultViewModel.FromResult(new StepResult
            {
                StepName = "Step",
                Status = status
            });

            Assert.Equal(expectedFailure, result.IsFailure);
        }

        [Fact]
        public void RunPageBindsResultRowsToRowBackground()
        {
            var xaml = File.ReadAllText(Path.Combine(
                TestPaths.RepoRoot(),
                "src",
                "RfpTestStation",
                "RfpTestStation.App",
                "MainWindow.xaml"));

            Assert.Contains("RowBackground", xaml);
            Assert.DoesNotContain("CornerRadius=\"3\" Padding=\"8,2\" HorizontalAlignment=\"Left\"", xaml);
        }

        [Fact]
        public void RunPageDetailGridBindsToFailureResultsAndReason()
        {
            var xaml = File.ReadAllText(Path.Combine(
                TestPaths.RepoRoot(),
                "src",
                "RfpTestStation",
                "RfpTestStation.App",
                "MainWindow.xaml"));

            Assert.Contains("ItemsSource=\"{Binding FailureResults}\"", xaml);
            Assert.Contains("DetailReasonColumn", xaml);
            Assert.Contains("Binding=\"{Binding Message, Mode=OneWay}\"", xaml);
        }

        [Fact]
        public void TestPlanEditorUsesOneWayBindingsForReadOnlyDisplayProperties()
        {
            var xaml = File.ReadAllText(Path.Combine(
                TestPaths.RepoRoot(),
                "src",
                "RfpTestStation",
                "RfpTestStation.App",
                "MainWindow.xaml"));

            Assert.Contains("Binding=\"{Binding SummaryText, Mode=OneWay}\"", xaml);
            Assert.Contains("Text=\"{Binding SelectedTestPlanDetailText, Mode=OneWay}\"", xaml);
        }

        [Fact]
        public void LanguageSwitchUpdatesVisibleLabels()
        {
            var viewModel = new MainViewModel();

            viewModel.SelectedLanguage = "English";

            Assert.Equal("Automated Test System", viewModel.TitleText);
            Assert.Equal("Current User", viewModel.CurrentUserLabel);
            Assert.Equal("Product Name", viewModel.ProductNameLabel);
            Assert.Equal("Current Language", viewModel.LanguageLabel);
            Assert.Equal("Test Plan", viewModel.TestPlanLabel);
            Assert.Equal("Test Config", viewModel.ConfigLabel);
            Assert.Equal("Execution Mode", viewModel.ExecutionModeLabel);
            Assert.Equal("Operation", viewModel.OperationsLabel);
            Assert.Equal("Start", viewModel.StartButtonText);
            Assert.Equal("Reset Statistics", viewModel.ResetStatisticsText);
            Assert.Equal("Idle", viewModel.OverallStatusText);
            Assert.Equal("Yield", viewModel.YieldLabel);
            Assert.Equal("High Reliability . High Precision . Fully Digital", viewModel.FooterSloganText);
        }

        [Fact]
        public void StartsWithSavedStartupSettingsWhenSettingsFileExists()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var stationPaths = new StationPaths(repoRoot);
                new AppSettingsRepository(stationPaths.AppSettingsPath, repoRoot).Save(new AppSettings
                {
                    CurrentUser = "Engineer",
                    ProductName = "K7000-ALT",
                    SelectedLanguage = "English",
                    ExecutionMode = "Hardware",
                    TestPlanPath = "Runtime/TestPlans/Alt.testplan.json",
                    ConfigJsonPath = "Runtime/Config/AltConfig.json"
                });

                var viewModel = new MainViewModel(repoRoot);

                Assert.Equal("Engineer", viewModel.CurrentUser);
                Assert.Equal("K7000-ALT", viewModel.ProductName);
                Assert.Equal("English", viewModel.SelectedLanguage);
                Assert.Equal("Hardware", viewModel.ExecutionMode);
                Assert.Equal("Runtime/TestPlans/Alt.testplan.json", viewModel.TestPlanPath);
                Assert.Equal("Runtime/Config/AltConfig.json", viewModel.ConfigJsonPath);
                Assert.Equal("Alt Test Plan", viewModel.TestPlanName);
                Assert.Equal("AltConfig.json", viewModel.ConfigName);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveSettingsCommandPersistsEditableStartupSettings()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var stationPaths = new StationPaths(repoRoot);
                var viewModel = new MainViewModel(repoRoot)
                {
                    CurrentUser = "Operator2",
                    ProductName = "K7000-PROD",
                    SelectedLanguage = "English",
                    ExecutionMode = "Hardware",
                    TestPlanPath = "Runtime/TestPlans/Alt.testplan.json",
                    ConfigJsonPath = "Runtime/Config/AltConfig.json"
                };

                viewModel.SaveSettingsCommand.Execute(null);

                var loaded = new AppSettingsRepository(stationPaths.AppSettingsPath, repoRoot).LoadOrDefault(stationPaths);
                Assert.Equal("Operator2", loaded.CurrentUser);
                Assert.Equal("K7000-PROD", loaded.ProductName);
                Assert.Equal("English", loaded.SelectedLanguage);
                Assert.Equal("Hardware", loaded.ExecutionMode);
                Assert.Equal("Runtime/TestPlans/Alt.testplan.json", loaded.TestPlanPath);
                Assert.Equal("Runtime/Config/AltConfig.json", loaded.ConfigJsonPath);
                Assert.Contains(viewModel.Logs, x => x.Contains("Saved startup settings"));
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void StartsWithEditableStationConfigLoadedFromCurrentConfig()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var viewModel = new MainViewModel(repoRoot);

                Assert.Equal(@"..\Flash\RFP_Auto\Scripts\Flash_once.bat", viewModel.RfpFlashScriptPath);
                Assert.Equal(@"..\Flash\RedCase_Auto\Debug\FlashUpdate_Run.bat", viewModel.TconFlashScriptPath);
                Assert.Equal(@"..\Flash\RedCase_Auto\Debug\Firmware_Local\HX6330B03_C_Sharp_GM_Mobis_2992x1299_16.3_Falcon_20251223_S1C7A404_Test_Ver4-0.bin", viewModel.TconBinFilePath);
                Assert.Equal(@"..\Flash\TDDI_Auto\Test\Test\bin\Debug\flash_run.bat", viewModel.TddiFlashScriptPath);
                Assert.Equal("COM14", viewModel.TddiSerialPort);
                Assert.Equal("COM4", viewModel.IoDaqCom);
                Assert.Equal("COM1", viewModel.ScannerCom);
                Assert.Equal("192.168.1.13", viewModel.OscilloscopeHost);
                Assert.Equal("StationA", viewModel.FctStation);
                Assert.True(viewModel.SafetyEnabled);
                Assert.Equal(50, viewModel.SafetyPollIntervalMs);
                Assert.Equal(4, viewModel.EmergencyStopChannel);
                Assert.True(viewModel.EmergencyStopTriggeredValue);
                Assert.Equal(5, viewModel.LightCurtainChannel);
                Assert.False(viewModel.LightCurtainTriggeredValue);
                Assert.Equal(2, viewModel.FixtureDownOutputChannel);
                Assert.Equal(1, viewModel.FixtureUpOutputChannel);
                Assert.Equal(1000, viewModel.FixtureUpDelayMs);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveStationConfigCommandPersistsEditableStationConfig()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var configPath = Path.Combine(repoRoot, "Runtime", "Config", "Config.json");
                var viewModel = new MainViewModel(repoRoot)
                {
                    RfpFlashScriptPath = @".\new_rfp.bat",
                    TconFlashScriptPath = @".\new_tcon.bat",
                    TconBinFilePath = @"D:\Firmware\new_tcon.bin",
                    TddiFlashScriptPath = @".\new_tddi.bat",
                    TddiSerialPort = "COM9",
                    IoDaqCom = "COM8",
                    ScannerCom = "COM7",
                    OscilloscopeHost = "192.168.10.20",
                    FctStation = "StationB",
                    SafetyEnabled = false,
                    SafetyPollIntervalMs = 80,
                    EmergencyStopChannel = 6,
                    EmergencyStopTriggeredValue = false,
                    LightCurtainChannel = 7,
                    LightCurtainTriggeredValue = true,
                    FixtureDownOutputChannel = 8,
                    FixtureUpOutputChannel = 9,
                    FixtureUpDelayMs = 1200
                };

                viewModel.SaveStationConfigCommand.Execute(null);

                var json = JObject.Parse(File.ReadAllText(configPath));
                Assert.Equal(@".\new_rfp.bat", (string)json.SelectToken("Burn1.Params.FilePath")!);
                Assert.Equal(@".\new_tcon.bat", (string)json.SelectToken("Burn2.Params.FilePath")!);
                Assert.Equal(@"D:\Firmware\new_tcon.bin", (string)json.SelectToken("Burn2.Params.BinFilePath")!);
                Assert.Equal(@".\new_tddi.bat", (string)json.SelectToken("Burn3.Params.FilePath")!);
                Assert.Equal("COM9", (string)json.SelectToken("Burn3.Params.Serial.PortName")!);
                Assert.Equal("COM8", (string)json.SelectToken("Instruments[0].Com")!);
                Assert.Equal("COM7", (string)json.SelectToken("Instruments[1].Com")!);
                Assert.Equal("192.168.10.20", (string)json.SelectToken("Instruments[2].Host")!);
                Assert.Equal("StationB", (string)json.SelectToken("FctTest.Global.Station")!);
                Assert.False((bool)json.SelectToken("Safety.Enabled")!);
                Assert.Equal(80, (int)json.SelectToken("Safety.PollIntervalMs")!);
                Assert.Equal(6, (int)json.SelectToken("Safety.Inputs.EmergencyStop.Channel")!);
                Assert.False((bool)json.SelectToken("Safety.Inputs.EmergencyStop.TriggeredValue")!);
                Assert.Equal(7, (int)json.SelectToken("Safety.Inputs.LightCurtain.Channel")!);
                Assert.True((bool)json.SelectToken("Safety.Inputs.LightCurtain.TriggeredValue")!);
                Assert.Equal(8, (int)json.SelectToken("Safety.ReleaseFixture.DownOutputChannel")!);
                Assert.Equal(9, (int)json.SelectToken("Safety.ReleaseFixture.UpOutputChannel")!);
                Assert.Equal(1200, (int)json.SelectToken("Safety.ReleaseFixture.UpDelayMs")!);
                Assert.Equal(42, (int)json.SelectToken("Unknown")!);
                Assert.Contains(viewModel.Logs, x => x.Contains("Saved station config"));
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SettingsPageDefaultsToCommonSectionAndCanSwitchSections()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var viewModel = new MainViewModel(repoRoot);

                Assert.Equal("Common", viewModel.SelectedSettingsSection);
                Assert.True(viewModel.IsCommonSettingsSection);

                viewModel.SelectedSettingsSection = "Safety";

                Assert.False(viewModel.IsCommonSettingsSection);
                Assert.True(viewModel.IsSafetySettingsSection);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveStationConfigCommandBlocksInvalidComPort()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var configPath = Path.Combine(repoRoot, "Runtime", "Config", "Config.json");
                var viewModel = new MainViewModel(repoRoot)
                {
                    IoDaqCom = "DAQ4"
                };

                viewModel.SaveStationConfigCommand.Execute(null);

                var json = JObject.Parse(File.ReadAllText(configPath));
                Assert.Equal("COM4", (string)json.SelectToken("Instruments[0].Com")!);
                Assert.True(viewModel.HasStationConfigValidationError);
                Assert.Contains("COM", viewModel.StationConfigStatusText);
                Assert.Contains(viewModel.Logs, x => x.Contains("Station config validation failed"));
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveStationConfigCommandBlocksInvalidSafetyPollInterval()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var configPath = Path.Combine(repoRoot, "Runtime", "Config", "Config.json");
                var viewModel = new MainViewModel(repoRoot)
                {
                    SelectedLanguage = "English",
                    SafetyPollIntervalMs = -1
                };

                viewModel.SaveStationConfigCommand.Execute(null);

                var json = JObject.Parse(File.ReadAllText(configPath));
                Assert.Equal(50, (int)json.SelectToken("Safety.PollIntervalMs")!);
                Assert.True(viewModel.HasStationConfigValidationError);
                Assert.Contains("poll", viewModel.StationConfigStatusText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveStationConfigCommandBlocksMissingRequiredPath()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var configPath = Path.Combine(repoRoot, "Runtime", "Config", "Config.json");
                var viewModel = new MainViewModel(repoRoot)
                {
                    SelectedLanguage = "English",
                    RfpFlashScriptPath = string.Empty
                };

                viewModel.SaveStationConfigCommand.Execute(null);

                var json = JObject.Parse(File.ReadAllText(configPath));
                Assert.Equal(@"..\Flash\RFP_Auto\Scripts\Flash_once.bat", (string)json.SelectToken("Burn1.Params.FilePath")!);
                Assert.True(viewModel.HasStationConfigValidationError);
                Assert.Contains("path", viewModel.StationConfigStatusText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SettingsRedesignLabelsAreLocalized()
        {
            var viewModel = new MainViewModel
            {
                SelectedLanguage = "English"
            };

            Assert.Equal("Common", viewModel.CommonSettingsSectionText);
            Assert.Equal("Startup", viewModel.StartupSettingsSectionText);
            Assert.Equal("Flashing", viewModel.FlashingSettingsSectionText);
            Assert.Equal("Instruments", viewModel.InstrumentSettingsSectionText);
            Assert.Equal("FCT", viewModel.FctSettingsSectionText);
            Assert.Equal("Safety", viewModel.SafetySettingsSectionText);
            Assert.Equal("Advanced", viewModel.AdvancedSettingsSectionText);
            Assert.Equal("Save All Config", viewModel.SaveAllStationConfigButtonText);
        }

        [Fact]
        public void StartsWithEditableTestPlanLoadedFromCurrentTestPlan()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var viewModel = new MainViewModel(repoRoot);

                Assert.Equal("Default Test Plan", viewModel.EditableTestPlanName);
                Assert.Equal("1.0", viewModel.EditableTestPlanVersion);
                Assert.Equal("K7000", viewModel.EditableTestPlanProduct);
                var item = Assert.Single(viewModel.TestPlanItems);
                Assert.Equal("fixture.prepare", item.Id);
                Assert.Equal("Fixture Prepare", item.Name);
                Assert.Equal("FixturePrepare", item.KindText);
                Assert.True(item.IsEnabled);
                Assert.True(item.IsRequired);
                Assert.True(item.StopOnFailure);
                Assert.Equal(60, item.TimeoutSeconds);
                Assert.Equal("keep", item.Note);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveTestPlanCommandPersistsEditableTestPlan()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var testPlanPath = Path.Combine(repoRoot, "Runtime", "TestPlans", "Rfp7000V2.testplan.json");
                var viewModel = new MainViewModel(repoRoot)
                {
                    EditableTestPlanName = "Edited Plan",
                    EditableTestPlanVersion = "1.1",
                    EditableTestPlanProduct = "K7000-EDIT"
                };
                var item = viewModel.TestPlanItems.Single();
                item.Name = "Edited Fixture";
                item.KindText = "LimitCheck";
                item.IsEnabled = false;
                item.IsRequired = false;
                item.StopOnFailure = false;
                item.TimeoutSeconds = 75;
                item.Adapter = "DaqVoltage";
                item.LowLimit = "1.1";
                item.HighLimit = "4.4";
                item.Unit = "V";
                item.ComparisonType = "GELE";

                viewModel.SaveTestPlanCommand.Execute(null);

                var json = JObject.Parse(File.ReadAllText(testPlanPath));
                var savedItem = json.SelectToken("items[0]")!;
                Assert.Equal("Edited Plan", (string)json.SelectToken("name")!);
                Assert.Equal("1.1", (string)json.SelectToken("version")!);
                Assert.Equal("K7000-EDIT", (string)json.SelectToken("product")!);
                Assert.Equal("Edited Fixture", (string)savedItem.SelectToken("name")!);
                Assert.Equal("LimitCheck", (string)savedItem.SelectToken("kind")!);
                Assert.False((bool)savedItem.SelectToken("enabled")!);
                Assert.False((bool)savedItem.SelectToken("required")!);
                Assert.False((bool)savedItem.SelectToken("stopOnFailure")!);
                Assert.Equal(75, (int)savedItem.SelectToken("timeoutSeconds")!);
                Assert.Equal("DaqVoltage", (string)savedItem.SelectToken("parameters.adapter")!);
                Assert.Equal(1.1, (double)savedItem.SelectToken("parameters.low")!);
                Assert.Equal(4.4, (double)savedItem.SelectToken("parameters.high")!);
                Assert.Equal("V", (string)savedItem.SelectToken("parameters.unit")!);
                Assert.Equal("GELE", (string)savedItem.SelectToken("parameters.comparisonType")!);
                Assert.Equal("keep", (string)savedItem.SelectToken("parameters.note")!);
                Assert.Contains(viewModel.Logs, x => x.Contains("Saved test plan"));
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void TestPlanEditorShowsFunctionalGroupSummaryAndDetails()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                File.WriteAllText(
                    Path.Combine(repoRoot, "Runtime", "TestPlans", "Rfp7000V2.testplan.json"),
                    @"{
  ""name"": ""Grouped Plan"",
  ""version"": ""1.0"",
  ""product"": ""K7000"",
  ""items"": [
    {
      ""id"": ""fct.hvac-sw.group"",
      ""name"": ""HVAC Switch Group"",
      ""kind"": ""FunctionalCheck"",
      ""required"": true,
      ""stopOnFailure"": true,
      ""timeoutSeconds"": 150,
      ""parameters"": {
        ""template"": ""I2cFunctionalGroup"",
        ""address"": ""0x12"",
        ""readRegister"": ""0xD4"",
        ""powerOnBefore"": [ { ""channel"": 1, ""voltage"": 12.2 } ],
        ""items"": [
          { ""id"": ""fct.hvac-sw.def-frt"", ""name"": ""DEF_FRT_SW"", ""template"": ""I2cByteSequence"", ""relayOutputChannel"": 31, ""checks"": [ { ""name"": ""LO"", ""expectedBytes"": ""88 88 08"" } ] },
          { ""id"": ""fct.hvac-sw.dfg-rr"", ""name"": ""DFG_RR_SW"", ""template"": ""I2cByteSequence"", ""relayOutputChannel"": 32, ""checks"": [ { ""name"": ""LO"", ""expectedBytes"": ""88 88 08"" } ] }
        ]
      }
    }
  ]
}");

                var viewModel = new MainViewModel(repoRoot);
                var group = Assert.Single(viewModel.TestPlanItems);
                viewModel.SelectedTestPlanItem = group;

                Assert.True(group.IsFunctionalGroup);
                Assert.Contains("2 child", group.SummaryText);
                Assert.Contains("CH1 12.2V", group.SummaryText);
                Assert.Contains("DEF_FRT_SW", group.GroupChildrenText);
                Assert.Contains("DFG_RR_SW", group.GroupChildrenText);
                Assert.Contains("HVAC Switch Group", viewModel.SelectedTestPlanDetailText);
                Assert.Contains("DEF_FRT_SW", viewModel.SelectedTestPlanDetailText);
                Assert.Contains("0x12", viewModel.SelectedTestPlanDetailText);
                Assert.Contains(Environment.NewLine + "  \"template\": \"I2cFunctionalGroup\"", viewModel.SelectedTestPlanDetailText);
                Assert.DoesNotContain(@"{""template"":""I2cFunctionalGroup"",""address"":""0x12""", viewModel.SelectedTestPlanDetailText);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void SaveTestPlanCommandReportsConcreteValidationReasons()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var viewModel = new MainViewModel(repoRoot);
                var item = viewModel.TestPlanItems.Single();
                item.TimeoutSeconds = 0;
                item.LowLimit = "5";
                item.HighLimit = "3";
                viewModel.TestPlanItems.Add(new TestPlanItemEditorViewModel
                {
                    Id = "fixture.prepare",
                    Name = "Duplicate",
                    KindText = "Flash",
                    TimeoutSeconds = 30,
                    Script = "Runtime/Flash/Missing.bat",
                    ParametersJson = "{}"
                });
                viewModel.TestPlanItems.Add(new TestPlanItemEditorViewModel
                {
                    Id = "fct.empty.group",
                    Name = "Empty Group",
                    KindText = "FunctionalCheck",
                    TimeoutSeconds = 30,
                    ParametersJson = @"{""template"":""I2cFunctionalGroup"",""items"":[]}"
                });

                viewModel.SaveTestPlanCommand.Execute(null);

                Assert.True(viewModel.HasTestPlanValidationIssues);
                Assert.Contains(viewModel.TestPlanValidationIssues, x => x.Contains("fixture.prepare") && x.Contains("timeoutSeconds"));
                Assert.Contains(viewModel.TestPlanValidationIssues, x => x.Contains("Duplicate item ID"));
                Assert.Contains(viewModel.TestPlanValidationIssues, x => x.Contains("low") && x.Contains("high"));
                Assert.Contains(viewModel.TestPlanValidationIssues, x => x.Contains("Script file does not exist"));
                Assert.Contains(viewModel.TestPlanValidationIssues, x => x.Contains("Functional group has no child items"));
                Assert.Contains("validation failed", viewModel.TestPlanEditorStatusText, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public void ResetClearsRunCollections()
        {
            var viewModel = new MainViewModel();
            viewModel.Logs.Add("log");
            viewModel.Results.Add(new StepResultViewModel { StepName = "Step" });
            viewModel.FailureResults.Add(new StepResultViewModel { StepName = "Fail Step" });

            viewModel.ResetCommand.Execute(null);

            Assert.Empty(viewModel.Logs);
            Assert.Empty(viewModel.Results);
            Assert.Empty(viewModel.FailureResults);
            Assert.Equal("Idle", viewModel.OverallStatus);
        }

        [Fact]
        public async Task StartRunBlocksWhenSerialNumberIsEmpty()
        {
            var viewModel = new MainViewModel
            {
                SerialNumber = "   ",
                ExecutionMode = "Mock"
            };

            await viewModel.StartRunAsync();

            Assert.Empty(viewModel.Results);
            Assert.Equal("Error", viewModel.OverallStatus);
            Assert.Contains("Serial number is required", viewModel.OverallStatusText);
            var failure = Assert.Single(viewModel.FailureResults);
            Assert.Equal("Run Start", failure.StepName);
            Assert.Contains("Serial number is required", failure.Message);
            Assert.Contains(viewModel.Logs, x => x.Contains("Serial number is required"));
        }

        [Fact]
        public async Task StartRunBlocksWhenPreflightRequiredScriptIsMissing()
        {
            var repoRoot = CreateTempRepoRoot();
            try
            {
                var viewModel = new MainViewModel(repoRoot)
                {
                    SerialNumber = "SN-PREFLIGHT",
                    ExecutionMode = "Mock"
                };

                await viewModel.StartRunAsync();

                Assert.Empty(viewModel.Results);
                Assert.Equal("Error", viewModel.OverallStatus);
                Assert.Contains(@"..\Flash\RFP_Auto\Scripts\Flash_once.bat", viewModel.OverallStatusText);
                Assert.Contains(viewModel.FailureResults, x =>
                    x.StepName.Contains("Preflight")
                    && x.Message.Contains(@"..\Flash\RFP_Auto\Scripts\Flash_once.bat"));
                Assert.Contains(viewModel.Logs, x => x.Contains("Preflight failed"));
                Assert.Contains(viewModel.Logs, x => x.Contains(@"..\Flash\RFP_Auto\Scripts\Flash_once.bat"));
            }
            finally
            {
                Directory.Delete(repoRoot, true);
            }
        }

        [Fact]
        public async Task MockRunWritesJsonCsvAndReadableRunLogReports()
        {
            var serialNumber = "UT" + Guid.NewGuid().ToString("N");
            var reportDirectory = Path.Combine(TestPaths.RepoRoot(), "Reports");

            try
            {
                var viewModel = new MainViewModel
                {
                    SerialNumber = serialNumber,
                    ExecutionMode = "Mock"
                };

                await viewModel.StartRunAsync();

                Assert.NotEmpty(viewModel.Results);
                Assert.Empty(viewModel.FailureResults);
                Assert.NotEqual("Error", viewModel.OverallStatus);

                var jsonPath = Assert.Single(Directory.GetFiles(reportDirectory, serialNumber + "_*.json"));
                var csvPath = Assert.Single(Directory.GetFiles(reportDirectory, serialNumber + "_*.csv"));
                var logPath = Assert.Single(Directory.GetFiles(reportDirectory, serialNumber + "_*.log"));

                Assert.Contains(serialNumber, File.ReadAllText(jsonPath));
                Assert.Contains("StepName,Status,Value,LowLimit,HighLimit,Unit,StartTime,EndTime,Message", File.ReadAllText(csvPath));
                var log = File.ReadAllText(logPath);
                Assert.Contains("RUN START", log);
                Assert.Contains("SerialNumber: " + serialNumber, log);
                Assert.Contains("TestPlan: RFP 7000 V2", log);
                Assert.Contains("Config:", log);
                Assert.Contains("[1] STEP START", log);
                Assert.Contains("Parameters:", log);
                Assert.Contains("[1] STEP END", log);
                Assert.Contains("Status:", log);
                Assert.Contains(viewModel.Logs, x => x.Contains("Run log: " + logPath));
                Assert.Contains(viewModel.Logs, x => x.Contains("Step 1/") && x.Contains("Fixture Prepare") && x.Contains("Passed"));
            }
            finally
            {
                if (Directory.Exists(reportDirectory))
                {
                    foreach (var path in Directory.GetFiles(reportDirectory, serialNumber + "_*.*"))
                    {
                        File.Delete(path);
                    }
                }
            }
        }

        [Fact]
        public async Task MockRunUsesStableTestPlanInsteadOfProfileHtml()
        {
            var serialNumber = "UT" + Guid.NewGuid().ToString("N");
            var reportDirectory = Path.Combine(TestPaths.RepoRoot(), "Reports");

            try
            {
                var viewModel = new MainViewModel
                {
                    SerialNumber = serialNumber,
                    ExecutionMode = "Mock"
                };

                await viewModel.StartRunAsync();

                Assert.Contains(viewModel.Logs, x => x.Contains("Loading test plan:"));
                Assert.DoesNotContain(viewModel.Logs, x => x.Contains("Loading profile:"));
                Assert.Contains(viewModel.Logs, x => x.Contains("Test plan: RFP 7000 V2"));
                Assert.Contains(viewModel.Results, x => x.StepName == "MCU Simple Flash");
            }
            finally
            {
                if (Directory.Exists(reportDirectory))
                {
                    foreach (var path in Directory.GetFiles(reportDirectory, serialNumber + "_*.*"))
                    {
                        File.Delete(path);
                    }
                }
            }
        }

        [Fact]
        public async Task MockRunUpdatesProductionStatistics()
        {
            var serialNumber = "UT" + Guid.NewGuid().ToString("N");
            var reportDirectory = Path.Combine(TestPaths.RepoRoot(), "Reports");

            try
            {
                var viewModel = new MainViewModel
                {
                    SerialNumber = serialNumber,
                    ExecutionMode = "Mock"
                };

                await viewModel.StartRunAsync();

                Assert.Equal(1, viewModel.TotalCount);
                Assert.Equal(1, viewModel.GoodCount);
                Assert.Equal(0, viewModel.FailCount);
                Assert.Equal(0, viewModel.ErrorCount);
                Assert.Equal("100.00%", viewModel.YieldRateText);
                Assert.Equal(100.0, viewModel.ProgressPercent);

                viewModel.ResetStatisticsCommand.Execute(null);

                Assert.Equal(0, viewModel.TotalCount);
                Assert.Equal("0.00%", viewModel.YieldRateText);
            }
            finally
            {
                if (Directory.Exists(reportDirectory))
                {
                    foreach (var path in Directory.GetFiles(reportDirectory, serialNumber + "_*.*"))
                    {
                        File.Delete(path);
                    }
                }
            }
        }

        private static string CreateTempRepoRoot()
        {
            var repoRoot = Path.Combine(Path.GetTempPath(), "RfpTestStation_ViewModel_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Runtime", "Config"));
            Directory.CreateDirectory(Path.Combine(repoRoot, "Runtime", "TestPlans"));
            File.WriteAllText(
                Path.Combine(repoRoot, "Runtime", "TestPlans", "Rfp7000V2.testplan.json"),
                @"{
  ""name"": ""Default Test Plan"",
  ""version"": ""1.0"",
  ""product"": ""K7000"",
  ""items"": [
    {
      ""id"": ""fixture.prepare"",
      ""name"": ""Fixture Prepare"",
      ""kind"": ""FixturePrepare"",
      ""required"": true,
      ""stopOnFailure"": true,
      ""timeoutSeconds"": 60,
      ""parameters"": {
        ""note"": ""keep""
      }
    }
  ]
}");
            File.WriteAllText(
                Path.Combine(repoRoot, "Runtime", "TestPlans", "Alt.testplan.json"),
                @"{
  ""name"": ""Alt Test Plan"",
  ""version"": ""1.0"",
  ""product"": ""K7000"",
  ""items"": [
    {
      ""id"": ""fixture.prepare"",
      ""name"": ""Fixture Prepare"",
      ""kind"": ""FixturePrepare"",
      ""required"": true,
      ""stopOnFailure"": true,
      ""timeoutSeconds"": 60,
      ""parameters"": {}
    }
  ]
}");
            File.WriteAllText(
                Path.Combine(repoRoot, "Runtime", "Config", "Config.json"),
                @"{
  ""Unknown"": 42,
  ""Burn1"": {
    ""Params"": {
      ""FilePath"": ""..\\Flash\\RFP_Auto\\Scripts\\Flash_once.bat""
    }
  },
  ""Burn2"": {
    ""Params"": {
      ""FilePath"": ""..\\Flash\\RedCase_Auto\\Debug\\FlashUpdate_Run.bat"",
      ""BinFilePath"": ""..\\Flash\\RedCase_Auto\\Debug\\Firmware_Local\\HX6330B03_C_Sharp_GM_Mobis_2992x1299_16.3_Falcon_20251223_S1C7A404_Test_Ver4-0.bin""
    }
  },
  ""Burn3"": {
    ""Params"": {
      ""FilePath"": ""..\\Flash\\TDDI_Auto\\Test\\Test\\bin\\Debug\\flash_run.bat"",
      ""Serial"": {
        ""PortName"": ""COM14""
      }
    }
  },
  ""FctTest"": {
    ""Global"": {
      ""Station"": ""StationA""
    }
  },
  ""Instruments"": [
    {
      ""Name"": ""IO DAQ"",
      ""Com"": ""COM4""
    },
    {
      ""Name"": ""Scanner"",
      ""Com"": ""COM1""
    },
    {
      ""Name"": ""Oscil"",
      ""Host"": ""192.168.1.13""
    }
  ],
  ""Safety"": {
    ""Enabled"": true,
    ""PollIntervalMs"": 50,
    ""Inputs"": {
      ""EmergencyStop"": {
        ""Channel"": 4,
        ""TriggeredValue"": true
      },
      ""LightCurtain"": {
        ""Channel"": 5,
        ""TriggeredValue"": false
      }
    },
    ""ReleaseFixture"": {
      ""DownOutputChannel"": 2,
      ""DownSafeValue"": false,
      ""UpOutputChannel"": 1,
      ""UpPreValue"": false,
      ""UpDelayMs"": 1000,
      ""UpSafeValue"": true
    },
    ""OnTrigger"": {
      ""CancelRun"": true,
      ""KillExternalProcessTree"": true,
      ""LatchUntilManualReset"": true
    }
  }
}");
            File.WriteAllText(Path.Combine(repoRoot, "Runtime", "Config", "AltConfig.json"), "{}");
            return repoRoot;
        }
    }
}
