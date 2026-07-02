using System;
using System.Threading;
using System.Threading.Tasks;

namespace RfpTestStation.Core.Abstractions
{
    public interface IExternalProcessRunner
    {
        Task<ExternalProcessResult> RunAsync(ExternalProcessStartInfo startInfo, CancellationToken cancellationToken);
    }

    public sealed class ExternalProcessStartInfo
    {
        public string FileName { get; set; } = string.Empty;

        public string Arguments { get; set; } = string.Empty;

        public string WorkingDirectory { get; set; } = string.Empty;

        public TimeSpan? Timeout { get; set; }
    }

    public sealed class ExternalProcessResult
    {
        public int ExitCode { get; set; }

        public string StandardOutput { get; set; } = string.Empty;

        public string StandardError { get; set; } = string.Empty;

        public bool TimedOut { get; set; }

        public bool Cancelled { get; set; }
    }
}
