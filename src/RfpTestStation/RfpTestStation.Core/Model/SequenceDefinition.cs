using System.Collections.Generic;

namespace RfpTestStation.Core.Model
{
    public sealed class SequenceDefinition
    {
        public string Name { get; set; } = string.Empty;

        public IDictionary<string, object> Locals { get; } = new Dictionary<string, object>();

        public IList<StepDefinition> SetupSteps { get; } = new List<StepDefinition>();

        public IList<StepDefinition> MainSteps { get; } = new List<StepDefinition>();

        public IList<StepDefinition> CleanupSteps { get; } = new List<StepDefinition>();

        public IList<StepDefinition> AllSteps { get; } = new List<StepDefinition>();
    }
}
