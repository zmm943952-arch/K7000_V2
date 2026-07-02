using System;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Workflow
{
    public sealed class TestItemResult
    {
        public string ItemId { get; set; } = string.Empty;

        public string ItemName { get; set; } = string.Empty;

        public TestItemKind Kind { get; set; }

        public string SourceReference { get; set; } = string.Empty;

        public StepStatus Status { get; set; }

        public object? Value { get; set; }

        public object? ExpectedValue { get; set; }

        public string? CompareType { get; set; }

        public string? Target { get; set; }

        public string? Sent { get; set; }

        public string? Reply { get; set; }

        public string? Unit { get; set; }

        public double? LowLimit { get; set; }

        public double? HighLimit { get; set; }

        public string? Message { get; set; }

        public string? ExternalLogPath { get; set; }

        public DateTimeOffset StartTime { get; set; }

        public DateTimeOffset EndTime { get; set; }

        public Exception? Error { get; set; }

        public static TestItemResult Passed(TestItem item, string? message = null)
        {
            return FromItem(item, StepStatus.Passed, message, null);
        }

        public static TestItemResult Failed(TestItem item, string? message = null)
        {
            return FromItem(item, StepStatus.Failed, message, null);
        }

        public static TestItemResult FromError(TestItem item, Exception error, string? message = null)
        {
            return FromItem(item, StepStatus.Error, message ?? error.Message, error);
        }

        public static TestItemResult Stopped(TestItem item, string? message = null)
        {
            return FromItem(item, StepStatus.Stopped, message, null);
        }

        private static TestItemResult FromItem(TestItem item, StepStatus status, string? message, Exception? error)
        {
            return new TestItemResult
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Kind = item.Kind,
                SourceReference = item.SourceReference,
                Status = status,
                Message = message,
                Error = error
            };
        }
    }
}
