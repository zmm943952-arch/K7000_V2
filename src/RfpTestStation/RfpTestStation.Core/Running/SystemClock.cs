using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;

namespace RfpTestStation.Core.Running
{
    public sealed class SystemClock : IClock
    {
        public DateTimeOffset UtcNow
        {
            get { return DateTimeOffset.UtcNow; }
        }

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(delay, cancellationToken);
        }
    }
}
