using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fct.Acquire;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class DaqVoltageAdapter : IDaqVoltageAdapter, IDisposable
    {
        private readonly string _defaultPortName;
        private readonly byte _slaveId;
        private YkDaq20081Client? _client;

        public DaqVoltageAdapter(string defaultPortName, byte slaveId)
        {
            _defaultPortName = defaultPortName;
            _slaveId = slaveId;
        }

        public string AdapterName
        {
            get { return "DaqVoltage"; }
        }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            try
            {
                var text = AdapterStepParser.Text(step);
                if (text.IndexOf("Connect()", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var quoted = AdapterStepParser.QuotedStrings(step);
                    var port = quoted.Count > 0 ? quoted[0] : _defaultPortName;
                    _client = new YkDaq20081Client(port, _slaveId);
                    _client.Connect();
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "Connected " + port));
                }

                if (text.IndexOf("Disconnect()", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _client?.Disconnect();
                    _client = null;
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "Disconnected"));
                }

                var channel = ParseChannel(step);
                var value = EnsureClient().ReadVoltage(channel);
                AdapterStepParser.SetContextValue(executionContext, "Locals.AC_Input" + channel, value);
                return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value, "ReadVoltage(" + channel + ")"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HardwareStepResult.Error(step, AdapterName, ex));
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            _client = null;
        }

        private static int ParseChannel(StepDefinition step)
        {
            var match = Regex.Match(step.Name + " " + AdapterStepParser.Text(step), @"AC_Input(?<channel>\d+)", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return int.Parse(match.Groups["channel"].Value);
            }

            return 1;
        }

        private YkDaq20081Client EnsureClient()
        {
            if (_client == null)
            {
                _client = new YkDaq20081Client(_defaultPortName, _slaveId);
                _client.Connect();
            }

            return _client;
        }
    }
}
