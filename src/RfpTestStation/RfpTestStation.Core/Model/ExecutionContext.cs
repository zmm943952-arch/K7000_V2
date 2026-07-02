using System.Collections.Generic;

namespace RfpTestStation.Core.Model
{
    public sealed class ExecutionContext
    {
        public IDictionary<string, object> FileGlobals { get; } = new Dictionary<string, object>();

        public IDictionary<string, object> Locals { get; } = new Dictionary<string, object>();

        public IDictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        public IDictionary<string, object> RunState { get; } = new Dictionary<string, object>();

        public IList<StepResult> StepResults { get; } = new List<StepResult>();

        public bool SequenceFailed { get; set; }

        public bool StopRequested { get; set; }
    }
}
