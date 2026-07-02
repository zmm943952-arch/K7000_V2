using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace RfpTestStation.Core.Preflight
{
    public sealed class StationPreflightChecker
    {
        public IReadOnlyList<PreflightCheckResult> Check(string testPlanPath, string configPath)
        {
            var results = new List<PreflightCheckResult>();
            AddFileCheck(results, "Test plan", testPlanPath, testPlanPath);
            AddFileCheck(results, "Config", configPath, configPath);

            if (!File.Exists(configPath))
            {
                return results;
            }

            var configDir = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? Environment.CurrentDirectory;
            var root = JObject.Parse(File.ReadAllText(configPath));
            AddConfiguredFileCheck(results, configDir, root, "Burn1 RFP script", "Burn1.Params.FilePath", required: true);
            AddConfiguredFileCheck(results, configDir, root, "Burn2 TCON script", "Burn2.Params.FilePath", required: true);
            AddConfiguredFileCheck(results, configDir, root, "Burn2 TCON bin", "Burn2.Params.BinFilePath", required: false);
            AddConfiguredFileCheck(results, configDir, root, "Burn3 TDDI script", "Burn3.Params.FilePath", required: true);

            return results;
        }

        private static void AddConfiguredFileCheck(
            IList<PreflightCheckResult> results,
            string baseDir,
            JObject root,
            string name,
            string tokenPath,
            bool required)
        {
            var configuredPath = (string?)root.SelectToken(tokenPath);
            if (string.IsNullOrWhiteSpace(configuredPath))
            {
                if (required)
                {
                    results.Add(new PreflightCheckResult(name, false, tokenPath + " is empty."));
                }
                else
                {
                    results.Add(new PreflightCheckResult(name, true, tokenPath + " is empty; skipped."));
                }

                return;
            }

            var fullPath = ResolveConfiguredPath(baseDir, configuredPath!);
            AddFileCheck(results, name, fullPath, configuredPath!);
        }

        private static void AddFileCheck(IList<PreflightCheckResult> results, string name, string fullPath, string displayPath)
        {
            var exists = File.Exists(fullPath);
            results.Add(new PreflightCheckResult(
                name,
                exists,
                exists ? "OK: " + displayPath : "Missing file: " + displayPath + " => " + fullPath));
        }

        private static string ResolveConfiguredPath(string baseDir, string path)
        {
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDir, path));
        }
    }
}
