using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RfpTestStation.Core.MockScenarios
{
    public sealed class MockScenarioDefinition
    {
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public IDictionary<string, JObject> Items { get; } = new Dictionary<string, JObject>(System.StringComparer.OrdinalIgnoreCase);
    }
}
