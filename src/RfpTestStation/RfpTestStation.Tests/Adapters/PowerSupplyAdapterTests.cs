using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Adapters.Hardware;
using RfpTestStation.Core.Model;
using Xunit;

namespace RfpTestStation.Tests.Adapters
{
    public sealed class PowerSupplyAdapterTests
    {
        [Fact]
        public async Task PowerOnStepSendsVoltageThenOutputOnWithCrlf()
        {
            var transport = new CapturingPowerSupplyTransport();
            var adapter = new PowerSupplyAdapter(transport);

            var result = await adapter.ExecuteAsync(
                new StepDefinition { Name = "PowerOn-Channel1-12.2V", DescriptionRaw = "Action, PowerControl.vi" },
                new Core.Model.ExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("192.168.1.15", transport.Host);
            Assert.Equal(8088, transport.Port);
            Assert.Equal(TimeSpan.FromMilliseconds(2000), transport.Timeout);
            Assert.Collection(
                transport.Commands,
                command => Assert.Equal(":SOURce1:VOLTage 12.2\r\n", command),
                command => Assert.Equal(":OUTPut1:STATe ON\r\n", command));
        }

        [Fact]
        public async Task PowerOffStepSendsOnlyOutputOffWithCrlf()
        {
            var transport = new CapturingPowerSupplyTransport();
            var adapter = new PowerSupplyAdapter(transport);

            var result = await adapter.ExecuteAsync(
                new StepDefinition { Name = "PowerOff-Channel3-3.3V", DescriptionRaw = "Action, PowerControl.vi" },
                new Core.Model.ExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Collection(
                transport.Commands,
                command => Assert.Equal(":OUTPut3:STATe OFF\r\n", command));
        }

        [Fact]
        public async Task InvalidPowerStepNameReturnsErrorWithoutSending()
        {
            var transport = new CapturingPowerSupplyTransport();
            var adapter = new PowerSupplyAdapter(transport);

            var result = await adapter.ExecuteAsync(
                new StepDefinition { Name = "PowerOn", DescriptionRaw = "Action, PowerControl.vi" },
                new Core.Model.ExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Error, result.Status);
            Assert.Empty(transport.Commands);
            Assert.Contains("Cannot parse power supply step name", result.Message);
        }

        [Fact]
        public async Task TransportFailureReturnsError()
        {
            var transport = new CapturingPowerSupplyTransport
            {
                ExceptionToThrow = new InvalidOperationException("network failed")
            };
            var adapter = new PowerSupplyAdapter(transport);

            var result = await adapter.ExecuteAsync(
                new StepDefinition { Name = "PowerOn-Channel1-12.2V", DescriptionRaw = "Action, PowerControl.vi" },
                new Core.Model.ExecutionContext(),
                CancellationToken.None);

            Assert.Equal(StepStatus.Error, result.Status);
            Assert.Contains("network failed", result.Message);
        }

        private sealed class CapturingPowerSupplyTransport : IPowerSupplyTransport
        {
            public string Host { get; private set; } = string.Empty;

            public int Port { get; private set; }

            public TimeSpan Timeout { get; private set; }

            public List<string> Commands { get; } = new List<string>();

            public Exception? ExceptionToThrow { get; set; }

            public Task SendAsync(
                string host,
                int port,
                TimeSpan timeout,
                IReadOnlyList<string> commands,
                CancellationToken cancellationToken)
            {
                Host = host;
                Port = port;
                Timeout = timeout;
                Commands.AddRange(commands);

                if (ExceptionToThrow != null)
                {
                    throw ExceptionToThrow;
                }

                return Task.CompletedTask;
            }
        }
    }
}
