using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RfpTestStation.Core.TestPlans
{
    public static class TestPlanRepository
    {
        public static TestPlanDefinition Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Test plan path is required.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Test plan file not found.", path);
            }

            var json = File.ReadAllText(path);
            var plan = JsonConvert.DeserializeObject<TestPlanDefinition>(
                json,
                new JsonSerializerSettings
                {
                    Converters = { new StringEnumConverter() }
                });
            if (plan == null)
            {
                throw new TestPlanValidationException("Test plan JSON is empty or invalid.");
            }

            plan.SourcePath = path;
            Validate(plan);
            return plan;
        }

        public static void Save(TestPlanDefinition plan, string path)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Test plan path is required.", nameof(path));
            }

            Validate(plan);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonConvert.SerializeObject(
                plan,
                Formatting.Indented,
                new JsonSerializerSettings
                {
                    Converters = { new StringEnumConverter() }
                });
            File.WriteAllText(path, json);
            plan.SourcePath = path;
        }

        private static void Validate(TestPlanDefinition plan)
        {
            Require(plan.Name, "Test plan name is required.");
            Require(plan.Version, "Test plan version is required.");
            Require(plan.Product, "Test plan product is required.");

            if (plan.Items.Count == 0)
            {
                throw new TestPlanValidationException("Test plan must contain at least one item.");
            }

            for (var i = 0; i < plan.Items.Count; i++)
            {
                var item = plan.Items[i];
                var prefix = "Test plan item " + (i + 1) + ": ";
                Require(item.Id, prefix + "id is required.");
                Require(item.Name, prefix + "name is required.");
                if (item.TimeoutSeconds <= 0)
                {
                    throw new TestPlanValidationException(prefix + "timeoutSeconds must be greater than zero.");
                }
            }
        }

        private static void Require(string value, string message)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new TestPlanValidationException(message);
            }
        }
    }
}
