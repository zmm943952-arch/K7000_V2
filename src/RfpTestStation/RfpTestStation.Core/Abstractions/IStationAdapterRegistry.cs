using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Abstractions
{
    public interface IStationAdapterRegistry
    {
        IModbusIoAdapter ModbusIo { get; }

        IPowerSupplyAdapter PowerSupply { get; }

        IUsbI2cAdapter UsbI2c { get; }

        IDaqVoltageAdapter DaqVoltage { get; }

        IOscilloscopeAdapter Oscilloscope { get; }

        ISignalGeneratorAdapter SignalGenerator { get; }

        IConfigAdapter Config { get; }

        IFlashAdapter Flash { get; }

        ISerialNumberAdapter SerialNumber { get; }

        IMesAdapter Mes { get; }

        IPlcAdapter Plc { get; }

        IStationStepAdapter ResolveActionAdapter(StepDefinition step);
    }

    public interface IModbusIoAdapter : IStationStepAdapter
    {
    }

    public interface IStationIoController
    {
        System.Threading.Tasks.Task<bool> ReadInputAsync(int channel, System.Threading.CancellationToken cancellationToken);

        System.Threading.Tasks.Task WriteOutputAsync(int channel, bool value, System.Threading.CancellationToken cancellationToken);
    }

    public interface IPowerSupplyAdapter : IStationStepAdapter
    {
    }

    public interface IUsbI2cAdapter : IStationStepAdapter
    {
    }

    public interface IDaqVoltageAdapter : IStationStepAdapter
    {
    }

    public interface IOscilloscopeAdapter : IStationStepAdapter
    {
    }

    public interface ISignalGeneratorAdapter : IStationStepAdapter
    {
    }

    public interface IConfigAdapter : IStationStepAdapter
    {
    }

    public interface IFlashAdapter : IStationStepAdapter
    {
    }

    public interface ISerialNumberAdapter : IStationStepAdapter
    {
    }

    public interface IMesAdapter : IStationStepAdapter
    {
    }

    public interface IPlcAdapter : IStationStepAdapter
    {
    }
}
