using System;
using System.Threading;
using System.Threading.Tasks;
using Oscil;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class OscilloscopeAdapter : IOscilloscopeAdapter, IDisposable
    {
        private ScopeMeasurementClient? _client;

        public string AdapterName
        {
            get { return "Oscilloscope"; }
        }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            try
            {
                var text = AdapterStepParser.Text(step);
                if (text.IndexOf("ScopeMeasurementClient", StringComparison.OrdinalIgnoreCase) >= 0
                    && text.IndexOf("Connect()", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var quoted = AdapterStepParser.QuotedStrings(step);
                    var host = quoted.Count > 0 ? quoted[0] : "192.168.1.13";
                    var args = AdapterStepParser.ArgumentsFor(step, "ScopeMeasurementClient");
                    var port = args.Count > 1 ? AdapterStepParser.ResolveIntLiteral(args[1], 5555) : 5555;
                    var timeout = args.Count > 2 ? AdapterStepParser.ResolveIntLiteral(args[2], 3000) : 3000;
                    _client = new ScopeMeasurementClient(host, port, timeout);
                    _client.Connect();
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "Connected " + host + ":" + port));
                }

                if (text.IndexOf("ReadVavg", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var channel = AdapterStepParser.FirstIntArgument(step, "ReadVavg", executionContext, 1);
                    var value = TestStandApi.ReadVavg(channel);
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), value);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value, "ReadVavg(" + channel + ")"));
                }

                if (text.IndexOf("ReadFrequencyAndDutyCycle", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var channel = AdapterStepParser.FirstIntArgument(step, "ReadFrequencyAndDutyCycle", executionContext, 1);
                    double frequency;
                    double dutyCycle;
                    TestStandApi.ReadFrequencyAndDutyCycle(channel, out frequency, out dutyCycle);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, frequency, "DutyCycle=" + dutyCycle));
                }

                if (text.IndexOf("ReadFrequency", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var channel = AdapterStepParser.FirstIntArgument(step, "ReadFrequency", executionContext, 1);
                    var value = TestStandApi.ReadFrequency(channel);
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), value);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value, "ReadFrequency(" + channel + ")"));
                }

                if (text.IndexOf("ReadDutyCycle", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var channel = AdapterStepParser.FirstIntArgument(step, "ReadDutyCycle", executionContext, 1);
                    var value = TestStandApi.ReadDutyCycle(channel);
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), value);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value, "ReadDutyCycle(" + channel + ")"));
                }

                return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "No oscilloscope action matched."));
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
    }
}
