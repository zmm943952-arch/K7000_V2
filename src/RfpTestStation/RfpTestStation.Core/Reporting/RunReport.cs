using System;
using System.Collections.Generic;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.Core.Reporting
{
    public sealed class RunReport
    {
        public string SerialNumber { get; set; } = string.Empty;

        public string TestPlanName { get; set; } = string.Empty;

        public string TestPlanPath { get; set; } = string.Empty;

        public string ConfigPath { get; set; } = string.Empty;

        public string ExecutionMode { get; set; } = string.Empty;

        public string Operator { get; set; } = string.Empty;

        public string Station { get; set; } = string.Empty;

        public DateTimeOffset StartedAt { get; set; }

        public DateTimeOffset FinishedAt { get; set; }

        public bool Passed { get; set; }

        public IList<TestItem> StepItems { get; } = new List<TestItem>();

        public IList<StepResult> StepResults { get; } = new List<StepResult>();
    }
}
