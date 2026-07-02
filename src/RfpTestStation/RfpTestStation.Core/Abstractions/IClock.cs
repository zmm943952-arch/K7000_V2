using System;
using System.Threading;
using System.Threading.Tasks;

namespace RfpTestStation.Core.Abstractions
{
    public interface IClock
    {
        DateTimeOffset UtcNow { get; }

        Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
    }
}
