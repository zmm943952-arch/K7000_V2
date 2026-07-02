using System.Linq;
using RfpTestStation.Adapters.Hardware;
using RfpTestStation.Core.Importing;
using Xunit;

namespace RfpTestStation.Tests.Adapters
{
    public sealed class HardwareAdapterRegistryTests
    {
        [Theory]
        [InlineData("PowerControl.vi", "PowerSupply")]
        [InlineData("ReadI2C.vi", "UsbI2c")]
        [InlineData("ReadVolt.vi", "DaqVoltage")]
        [InlineData("USB2IICDll.TestStandApi", "UsbI2c")]
        [InlineData("Fct.ModbusRtu.ModbusRtuClient", "ModbusIo")]
        [InlineData("Oscil.", "Oscilloscope")]
        [InlineData("SG.TestStandApi", "SignalGenerator")]
        [InlineData("ReadConfigLib.JsonConfigReader", "Config")]
        [InlineData("Test RFP_Flash_once.bat.vi", "Flash")]
        public void RegistryRoutesImportedStepsToHardwareAdapters(string moduleToken, string expectedAdapter)
        {
            var imported = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());
            var step = imported.GetSequence("MainSequence").AllSteps
                .First(x => (x.DescriptionRaw ?? string.Empty).Contains(moduleToken));
            var registry = new HardwareStationAdapterRegistry(TestPaths.RepoRoot());

            var adapter = registry.ResolveActionAdapter(step);

            Assert.Equal(expectedAdapter, adapter.AdapterName);
        }

        [Fact]
        public void PowerSupplyUsesDirectHardwareAdapter()
        {
            var registry = new HardwareStationAdapterRegistry(TestPaths.RepoRoot());

            Assert.IsType<PowerSupplyAdapter>(registry.PowerSupply);
        }
    }
}
