using System;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;

namespace RfpTestStation.Core.Reporting
{
    public sealed class RunLogWriter
    {
        public string Write(string directory, RunReport report)
        {
            var path = ReportFileNames.Build(directory, report, "log");
            var builder = new StringBuilder();

            AppendRunHeader(builder, report);
            var stepCount = Math.Max(report.StepItems.Count, report.StepResults.Count);
            for (var i = 0; i < stepCount; i++)
            {
                var item = i < report.StepItems.Count ? report.StepItems[i] : null;
                var result = i < report.StepResults.Count ? report.StepResults[i] : null;
                AppendStep(builder, i + 1, item, result);
            }

            AppendRunFooter(builder, report);
            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            return path;
        }

        private static void AppendRunHeader(StringBuilder builder, RunReport report)
        {
            builder.AppendLine("========== RUN START ==========");
            builder.AppendLine("SerialNumber: " + EmptyIfNull(report.SerialNumber));
            builder.AppendLine("Operator: " + EmptyIfNull(report.Operator));
            builder.AppendLine("ExecutionMode: " + EmptyIfNull(report.ExecutionMode));
            builder.AppendLine("TestPlan: " + EmptyIfNull(report.TestPlanName));
            builder.AppendLine("TestPlanPath: " + EmptyIfNull(report.TestPlanPath));
            builder.AppendLine("Config: " + EmptyIfNull(report.ConfigPath));
            builder.AppendLine("StartedAt: " + FormatTime(report.StartedAt));
            builder.AppendLine();
        }

        private static void AppendStep(StringBuilder builder, int index, TestItem? item, StepResult? result)
        {
            var startTime = result == null ? default(DateTimeOffset) : result.StartTime;
            var endTime = result == null ? default(DateTimeOffset) : result.EndTime;

            builder.AppendLine("[" + index.ToString(CultureInfo.InvariantCulture) + "] STEP START");
            builder.AppendLine("StartTime: " + FormatTime(startTime));
            if (item != null)
            {
                builder.AppendLine("Id: " + item.Id);
                builder.AppendLine("Name: " + item.Name);
                builder.AppendLine("Kind: " + item.Kind);
                builder.AppendLine("SourceReference: " + EmptyIfNull(item.SourceReference));
                builder.AppendLine("Required: " + item.IsRequired);
                builder.AppendLine("StopOnFailure: " + item.StopOnFailure);
                builder.AppendLine("TimeoutSeconds: " + item.Timeout.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture));
                builder.AppendLine("Parameters:");
                builder.AppendLine(item.Parameters.ToString(Formatting.Indented));
            }
            else if (result != null)
            {
                builder.AppendLine("Name: " + EmptyIfNull(result.StepName));
            }

            builder.AppendLine("[" + index.ToString(CultureInfo.InvariantCulture) + "] STEP END");
            builder.AppendLine("EndTime: " + FormatTime(endTime));
            builder.AppendLine("DurationMs: " + FormatDurationMs(startTime, endTime));
            if (result != null)
            {
                builder.AppendLine("Status: " + result.Status);
                builder.AppendLine("Value: " + FormatValue(result.Value));
                builder.AppendLine("ExpectedValue: " + FormatValue(result.ExpectedValue));
                builder.AppendLine("CompareType: " + EmptyIfNull(result.CompareType));
                builder.AppendLine("Target: " + EmptyIfNull(result.Target));
                builder.AppendLine("Unit: " + EmptyIfNull(result.Unit));
                builder.AppendLine("LowLimit: " + FormatNullable(result.LowLimit));
                builder.AppendLine("HighLimit: " + FormatNullable(result.HighLimit));
                builder.AppendLine("ExternalLogPath: " + EmptyIfNull(result.ExternalLogPath));
                builder.AppendLine("Message: " + EmptyIfNull(result.Message));
                if (result.Error != null)
                {
                    builder.AppendLine("Exception: " + result.Error);
                }
            }

            builder.AppendLine();
        }

        private static void AppendRunFooter(StringBuilder builder, RunReport report)
        {
            builder.AppendLine("========== RUN END ==========");
            builder.AppendLine("FinishedAt: " + FormatTime(report.FinishedAt));
            builder.AppendLine("OverallStatus: " + (report.Passed ? "Pass" : "Fail"));
        }

        private static string FormatTime(DateTimeOffset value)
        {
            return value == default(DateTimeOffset) ? string.Empty : value.ToString("o", CultureInfo.InvariantCulture);
        }

        private static string FormatDurationMs(DateTimeOffset startTime, DateTimeOffset endTime)
        {
            if (startTime == default(DateTimeOffset) || endTime == default(DateTimeOffset))
            {
                return string.Empty;
            }

            return Math.Round((endTime - startTime).TotalMilliseconds).ToString("0", CultureInfo.InvariantCulture);
        }

        private static string FormatNullable(double? value)
        {
            return value.HasValue ? value.Value.ToString("G", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            if (value is byte[] bytes)
            {
                return BitConverter.ToString(bytes).Replace("-", " ");
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string EmptyIfNull(string? value)
        {
            return value ?? string.Empty;
        }
    }
}
