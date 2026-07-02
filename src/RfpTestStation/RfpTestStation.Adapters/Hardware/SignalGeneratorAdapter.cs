using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using SG;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class SignalGeneratorAdapter : ISignalGeneratorAdapter
    {
        public string AdapterName
        {
            get { return "SignalGenerator"; }
        }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            try
            {
                var text = AdapterStepParser.Text(step);
                var comPort = AdapterStepParser.QuotedStrings(step).Count > 0 ? AdapterStepParser.QuotedStrings(step)[0] : "COM13";

                if (text.IndexOf("ConfigureArbBurstAndOutput", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    TestStandApi.ConfigureArbBurstAndOutput(comPort);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "ConfigureArbBurstAndOutput(" + comPort + ")"));
                }

                if (text.IndexOf("RecallMemoryAndOutput", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var slot = AdapterStepParser.ArgumentsFor(step, "RecallMemoryAndOutput").Count > 1
                        ? AdapterStepParser.ResolveIntLiteral(AdapterStepParser.ArgumentsFor(step, "RecallMemoryAndOutput")[1], 0)
                        : 0;
                    TestStandApi.RecallMemoryAndOutput(comPort, slot);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "RecallMemoryAndOutput(" + comPort + ", " + slot + ")"));
                }

                return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "No signal generator action matched."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HardwareStepResult.Error(step, AdapterName, ex));
            }
        }
    }
}
