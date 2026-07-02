using System;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Core.Running
{
    public sealed class NullStationAdapterRegistry : IStationAdapterRegistry
    {
        public static NullStationAdapterRegistry Instance { get; } = new NullStationAdapterRegistry();

        private readonly NullStationStepAdapter _adapter = new NullStationStepAdapter();

        private NullStationAdapterRegistry()
        {
        }

        public IModbusIoAdapter ModbusIo => _adapter;

        public IPowerSupplyAdapter PowerSupply => _adapter;

        public IUsbI2cAdapter UsbI2c => _adapter;

        public IDaqVoltageAdapter DaqVoltage => _adapter;

        public IOscilloscopeAdapter Oscilloscope => _adapter;

        public ISignalGeneratorAdapter SignalGenerator => _adapter;

        public IConfigAdapter Config => _adapter;

        public IFlashAdapter Flash => _adapter;

        public ISerialNumberAdapter SerialNumber => _adapter;

        public IMesAdapter Mes => _adapter;

        public IPlcAdapter Plc => _adapter;

        public IStationStepAdapter ResolveActionAdapter(StepDefinition step)
        {
            return _adapter;
        }

        private sealed class NullStationStepAdapter :
            IModbusIoAdapter,
            IPowerSupplyAdapter,
            IUsbI2cAdapter,
            IDaqVoltageAdapter,
            IOscilloscopeAdapter,
            ISignalGeneratorAdapter,
            IConfigAdapter,
            IFlashAdapter,
            ISerialNumberAdapter,
            IMesAdapter,
            IPlcAdapter
        {
            public string AdapterName => "Null";

            public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(new StepResult
                {
                    StepName = step.Name,
                    Status = StepStatus.Passed,
                    Message = "No station adapter registered."
                });
            }
        }
    }
}
