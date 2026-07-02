using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Workflow
{
    public static class MigrationWorkflowBuilder
    {
        public static MigrationWorkflowBuildResult Build(SequenceDocument document, string sequenceName = "MainSequence")
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var sequence = document.GetSequence(sequenceName);
            var items = new List<TestItem>();
            var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var mappedStepIds = new HashSet<int>();

            foreach (var step in sequence.AllSteps)
            {
                var item = TryCreateItem(sequence, step, itemIds);
                if (item == null)
                {
                    continue;
                }

                items.Add(item);
                mappedStepIds.Add(step.Id);
            }

            var allSteps = document.Sequences
                .SelectMany(x => x.AllSteps.Select(step => new StepSource(x, step)))
                .ToList();
            var unmappedProductionSteps = allSteps
                .Where(x => !mappedStepIds.Contains(x.Step.Id))
                .Where(x => !IsReferenceOnly(x.Step))
                .Where(x => IsProductionLooking(x.Step))
                .Select(x => SourceReference(x.Sequence, x.Step))
                .ToList();
            var summary = new MigrationWorkflowSummary(
                importedStepCount: allSteps.Count,
                mappedFunctionalItemCount: items.Count,
                referenceOnlyStepCount: allSteps.Count - items.Count - unmappedProductionSteps.Count,
                unmappedProductionLookingSteps: unmappedProductionSteps);

            return new MigrationWorkflowBuildResult(items, summary);
        }

        private static TestItem? TryCreateItem(
            SequenceDefinition sequence,
            StepDefinition step,
            ISet<string> itemIds)
        {
            if (step.RunMode == RunMode.Skip)
            {
                return null;
            }

            var searchable = SearchableText(step);
            string? id = null;
            string? name = null;
            var kind = TestItemKind.Measurement;

            if (Contains(searchable, "MCU简易"))
            {
                id = "flash.mcu.simple";
                name = "FlashMcuSimple";
                kind = TestItemKind.Flash;
            }
            else if (Contains(searchable, "MCU出货"))
            {
                id = "flash.mcu.shipping";
                name = "FlashMcuShipping";
                kind = TestItemKind.Flash;
            }
            else if (Contains(searchable, "RedCase_FlashUpdate_Run") || Contains(searchable, "TCON"))
            {
                id = "flash.tcon";
                name = "FlashTcon";
                kind = TestItemKind.Flash;
            }
            else if (Contains(searchable, "TDDI_Flash_once") || Contains(searchable, "TDDI"))
            {
                id = "flash.tddi";
                name = "FlashTddi";
                kind = TestItemKind.Flash;
            }
            else if (Contains(searchable, "气缸到位"))
            {
                id = "safety.fixture-position." + step.Id.ToString("D4");
                name = "SafetyWaitFixturePosition";
                kind = TestItemKind.SafetyCheck;
            }
            else if (Contains(searchable, "USB2IICDll"))
            {
                id = "fct.i2c." + step.Id.ToString("D4");
                name = "FctI2cCheck";
                kind = TestItemKind.Measurement;
            }
            else if (Contains(searchable, "Oscil.") || Contains(searchable, "ScopeMeasurementClient") || Contains(searchable, "ReadVavg"))
            {
                id = "fct.oscilloscope." + step.Id.ToString("D4");
                name = "FctOscilloscopeCheck";
                kind = TestItemKind.Measurement;
            }
            else if (Contains(searchable, "AC_Input"))
            {
                id = "fct.ac-input." + step.Id.ToString("D4");
                name = "FctAcInputCheck";
                kind = TestItemKind.LimitCheck;
            }
            else if (string.Equals(step.SectionName, "Setup", StringComparison.OrdinalIgnoreCase)
                && step.StepType == StepType.Action
                && (Contains(searchable, "Connect()") || Contains(searchable, "JsonConfigReader") || Contains(searchable, "ModbusRtu")))
            {
                id = "fixture.prepare." + step.Id.ToString("D4");
                name = "FixturePrepare";
                kind = TestItemKind.FixturePrepare;
            }
            else if (string.Equals(step.SectionName, "Cleanup", StringComparison.OrdinalIgnoreCase))
            {
                id = "cleanup." + step.Id.ToString("D4");
                name = "Cleanup";
                kind = TestItemKind.Cleanup;
            }

            if (id == null || name == null || !itemIds.Add(id))
            {
                return null;
            }

            return new TestItem(id, name, kind)
            {
                IsRequired = true,
                SourceReference = SourceReference(sequence, step)
            };
        }

        private static bool IsReferenceOnly(StepDefinition step)
        {
            if (step.RunMode == RunMode.Skip)
            {
                return true;
            }

            switch (step.StepType)
            {
                case StepType.If:
                case StepType.Else:
                case StepType.ElseIf:
                case StepType.While:
                case StepType.For:
                case StepType.ForEach:
                case StepType.End:
                case StepType.Wait:
                case StepType.Statement:
                case StepType.Label:
                case StepType.Goto:
                case StepType.MessagePopup:
                case StepType.SequenceCall:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsProductionLooking(StepDefinition step)
        {
            switch (step.StepType)
            {
                case StepType.Action:
                case StepType.PassFailTest:
                case StepType.NumericLimitTest:
                case StepType.MultipleNumericLimitTest:
                case StepType.StringValueTest:
                    return true;
                default:
                    return false;
            }
        }

        private static string SourceReference(SequenceDefinition sequence, StepDefinition step)
        {
            var detail = FirstNonEmpty(
                step.ModuleCall == null ? null : step.ModuleCall.RawText,
                step.DescriptionRaw,
                step.ConditionExpression,
                step.PreExpression);
            var reference = sequence.Name + "/" + (step.SectionName ?? "?") + "/Step " + step.Id + ": " + step.Name;
            if (!string.IsNullOrWhiteSpace(detail))
            {
                reference += " | " + OneLine(detail!);
            }

            return reference;
        }

        private static string SearchableText(StepDefinition step)
        {
            return string.Join(
                "\n",
                new[]
                {
                    step.Name,
                    step.SectionName,
                    step.StepTypeRaw,
                    step.AdapterName,
                    step.DescriptionRaw,
                    step.SettingsRaw,
                    step.FlowPropertiesRaw,
                    step.PreExpression,
                    step.PostExpression,
                    step.ConditionExpression,
                    step.ModuleCall == null ? null : step.ModuleCall.RawText,
                    step.ModuleCall == null ? null : step.ModuleCall.ViPath,
                    step.ModuleCall == null ? null : step.ModuleCall.TypeName,
                    step.ModuleCall == null ? null : step.ModuleCall.MethodName
                }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static bool Contains(string value, string expected)
        {
            return value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
        }

        private static string OneLine(string value)
        {
            return string.Join(" ", value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()));
        }

        private sealed class StepSource
        {
            public StepSource(SequenceDefinition sequence, StepDefinition step)
            {
                Sequence = sequence;
                Step = step;
            }

            public SequenceDefinition Sequence { get; }

            public StepDefinition Step { get; }
        }
    }

    public sealed class MigrationWorkflowBuildResult
    {
        public MigrationWorkflowBuildResult(IEnumerable<TestItem> items, MigrationWorkflowSummary summary)
        {
            Items = new ReadOnlyCollection<TestItem>(new List<TestItem>(items));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
        }

        public IReadOnlyList<TestItem> Items { get; }

        public MigrationWorkflowSummary Summary { get; }
    }

    public sealed class MigrationWorkflowSummary
    {
        public MigrationWorkflowSummary(
            int importedStepCount,
            int mappedFunctionalItemCount,
            int referenceOnlyStepCount,
            IEnumerable<string> unmappedProductionLookingSteps)
        {
            ImportedStepCount = importedStepCount;
            MappedFunctionalItemCount = mappedFunctionalItemCount;
            ReferenceOnlyStepCount = referenceOnlyStepCount;
            UnmappedProductionLookingSteps = new ReadOnlyCollection<string>(new List<string>(unmappedProductionLookingSteps));
        }

        public int ImportedStepCount { get; }

        public int MappedFunctionalItemCount { get; }

        public int ReferenceOnlyStepCount { get; }

        public IReadOnlyList<string> UnmappedProductionLookingSteps { get; }

        public int UnmappedProductionLookingStepCount
        {
            get { return UnmappedProductionLookingSteps.Count; }
        }

        public string ToText()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Imported steps: " + ImportedStepCount);
            builder.AppendLine("Mapped functional items: " + MappedFunctionalItemCount);
            builder.AppendLine("Reference-only steps: " + ReferenceOnlyStepCount);
            builder.AppendLine("Unmapped production-looking steps: " + UnmappedProductionLookingStepCount);
            foreach (var step in UnmappedProductionLookingSteps.Take(20))
            {
                builder.AppendLine("- " + step);
            }

            return builder.ToString();
        }
    }
}
