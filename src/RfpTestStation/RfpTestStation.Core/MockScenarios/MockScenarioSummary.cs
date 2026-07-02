namespace RfpTestStation.Core.MockScenarios
{
    public sealed class MockScenarioSummary
    {
        public MockScenarioSummary(string name, string path)
        {
            Name = name;
            Path = path;
        }

        public string Name { get; }

        public string Path { get; }
    }
}
