using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Adapters.Mock;
using RfpTestStation.Core.Importing;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;
using Xunit;

namespace RfpTestStation.Tests.Adapters
{
    public sealed class ActionDispatchTests
    {
        [Theory]
        [InlineData("PowerControl.vi", "PowerSupply")]
        [InlineData("ReadI2C.vi", "UsbI2c")]
        [InlineData("ReadVolt.vi", "DaqVoltage")]
        [InlineData("Test RFP_Flash_once.bat.vi", "Flash")]
        [InlineData("Test RedCase_FlashUpdate_Run.bat.vi", "Flash")]
        [InlineData("Test TDDI_Flash_once.bat.vi", "Flash")]
        public async Task ImportedModuleStepRoutesToExpectedAdapter(string moduleToken, string expectedAdapter)
        {
            var imported = ProfileHtmlImporter.Load(TestPaths.ProfileHtml());
            var step = imported.GetSequence("MainSequence").AllSteps
                .First(x => (x.DescriptionRaw ?? string.Empty).Contains(moduleToken));
            var mockAdapters = new MockStationAdapterRegistry();
            var runner = new SequenceRunner(
                Document(Sequence("MainSequence", step)),
                StepExecutorRegistry.CreateDefault(),
                new NoDelayClock(),
                mockAdapters);

            await runner.RunSequenceAsync("MainSequence");

            Assert.Contains(step.Name, mockAdapters.CallsFor(expectedAdapter));
        }

        [Theory]
        [InlineData("Locals.X4急停", false)]
        [InlineData("Locals.X5光栅", true)]
        [InlineData("Locals.X2气缸到位", true)]
        public async Task MockReadInputChannelWritesSafeInputDefault(string target, bool expectedValue)
        {
            var adapter = new MockStationStepAdapter("ModbusIo");
            var context = new Core.Model.ExecutionContext();
            var step = new StepDefinition
            {
                Name = "Read input",
                DescriptionRaw = "Action, Fct.ModbusRtu.ModbusRtuClient: " + target + " = Use Existing Object(Locals.ModbusClientRef).ReadInputChannel(4, {True, 1})"
            };

            await adapter.ExecuteAsync(step, context, CancellationToken.None);

            Assert.Equal(expectedValue, context.Locals[target.Substring("Locals.".Length)]);
        }

        [Fact]
        public async Task MockReadBytesWritesByteArrayAssignmentTarget()
        {
            var adapter = new MockStationStepAdapter("UsbI2c");
            var context = new Core.Model.ExecutionContext();
            var step = new StepDefinition
            {
                Name = "Read I2C",
                DescriptionRaw = "Pass/Fail Test, USB2IICDll.TestStandApi: Locals.I2C_ReadValue = ReadBytes(0X12, 7)"
            };

            await adapter.ExecuteAsync(step, context, CancellationToken.None);

            var value = Assert.IsType<byte[]>(context.Locals["I2C_ReadValue"]);
            Assert.Equal(7, value.Length);
        }

        [Fact]
        public async Task MockReadVavgWritesVoltageAssignmentTarget()
        {
            var adapter = new MockStationStepAdapter("Oscilloscope");
            var context = new Core.Model.ExecutionContext();
            var step = new StepDefinition
            {
                Name = "Read Vavg",
                DescriptionRaw = "Action, Oscil.ScopeMeasurementClient: Locals.CH1_VAVG = ReadVavg(1)"
            };

            await adapter.ExecuteAsync(step, context, CancellationToken.None);

            Assert.Equal(1.2, context.Locals["CH1_VAVG"]);
        }

        [Fact]
        public async Task MockReadVoltWritesAcInputFromStepName()
        {
            var adapter = new MockStationStepAdapter("DaqVoltage");
            var context = new Core.Model.ExecutionContext();
            var step = new StepDefinition
            {
                Name = "Read AC_Input3",
                DescriptionRaw = "Action, ReadVolt.vi"
            };

            await adapter.ExecuteAsync(step, context, CancellationToken.None);

            Assert.Equal(3.3, context.Locals["AC_Input3"]);
        }

        [Fact]
        public async Task MockReadVoltAlternatesHighAndLowDefaultsPerAcInput()
        {
            var adapter = new MockStationStepAdapter("DaqVoltage");
            var context = new Core.Model.ExecutionContext();
            var step = new StepDefinition
            {
                Name = "Read AC_Input3",
                DescriptionRaw = "Action, ReadVolt.vi"
            };

            await adapter.ExecuteAsync(step, context, CancellationToken.None);
            await adapter.ExecuteAsync(step, context, CancellationToken.None);

            Assert.Equal(0.0, context.Locals["AC_Input3"]);
        }

        private static SequenceDocument Document(SequenceDefinition sequence)
        {
            var document = new SequenceDocument();
            document.Sequences.Add(sequence);
            return document;
        }

        private static SequenceDefinition Sequence(string name, StepDefinition step)
        {
            var sequence = new SequenceDefinition { Name = name };
            sequence.MainSteps.Add(step);
            sequence.AllSteps.Add(step);
            return sequence;
        }

        private sealed class NoDelayClock : Core.Abstractions.IClock
        {
            public System.DateTimeOffset UtcNow { get; } = System.DateTimeOffset.Parse("2026-06-30T00:00:00Z");

            public Task DelayAsync(System.TimeSpan delay, System.Threading.CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
