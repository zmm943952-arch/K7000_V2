using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RfpTestStation.Adapters.Flashing;
using RfpTestStation.Adapters.TestPlans;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;
using Xunit;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Tests.TestPlans
{
    public sealed class TestPlanItemExecutorTests
    {
        [Fact]
        public async Task MockModeReturnsDeterministicPassedFlashResult()
        {
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter()));

            var result = await executor.ExecuteAsync(
                FlashItem(),
                new WorkflowRunContext { Mode = "Mock" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("PASS", result.Value);
            Assert.Contains("Mock", result.Message);
        }

        [Fact]
        public async Task MockModeCanInjectFailedResultWithDetailedReason()
        {
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter()));
            var item = new TestItem("fct.ac-input.3.hi", "AC Input 3 High Limit", TestItemKind.LimitCheck)
            {
                Parameters = JObject.Parse(@"
                {
                  ""low"": 3.135,
                  ""high"": 3.465,
                  ""unit"": ""V"",
                  ""mock"": {
                    ""status"": ""Failed"",
                    ""reason"": ""Mock DAQ voltage outside limits: channel=3; expected=3.135..3.465V; actual=2.900V"",
                    ""value"": 2.9,
                    ""expected"": ""3.135..3.465"",
                    ""compareType"": ""Range"",
                    ""target"": ""DAQ CH3"",
                    ""low"": 3.135,
                    ""high"": 3.465,
                    ""unit"": ""V""
                  }
                }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Mock" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(2.9, result.Value);
            Assert.Equal("3.135..3.465", result.ExpectedValue);
            Assert.Equal("Range", result.CompareType);
            Assert.Equal("DAQ CH3", result.Target);
            Assert.Equal(3.135, result.LowLimit);
            Assert.Equal(3.465, result.HighLimit);
            Assert.Equal("V", result.Unit);
            Assert.Equal("Mock DAQ voltage outside limits: channel=3; expected=3.135..3.465V; actual=2.900V", result.Message);
        }

        [Fact]
        public async Task HardwareModeRunsFlashAdapterFromTestPlanParameters()
        {
            var flash = new FakeFlashAdapter
            {
                Result = new StepResult
                {
                    StepName = "MCU Simple Flash",
                    Status = StepStatus.Passed,
                    Value = 0,
                    Message = "FlashKind=RfpMcuSimple; Script=Project/RFP_Auto/Scripts/Flash_once.bat"
                }
            };
            var executor = new TestPlanItemExecutor(new FakeRegistry(flash));
            var item = FlashItem();

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Same(item, flash.LastItem);
            Assert.Contains("RfpMcuSimple", result.Message);
        }

        [Fact]
        public async Task HardwareModePressesFixtureThroughStationIo()
        {
            var io = new FakeStationIo();
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io));

            var result = await executor.ExecuteAsync(
                new TestItem("fixture.prepare", "Fixture Prepare", TestItemKind.FixturePrepare),
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("FixtureDown", result.Value);
            Assert.Equal(36, io.Calls.Count);
            Assert.Equal("Write 10=False", io.Calls[0]);
            Assert.Equal("Write 43=False", io.Calls[33]);
            Assert.Equal("Write 1=False", io.Calls[34]);
            Assert.Equal("Write 2=True", io.Calls[35]);
        }

        [Fact]
        public async Task HardwareModePassesSafetyCheckWhenFixtureInputMatchesExpectedValue()
        {
            var io = new FakeStationIo();
            io.Inputs[2] = true;
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io));
            var item = new TestItem("safety.fixture-position", "Fixture Position Safety Wait", TestItemKind.SafetyCheck)
            {
                Parameters = JObject.Parse(@"{ ""inputChannel"": 2, ""expected"": true }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(true, result.Value);
            Assert.Equal(new[] { "Read 2" }, io.Calls);
        }

        [Fact]
        public async Task HardwareModeFailsSafetyCheckWhenFixtureInputDoesNotMatchExpectedValue()
        {
            var io = new FakeStationIo();
            io.Inputs[2] = false;
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io));
            var item = new TestItem("safety.fixture-position", "Fixture Position Safety Wait", TestItemKind.SafetyCheck)
            {
                Parameters = JObject.Parse(@"{ ""inputChannel"": 2, ""expected"": true }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(false, result.Value);
            Assert.Equal(new[] { "Read 2" }, io.Calls);
        }

        [Fact]
        public async Task HardwareModeWritesOkResultWhenRunHasNoBlockingFailure()
        {
            var io = new FakeStationIo();
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io));
            var item = new TestItem("result.output", "Result Output", TestItemKind.ResultOutput)
            {
                Parameters = JObject.Parse(@"{ ""okOutputChannel"": 3, ""ngOutputChannel"": 4 }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("OK", result.Value);
            Assert.Equal(new[] { "Write 3=True", "Write 4=False" }, io.Calls);
        }

        [Fact]
        public async Task HardwareModeWritesNgResultWhenRunHasBlockingFailure()
        {
            var io = new FakeStationIo();
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io));
            var item = new TestItem("result.output", "Result Output", TestItemKind.ResultOutput)
            {
                Parameters = JObject.Parse(@"{ ""okOutputChannel"": 3, ""ngOutputChannel"": 4 }")
            };
            var context = new WorkflowRunContext { Mode = "Hardware" };
            context.Values["RunHasBlockingFailure"] = true;

            var result = await executor.ExecuteAsync(
                item,
                context,
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("NG", result.Value);
            Assert.Equal(new[] { "Write 3=False", "Write 4=True" }, io.Calls);
        }

        [Fact]
        public async Task HardwareModeReleasesFixtureDuringCleanup()
        {
            var io = new FakeStationIo();
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io));
            var item = new TestItem("cleanup.fixture", "Fixture Cleanup", TestItemKind.Cleanup)
            {
                Parameters = JObject.Parse(@"{ ""fixtureUpDelayMs"": 0 }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("FixtureReleased", result.Value);
            Assert.Equal(new[] { "Write 2=False", "Write 1=False", "Write 1=True" }, io.Calls);
        }

        [Fact]
        public async Task HardwareModeRoutesUsbI2cDebugModeMeasurementToUsbI2cAdapter()
        {
            var usbI2c = new FakeStepAdapter("UsbI2c")
            {
                Result = new StepResult { Status = StepStatus.Passed, Value = true }
            };
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), usbI2c: usbI2c));
            var item = new TestItem("fct.i2c.debug-mode", "I2C Debug Mode Check", TestItemKind.Measurement)
            {
                Parameters = JObject.Parse(@"{ ""adapter"": ""UsbI2c"", ""address"": ""0x12"", ""operation"": ""EnterAndCheckDebugMode"" }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(true, result.Value);
            Assert.NotNull(usbI2c.LastStep);
            Assert.Contains("EnterAndCheckDebugMode", usbI2c.LastStep!.DescriptionRaw);
            Assert.Contains("0x12", usbI2c.LastStep.DescriptionRaw);
        }

        [Fact]
        public async Task HardwareModeRoutesOscilloscopeMeasurementAndAppliesLimits()
        {
            var oscilloscope = new FakeStepAdapter("Oscilloscope")
            {
                Result = new StepResult { Status = StepStatus.Passed, Value = 2.5 }
            };
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), oscilloscope: oscilloscope));
            var item = new TestItem("fct.oscilloscope.vavg", "Oscilloscope Vavg Check", TestItemKind.Measurement)
            {
                Parameters = JObject.Parse(@"{ ""adapter"": ""Oscilloscope"", ""operation"": ""ReadVavg"", ""channel"": 3, ""low"": 1.0, ""high"": 4.5, ""unit"": ""V"" }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(2.5, result.Value);
            Assert.Equal("V", result.Unit);
            Assert.Equal(1.0, result.LowLimit);
            Assert.Equal(4.5, result.HighLimit);
            Assert.NotNull(oscilloscope.LastStep);
            Assert.Contains("ReadVavg(3)", oscilloscope.LastStep!.DescriptionRaw);
        }

        [Fact]
        public async Task HardwareModeRoutesDaqLimitCheckAndFailsOutOfRangeValue()
        {
            var daq = new FakeStepAdapter("DaqVoltage")
            {
                Result = new StepResult { Status = StepStatus.Passed, Value = 3.0 }
            };
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), daqVoltage: daq));
            var item = new TestItem("fct.ac-input.3.hi", "AC Input 3 High Limit", TestItemKind.LimitCheck)
            {
                Parameters = JObject.Parse(@"{ ""adapter"": ""DaqVoltage"", ""channel"": 3, ""low"": 3.135, ""high"": 3.465, ""unit"": ""V"" }")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Equal(3.0, result.Value);
            Assert.Equal("V", result.Unit);
            Assert.Equal(3.135, result.LowLimit);
            Assert.Equal(3.465, result.HighLimit);
            Assert.NotNull(daq.LastStep);
            Assert.Contains("AC_Input3", daq.LastStep!.DescriptionRaw);
        }

        [Fact]
        public async Task HardwareModeExecutesI2cByteSequenceFunctionalCheck()
        {
            var io = new FakeStationIo();
            var power = new FakeStepAdapter("PowerSupply");
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,88,08,00,3F" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,8A,08,02,06" });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                io,
                usbI2c: usbI2c,
                powerSupply: power));
            var item = new TestItem("fct.hvac-sw.def-frt", "DEF_FRT_SW", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cByteSequence"",
  ""relayOutputChannel"": 31,
  ""powerOnBefore"": [ { ""channel"": 1, ""voltage"": 12.2 } ],
  ""powerOffBefore"": [ { ""channel"": 3 } ],
  ""settleMs"": 0,
  ""address"": ""0x12"",
  ""readRegister"": ""0xD4"",
  ""readLength"": 5,
  ""checks"": [
    { ""name"": ""LO"", ""expectedBytes"": ""88 88 08"" },
    { ""name"": ""HI"", ""powerOn"": [ { ""channel"": 3, ""voltage"": 3.3 } ], ""expectedBytes"": ""88 8A 08"" }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("88 8A 08", result.Value);
            Assert.Equal(new[] { "Write 31=True", "Write 31=False" }, io.Calls);
            Assert.Contains(power.StepNames, name => name.Contains("PowerOn-Channel1-12.2V"));
            Assert.Contains(power.StepNames, name => name.Contains("PowerOff-Channel3"));
            Assert.Contains(power.StepNames, name => name.Contains("PowerOn-Channel3-3.3V"));
            Assert.Equal(2, usbI2c.StepNames.Count);
            Assert.All(usbI2c.StepNames, name => Assert.Contains("ReadBytes", name));
        }

        [Fact]
        public async Task HardwareModeFailsI2cByteSequenceFunctionalCheckWhenBytesDoNotMatch()
        {
            var io = new FakeStationIo();
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,88,08,00,3F" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,88,08,02,06" });
            var executor = new TestPlanItemExecutor(new FakeRegistry(new FakeFlashAdapter(), io, usbI2c: usbI2c));
            var item = new TestItem("fct.hvac-sw.def-frt", "DEF_FRT_SW", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cByteSequence"",
  ""relayOutputChannel"": 31,
  ""settleMs"": 0,
  ""address"": ""0x12"",
  ""readRegister"": ""0xD4"",
  ""readLength"": 5,
  ""checks"": [
    { ""name"": ""LO"", ""expectedBytes"": ""88 88 08"" },
    { ""name"": ""HI"", ""expectedBytes"": ""88 8A 08"" }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Contains("HI", result.Message);
            Assert.Equal(new[] { "Write 31=True", "Write 31=False" }, io.Calls);
        }

        [Fact]
        public async Task HardwareModeExecutesI2cWriteReadWordRangeFunctionalCheck()
        {
            var io = new FakeStationIo();
            var power = new FakeStepAdapter("PowerSupply");
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "FF,10,20,01,00,00,D0" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "FF,10,20,01,94,0E,2E" });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                io,
                usbI2c: usbI2c,
                powerSupply: power));
            var item = new TestItem("fct.hvac-position.tmp1-phsa", "TMP1_PHSA", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cWriteReadWordRange"",
  ""relayOutputChannel"": 27,
  ""powerOffBefore"": [ { ""channel"": 3, ""voltage"": 3.3 } ],
  ""settleMs"": 0,
  ""address"": ""0x12"",
  ""readLength"": 7,
  ""wordHighIndex"": 5,
  ""wordLowIndex"": 4,
  ""checks"": [
    { ""name"": ""0V"", ""writeData"": ""FF 10 20 D1"", ""low"": ""0x0000"", ""high"": ""0x00BA"" },
    { ""name"": ""3.3V"", ""powerOn"": [ { ""channel"": 3, ""voltage"": 3.3 } ], ""writeData"": ""FF 10 20 D1"", ""low"": ""0x0D16"", ""high"": ""0x0FFF"" }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("0x0E94", result.Value);
            Assert.Equal(new[] { "Write 27=True", "Write 27=False" }, io.Calls);
            Assert.Contains(power.StepNames, name => name.Contains("PowerOff-Channel3"));
            Assert.Contains(power.StepNames, name => name.Contains("PowerOn-Channel3-3.3V"));
            Assert.Equal(4, usbI2c.StepNames.Count);
            Assert.Contains("WriteBytes(0x12,{0xFF,0x10,0x20,0xD1})", usbI2c.StepNames[0]);
            Assert.Contains("ReadBytes(0x12,7)", usbI2c.StepNames[1]);
            Assert.Contains("WriteBytes(0x12,{0xFF,0x10,0x20,0xD1})", usbI2c.StepNames[2]);
            Assert.Contains("ReadBytes(0x12,7)", usbI2c.StepNames[3]);
        }

        [Fact]
        public async Task HardwareModeExecutesI2cWriteDaqVoltageFunctionalCheck()
        {
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            var daq = new QueuedStepAdapter("DaqVoltage");
            daq.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = 3.2 });
            daq.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = 0.1 });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                usbI2c: usbI2c,
                daqVoltage: daq));
            var item = new TestItem("fct.hvac-ind.auto", "AUTO_IND", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cWriteDaqVoltage"",
  ""address"": ""0x12"",
  ""register"": ""0x15"",
  ""daqChannel"": 4,
  ""settleMs"": 0,
  ""checks"": [
    { ""name"": ""HI"", ""writeData"": ""01"", ""low"": 3.135, ""high"": 3.465 },
    { ""name"": ""LO"", ""writeData"": ""00"", ""low"": -1.0, ""high"": 1.0 }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(0.1, result.Value);
            Assert.Equal("V", result.Unit);
            Assert.Contains("0x15,0x01", result.Message);
            Assert.Contains("0x15,0x00", result.Message);
            Assert.Equal(2, usbI2c.StepNames.Count);
            Assert.Equal(2, daq.StepNames.Count);
            Assert.All(daq.StepNames, name => Assert.Contains("ReadVoltage(4)", name));
        }

        [Fact]
        public async Task HardwareModeExecutesI2cSingleByteSwitchFunctionalCheck()
        {
            var io = new FakeStationIo();
            var power = new FakeStepAdapter("PowerSupply");
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "00" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "01" });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                io,
                usbI2c: usbI2c,
                powerSupply: power));
            var item = new TestItem("fct.button.s1", "Button S1", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cSingleByteSwitch"",
  ""relayOutputChannel"": 16,
  ""powerOffBefore"": [
    { ""channel"": 1, ""voltage"": 12.2 },
    { ""channel"": 3, ""voltage"": 3.3 }
  ],
  ""settleMs"": 0,
  ""address"": ""0x12"",
  ""readRegister"": ""0x0A"",
  ""checks"": [
    { ""name"": ""Open"", ""expected"": ""00"" },
    {
      ""name"": ""Active"",
      ""powerOn"": [
        { ""channel"": 1, ""voltage"": 12.2 },
        { ""channel"": 3, ""voltage"": 3.3 }
      ],
      ""powerOff"": [ { ""channel"": 3, ""voltage"": 3.3 } ],
      ""expected"": ""01""
    }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal("01", result.Value);
            Assert.Equal(new[] { "Write 16=True", "Write 16=False" }, io.Calls);
            Assert.Equal(2, usbI2c.StepNames.Count);
            Assert.All(usbI2c.StepNames, name => Assert.Contains("WriteReadBytes(0x12,{0x0A},1)", name));
            Assert.Contains(power.StepNames, name => name.Contains("PowerOff-Channel1"));
            Assert.Contains(power.StepNames, name => name.Contains("PowerOff-Channel3"));
            Assert.Contains(power.StepNames, name => name.Contains("PowerOn-Channel1-12.2V"));
        }

        [Fact]
        public async Task HardwareModeExecutesI2cWriteOscilloscopeVavgFunctionalCheck()
        {
            var io = new FakeStationIo();
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            var oscilloscope = new QueuedStepAdapter("Oscilloscope");
            oscilloscope.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = 3.4 });
            oscilloscope.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = 0.1 });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                io,
                usbI2c: usbI2c,
                oscilloscope: oscilloscope));
            var item = new TestItem("fct.swpack-pwm.pwm1", "PWM1", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cWriteOscilloscopeVavg"",
  ""relayOutputChannel"": 22,
  ""address"": ""0x12"",
  ""register"": ""0x25"",
  ""scopeChannel"": 3,
  ""settleMs"": 0,
  ""checks"": [
    { ""name"": ""HI"", ""writeData"": ""FF FF"", ""low"": 1.0, ""high"": 4.5 },
    { ""name"": ""LO"", ""writeData"": ""00 00"", ""low"": -1.0, ""high"": 1.0 }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Equal(0.1, result.Value);
            Assert.Equal("V", result.Unit);
            Assert.Equal(new[] { "Write 22=True", "Write 22=False" }, io.Calls);
            Assert.Equal(2, usbI2c.StepNames.Count);
            Assert.Contains("WriteBytes(0x12,{0x25,0xFF,0xFF})", usbI2c.StepNames[0]);
            Assert.Contains("WriteBytes(0x12,{0x25,0x00,0x00})", usbI2c.StepNames[1]);
            Assert.Equal(2, oscilloscope.StepNames.Count);
            Assert.All(oscilloscope.StepNames, name => Assert.Contains("ReadVavg(3)", name));
        }

        [Fact]
        public async Task HardwareModeExecutesI2cFunctionalGroupWithSharedPowerSetup()
        {
            var io = new FakeStationIo();
            var power = new FakeStepAdapter("PowerSupply");
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,88,08,00,3F" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,8A,08,02,06" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,88,08,00,3F" });
            usbI2c.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = "88,8A,08,02,06" });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                io,
                usbI2c: usbI2c,
                powerSupply: power));
            var item = new TestItem("fct.hvac-sw.group", "HVAC Switch Group", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cFunctionalGroup"",
  ""powerOnBefore"": [ { ""channel"": 1, ""voltage"": 12.2 } ],
  ""settleMs"": 0,
  ""address"": ""0x12"",
  ""readRegister"": ""0xD4"",
  ""readLength"": 5,
  ""items"": [
    {
      ""name"": ""DEF_FRT_SW"",
      ""template"": ""I2cByteSequence"",
      ""relayOutputChannel"": 31,
      ""checks"": [
        { ""name"": ""LO"", ""expectedBytes"": ""88 88 08"" },
        { ""name"": ""HI"", ""expectedBytes"": ""88 8A 08"" }
      ]
    },
    {
      ""name"": ""DFG_RR_SW"",
      ""template"": ""I2cByteSequence"",
      ""relayOutputChannel"": 32,
      ""checks"": [
        { ""name"": ""LO"", ""expectedBytes"": ""88 88 08"" },
        { ""name"": ""HI"", ""expectedBytes"": ""88 8A 08"" }
      ]
    }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Passed, result.Status);
            Assert.Contains("DEF_FRT_SW", result.Message);
            Assert.Contains("DFG_RR_SW", result.Message);
            Assert.Equal(new[] { "Write 31=True", "Write 31=False", "Write 32=True", "Write 32=False" }, io.Calls);
            Assert.Single(power.StepNames, name => name.Contains("PowerOn-Channel1-12.2V"));
            Assert.Equal(4, usbI2c.StepNames.Count);
        }

        [Fact]
        public async Task HardwareModeI2cFunctionalGroupReturnsChildFailureReason()
        {
            var usbI2c = new QueuedStepAdapter("UsbI2c");
            var daq = new QueuedStepAdapter("DaqVoltage");
            daq.Results.Enqueue(new StepResult { Status = StepStatus.Passed, Value = 2.0 });
            var executor = new TestPlanItemExecutor(new FakeRegistry(
                new FakeFlashAdapter(),
                usbI2c: usbI2c,
                daqVoltage: daq));
            var item = new TestItem("fct.hvac-ind.group", "HVAC Indicator Group", TestItemKind.FunctionalCheck)
            {
                Parameters = JObject.Parse(@"{
  ""template"": ""I2cFunctionalGroup"",
  ""address"": ""0x12"",
  ""register"": ""0x15"",
  ""settleMs"": 0,
  ""items"": [
    {
      ""name"": ""AUTO_IND"",
      ""template"": ""I2cWriteDaqVoltage"",
      ""daqChannel"": 4,
      ""checks"": [
        { ""name"": ""HI"", ""writeData"": ""01"", ""low"": 3.135, ""high"": 3.465 }
      ]
    }
  ]
}")
            };

            var result = await executor.ExecuteAsync(
                item,
                new WorkflowRunContext { Mode = "Hardware" },
                CancellationToken.None);

            Assert.Equal(StepStatus.Failed, result.Status);
            Assert.Contains("AUTO_IND", result.Message);
            Assert.Contains("HI voltage outside limits", result.Message);
            Assert.Equal(2.0, result.Value);
            Assert.Equal("V", result.Unit);
        }

        private static TestItem FlashItem()
        {
            return new TestItem("flash.mcu.simple", "MCU Simple Flash", TestItemKind.Flash)
            {
                IsRequired = true,
                Timeout = TimeSpan.FromSeconds(600),
                Parameters = JObject.Parse(@"{
  ""flashKind"": ""RfpMcuSimple"",
  ""script"": ""Project/RFP_Auto/Scripts/Flash_once.bat""
}")
            };
        }

        private sealed class FakeFlashAdapter : IFlashAdapter, ITestPlanFlashAdapter
        {
            public StepResult Result { get; set; } = new StepResult
            {
                Status = StepStatus.Passed
            };

            public TestItem? LastItem { get; private set; }

            public string AdapterName
            {
                get { return "FakeFlash"; }
            }

            public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(Result);
            }

            public Task<StepResult> ExecuteAsync(TestItem item, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                LastItem = item;
                return Task.FromResult(Result);
            }
        }

        private sealed class FakeRegistry : IStationAdapterRegistry
        {
            private readonly IFlashAdapter _flash;
            private readonly IModbusIoAdapter _modbusIo;
            private readonly IUsbI2cAdapter _usbI2c;
            private readonly IDaqVoltageAdapter _daqVoltage;
            private readonly IOscilloscopeAdapter _oscilloscope;
            private readonly IPowerSupplyAdapter _powerSupply;
            private readonly NullAdapter _null = new NullAdapter();

            public FakeRegistry(
                IFlashAdapter flash,
                IModbusIoAdapter? modbusIo = null,
                IUsbI2cAdapter? usbI2c = null,
                IDaqVoltageAdapter? daqVoltage = null,
                IOscilloscopeAdapter? oscilloscope = null,
                IPowerSupplyAdapter? powerSupply = null)
            {
                _flash = flash;
                _modbusIo = modbusIo ?? _null;
                _usbI2c = usbI2c ?? _null;
                _daqVoltage = daqVoltage ?? _null;
                _oscilloscope = oscilloscope ?? _null;
                _powerSupply = powerSupply ?? _null;
            }

            public IModbusIoAdapter ModbusIo { get { return _modbusIo; } }

            public IPowerSupplyAdapter PowerSupply { get { return _powerSupply; } }

            public IUsbI2cAdapter UsbI2c { get { return _usbI2c; } }

            public IDaqVoltageAdapter DaqVoltage { get { return _daqVoltage; } }

            public IOscilloscopeAdapter Oscilloscope { get { return _oscilloscope; } }

            public ISignalGeneratorAdapter SignalGenerator { get { return _null; } }

            public IConfigAdapter Config { get { return _null; } }

            public IFlashAdapter Flash { get { return _flash; } }

            public ISerialNumberAdapter SerialNumber { get { return _null; } }

            public IMesAdapter Mes { get { return _null; } }

            public IPlcAdapter Plc { get { return _null; } }

            public IStationStepAdapter ResolveActionAdapter(StepDefinition step)
            {
                return _null;
            }
        }

        private sealed class FakeStepAdapter : IUsbI2cAdapter, IDaqVoltageAdapter, IOscilloscopeAdapter, IPowerSupplyAdapter
        {
            public FakeStepAdapter(string adapterName)
            {
                AdapterName = adapterName;
            }

            public string AdapterName { get; }

            public StepDefinition? LastStep { get; private set; }

            public StepResult Result { get; set; } = new StepResult { Status = StepStatus.Passed };

            public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                LastStep = step;
                StepNames.Add(step.DescriptionRaw ?? step.Name);
                Result.StepName = step.Name;
                return Task.FromResult(Result);
            }

            public IList<string> StepNames { get; } = new List<string>();
        }

        private sealed class QueuedStepAdapter : IUsbI2cAdapter, IDaqVoltageAdapter, IOscilloscopeAdapter
        {
            public QueuedStepAdapter(string adapterName)
            {
                AdapterName = adapterName;
            }

            public string AdapterName { get; }

            public Queue<StepResult> Results { get; } = new Queue<StepResult>();

            public IList<string> StepNames { get; } = new List<string>();

            public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                StepNames.Add(step.DescriptionRaw ?? step.Name);
                var result = Results.Count == 0
                    ? new StepResult { Status = StepStatus.Passed }
                    : Results.Dequeue();
                result.StepName = step.Name;
                return Task.FromResult(result);
            }
        }

        private sealed class NullAdapter :
            IModbusIoAdapter,
            IPowerSupplyAdapter,
            IUsbI2cAdapter,
            IDaqVoltageAdapter,
            IOscilloscopeAdapter,
            ISignalGeneratorAdapter,
            IConfigAdapter,
            ISerialNumberAdapter,
            IMesAdapter,
            IPlcAdapter
        {
            public string AdapterName
            {
                get { return "Null"; }
            }

            public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(new StepResult { StepName = step.Name, Status = StepStatus.Passed });
            }
        }

        private sealed class FakeStationIo : IModbusIoAdapter, IStationIoController
        {
            public IDictionary<int, bool> Inputs { get; } = new Dictionary<int, bool>();

            public IList<string> Calls { get; } = new List<string>();

            public string AdapterName
            {
                get { return "FakeStationIo"; }
            }

            public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
            {
                return Task.FromResult(new StepResult { StepName = step.Name, Status = StepStatus.Passed });
            }

            public Task<bool> ReadInputAsync(int channel, CancellationToken cancellationToken)
            {
                Calls.Add("Read " + channel);
                return Task.FromResult(Inputs.ContainsKey(channel) && Inputs[channel]);
            }

            public Task WriteOutputAsync(int channel, bool value, CancellationToken cancellationToken)
            {
                Calls.Add("Write " + channel + "=" + value);
                return Task.CompletedTask;
            }
        }
    }
}
