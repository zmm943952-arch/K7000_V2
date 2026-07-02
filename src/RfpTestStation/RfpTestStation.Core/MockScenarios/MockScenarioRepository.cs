using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.Core.MockScenarios
{
    public sealed class MockScenarioRepository
    {
        private readonly string _directory;

        public MockScenarioRepository(string directory)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        }

        public IEnumerable<MockScenarioSummary> LoadAvailable()
        {
            if (!Directory.Exists(_directory))
            {
                return Enumerable.Empty<MockScenarioSummary>();
            }

            return Directory.GetFiles(_directory, "*.mockscenario.json")
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Select(path =>
                {
                    var scenario = Load(path);
                    return new MockScenarioSummary(
                        string.IsNullOrWhiteSpace(scenario.Name)
                            ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(path))
                            : scenario.Name,
                        path);
                })
                .ToList();
        }

        public int Apply(string path, IEnumerable<TestItem> items)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return 0;
            }

            var scenario = Load(path);
            var count = 0;
            foreach (var item in items)
            {
                JObject mock;
                if (!scenario.Items.TryGetValue(item.Id, out mock))
                {
                    continue;
                }

                item.Parameters["mock"] = mock.DeepClone();
                count++;
            }

            return count;
        }

        public MockScenarioDefinition Load(string path)
        {
            var root = JObject.Parse(File.ReadAllText(path));
            var scenario = new MockScenarioDefinition
            {
                Name = ReadString(root, "name"),
                Description = ReadString(root, "description")
            };

            var items = root["items"] as JObject;
            if (items == null)
            {
                return scenario;
            }

            foreach (var property in items.Properties())
            {
                var mock = property.Value as JObject;
                if (mock == null)
                {
                    throw new JsonException("Mock scenario item must be an object: " + property.Name);
                }

                scenario.Items[property.Name] = (JObject)mock.DeepClone();
            }

            return scenario;
        }

        private static string ReadString(JObject value, string name)
        {
            var token = value[name];
            return token == null || token.Type == JTokenType.Null ? string.Empty : token.ToString();
        }
    }
}
