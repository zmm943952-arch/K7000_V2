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
                Sent = SentText(step),
                Reply = ReplyText(value, message ?? adapterName),
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
                Sent = SentText(step),
                Reply = ReplyText(value, message ?? adapterName),
                Message = message ?? adapterName
            };
        }

        public static StepResult Error(StepDefinition step, string adapterName, Exception exception)
        {
            return new StepResult
            {
                StepName = step.Name,
                Status = StepStatus.Error,
                Sent = SentText(step),
                Reply = exception.Message,
                Message = adapterName + ": " + exception.Message,
                Error = exception
            };
        }

        private static string SentText(StepDefinition step)
        {
            return step.DescriptionRaw ?? step.SettingsRaw ?? step.Name;
        }

        private static string ReplyText(object? value, string message)
        {
            return "Value=" + (value ?? string.Empty) + "; Message=" + message;
        }
    }
}
