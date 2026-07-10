using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Xunit;

namespace RfpTestStation.Tests.Scripts
{
    public sealed class FlashPreparationScriptTests
    {
        [Fact]
        public void McuFlashItemsUseExplicitRfpProjectArguments()
        {
            var violations = new[]
                {
                    Tuple.Create("flash.mcu.simple", "k7000_1.rpj"),
                    Tuple.Create("flash.mcu.shipping", "k7000_2.rpj")
                }
                .SelectMany(expected => new[] { SourceTestPlanPath(), RuntimeTestPlanPath() }
                    .Select(path => new { Expected = expected, Path = path, Item = FlashItem(path, expected.Item1) }))
                .Where(x => !string.Equals((string?)x.Item.SelectToken("parameters.arguments"), x.Expected.Item2, StringComparison.OrdinalIgnoreCase))
                .Select(x => Path.GetFileName(x.Path) + ":" + x.Expected.Item1)
                .ToList();

            Assert.Empty(violations);
        }

        [Fact]
        public void TddiFlashBatchDoesNotSpawnPowerShellForRoutineLogWrites()
        {
            var text = File.ReadAllText(TddiFlashBatchPath());

            Assert.DoesNotContain("Add-Content", text, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void PreparationScriptRuntimeCopiesAreIdentical()
        {
            var repoRoot = TestPaths.RepoRoot();
            var pairs = new[]
            {
                Tuple.Create(
                    Path.Combine(repoRoot, "Runtime", "Flash", "RFP_Auto", "Scripts", "Prepare_RfpFirmware.ps1"),
                    Path.Combine(repoRoot, "src", "RfpTestStation", "StationRuntime", "Flash", "RFP_Auto", "Scripts", "Prepare_RfpFirmware.ps1")),
                Tuple.Create(
                    Path.Combine(repoRoot, "Runtime", "Flash", "RedCase_Auto", "Debug", "Prepare_RedCaseFirmware.ps1"),
                    Path.Combine(repoRoot, "src", "RfpTestStation", "StationRuntime", "Flash", "RedCase_Auto", "Debug", "Prepare_RedCaseFirmware.ps1"))
            };

            foreach (var pair in pairs)
            {
                Assert.Equal(File.ReadAllBytes(pair.Item1), File.ReadAllBytes(pair.Item2));
            }
        }

        [Fact]
        public async Task RfpPrepareSkipsProjectRewriteWhenLocalFirmwareAndProjectAreCurrent()
        {
            var root = Path.Combine(Path.GetTempPath(), "rfp-prepare-" + Guid.NewGuid().ToString("N"));
            try
            {
                var configDir = Path.Combine(root, "Config");
                var projectDir = Path.Combine(root, "Flash", "RFP_Auto", "Project");
                var mesDir = Path.Combine(root, "Mes", "k7000_1");
                var localDir = Path.Combine(root, "Flash", "RFP_Auto", "Firmware", "k7000_1");
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(projectDir);
                Directory.CreateDirectory(mesDir);
                var scriptPath = CopyRfpPrepareScript(root);

                File.WriteAllText(Path.Combine(mesDir, "Falcon_app.mot"), "S1130000285F245F2212226A000424290008237C2A");
                File.WriteAllText(Path.Combine(mesDir, "Falcon_boot.mot"), "S11300100002000800082629001853812341001813");

                var projectPath = Path.Combine(projectDir, "k7000_1.rpj");
                File.WriteAllText(projectPath, @"<?xml version=""1.0"" encoding=""utf-8""?>
<RfpProject>
  <OperationTab>
    <ProgramFiles>
      <Item Address=""00000000"" Type=""SREC"">old_app.mot</Item>
      <Item Address=""00000000"" Type=""SREC"">old_boot.mot</Item>
    </ProgramFiles>
  </OperationTab>
</RfpProject>");

                var configPath = Path.Combine(configDir, "Config.json");
                File.WriteAllText(configPath, new JObject
                {
                    ["Burn1"] = new JObject
                    {
                        ["Params"] = new JObject
                        {
                            ["RfpProjects"] = new JArray
                            {
                                new JObject
                                {
                                    ["ProjectName"] = "k7000_1.rpj",
                                    ["MesFirmwarePath"] = mesDir,
                                    ["LocalFirmwarePath"] = localDir
                                }
                            }
                        }
                    }
                }.ToString());

                var first = await RunPowerShellAsync(scriptPath, "-ConfigPath " + Quote(configPath) + " -ProjectPath " + Quote(projectPath));
                Assert.Equal(0, first.ExitCode);

                var projectAfterFirstRun = File.ReadAllText(projectPath);
                var backupDir = Path.Combine(projectDir, "Backup");
                var backupCountAfterFirstRun = Directory.GetFiles(backupDir, "*.rpj").Length;
                Assert.True(backupCountAfterFirstRun > 0);

                await Task.Delay(25);

                var second = await RunPowerShellAsync(scriptPath, "-ConfigPath " + Quote(configPath) + " -ProjectPath " + Quote(projectPath));

                Assert.Equal(0, second.ExitCode);
                Assert.Contains("already current", second.Output, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(projectAfterFirstRun, File.ReadAllText(projectPath));
                Assert.Equal(backupCountAfterFirstRun, Directory.GetFiles(backupDir, "*.rpj").Length);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        [Fact]
        public async Task RedCasePrepareSkipsConfigRewriteWhenLocalBinIsCurrent()
        {
            var root = Path.Combine(Path.GetTempPath(), "redcase-prepare-" + Guid.NewGuid().ToString("N"));
            try
            {
                var configDir = Path.Combine(root, "Config");
                var mesDir = Path.Combine(root, "Mes", "TCON");
                var localDir = Path.Combine(root, "Flash", "RedCase_Auto", "Debug", "Firmware_Local");
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(mesDir);
                var scriptPath = CopyRedCasePrepareScript(root);

                File.WriteAllText(Path.Combine(mesDir, "panel.bin"), "redcase-firmware");

                var configPath = Path.Combine(configDir, "Config.json");
                File.WriteAllText(configPath, new JObject
                {
                    ["Burn2"] = new JObject
                    {
                        ["Params"] = new JObject
                        {
                            ["MesFirmwarePath"] = mesDir,
                            ["LocalFirmwarePath"] = localDir,
                            ["BinFilePath"] = ""
                        }
                    }
                }.ToString());

                var outputPathFile = Path.Combine(root, "prepared_bin_path.txt");
                var first = await RunPowerShellAsync(scriptPath, "-ConfigPath " + Quote(configPath) + " -OutputBinPathFile " + Quote(outputPathFile));
                Assert.Equal(0, first.ExitCode);

                var configAfterFirstRun = File.ReadAllText(configPath);
                var backupDir = Path.Combine(localDir, "Backup");
                var backupCountAfterFirstRun = Directory.Exists(backupDir)
                    ? Directory.GetFiles(backupDir, "*.bin").Length
                    : 0;

                await Task.Delay(25);

                var second = await RunPowerShellAsync(scriptPath, "-ConfigPath " + Quote(configPath) + " -OutputBinPathFile " + Quote(outputPathFile));

                Assert.Equal(0, second.ExitCode);
                Assert.Contains("already current", second.Output, StringComparison.OrdinalIgnoreCase);
                Assert.Equal(configAfterFirstRun, File.ReadAllText(configPath));
                Assert.Equal(backupCountAfterFirstRun, Directory.Exists(backupDir)
                    ? Directory.GetFiles(backupDir, "*.bin").Length
                    : 0);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
        }

        [Fact]
        public async Task RfpPrepareRejectsNonEmptyUnownedCacheWithoutDeletingSentinel()
        {
            var root = Path.Combine(Path.GetTempPath(), "rfp-unsafe-" + Guid.NewGuid().ToString("N"));
            try
            {
                var scriptPath = CopyRfpPrepareScript(root);
                var configDir = Path.Combine(root, "Config");
                var projectDir = Path.Combine(root, "Flash", "RFP_Auto", "Project");
                var mesDir = Path.Combine(root, "Mes");
                var localDir = Path.Combine(root, "Flash", "RFP_Auto", "Firmware", "product");
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(projectDir);
                Directory.CreateDirectory(mesDir);
                Directory.CreateDirectory(localDir);
                var sentinel = Path.Combine(localDir, "do-not-delete.txt");
                File.WriteAllText(sentinel, "keep");
                File.WriteAllText(Path.Combine(mesDir, "app.mot"), "firmware");
                var projectPath = WriteRfpProject(projectDir, "product.rpj");
                var configPath = WriteRfpConfig(configDir, "product.rpj", mesDir, localDir);

                var result = await RunPowerShellAsync(scriptPath, "-ConfigPath " + Quote(configPath) + " -ProjectPath " + Quote(projectPath));

                Assert.NotEqual(0, result.ExitCode);
                Assert.Contains("marker", result.Output, StringComparison.OrdinalIgnoreCase);
                Assert.True(File.Exists(sentinel));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Fact]
        public async Task RedCasePrepareRejectsPathOutsideFixedCacheWithoutDeletingSentinel()
        {
            var root = Path.Combine(Path.GetTempPath(), "redcase-unsafe-" + Guid.NewGuid().ToString("N"));
            try
            {
                var scriptPath = CopyRedCasePrepareScript(root);
                var configDir = Path.Combine(root, "Config");
                var mesDir = Path.Combine(root, "Mes");
                var localDir = Path.Combine(root, "unrelated");
                Directory.CreateDirectory(configDir);
                Directory.CreateDirectory(mesDir);
                Directory.CreateDirectory(localDir);
                var sentinel = Path.Combine(localDir, "do-not-delete.txt");
                File.WriteAllText(sentinel, "keep");
                File.WriteAllText(Path.Combine(mesDir, "panel.bin"), "firmware");
                var configPath = WriteRedCaseConfig(configDir, mesDir, localDir);

                var result = await RunPowerShellAsync(scriptPath, "-ConfigPath " + Quote(configPath) + " -OutputBinPathFile " + Quote(Path.Combine(root, "prepared.txt")));

                Assert.NotEqual(0, result.ExitCode);
                Assert.Contains("Firmware_Local", result.Output, StringComparison.OrdinalIgnoreCase);
                Assert.True(File.Exists(sentinel));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        private static string CopyRfpPrepareScript(string root)
        {
            var path = Path.Combine(root, "Flash", "RFP_Auto", "Scripts", "Prepare_RfpFirmware.ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(RfpPrepareScriptPath(), path);
            return path;
        }

        private static string CopyRedCasePrepareScript(string root)
        {
            var path = Path.Combine(root, "Flash", "RedCase_Auto", "Debug", "Prepare_RedCaseFirmware.ps1");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(RedCasePrepareScriptPath(), path);
            return path;
        }

        private static string WriteRfpProject(string projectDir, string projectName)
        {
            var path = Path.Combine(projectDir, projectName);
            File.WriteAllText(path, "<RfpProject><OperationTab><ProgramFiles><Item Address=\"00000000\" Type=\"SREC\">old.mot</Item></ProgramFiles></OperationTab></RfpProject>");
            return path;
        }

        private static string WriteRfpConfig(string configDir, string projectName, string mesDir, string localDir)
        {
            var path = Path.Combine(configDir, "Config.json");
            File.WriteAllText(path, new JObject { ["Burn1"] = new JObject { ["Params"] = new JObject { ["RfpProjects"] = new JArray { new JObject { ["ProjectName"] = projectName, ["MesFirmwarePath"] = mesDir, ["LocalFirmwarePath"] = localDir } } } } }.ToString());
            return path;
        }

        private static string WriteRedCaseConfig(string configDir, string mesDir, string localDir)
        {
            var path = Path.Combine(configDir, "Config.json");
            File.WriteAllText(path, new JObject { ["Burn2"] = new JObject { ["Params"] = new JObject { ["MesFirmwarePath"] = mesDir, ["LocalFirmwarePath"] = localDir, ["BinFilePath"] = "" } } }.ToString());
            return path;
        }

        private static JObject FlashItem(string path, string id)
        {
            var json = JObject.Parse(File.ReadAllText(path));
            return ((JArray)json["items"]!).OfType<JObject>().Single(x => (string?)x["id"] == id);
        }

        private static async Task<ProcessRunResult> RunPowerShellAsync(string scriptPath, string arguments)
        {
            using (var process = new Process())
            {
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -File " + Quote(scriptPath) + " " + arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                process.Start();
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();
                await Task.Run(() => process.WaitForExit());
                return new ProcessRunResult(process.ExitCode, await outputTask + await errorTask);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string SourceTestPlanPath()
        {
            return Path.Combine(TestPaths.RepoRoot(), "src", "RfpTestStation", "Rfp7000V2.testplan.json");
        }

        private static string RuntimeTestPlanPath()
        {
            return Path.Combine(TestPaths.RepoRoot(), "Runtime", "TestPlans", "Rfp7000V2.testplan.json");
        }

        private static string TddiFlashBatchPath()
        {
            return Path.Combine(TestPaths.RepoRoot(), "Runtime", "Flash", "TDDI_Auto", "Test", "Test", "bin", "Debug", "flash_run.bat");
        }

        private static string RfpPrepareScriptPath()
        {
            return Path.Combine(TestPaths.RepoRoot(), "Runtime", "Flash", "RFP_Auto", "Scripts", "Prepare_RfpFirmware.ps1");
        }

        private static string RedCasePrepareScriptPath()
        {
            return Path.Combine(TestPaths.RepoRoot(), "Runtime", "Flash", "RedCase_Auto", "Debug", "Prepare_RedCaseFirmware.ps1");
        }

        private sealed class ProcessRunResult
        {
            public ProcessRunResult(int exitCode, string output)
            {
                ExitCode = exitCode;
                Output = output;
            }

            public int ExitCode { get; }

            public string Output { get; }
        }
    }
}
