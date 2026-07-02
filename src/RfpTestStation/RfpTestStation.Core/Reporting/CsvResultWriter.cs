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
            builder.AppendLine("StepName,Status,Value,LowLimit,HighLimit,Unit,StartTime,EndTime,Message");

            foreach (var result in report.StepResults)
            {
                builder.AppendLine(string.Join(",", new[]
                {
                    Escape(result.StepName),
                    Escape(result.Status.ToString()),
                    Escape(FormatValue(result.Value)),
                    Escape(FormatNullable(result.LowLimit)),
                    Escape(FormatNullable(result.HighLimit)),
                    Escape(result.Unit),
                    Escape(FormatTime(result.StartTime)),
                    Escape(FormatTime(result.EndTime)),
                    Escape(result.Message)
                }));
            }

            File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
            return path;
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
