namespace RfpTestStation.Core.Preflight
{
    public sealed class PreflightCheckResult
    {
        public PreflightCheckResult(string name, bool passed, string message)
        {
            Name = name;
            Passed = passed;
            Message = message;
        }

        public string Name { get; }

        public bool Passed { get; }

        public string Message { get; }
    }
}
