using System.Collections.Generic;

namespace RfpTestStation.Core.Workflow
{
    public sealed class WorkflowRunContext
    {
        public const string RunHasBlockingFailureKey = "RunHasBlockingFailure";

        public string SerialNumber { get; set; } = string.Empty;

        public string Mode { get; set; } = string.Empty;

        public IDictionary<string, object?> Values { get; } = new Dictionary<string, object?>();
    }
}
