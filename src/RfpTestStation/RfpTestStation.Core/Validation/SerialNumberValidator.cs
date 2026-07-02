using System.Text.RegularExpressions;

namespace RfpTestStation.Core.Validation
{
    public static class SerialNumberValidator
    {
        public static string? Validate(string? serialNumber, bool required, string? pattern)
        {
            var value = serialNumber == null ? string.Empty : serialNumber.Trim();
            if (required && string.IsNullOrWhiteSpace(value))
            {
                return "Serial number is required.";
            }

            if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(pattern))
            {
                if (!Regex.IsMatch(value, pattern!))
                {
                    return "Serial number does not match required pattern: " + pattern;
                }
            }

            return null;
        }
    }
}
