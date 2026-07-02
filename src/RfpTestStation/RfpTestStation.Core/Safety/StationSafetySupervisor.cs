using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;

namespace RfpTestStation.Core.Safety
{
    public sealed class StationSafetySupervisor
    {
        private readonly IStationIoController _io;
        private readonly SafetyOptions _options;

        public StationSafetySupervisor(IStationIoController io, SafetyOptions options)
        {
            _io = io ?? throw new ArgumentNullException(nameof(io));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public event EventHandler<SafetyTrigger>? Triggered;

        public SafetyTrigger? LastTrigger { get; private set; }

        public bool IsLatched { get; private set; }

        public async Task RunAsync(CancellationTokenSource runCancellation, CancellationToken monitorCancellation)
        {
            if (runCancellation == null)
            {
                throw new ArgumentNullException(nameof(runCancellation));
            }

            if (!_options.Enabled)
            {
                return;
            }

            try
            {
                while (!runCancellation.IsCancellationRequested && !monitorCancellation.IsCancellationRequested)
                {
                    var trigger = await ReadTriggerAsync(monitorCancellation).ConfigureAwait(false);
                    if (trigger != null)
                    {
                        LastTrigger = trigger;
                        IsLatched = _options.OnTrigger.LatchUntilManualReset;
                        await ReleaseFixtureAsync(monitorCancellation).ConfigureAwait(false);
                        if (_options.OnTrigger.CancelRun)
                        {
                            runCancellation.Cancel();
                        }

                        Triggered?.Invoke(this, trigger);
                        return;
                    }

                    await DelayAsync(_options.PollIntervalMs, monitorCancellation).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        public void ResetLatch()
        {
            IsLatched = false;
            LastTrigger = null;
        }

        private async Task<SafetyTrigger?> ReadTriggerAsync(CancellationToken cancellationToken)
        {
            var emergencyValue = await _io.ReadInputAsync(_options.EmergencyStop.Channel, cancellationToken).ConfigureAwait(false);
            if (emergencyValue == _options.EmergencyStop.TriggeredValue)
            {
                return new SafetyTrigger(SafetyTriggerKind.EmergencyStop, _options.EmergencyStop.Channel, emergencyValue);
            }

            var lightCurtainValue = await _io.ReadInputAsync(_options.LightCurtain.Channel, cancellationToken).ConfigureAwait(false);
            if (lightCurtainValue == _options.LightCurtain.TriggeredValue)
            {
                return new SafetyTrigger(SafetyTriggerKind.LightCurtain, _options.LightCurtain.Channel, lightCurtainValue);
            }

            return null;
        }

        private async Task ReleaseFixtureAsync(CancellationToken cancellationToken)
        {
            var release = _options.ReleaseFixture;
            await _io.WriteOutputAsync(release.DownOutputChannel, release.DownSafeValue, cancellationToken).ConfigureAwait(false);
            await _io.WriteOutputAsync(release.UpOutputChannel, release.UpPreValue, cancellationToken).ConfigureAwait(false);
            await DelayAsync(release.UpDelayMs, cancellationToken).ConfigureAwait(false);
            await _io.WriteOutputAsync(release.UpOutputChannel, release.UpSafeValue, cancellationToken).ConfigureAwait(false);
        }

        private static Task DelayAsync(int delayMs, CancellationToken cancellationToken)
        {
            return delayMs <= 0
                ? Task.CompletedTask
                : Task.Delay(delayMs, cancellationToken);
        }
    }
}
