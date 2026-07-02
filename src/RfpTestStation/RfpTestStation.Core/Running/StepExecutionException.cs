using System;

namespace RfpTestStation.Core.Running
{
    public sealed class StepExecutionException : Exception
    {
        public StepExecutionException(string stepName, string message, Exception innerException)
            : base("Step '" + stepName + "' failed: " + message, innerException)
        {
            StepName = stepName;
        }

        public string StepName { get; }
    }
}
