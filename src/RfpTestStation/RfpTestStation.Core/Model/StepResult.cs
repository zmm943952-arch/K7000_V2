using System;

namespace RfpTestStation.Core.Model
{
    public sealed class StepResult
    {
        public string StepName { get; set; } = string.Empty;

        public StepStatus Status { get; set; }

        public object? Value { get; set; }

        public object? ExpectedValue { get; set; }

        public string? CompareType { get; set; }

        public string? Target { get; set; }

        public string? Unit { get; set; }

        public double? LowLimit { get; set; }

        public double? HighLimit { get; set; }

        public string? Message { get; set; }

        public string? ExternalLogPath { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public Exception? Error { get; set; }
    }
}
