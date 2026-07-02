using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Safety;
using Xunit;

namespace RfpTestStation.Tests.Safety
{
    public sealed class StationSafetySupervisorTests
    {
        [Fact]
        public async Task EmergencyStopReleasesFixtureAndCancelsRun()
        {
            var io = new FakeStationIoController();
            io.Inputs[4] = true;
            io.Inputs[5] = true;
            var options = SafetyOptions.Default();
            options.PollIntervalMs = 1;
            options.ReleaseFixture.UpDelayMs = 0;
            var runCancellation = new CancellationTokenSource();
            var supervisor = new StationSafetySupervisor(io, options);

            await supervisor.RunAsync(runCancellation, CancellationToken.None);

            Assert.True(runCancellation.IsCancellationRequested);
            Assert.NotNull(supervisor.LastTrigger);
            Assert.Equal(SafetyTriggerKind.EmergencyStop, supervisor.LastTrigger!.Kind);
            Assert.Equal(new[]
            {
                "Write 2=False",
                "Write 1=False",
                "Write 1=True"
            }, io.Writes);
        }

        [Fact]
        public async Task LightCurtainReleasesFixtureAndCancelsRun()
        {
            var io = new FakeStationIoController();
            io.Inputs[4] = false;
            io.Inputs[5] = false;
            var options = SafetyOptions.Default();
            options.PollIntervalMs = 1;
            options.ReleaseFixture.UpDelayMs = 0;
            var runCancellation = new CancellationTokenSource();
            var supervisor = new StationSafetySupervisor(io, options);

            await supervisor.RunAsync(runCancellation, CancellationToken.None);

            Assert.True(runCancellation.IsCancellationRequested);
            Assert.NotNull(supervisor.LastTrigger);
            Assert.Equal(SafetyTriggerKind.LightCurtain, supervisor.LastTrigger!.Kind);
            Assert.Equal(new[]
            {
                "Write 2=False",
                "Write 1=False",
                "Write 1=True"
            }, io.Writes);
        }

        [Fact]
        public async Task DisabledSafetyReturnsWithoutReadingIo()
        {
            var io = new FakeStationIoController();
            var options = SafetyOptions.Default();
            options.Enabled = false;
            var runCancellation = new CancellationTokenSource();
            var supervisor = new StationSafetySupervisor(io, options);

            await supervisor.RunAsync(runCancellation, CancellationToken.None);

            Assert.False(runCancellation.IsCancellationRequested);
            Assert.Empty(io.Reads);
            Assert.Empty(io.Writes);
        }

        private sealed class FakeStationIoController : IStationIoController
        {
            public Dictionary<int, bool> Inputs { get; } = new Dictionary<int, bool>();

            public List<int> Reads { get; } = new List<int>();

            public List<string> Writes { get; } = new List<string>();

            public Task<bool> ReadInputAsync(int channel, CancellationToken cancellationToken)
            {
                Reads.Add(channel);
                return Task.FromResult(Inputs.TryGetValue(channel, out var value) && value);
            }

            public Task WriteOutputAsync(int channel, bool value, CancellationToken cancellationToken)
            {
                Writes.Add("Write " + channel + "=" + value);
                return Task.CompletedTask;
            }
        }
    }
}
