using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Workflow
{
    public sealed class WorkflowResult
    {
        public WorkflowResult(
            StepStatus status,
            IEnumerable<TestItemResult> results,
            DateTimeOffset startTime,
            DateTimeOffset endTime)
        {
            Status = status;
            Results = new ReadOnlyCollection<TestItemResult>(new List<TestItemResult>(results));
            StartTime = startTime;
            EndTime = endTime;
        }

        public StepStatus Status { get; }

        public bool Passed
        {
            get { return Status == StepStatus.Passed; }
        }

        public IReadOnlyList<TestItemResult> Results { get; }

        public DateTimeOffset StartTime { get; }

        public DateTimeOffset EndTime { get; }
    }
}
