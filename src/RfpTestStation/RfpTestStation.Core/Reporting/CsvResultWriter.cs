using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Reporting
{
    public sealed class CsvResultWriter
    {
        public string Write(string directory, RunReport report)
        {
            var path = ReportFileNames.Build(directory, report, "csv");
            var builder = new StringBuilder();
            AppendRunHeader(builder, report);
            builder.AppendLine("Step,Status,Measurement,Expected Value,Units,Low Limit,High Limit,Comparison Type,Target,Sent,Reply,Reason,StartTime,EndTime");

            foreach (var result in report.StepResults)
            {
                builder.AppendLine(string.Join(",", new[]
                {
                    Escape(result.StepName),
                    Escape(result.Status.ToString()),
                    Escape(FormatValue(result.Value)),
                    Escape(FormatValue(result.ExpectedValue)),
                    Escape(result.Unit),
                    Escape(FormatNullable(result.LowLimit)),
                    Escape(FormatNullable(result.HighLimit)),
                    Escape(result.CompareType),
                    Escape(result.Target),
                    Escape(result.Sent),
                    Escape(result.Reply),
                    Escape(result.Message),
                    Escape(FormatTime(result.StartTime)),
                    Escape(FormatTime(result.EndTime))
                }));
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            return path;
        }

        private static void AppendRunHeader(StringBuilder builder, RunReport report)
        {
            builder.AppendLine("[SN]," + Escape(report.SerialNumber));
            builder.AppendLine("[Result]," + (report.Passed ? "Passed" : "Failed"));
            builder.AppendLine("[Start Time]," + Escape(FormatStationTime(report.StartedAt)));
            builder.AppendLine("[End Time]," + Escape(FormatStationTime(report.FinishedAt)));
            builder.AppendLine("[Test Time]," + FormatDurationSeconds(report.StartedAt, report.FinishedAt));
            builder.AppendLine("[UserName]," + Escape(report.Operator));
            builder.AppendLine("[Authority]," + Escape(string.IsNullOrWhiteSpace(report.Operator) ? string.Empty : "Operator"));
            builder.AppendLine("[Station]," + Escape(report.Station));
        }

        private static string FormatValue(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is IFormattable formattable)
            {
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            }

            if (value is byte[] bytes)
            {
                return string.Join(" ", bytes.Select(x => x.ToString("X2", CultureInfo.InvariantCulture)));
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string FormatNullable(double? value)
        {
            return value.HasValue ? value.Value.ToString("G", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string FormatTime(DateTimeOffset value)
        {
            return value == default ? string.Empty : value.ToString("o", CultureInfo.InvariantCulture);
        }

        private static string FormatStationTime(DateTimeOffset value)
        {
            return value == default ? string.Empty : value.ToString("yyyy_MM_dd  HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static string FormatDurationSeconds(DateTimeOffset startTime, DateTimeOffset endTime)
        {
            if (startTime == default || endTime == default)
            {
                return string.Empty;
            }

            return (endTime - startTime).TotalSeconds.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private static string Escape(string? value)
        {
            var text = value ?? string.Empty;
            if (text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return text;
            }

            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
    }
}
