using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    internal static class AdapterStepParser
    {
        public static string Text(StepDefinition step)
        {
            return ModuleStepClassifier.ModuleText(step);
        }

        public static bool Contains(StepDefinition step, string value)
        {
            return Text(step).IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static string? AssignmentTarget(StepDefinition step)
        {
            var text = Text(step);
            var match = Regex.Match(text, @"(?<target>(?:Locals|FileGlobals|Parameters)\.[A-Za-z0-9_\u0080-\uFFFF\[\]\.]+)\s*=");
            return match.Success ? match.Groups["target"].Value : null;
        }

        public static void SetContextValue(StationExecutionContext context, string? target, object value)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                return;
            }

            var targetName = target!;
            if (targetName.StartsWith("Locals.", StringComparison.OrdinalIgnoreCase))
            {
                context.Locals[targetName.Substring("Locals.".Length)] = value;
            }
            else if (targetName.StartsWith("FileGlobals.", StringComparison.OrdinalIgnoreCase))
            {
                context.FileGlobals[targetName.Substring("FileGlobals.".Length)] = value;
            }
            else if (targetName.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase))
            {
                context.Parameters[targetName.Substring("Parameters.".Length)] = value;
            }
        }

        public static string ResolveJsonPath(StationExecutionContext context, string repoRoot)
        {
            if (context.FileGlobals.TryGetValue("Json_FilePath", out var value))
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return NormalizeProjectPath(text!, repoRoot);
                }
            }

            return System.IO.Path.Combine(repoRoot, "Runtime", "Config", "Config.json");
        }

        public static string NormalizeProjectPath(string path, string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            var normalized = path.Replace("/", "\\");
            if (normalized.StartsWith("D:\\Project", StringComparison.OrdinalIgnoreCase))
            {
                return System.IO.Path.Combine(repoRoot, "Project", normalized.Substring("D:\\Project".Length).TrimStart('\\'));
            }

            if (normalized.StartsWith(".\\", StringComparison.Ordinal))
            {
                return System.IO.Path.Combine(repoRoot, "Project", normalized.Substring(2));
            }

            return System.IO.Path.IsPathRooted(path) ? path : System.IO.Path.Combine(repoRoot, path);
        }

        public static IReadOnlyList<string> QuotedStrings(StepDefinition step)
        {
            return Regex.Matches(Text(step), "\"(?<value>[^\"]*)\"")
                .Cast<Match>()
                .Select(x => x.Groups["value"].Value)
                .ToArray();
        }

        public static int FirstIntArgument(StepDefinition step, string methodName, StationExecutionContext context, int defaultValue = 0)
        {
            var args = ArgumentsFor(step, methodName);
            return args.Count == 0 ? defaultValue : ResolveInt(args[0], context, defaultValue);
        }

        public static bool SecondBoolArgument(StepDefinition step, string methodName, bool defaultValue = false)
        {
            var args = ArgumentsFor(step, methodName);
            return args.Count < 2 ? defaultValue : ResolveBool(args[1], defaultValue);
        }

        public static IReadOnlyList<string> ArgumentsFor(StepDefinition step, string methodName)
        {
            var text = Text(step);
            var match = Regex.Match(text, Regex.Escape(methodName) + @"\((?<args>[^)]*)\)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return Array.Empty<string>();
            }

            return SplitArguments(match.Groups["args"].Value);
        }

        public static byte[] ByteArrayArgument(string argument)
        {
            var match = Regex.Match(argument, @"\{(?<items>[^}]*)\}");
            if (!match.Success)
            {
                return Array.Empty<byte>();
            }

            return match.Groups["items"].Value
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (byte)ResolveIntLiteral(x.Trim(), 0))
                .ToArray();
        }

        public static int ResolveInt(string argument, StationExecutionContext context, int defaultValue = 0)
        {
            var text = argument.Trim();
            if (text.StartsWith("Locals.", StringComparison.OrdinalIgnoreCase)
                && context.Locals.TryGetValue(text.Substring("Locals.".Length), out var local))
            {
                return Convert.ToInt32(local, CultureInfo.InvariantCulture);
            }

            if (text.StartsWith("FileGlobals.", StringComparison.OrdinalIgnoreCase)
                && context.FileGlobals.TryGetValue(text.Substring("FileGlobals.".Length), out var fileGlobal))
            {
                return Convert.ToInt32(fileGlobal, CultureInfo.InvariantCulture);
            }

            if (text.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase)
                && context.Parameters.TryGetValue(text.Substring("Parameters.".Length), out var parameter))
            {
                return Convert.ToInt32(parameter, CultureInfo.InvariantCulture);
            }

            return ResolveIntLiteral(text, defaultValue);
        }

        public static int ResolveIntLiteral(string text, int defaultValue = 0)
        {
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : defaultValue;
        }

        public static bool ResolveBool(string argument, bool defaultValue = false)
        {
            var text = argument.Trim();
            return bool.TryParse(text, out var value) ? value : defaultValue;
        }

        private static IReadOnlyList<string> SplitArguments(string rawArgs)
        {
            var args = new List<string>();
            var depth = 0;
            var start = 0;
            for (var i = 0; i < rawArgs.Length; i++)
            {
                if (rawArgs[i] == '{')
                {
                    depth++;
                }
                else if (rawArgs[i] == '}')
                {
                    depth--;
                }
                else if (rawArgs[i] == ',' && depth == 0)
                {
                    args.Add(rawArgs.Substring(start, i - start).Trim());
                    start = i + 1;
                }
            }

            if (start <= rawArgs.Length)
            {
                var last = rawArgs.Substring(start).Trim();
                if (last.Length > 0)
                {
                    args.Add(last);
                }
            }

            return args;
        }
    }
}
