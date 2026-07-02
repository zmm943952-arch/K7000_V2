using System;
using System.IO;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.Adapters.Flashing
{
    public sealed class FlashScriptMap
    {
        private readonly string _repoRoot;

        public FlashScriptMap(string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                throw new ArgumentException("Repository root is required.", nameof(repoRoot));
            }

            _repoRoot = Path.GetFullPath(repoRoot);
        }

        public FlashScriptDefinition Resolve(StepDefinition step)
        {
            var text = ModuleStepClassifier.ModuleText(step);

            if (Contains(text, "Test RFP_Flash_once.bat.vi"))
            {
                return Create("RFP", "Runtime", "Flash", "RFP_Auto", "Scripts", "Flash_once.bat");
            }

            if (Contains(text, "Test RedCase_FlashUpdate_Run.bat.vi"))
            {
                return Create("RedCase", "Runtime", "Flash", "RedCase_Auto", "Debug", "FlashUpdate_Run.bat");
            }

            if (Contains(text, "Test TDDI_Flash_once.bat.vi"))
            {
                return Create("TDDI", "Runtime", "Flash", "TDDI_Auto", "Test", "Test", "bin", "Debug", "flash_run.bat");
            }

            throw new InvalidOperationException("No flashing script mapping was found for step: " + step.Name);
        }

        public FlashScriptDefinition Resolve(TestItem item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var script = RequiredParameter(item, "script");
            var flashKind = OptionalParameter(item, "flashKind") ?? item.Id;
            var arguments = OptionalParameter(item, "arguments") ?? string.Empty;
            var scriptPath = ResolvePath(script);
            var workingDirectory = OptionalParameter(item, "workingDirectory");
            var resolvedWorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Path.GetDirectoryName(scriptPath) ?? _repoRoot
                : ResolvePath(workingDirectory!);

            return new FlashScriptDefinition
            {
                FlashKind = flashKind,
                ScriptPath = scriptPath,
                WorkingDirectory = resolvedWorkingDirectory,
                Arguments = arguments
            };
        }

        private FlashScriptDefinition Create(string flashKind, params string[] relativeParts)
        {
            var parts = new string[relativeParts.Length + 1];
            parts[0] = _repoRoot;
            Array.Copy(relativeParts, 0, parts, 1, relativeParts.Length);
            var scriptPath = Path.GetFullPath(Path.Combine(parts));
            var workingDirectory = Path.GetDirectoryName(scriptPath) ?? _repoRoot;

            return new FlashScriptDefinition
            {
                FlashKind = flashKind,
                ScriptPath = scriptPath,
                WorkingDirectory = workingDirectory
            };
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string ResolvePath(string path)
        {
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(_repoRoot, path));
        }

        private static string RequiredParameter(TestItem item, string name)
        {
            var value = OptionalParameter(item, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException("Flash TestPlan item is missing parameter: " + name);
            }

            return value!;
        }

        private static string? OptionalParameter(TestItem item, string name)
        {
            var token = item.Parameters[name];
            return token == null ? null : token.ToString();
        }
    }

    public sealed class FlashScriptDefinition
    {
        public string FlashKind { get; set; } = string.Empty;

        public string ScriptPath { get; set; } = string.Empty;

        public string WorkingDirectory { get; set; } = string.Empty;

        public string Arguments { get; set; } = string.Empty;
    }
}
