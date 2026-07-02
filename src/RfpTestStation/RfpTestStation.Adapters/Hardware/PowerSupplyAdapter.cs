using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public interface IPowerSupplyTransport
    {
        Task SendAsync(
            string host,
            int port,
            TimeSpan timeout,
            IReadOnlyList<string> commands,
            CancellationToken cancellationToken);
    }

    public sealed class PowerSupplyAdapter : IPowerSupplyAdapter
    {
        private static readonly Regex StepNamePattern = new Regex(
            @"^(?<action>PowerOn|PowerOff)-Channel(?<channel>\d+)-(?<voltage>\d+(?:\.\d+)?)V$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly IPowerSupplyTransport _transport;
        private readonly string _host;
        private readonly int _port;
        private readonly TimeSpan _timeout;

        public PowerSupplyAdapter()
            : this(new TcpPowerSupplyTransport())
        {
        }

        public PowerSupplyAdapter(IPowerSupplyTransport transport)
            : this(transport, "192.168.1.15", 8088, TimeSpan.FromMilliseconds(2000))
        {
        }

        public PowerSupplyAdapter(IPowerSupplyTransport transport, string host, int port, TimeSpan timeout)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _host = string.IsNullOrWhiteSpace(host) ? throw new ArgumentException("Host is required.", nameof(host)) : host;
            _port = port;
            _timeout = timeout;
        }

        public string AdapterName
        {
            get { return "PowerSupply"; }
        }

        public async Task<StepResult> ExecuteAsync(
            StepDefinition step,
            StationExecutionContext executionContext,
            CancellationToken cancellationToken)
        {
            try
            {
                var request = ParseStepName(step);
                var commands = BuildCommands(request);
                await _transport
                    .SendAsync(_host, _port, _timeout, commands, cancellationToken)
                    .ConfigureAwait(false);

                return HardwareStepResult.Passed(
                    step,
                    AdapterName,
                    request.Action,
                    request.Action + " channel=" + request.Channel + "; voltage=" + request.Voltage);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return HardwareStepResult.Error(step, AdapterName, ex);
            }
        }

        private static PowerSupplyRequest ParseStepName(StepDefinition step)
        {
            var name = step == null ? string.Empty : step.Name ?? string.Empty;
            var match = StepNamePattern.Match(name);
            if (!match.Success)
            {
                throw new FormatException("Cannot parse power supply step name: " + name);
            }

            return new PowerSupplyRequest(
                match.Groups["action"].Value,
                int.Parse(match.Groups["channel"].Value),
                match.Groups["voltage"].Value);
        }

        private static IReadOnlyList<string> BuildCommands(PowerSupplyRequest request)
        {
            if (string.Equals(request.Action, "PowerOn", StringComparison.OrdinalIgnoreCase))
            {
                return new[]
                {
                    ":SOURce" + request.Channel + ":VOLTage " + request.Voltage + "\r\n",
                    ":OUTPut" + request.Channel + ":STATe ON\r\n"
                };
            }

            return new[]
            {
                ":OUTPut" + request.Channel + ":STATe OFF\r\n"
            };
        }

        private sealed class PowerSupplyRequest
        {
            public PowerSupplyRequest(string action, int channel, string voltage)
            {
                Action = action;
                Channel = channel;
                Voltage = voltage;
            }

            public string Action { get; }

            public int Channel { get; }

            public string Voltage { get; }
        }
    }

    public sealed class TcpPowerSupplyTransport : IPowerSupplyTransport
    {
        public async Task SendAsync(
            string host,
            int port,
            TimeSpan timeout,
            IReadOnlyList<string> commands,
            CancellationToken cancellationToken)
        {
            using (var client = new TcpClient())
            {
                var connectTask = client.ConnectAsync(host, port);
                var timeoutTask = Task.Delay(timeout, cancellationToken);
                var completed = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completed != connectTask)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    throw new TimeoutException("Timed out connecting to power supply " + host + ":" + port + ".");
                }

                await connectTask.ConfigureAwait(false);

                using (var stream = client.GetStream())
                {
                    foreach (var command in commands)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var bytes = Encoding.ASCII.GetBytes(command);
                        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
