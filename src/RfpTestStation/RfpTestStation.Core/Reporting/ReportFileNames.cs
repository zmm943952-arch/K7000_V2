using System;
using System.IO;
using System.Text.RegularExpressions;

namespace RfpTestStation.Core.Reporting
{
    internal static class ReportFileNames
    {
        public static string Build(string directory, RunReport report, string extension)
        {
            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("Report directory is required.", nameof(directory));
            }

            Directory.CreateDirectory(directory);

            var serialNumber = SanitizeFileName(string.IsNullOrWhiteSpace(report.SerialNumber) ? "UNKNOWN" : report.SerialNumber);
            var result = report.Passed ? "Passed" : "Failed";
            var fileName = string.Format(
                "{0}_{1:yyyyMMddHHmmss}_{2}.{3}",
                serialNumber,
                report.FinishedAt == default(DateTimeOffset) ? report.StartedAt : report.FinishedAt,
                result,
                extension.TrimStart('.'));

            return Path.Combine(directory, fileName);
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            return Regex.Replace(value, "[" + invalid + "]", "_");
        }
    }
}
