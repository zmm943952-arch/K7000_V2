using System;
using Newtonsoft.Json.Linq;

namespace RfpTestStation.Core.Workflow
{
    public sealed class TestItem
    {
        public TestItem(string id, string name, TestItemKind kind)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Test item id is required.", nameof(id));
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Test item name is required.", nameof(name));
            }

            Id = id;
            Name = name;
            Kind = kind;
        }

        public string Id { get; }

        public string Name { get; }

        public TestItemKind Kind { get; }

        public string SourceReference { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = true;

        public bool StopOnFailure { get; set; } = true;

        public TimeSpan Timeout { get; set; } = TimeSpan.Zero;

        public JObject Parameters { get; set; } = new JObject();
    }
}
