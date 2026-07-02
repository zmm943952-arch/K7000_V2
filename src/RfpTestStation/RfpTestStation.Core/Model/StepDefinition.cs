using System.Collections.Generic;

namespace RfpTestStation.Core.Model
{
    public sealed class StepDefinition
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public StepType StepType { get; set; } = StepType.Unknown;

        public string? StepTypeRaw { get; set; }

        public string? AdapterName { get; set; }

        public RunMode RunMode { get; set; } = RunMode.Normal;

        public bool IsNewThread { get; set; }

        public string? DescriptionRaw { get; set; }

        public string? SettingsRaw { get; set; }

        public string? FlowPropertiesRaw { get; set; }

        public string? PreExpression { get; set; }

        public string? PostExpression { get; set; }

        public string? ConditionExpression { get; set; }

        public int IndentLevel { get; set; }

        public string? SectionName { get; set; }

        public ModuleCallDefinition? ModuleCall { get; set; }

        public IList<LimitDefinition> Limits { get; } = new List<LimitDefinition>();

        public IList<StepDefinition> Children { get; } = new List<StepDefinition>();

        public string? RawHtml { get; set; }
    }
}
