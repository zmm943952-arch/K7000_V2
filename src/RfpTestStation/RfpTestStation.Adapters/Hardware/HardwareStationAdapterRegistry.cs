using System;
using RfpTestStation.Adapters.Config;
using RfpTestStation.Adapters.Flashing;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class HardwareStationAdapterRegistry : IStationAdapterRegistry
    {
        public static readonly string[] LabViewPlaceholders =
        {
            "ReadSN.vi -> SerialNumber placeholder",
            "MES LabVIEW steps -> Mes placeholder",
            "PLC LabVIEW steps -> Plc placeholder"
        };

        public HardwareStationAdapterRegistry(string repoRoot)
        {
            ModbusIo = new ModbusIoAdapter("COM2");
            PowerSupply = new PowerSupplyAdapter();
            UsbI2c = new UsbI2cAdapter();
            DaqVoltage = new DaqVoltageAdapter("COM4", 1);
            Oscilloscope = new OscilloscopeAdapter();
            SignalGenerator = new SignalGeneratorAdapter();
            Config = new ConfigAdapter(repoRoot);
            Flash = new FlashAdapter(new FlashScriptMap(repoRoot), new ProcessRunner(), TimeSpan.FromMinutes(10));
            SerialNumber = new HardwareUnsupportedAdapter("SerialNumber", "ReadSN.vi has no direct C# replacement wired yet.");
            Mes = new HardwareUnsupportedAdapter("Mes", "MES LabVIEW steps have no direct C# replacement wired yet.");
            Plc = new HardwareUnsupportedAdapter("Plc", "PLC LabVIEW steps have no direct C# replacement wired yet.");
        }

        public IModbusIoAdapter ModbusIo { get; }

        public IPowerSupplyAdapter PowerSupply { get; }

        public IUsbI2cAdapter UsbI2c { get; }

        public IDaqVoltageAdapter DaqVoltage { get; }

        public IOscilloscopeAdapter Oscilloscope { get; }

        public ISignalGeneratorAdapter SignalGenerator { get; }

        public IConfigAdapter Config { get; }

        public IFlashAdapter Flash { get; }

        public ISerialNumberAdapter SerialNumber { get; }

        public IMesAdapter Mes { get; }

        public IPlcAdapter Plc { get; }

        public IStationStepAdapter ResolveActionAdapter(StepDefinition step)
        {
            var text = ModuleStepClassifier.ModuleText(step);

            if (Contains(text, "PowerControl.vi"))
            {
                return PowerSupply;
            }

            if (Contains(text, "ReadI2C.vi") || Contains(text, "WriteI2C.vi") || Contains(text, "USB2IICDll"))
            {
                return UsbI2c;
            }

            if (Contains(text, "ReadConfigLib") || Contains(text, "PreparedArtifactPaths"))
            {
                return Config;
            }

            if (Contains(text, "ReadVolt.vi") || Contains(text, "YkDaq"))
            {
                return DaqVoltage;
            }

            if (Contains(text, "Oscil"))
            {
                return Oscilloscope;
            }

            if (Contains(text, "SG.") || Contains(text, "SignalGenerator") || Contains(text, "Afg"))
            {
                return SignalGenerator;
            }

            if (Contains(text, "Flash_once.bat.vi")
                || Contains(text, "FlashUpdate_Run.bat.vi")
                || Contains(text, "TDDI_Flash_once.bat.vi"))
            {
                return Flash;
            }

            if (Contains(text, "ReadSN.vi"))
            {
                return SerialNumber;
            }

            if (Contains(text, "MES"))
            {
                return Mes;
            }

            if (Contains(text, "PLC"))
            {
                return Plc;
            }

            if (Contains(text, "ModbusRtu"))
            {
                return ModbusIo;
            }

            return ModbusIo;
        }

        private static bool Contains(string text, string value)
        {
            return text.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
