using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class HardwareUnsupportedAdapter :
        IPowerSupplyAdapter,
        ISerialNumberAdapter,
        IMesAdapter,
        IPlcAdapter
    {
        public HardwareUnsupportedAdapter(string adapterName, string reason)
        {
            AdapterName = adapterName;
            _reason = reason;
        }

        private readonly string _reason;

        public string AdapterName { get; }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StepResult
            {
                StepName = step.Name,
                Status = StepStatus.Error,
                Message = "PLACEHOLDER: " + AdapterName + " is not implemented yet. " + _reason
            });
        }
    }
}
