using System;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Adapters.Hardware
{
    internal static class HardwareStepResult
    {
        public static StepResult Passed(StepDefinition step, string adapterName, object? value = null, string? message = null)
        {
            return new StepResult
            {
                StepName = step.Name,
                Status = StepStatus.Passed,
                Value = value,
                Message = message ?? adapterName
            };
        }

        public static StepResult Failed(StepDefinition step, string adapterName, object? value = null, string? message = null)
        {
            return new StepResult
            {
                StepName = step.Name,
                Status = StepStatus.Failed,
                Value = value,
                Message = message ?? adapterName
            };
        }

        public static StepResult Error(StepDefinition step, string adapterName, Exception exception)
        {
            return new StepResult
            {
                StepName = step.Name,
                Status = StepStatus.Error,
                Message = adapterName + ": " + exception.Message,
                Error = exception
            };
        }
    }
}
