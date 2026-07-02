using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.Core.TestPlans
{
    public sealed class TestPlanItemDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("kind")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TestItemKind Kind { get; set; }

        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; } = true;

        [JsonProperty("required")]
        public bool IsRequired { get; set; } = true;

        [JsonProperty("stopOnFailure")]
        public bool StopOnFailure { get; set; } = true;

        [JsonProperty("timeoutSeconds")]
        public int TimeoutSeconds { get; set; }

        [JsonProperty("sourceReference")]
        public string SourceReference { get; set; } = string.Empty;

        [JsonProperty("parameters")]
        public JObject Parameters { get; set; } = new JObject();
    }
}
