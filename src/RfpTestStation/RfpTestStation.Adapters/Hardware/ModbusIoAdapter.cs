using System;
using System.Threading;
using System.Threading.Tasks;
using Fct.ModbusRtu;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class ModbusIoAdapter : IModbusIoAdapter, IStationIoController, IDisposable
    {
        private readonly string _defaultPortName;
        private readonly object _syncRoot = new object();
        private ModbusRtuClient? _client;

        public ModbusIoAdapter(string defaultPortName)
        {
            _defaultPortName = defaultPortName;
        }

        public string AdapterName
        {
            get { return "ModbusIo"; }
        }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            try
            {
                var text = AdapterStepParser.Text(step);
                if (text.IndexOf("Create(", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var port = AdapterStepParser.QuotedStrings(step).Count > 0 ? AdapterStepParser.QuotedStrings(step)[0] : _defaultPortName;
                    _client = ModbusRtuClient.Create(port);
                    _client.Connect();
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), _client);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "Connected " + port));
                }

                if (text.IndexOf("Disconnect()", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _client?.Disconnect();
                    _client = null;
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "Disconnected"));
                }

                if (text.IndexOf("WriteOutputChannel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var channel = AdapterStepParser.FirstIntArgument(step, "WriteOutputChannel", executionContext);
                    var value = AdapterStepParser.SecondBoolArgument(step, "WriteOutputChannel");
                    WriteOutputChannel(channel, value);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value, "WriteOutputChannel(" + channel + ", " + value + ")"));
                }

                if (text.IndexOf("ReadInputChannel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var channel = AdapterStepParser.FirstIntArgument(step, "ReadInputChannel", executionContext);
                    var value = ReadInputChannel(channel);
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), value);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value, "ReadInputChannel(" + channel + ")"));
                }

                return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "No Modbus action matched."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HardwareStepResult.Error(step, AdapterName, ex));
            }
        }

        public Task<bool> ReadInputAsync(int channel, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ReadInputChannel(channel));
        }

        public Task WriteOutputAsync(int channel, bool value, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteOutputChannel(channel, value);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            lock (_syncRoot)
            {
                _client?.Dispose();
                _client = null;
            }
        }

        private bool ReadInputChannel(int channel)
        {
            lock (_syncRoot)
            {
                return EnsureClient().ReadInputChannel(channel);
            }
        }

        private void WriteOutputChannel(int channel, bool value)
        {
            lock (_syncRoot)
            {
                EnsureClient().WriteOutputChannel(channel, value);
            }
        }

        private ModbusRtuClient EnsureClient()
        {
            if (_client == null)
            {
                _client = ModbusRtuClient.Create(_defaultPortName);
                _client.Connect();
            }

            return _client;
        }
    }
}
