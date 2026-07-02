using System;
using System.Collections.Generic;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;

namespace RfpTestStation.Adapters.Mock
{
    public sealed class MockStationAdapterRegistry : IStationAdapterRegistry
    {
        public MockStationAdapterRegistry()
        {
            ModbusIoMock = new MockStationStepAdapter("ModbusIo");
            PowerSupplyMock = new MockStationStepAdapter("PowerSupply");
            UsbI2cMock = new MockStationStepAdapter("UsbI2c");
            DaqVoltageMock = new MockStationStepAdapter("DaqVoltage");
            OscilloscopeMock = new MockStationStepAdapter("Oscilloscope");
            SignalGeneratorMock = new MockStationStepAdapter("SignalGenerator");
            ConfigMock = new MockStationStepAdapter("Config");
            FlashMock = new MockStationStepAdapter("Flash");
            SerialNumberMock = new MockStationStepAdapter("SerialNumber");
            MesMock = new MockStationStepAdapter("Mes");
            PlcMock = new MockStationStepAdapter("Plc");
        }

        public MockStationStepAdapter ModbusIoMock { get; }

        public MockStationStepAdapter PowerSupplyMock { get; }

        public MockStationStepAdapter UsbI2cMock { get; }

        public MockStationStepAdapter DaqVoltageMock { get; }

        public MockStationStepAdapter OscilloscopeMock { get; }

        public MockStationStepAdapter SignalGeneratorMock { get; }

        public MockStationStepAdapter ConfigMock { get; }

        public MockStationStepAdapter FlashMock { get; }

        public MockStationStepAdapter SerialNumberMock { get; }

        public MockStationStepAdapter MesMock { get; }

        public MockStationStepAdapter PlcMock { get; }

        public IModbusIoAdapter ModbusIo => ModbusIoMock;

        public IPowerSupplyAdapter PowerSupply => PowerSupplyMock;

        public IUsbI2cAdapter UsbI2c => UsbI2cMock;

        public IDaqVoltageAdapter DaqVoltage => DaqVoltageMock;

        public IOscilloscopeAdapter Oscilloscope => OscilloscopeMock;

        public ISignalGeneratorAdapter SignalGenerator => SignalGeneratorMock;

        public IConfigAdapter Config => ConfigMock;

        public IFlashAdapter Flash => FlashMock;

        public ISerialNumberAdapter SerialNumber => SerialNumberMock;

        public IMesAdapter Mes => MesMock;

        public IPlcAdapter Plc => PlcMock;

        public IStationStepAdapter ResolveActionAdapter(StepDefinition step)
        {
            var text = ModuleStepClassifier.ModuleText(step);

            if (Contains(text, "PowerControl.vi"))
            {
                return PowerSupplyMock;
            }

            if (Contains(text, "ReadI2C.vi") || Contains(text, "WriteI2C.vi") || Contains(text, "USB2IICDll"))
            {
                return UsbI2cMock;
            }

            if (Contains(text, "ReadConfigLib") || Contains(text, "PreparedArtifactPaths"))
            {
                return ConfigMock;
            }

            if (Contains(text, "ReadVolt.vi") || Contains(text, "YkDaq"))
            {
                return DaqVoltageMock;
            }

            if (Contains(text, "Oscil"))
            {
                return OscilloscopeMock;
            }

            if (Contains(text, "SG.") || Contains(text, "SignalGenerator") || Contains(text, "Afg"))
            {
                return SignalGeneratorMock;
            }

            if (Contains(text, "Flash_once.bat.vi")
                || Contains(text, "FlashUpdate_Run.bat.vi")
                || Contains(text, "TDDI_Flash_once.bat.vi"))
            {
                return FlashMock;
            }

            if (Contains(text, "ReadSN.vi"))
            {
                return SerialNumberMock;
            }

            if (Contains(text, "MES"))
            {
                return MesMock;
            }

            if (Contains(text, "PLC"))
            {
                return PlcMock;
            }

            if (Contains(text, "ModbusRtu"))
            {
                return ModbusIoMock;
            }

            return ModbusIoMock;
        }

        public IReadOnlyList<string> CallsFor(string adapterName)
        {
            switch (adapterName)
            {
                case "ModbusIo":
                    return (IReadOnlyList<string>)ModbusIoMock.Calls;
                case "PowerSupply":
                    return (IReadOnlyList<string>)PowerSupplyMock.Calls;
                case "UsbI2c":
                    return (IReadOnlyList<string>)UsbI2cMock.Calls;
                case "DaqVoltage":
                    return (IReadOnlyList<string>)DaqVoltageMock.Calls;
                case "Oscilloscope":
                    return (IReadOnlyList<string>)OscilloscopeMock.Calls;
                case "SignalGenerator":
                    return (IReadOnlyList<string>)SignalGeneratorMock.Calls;
                case "Config":
                    return (IReadOnlyList<string>)ConfigMock.Calls;
                case "Flash":
                    return (IReadOnlyList<string>)FlashMock.Calls;
                case "SerialNumber":
                    return (IReadOnlyList<string>)SerialNumberMock.Calls;
                case "Mes":
                    return (IReadOnlyList<string>)MesMock.Calls;
                case "Plc":
                    return (IReadOnlyList<string>)PlcMock.Calls;
                default:
                    throw new ArgumentException("Unknown adapter name: " + adapterName, nameof(adapterName));
            }
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
