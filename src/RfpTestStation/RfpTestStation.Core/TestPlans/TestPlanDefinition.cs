using System.Collections.Generic;
using Newtonsoft.Json;

namespace RfpTestStation.Core.TestPlans
{
    public sealed class TestPlanDefinition
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("product")]
        public string Product { get; set; } = string.Empty;

        [JsonIgnore]
        public string SourcePath { get; set; } = string.Empty;

        [JsonProperty("items")]
        public IList<TestPlanItemDefinition> Items { get; } = new List<TestPlanItemDefinition>();
    }
}
