using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConfigTool
{
    public sealed class ConfigService
    {
        public const string RfpProject1Name = "k7000_1.rpj";
        public const string RfpProject2Name = "k7000_2.rpj";
        public const string RfpProject1LocalPath = @".\RFP_Auto\Firmware\k7000_1";
        public const string RfpProject2LocalPath = @".\RFP_Auto\Firmware\k7000_2";
        public const string RedCaseLocalPath = @".\RedCase_Auto\Debug\Firmware_Local";

        private readonly string _configPath;

        public ConfigService(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                throw new ArgumentException("Config path is required.", nameof(configPath));
            }

            _configPath = Path.GetFullPath(configPath);
        }

        public ConfigModel Load()
        {
            var root = LoadRoot();
            var model = new ConfigModel();

            var burn1Params = root["Burn1"]?["Params"] as JObject;
            var rfpProjects = burn1Params?["RfpProjects"] as JArray;
            if (rfpProjects != null)
            {
                model.RfpProject1MesPath = GetRfpProjectMesPath(rfpProjects, RfpProject1Name);
                model.RfpProject2MesPath = GetRfpProjectMesPath(rfpProjects, RfpProject2Name);
                LoadRfpProjectFiles(rfpProjects, RfpProject1Name, model.RfpProject1MesPath, model.RfpProject1FirmwareFiles);
                LoadRfpProjectFiles(rfpProjects, RfpProject2Name, model.RfpProject2MesPath, model.RfpProject2FirmwareFiles);
            }

            var burn2Params = root["Burn2"]?["Params"] as JObject;
            if (burn2Params != null)
            {
                model.RedCaseMesPath = (string)burn2Params["MesFirmwarePath"] ?? "";
                model.RedCaseBinPath = (string)burn2Params["BinFilePath"] ?? "";
            }

            return model;
        }

        public void Save(ConfigModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var root = LoadRoot();

            var burn1 = EnsureObject(root, "Burn1");
            var burn1Params = EnsureObject(burn1, "Params");
            var rfpProjects = EnsureArray(burn1Params, "RfpProjects");
            UpsertRfpProject(rfpProjects, RfpProject1Name, model.RfpProject1MesPath, RfpProject1LocalPath, model.RfpProject1FirmwareFiles);
            UpsertRfpProject(rfpProjects, RfpProject2Name, model.RfpProject2MesPath, RfpProject2LocalPath, model.RfpProject2FirmwareFiles);

            var burn2 = EnsureObject(root, "Burn2");
            var burn2Params = EnsureObject(burn2, "Params");
            burn2Params["MesFirmwarePath"] = model.RedCaseMesPath ?? "";
            burn2Params["LocalFirmwarePath"] = RedCaseLocalPath;
            if (!string.IsNullOrWhiteSpace(model.RedCaseBinPath))
            {
                burn2Params["BinFilePath"] = model.RedCaseBinPath;
            }
            if (!(burn2Params["FirmwareFiles"] is JArray))
            {
                burn2Params["FirmwareFiles"] = new JArray();
            }

            var json = root.ToString(Formatting.Indented);
            File.WriteAllText(_configPath, json, new UTF8Encoding(true));
        }

        public static string FindDefaultConfigPath()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.CurrentDirectory, "Config.json"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.json")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Config.json");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }

            return Path.GetFullPath("Config.json");
        }

        private JObject LoadRoot()
        {
            if (!File.Exists(_configPath))
            {
                throw new FileNotFoundException("Config.json not found.", _configPath);
            }

            return JObject.Parse(File.ReadAllText(_configPath, Encoding.UTF8));
        }

        private static string GetRfpProjectMesPath(JArray projects, string projectName)
        {
            foreach (var item in projects)
            {
                var obj = item as JObject;
                if (obj == null)
                {
                    continue;
                }

                if (string.Equals((string)obj["ProjectName"], projectName, StringComparison.OrdinalIgnoreCase))
                {
                    return (string)obj["MesFirmwarePath"] ?? "";
                }
            }

            return "";
        }

        private static void LoadRfpProjectFiles(JArray projects, string projectName, string mesPath, System.Collections.Generic.ICollection<string> target)
        {
            target.Clear();

            foreach (var item in projects)
            {
                var obj = item as JObject;
                if (obj == null)
                {
                    continue;
                }

                if (!string.Equals((string)obj["ProjectName"], projectName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var files = obj["FirmwareFiles"] as JArray;
                if (files == null)
                {
                    return;
                }

                foreach (var fileToken in files)
                {
                    var fileObj = fileToken as JObject;
                    if (fileObj == null)
                    {
                        continue;
                    }

                    var mesFileName = (string)fileObj["MesFileName"];
                    if (string.IsNullOrWhiteSpace(mesFileName))
                    {
                        continue;
                    }

                    target.Add(Path.IsPathRooted(mesFileName) ? mesFileName : Path.Combine(mesPath ?? "", mesFileName));
                }

                return;
            }
        }

        private static void UpsertRfpProject(JArray projects, string projectName, string mesPath, string localPath, System.Collections.Generic.IReadOnlyList<string> firmwareFiles)
        {
            JObject project = null;
            foreach (var item in projects)
            {
                var obj = item as JObject;
                if (obj != null && string.Equals((string)obj["ProjectName"], projectName, StringComparison.OrdinalIgnoreCase))
                {
                    project = obj;
                    break;
                }
            }

            if (project == null)
            {
                project = new JObject();
                projects.Add(project);
            }

            project["ProjectName"] = projectName;
            project["MesFirmwarePath"] = mesPath ?? "";
            project["LocalFirmwarePath"] = localPath;
            if (firmwareFiles != null && firmwareFiles.Count > 0)
            {
                var files = new JArray();
                for (var i = 0; i < firmwareFiles.Count; i++)
                {
                    var fileName = Path.GetFileName(firmwareFiles[i]);
                    files.Add(new JObject
                    {
                        ["RpjItemIndex"] = i,
                        ["MesFileName"] = fileName,
                        ["LocalFileName"] = fileName
                    });
                }

                project["FirmwareFiles"] = files;
            }
            else if (!(project["FirmwareFiles"] is JArray))
            {
                project["FirmwareFiles"] = new JArray();
            }
            else
            {
                project["FirmwareFiles"] = new JArray();
            }
        }

        private static JObject EnsureObject(JObject parent, string propertyName)
        {
            if (!(parent[propertyName] is JObject obj))
            {
                obj = new JObject();
                parent[propertyName] = obj;
            }

            return obj;
        }

        private static JArray EnsureArray(JObject parent, string propertyName)
        {
            if (!(parent[propertyName] is JArray array))
            {
                array = new JArray();
                parent[propertyName] = array;
            }

            return array;
        }
    }
}
