using System;
using System.IO;
using Newtonsoft.Json;

namespace RfpTestStation.Core.Configuration
{
    public sealed class AppSettingsRepository
    {
        private readonly string _path;
        private readonly string _repoRoot;

        public AppSettingsRepository(string path, string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Settings path is required.", nameof(path));
            }

            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                throw new ArgumentException("Repository root is required.", nameof(repoRoot));
            }

            _path = path;
            _repoRoot = Path.GetFullPath(repoRoot);
        }

        public AppSettings LoadOrDefault(StationPaths stationPaths)
        {
            if (stationPaths == null)
            {
                throw new ArgumentNullException(nameof(stationPaths));
            }

            if (!File.Exists(_path))
            {
                return CreateDefault(stationPaths);
            }

            var settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(_path)) ?? CreateDefault(stationPaths);
            FillMissingValues(settings, stationPaths);
            return settings;
        }

        public void Save(AppSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var directory = Path.GetDirectoryName(_path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var normalized = new AppSettings
            {
                CurrentUser = settings.CurrentUser,
                ProductName = settings.ProductName,
                SelectedLanguage = settings.SelectedLanguage,
                ExecutionMode = settings.ExecutionMode,
                TestPlanPath = ToStoredPath(settings.TestPlanPath),
                ConfigJsonPath = ToStoredPath(settings.ConfigJsonPath)
            };

            File.WriteAllText(_path, JsonConvert.SerializeObject(normalized, Formatting.Indented));
        }

        public string ResolvePath(string path)
        {
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_repoRoot, path));
        }

        private static AppSettings CreateDefault(StationPaths stationPaths)
        {
            return new AppSettings
            {
                CurrentUser = "Operator",
                ProductName = "K7000",
                SelectedLanguage = "中文",
                ExecutionMode = "Mock",
                TestPlanPath = ToForwardSlashes(MakeRelativePath(stationPaths.RepoRoot, stationPaths.TestPlanPath)),
                ConfigJsonPath = ToForwardSlashes(MakeRelativePath(stationPaths.RepoRoot, stationPaths.ConfigJsonPath))
            };
        }

        private static void FillMissingValues(AppSettings settings, StationPaths stationPaths)
        {
            var defaults = CreateDefault(stationPaths);
            if (string.IsNullOrWhiteSpace(settings.CurrentUser)) settings.CurrentUser = defaults.CurrentUser;
            if (string.IsNullOrWhiteSpace(settings.ProductName)) settings.ProductName = defaults.ProductName;
            if (string.IsNullOrWhiteSpace(settings.SelectedLanguage)) settings.SelectedLanguage = defaults.SelectedLanguage;
            if (string.IsNullOrWhiteSpace(settings.ExecutionMode)) settings.ExecutionMode = defaults.ExecutionMode;
            if (string.IsNullOrWhiteSpace(settings.TestPlanPath)) settings.TestPlanPath = defaults.TestPlanPath;
            if (IsLegacyDefaultTestPlanPath(settings.TestPlanPath)) settings.TestPlanPath = defaults.TestPlanPath;
            if (string.IsNullOrWhiteSpace(settings.ConfigJsonPath)) settings.ConfigJsonPath = defaults.ConfigJsonPath;
            if (IsLegacyDefaultConfigPath(settings.ConfigJsonPath)) settings.ConfigJsonPath = defaults.ConfigJsonPath;
        }

        private static bool IsLegacyDefaultTestPlanPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = ToForwardSlashes(path).Trim();
            return string.Equals(normalized, "Project/TestPlans/Rfp7000V2.testplan.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "src/RfpTestStation/Rfp7000V2.testplan.json", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLegacyDefaultConfigPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var normalized = ToForwardSlashes(path).Trim();
            return string.Equals(normalized, "Project/Config.json", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "src/RfpTestStation/StationRuntime/Config/Config.json", StringComparison.OrdinalIgnoreCase);
        }

        private string ToStoredPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var fullPath = ResolvePath(path);
            var relative = MakeRelativePath(_repoRoot, fullPath);
            return ToForwardSlashes(relative);
        }

        private static string MakeRelativePath(string fromDirectory, string toPath)
        {
            var fromUri = new Uri(AppendDirectorySeparator(Path.GetFullPath(fromDirectory)));
            var toUri = new Uri(Path.GetFullPath(toPath));
            if (!string.Equals(fromUri.Scheme, toUri.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return toPath;
            }

            return Uri.UnescapeDataString(fromUri.MakeRelativeUri(toUri).ToString()).Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparator(string path)
        {
            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }

        private static string ToForwardSlashes(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
