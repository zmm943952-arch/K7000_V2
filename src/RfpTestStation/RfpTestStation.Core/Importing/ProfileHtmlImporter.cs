using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Importing
{
    public static class ProfileHtmlImporter
    {
        public static SequenceDocument Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Profile path is required.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Profile HTML file not found.", path);
            }

            try
            {
                var html = File.ReadAllText(path, Encoding.GetEncoding(936));
                return LoadFromHtml(html);
            }
            catch (ProfileImportException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ProfileImportException("Failed to import profile HTML: " + path, ex);
            }
        }

        internal static SequenceDocument LoadFromHtml(string html)
        {
            var htmlDocument = new HtmlDocument
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };
            htmlDocument.LoadHtml(html);

            var stepHeaders = ExtractStepHeaderInfos(html);
            var document = new SequenceDocument();
            ApplyFileGlobals(html, document);
            SequenceDefinition? currentSequence = null;
            string? currentSection = null;
            var stepId = 0;

            foreach (var node in EnumerateStructuralNodes(htmlDocument.DocumentNode))
            {
                if (IsSectionHeading(node))
                {
                    var section = CleanScalar(node.InnerText);
                    currentSection = IsStepSection(section) ? section : currentSection;
                    continue;
                }

                if (!IsTable(node))
                {
                    continue;
                }

                var headerText = GetHeaderText(node);
                if (headerText == null)
                {
                    if (currentSequence != null && currentSection == null)
                    {
                        var tableTitle = GetTableTitle(node);
                        if (tableTitle.StartsWith("Locals:", StringComparison.OrdinalIgnoreCase))
                        {
                            ApplyVariableRows(node.OuterHtml, currentSequence.Locals);
                        }
                    }

                    continue;
                }

                if (headerText.StartsWith("Sequence:", StringComparison.OrdinalIgnoreCase))
                {
                    currentSequence = new SequenceDefinition
                    {
                        Name = CleanScalar(headerText.Substring("Sequence:".Length))
                    };
                    document.Sequences.Add(currentSequence);
                    currentSection = null;
                    continue;
                }

                if (headerText.StartsWith("Step:", StringComparison.OrdinalIgnoreCase))
                {
                    if (currentSequence == null || !IsStepSection(currentSection))
                    {
                        continue;
                    }

                    var headerInfo = stepId < stepHeaders.Count ? stepHeaders[stepId] : null;
                    var step = ParseStep(node, headerText, ++stepId, currentSection!, headerInfo);
                    AddStepToSection(currentSequence, step, currentSection!);
                }
            }

            ApplySequenceLocals(html, document);
            return document;
        }

        private static void ApplyFileGlobals(string html, SequenceDocument document)
        {
            var tableMatch = Regex.Match(
                html,
                @"(?is)<TABLE\b[^>]*>.*?Sequence File Globals:.*?</TABLE>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (!tableMatch.Success)
            {
                return;
            }

            ApplyVariableRows(tableMatch.Value, document.FileGlobals);
        }

        private static void ApplyVariableRows(string tableHtml, IDictionary<string, object> target)
        {
            const string rowPattern = @"(?is)<TR>\s*<TD\b(?![^>]*COLSPAN)[^>]*>(?<name>.*?)<TD\b[^>]*>(?<type>.*?)<TD\b[^>]*>(?<value>.*?)(?=<TR>\s*<TD\b|</TABLE>)";
            foreach (Match match in Regex.Matches(tableHtml, rowPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                var name = CleanHtmlScalar(match.Groups["name"].Value);
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = CleanHtmlScalar(match.Groups["type"].Value);
                var value = CleanHtmlScalar(match.Groups["value"].Value);
                target[name] = ParseGlobalValue(type, value);
            }
        }

        private static void ApplySequenceLocals(string html, SequenceDocument document)
        {
            const string sequencePattern = @"(?is)<TABLE\b[^>]*>\s*<TR>\s*<TD\b[^>]*COLSPAN=2[^>]*>.*?<B>\s*Sequence:\s*(?<name>.*?)</B>.*?</TABLE>(?<body>.*?)(?=<HR><TABLE\b[^>]*>\s*<TR>\s*<TD\b[^>]*COLSPAN=2[^>]*>.*?<B>\s*Sequence:|</BODY>)";
            foreach (Match sequenceMatch in Regex.Matches(html, sequencePattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                var sequenceName = CleanHtmlScalar(sequenceMatch.Groups["name"].Value);
                SequenceDefinition sequence;
                try
                {
                    sequence = document.GetSequence(sequenceName);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                var body = sequenceMatch.Groups["body"].Value;
                var localsMatch = Regex.Match(
                    body,
                    @"(?is)<TABLE\b[^>]*>\s*<TR>\s*<TD\b[^>]*COLSPAN=3[^>]*>.*?<FONT[^>]*>\s*Locals:\s*</FONT>.*?</TABLE>",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (localsMatch.Success)
                {
                    ApplyVariableRows(localsMatch.Value, sequence.Locals);
                }
            }
        }

        private static object ParseGlobalValue(string type, string value)
        {
            if (string.Equals(type, "Boolean", StringComparison.OrdinalIgnoreCase))
            {
                return string.Equals(value, "True", StringComparison.OrdinalIgnoreCase);
            }

            if (string.Equals(type, "Number", StringComparison.OrdinalIgnoreCase)
                && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            {
                return number;
            }

            if (string.Equals(type, "String", StringComparison.OrdinalIgnoreCase))
            {
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                {
                    return value.Substring(1, value.Length - 2).Replace("\"\"", "\"");
                }

                return value;
            }

            return value;
        }

        private static IEnumerable<HtmlNode> EnumerateStructuralNodes(HtmlNode root)
        {
            return root
                .Descendants()
                .Where(x => IsTable(x) || IsSectionHeading(x));
        }

        private static bool IsTable(HtmlNode node)
        {
            return string.Equals(node.Name, "table", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSectionHeading(HtmlNode node)
        {
            return string.Equals(node.Name, "h3", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStepSection(string? section)
        {
            return string.Equals(section, "Setup", StringComparison.OrdinalIgnoreCase)
                || string.Equals(section, "Main", StringComparison.OrdinalIgnoreCase)
                || string.Equals(section, "Cleanup", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetHeaderText(HtmlNode table)
        {
            var firstRow = table.Elements("tr").FirstOrDefault();
            if (firstRow == null)
            {
                return null;
            }

            foreach (var bold in firstRow.Descendants("b"))
            {
                var text = CleanScalar(bold.InnerText);
                if (text.StartsWith("Sequence:", StringComparison.OrdinalIgnoreCase)
                    || text.StartsWith("Step:", StringComparison.OrdinalIgnoreCase))
                {
                    return text;
                }
            }

            return null;
        }

        private static string GetTableTitle(HtmlNode table)
        {
            var firstRow = table.Elements("tr").FirstOrDefault();
            return firstRow == null ? string.Empty : CleanScalar(firstRow.InnerText);
        }

        private static StepDefinition ParseStep(HtmlNode table, string headerText, int stepId, string sectionName, StepHeaderInfo? headerInfo)
        {
            var rawHtml = headerInfo?.RawHtml ?? table.OuterHtml;
            var step = new StepDefinition
            {
                Id = stepId,
                Name = CleanScalar(headerText.Substring("Step:".Length)),
                SectionName = sectionName,
                IndentLevel = headerInfo?.IndentLevel ?? 0,
                RawHtml = rawHtml
            };

            foreach (var row in ParseRows(rawHtml))
            {
                if (row.Label.StartsWith("StepType, Adapter", StringComparison.OrdinalIgnoreCase))
                {
                    ApplyStepTypeAndAdapter(step, row.Value);
                }
                else if (row.Label.StartsWith("Description", StringComparison.OrdinalIgnoreCase))
                {
                    step.DescriptionRaw = CleanMultiline(row.Value);
                    ApplyModuleCall(step);
                    ApplyLimitsFromDescription(step);
                }
                else if (row.Label.StartsWith("Flow Properties", StringComparison.OrdinalIgnoreCase))
                {
                    step.FlowPropertiesRaw = CleanMultiline(row.Value);
                    ApplyFlowProperties(step);
                }
                else if (row.Label.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase))
                {
                    step.ConditionExpression = CleanMultiline(row.Value);
                }
            }

            if (string.IsNullOrWhiteSpace(step.StepTypeRaw))
            {
                step.StepTypeRaw = step.Name;
                step.StepType = MapStepType(step.StepTypeRaw);
            }

            return step;
        }

        private static IEnumerable<ProfileStepRow> ParseRows(string tableHtml)
        {
            const string rowPattern = @"(?is)<TR>\s*<TD\b(?![^>]*COLSPAN)[^>]*>(?<label>(?:(?!<TD\b|<TR\b).)*?)<TD\b[^>]*>(?<value>.*?)(?=<TR>\s*<TD\b|</TABLE>)";

            foreach (Match match in Regex.Matches(tableHtml, rowPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                yield return new ProfileStepRow
                {
                    Label = CleanHtmlScalar(match.Groups["label"].Value),
                    Value = CleanHtmlMultiline(match.Groups["value"].Value)
                };
            }
        }

        private static IList<StepHeaderInfo> ExtractStepHeaderInfos(string html)
        {
            const string pattern = @"(?is)(?<prefix>(?:\s*</?BLOCKQUOTE>)+)\s*(?<table><TABLE\b[^>]*>\s*<TR>\s*<TD\b[^>]*COLSPAN=2[^>]*>.*?<B>\s*Step:\s*(?<name>.*?)</B>.*?</TABLE>)";
            var headers = new List<StepHeaderInfo>();

            foreach (Match match in Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline))
            {
                var prefix = match.Groups["prefix"].Value;
                var openCount = Regex.Matches(prefix, "<BLOCKQUOTE>", RegexOptions.IgnoreCase).Count;
                var rawName = Regex.Replace(match.Groups["name"].Value, "<.*?>", string.Empty);

                headers.Add(new StepHeaderInfo
                {
                    Name = CleanScalar(rawName),
                    IndentLevel = Math.Max(0, openCount - 1),
                    RawHtml = match.Groups["table"].Value
                });
            }

            return headers;
        }

        private static void ApplyStepTypeAndAdapter(StepDefinition step, string rawValue)
        {
            var value = CleanScalar(rawValue);
            var parts = value.Split(new[] { ',' }, 2);

            step.StepTypeRaw = parts[0].Trim();
            step.StepType = MapStepType(step.StepTypeRaw);

            if (parts.Length > 1)
            {
                step.AdapterName = parts[1].Trim();
            }
        }

        private static StepType MapStepType(string? raw)
        {
            var value = CleanScalar(raw ?? string.Empty);
            switch (value)
            {
                case "Action":
                    return StepType.Action;
                case "Numeric Limit Test":
                    return StepType.NumericLimitTest;
                case "String Value Test":
                    return StepType.StringValueTest;
                case "Pass/Fail Test":
                    return StepType.PassFailTest;
                case "NI_MultipleNumericLimitTest":
                case "Multiple Numeric Limit Test":
                    return StepType.MultipleNumericLimitTest;
                case "Wait":
                    return StepType.Wait;
                case "NI_Flow_If":
                case "If":
                    return StepType.If;
                case "NI_Flow_Else":
                case "Else":
                    return StepType.Else;
                case "NI_Flow_ElseIf":
                case "Else If":
                    return StepType.ElseIf;
                case "NI_Flow_While":
                case "While":
                    return StepType.While;
                case "NI_Flow_For":
                case "For":
                    return StepType.For;
                case "NI_Flow_ForEach":
                case "For Each":
                    return StepType.ForEach;
                case "NI_Flow_End":
                case "End":
                    return StepType.End;
                case "Sequence Call":
                    return StepType.SequenceCall;
                case "Statement":
                    return StepType.Statement;
                case "Label":
                    return StepType.Label;
                case "Goto":
                    return StepType.Goto;
                case "Message Popup":
                    return StepType.MessagePopup;
                default:
                    return StepType.Unknown;
            }
        }

        private static void ApplyModuleCall(StepDefinition step)
        {
            var descriptionRaw = step.DescriptionRaw ?? string.Empty;
            if (string.IsNullOrWhiteSpace(descriptionRaw))
            {
                return;
            }

            var moduleCall = new ModuleCallDefinition
            {
                AdapterName = step.AdapterName,
                RawText = descriptionRaw
            };
            step.ModuleCall = moduleCall;

            var viMatch = Regex.Match(descriptionRaw, @"(?<path>[\w .\\/\-\(\)]+\.vi)", RegexOptions.IgnoreCase);
            if (viMatch.Success)
            {
                moduleCall.ViPath = CleanScalar(viMatch.Groups["path"].Value);
            }

            var assemblyMarker = "Assembly:";
            var assemblyIndex = descriptionRaw.IndexOf(assemblyMarker, StringComparison.OrdinalIgnoreCase);
            if (assemblyIndex >= 0)
            {
                moduleCall.AssemblyName = CleanScalar(descriptionRaw.Substring(assemblyIndex + assemblyMarker.Length));
            }

            var dotNetMatch = Regex.Match(descriptionRaw, @"(?<type>[A-Za-z0-9_.]+)\s*:\s*(?<call>.+)", RegexOptions.Singleline);
            if (dotNetMatch.Success)
            {
                moduleCall.TypeName = CleanScalar(dotNetMatch.Groups["type"].Value);
                moduleCall.MethodName = CleanScalar(dotNetMatch.Groups["call"].Value);
            }
        }

        private static void ApplyLimitsFromDescription(StepDefinition step)
        {
            if (step.StepType != StepType.NumericLimitTest && step.StepType != StepType.MultipleNumericLimitTest)
            {
                return;
            }

            var description = step.DescriptionRaw ?? string.Empty;
            var match = Regex.Match(
                description,
                @"(?<low>-?\d+(?:\.\d+)?)\s*<=\s*x\s*<=\s*(?<high>-?\d+(?:\.\d+)?)",
                RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return;
            }

            step.Limits.Add(new LimitDefinition
            {
                Low = double.Parse(match.Groups["low"].Value, CultureInfo.InvariantCulture),
                High = double.Parse(match.Groups["high"].Value, CultureInfo.InvariantCulture)
            });
        }

        private static void ApplyFlowProperties(StepDefinition step)
        {
            var flowPropertiesRaw = step.FlowPropertiesRaw ?? string.Empty;
            if (string.IsNullOrWhiteSpace(flowPropertiesRaw))
            {
                return;
            }

            if (flowPropertiesRaw.IndexOf("Run Mode: Skip", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                step.RunMode = RunMode.Skip;
            }

            if (flowPropertiesRaw.IndexOf("New Thread", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                step.IsNewThread = true;
            }

            step.PreExpression = ExtractFlowBlock(flowPropertiesRaw, "Pre Expression:");
            step.PostExpression = ExtractFlowBlock(flowPropertiesRaw, "Post Expression:");
        }

        private static string? ExtractFlowBlock(string text, string marker)
        {
            var start = text.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
            {
                return null;
            }

            start += marker.Length;
            var end = text.Length;
            foreach (var nextMarker in new[] { "Post Expression:", "Post Action:", "Status Expression:", "Run Mode:", "Records " })
            {
                var next = text.IndexOf(nextMarker, start, StringComparison.OrdinalIgnoreCase);
                if (next >= 0)
                {
                    end = Math.Min(end, next);
                }
            }

            return CleanMultiline(text.Substring(start, end - start));
        }

        private static void AddStepToSection(SequenceDefinition sequence, StepDefinition step, string sectionName)
        {
            sequence.AllSteps.Add(step);

            if (string.Equals(sectionName, "Setup", StringComparison.OrdinalIgnoreCase))
            {
                sequence.SetupSteps.Add(step);
            }
            else if (string.Equals(sectionName, "Main", StringComparison.OrdinalIgnoreCase))
            {
                sequence.MainSteps.Add(step);
            }
            else if (string.Equals(sectionName, "Cleanup", StringComparison.OrdinalIgnoreCase))
            {
                sequence.CleanupSteps.Add(step);
            }
        }

        private static string CleanScalar(string value)
        {
            var text = HtmlEntity.DeEntitize(value ?? string.Empty).Replace('\u00A0', ' ');
            return Regex.Replace(text, @"\s+", " ").Trim();
        }

        private static string CleanMultiline(string value)
        {
            var text = HtmlEntity.DeEntitize(value ?? string.Empty).Replace('\u00A0', ' ');
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = text.Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0);
            return string.Join(Environment.NewLine, lines);
        }

        private static string CleanHtmlScalar(string value)
        {
            return CleanScalar(CleanHtmlText(value));
        }

        private static string CleanHtmlMultiline(string value)
        {
            return CleanMultiline(CleanHtmlText(value));
        }

        private static string CleanHtmlText(string value)
        {
            var text = Regex.Replace(value ?? string.Empty, @"(?i)<BR\s*/?>", "\n");
            text = Regex.Replace(text, @"(?i)</?XMP>", string.Empty);
            text = Regex.Replace(text, @"(?is)</?(?:FONT|B|I|U|SPAN|P)\b[^>]*>", string.Empty);
            return text;
        }

        private sealed class StepHeaderInfo
        {
            public string Name { get; set; } = string.Empty;

            public int IndentLevel { get; set; }

            public string RawHtml { get; set; } = string.Empty;
        }
    }
}
