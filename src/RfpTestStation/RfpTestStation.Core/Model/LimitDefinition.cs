namespace RfpTestStation.Core.Model
{
    public sealed class LimitDefinition
    {
        public string? Name { get; set; }

        public double? Low { get; set; }

        public double? High { get; set; }

        public string? ExpectedString { get; set; }

        public string? Unit { get; set; }
    }
}
