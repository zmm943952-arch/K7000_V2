using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.TestPlans
{
    public sealed class TestPlanItemExecutor
    {
        private readonly IStationAdapterRegistry _adapterRegistry;

        public TestPlanItemExecutor(IStationAdapterRegistry adapterRegistry)
        {
            _adapterRegistry = adapterRegistry ?? throw new ArgumentNullException(nameof(adapterRegistry));
        }

        public async Task<TestItemResult> ExecuteAsync(
            TestItem item,
            WorkflowRunContext context,
            CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.Equals(context.Mode, "Mock", StringComparison.OrdinalIgnoreCase))
            {
                return MockResult(item);
            }

            if (item.Kind == TestItemKind.Flash)
            {
                return await ExecuteFlashAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (IsStationFlowItem(item.Kind))
            {
                return await ExecuteStationFlowAsync(item, context, cancellationToken).ConfigureAwait(false);
            }

            if (item.Kind == TestItemKind.FunctionalCheck)
            {
                return await ExecuteFunctionalCheckAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (item.Kind == TestItemKind.Measurement || item.Kind == TestItemKind.LimitCheck)
            {
                return await ExecuteMeasurementAsync(item, cancellationToken).ConfigureAwait(false);
            }

            return TestItemResult.FromError(
                item,
                new NotSupportedException("Hardware TestPlan executor is not wired yet: " + item.Kind));
        }

        private async Task<TestItemResult> ExecuteFlashAsync(TestItem item, CancellationToken cancellationToken)
        {
            var flashAdapter = _adapterRegistry.Flash as ITestPlanFlashAdapter;
            if (flashAdapter == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("Flash adapter does not support TestPlan execution."));
            }

            var stepResult = await flashAdapter
                .ExecuteAsync(item, new StationExecutionContext(), cancellationToken)
                .ConfigureAwait(false);
            return FromStepResult(item, stepResult);
        }

        private static TestItemResult FromStepResult(TestItem item, StepResult stepResult)
        {
            return new TestItemResult
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Kind = item.Kind,
                SourceReference = item.SourceReference,
                Status = stepResult.Status,
                Value = stepResult.Value,
                ExpectedValue = stepResult.ExpectedValue,
                CompareType = stepResult.CompareType,
                Target = stepResult.Target,
                Sent = stepResult.Sent,
                Reply = stepResult.Reply,
                Unit = stepResult.Unit,
                LowLimit = stepResult.LowLimit,
                HighLimit = stepResult.HighLimit,
                Message = stepResult.Message,
                ExternalLogPath = stepResult.ExternalLogPath,
                StartTime = stepResult.StartTime,
                EndTime = stepResult.EndTime,
                Error = stepResult.Error
            };
        }

        private async Task<TestItemResult> ExecuteStationFlowAsync(
            TestItem item,
            WorkflowRunContext context,
            CancellationToken cancellationToken)
        {
            var io = _adapterRegistry.ModbusIo as IStationIoController;
            if (io == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("Modbus IO adapter does not support direct station IO control."));
            }

            try
            {
                switch (item.Kind)
                {
                    case TestItemKind.FixturePrepare:
                        return await ExecuteFixturePrepareAsync(item, io, cancellationToken).ConfigureAwait(false);
                    case TestItemKind.SafetyCheck:
                        return await ExecuteSafetyCheckAsync(item, io, cancellationToken).ConfigureAwait(false);
                    case TestItemKind.ResultOutput:
                        return await ExecuteResultOutputAsync(item, context, io, cancellationToken).ConfigureAwait(false);
                    case TestItemKind.Cleanup:
                        return await ExecuteCleanupAsync(item, io, cancellationToken).ConfigureAwait(false);
                    default:
                        return TestItemResult.FromError(
                            item,
                            new NotSupportedException("Station flow item is not wired: " + item.Kind));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
        }

        private static async Task<TestItemResult> ExecuteFixturePrepareAsync(
            TestItem item,
            IStationIoController io,
            CancellationToken cancellationToken)
        {
            var clearStartChannel = ReadIntParameter(item, "clearOutputStartChannel", 10);
            var clearEndChannel = ReadIntParameter(item, "clearOutputEndChannel", 43);
            var upOutputChannel = ReadIntParameter(item, "fixtureUpOutputChannel", 1);
            var downOutputChannel = ReadIntParameter(item, "fixtureDownOutputChannel", 2);

            for (var channel = clearStartChannel; channel <= clearEndChannel; channel++)
            {
                await io.WriteOutputAsync(channel, false, cancellationToken).ConfigureAwait(false);
            }

            await io.WriteOutputAsync(upOutputChannel, false, cancellationToken).ConfigureAwait(false);
            await io.WriteOutputAsync(downOutputChannel, true, cancellationToken).ConfigureAwait(false);

            var result = TestItemResult.Passed(
                item,
                "Fixture pressed: clearOutputs=" + clearStartChannel + "-" + clearEndChannel
                    + "; upOutputChannel=" + upOutputChannel
                    + "; downOutputChannel=" + downOutputChannel);
            result.Value = "FixtureDown";
            return result;
        }

        private static async Task<TestItemResult> ExecuteSafetyCheckAsync(
            TestItem item,
            IStationIoController io,
            CancellationToken cancellationToken)
        {
            var inputChannel = ReadIntParameter(item, "inputChannel", 2);
            var expected = ReadBoolParameter(item, "expected", true);
            var actual = await io.ReadInputAsync(inputChannel, cancellationToken).ConfigureAwait(false);
            var result = actual == expected
                ? TestItemResult.Passed(item, "Safety input matched: channel=" + inputChannel + "; expected=" + expected)
                : TestItemResult.Failed(item, "Safety input mismatch: channel=" + inputChannel + "; expected=" + expected + "; actual=" + actual);
            result.Value = actual;
            result.ExpectedValue = expected;
            result.CompareType = "Equal";
            result.Target = "DI" + inputChannel.ToString(CultureInfo.InvariantCulture);
            result.Sent = "ReadInput(" + inputChannel.ToString(CultureInfo.InvariantCulture) + ")";
            result.Reply = actual.ToString(CultureInfo.InvariantCulture);
            return result;
        }

        private static async Task<TestItemResult> ExecuteResultOutputAsync(
            TestItem item,
            WorkflowRunContext context,
            IStationIoController io,
            CancellationToken cancellationToken)
        {
            var okOutputChannel = ReadIntParameter(item, "okOutputChannel", 3);
            var ngOutputChannel = ReadIntParameter(item, "ngOutputChannel", 4);
            var hasBlockingFailure = ReadContextBool(context, WorkflowRunContext.RunHasBlockingFailureKey);
            await io.WriteOutputAsync(okOutputChannel, !hasBlockingFailure, cancellationToken).ConfigureAwait(false);
            await io.WriteOutputAsync(ngOutputChannel, hasBlockingFailure, cancellationToken).ConfigureAwait(false);

            var resultText = hasBlockingFailure ? "NG" : "OK";
            var result = TestItemResult.Passed(
                item,
                "Result output: " + resultText + "; okOutputChannel=" + okOutputChannel + "; ngOutputChannel=" + ngOutputChannel);
            result.Value = resultText;
            return result;
        }

        private static async Task<TestItemResult> ExecuteCleanupAsync(
            TestItem item,
            IStationIoController io,
            CancellationToken cancellationToken)
        {
            var upOutputChannel = ReadIntParameter(item, "fixtureUpOutputChannel", 1);
            var downOutputChannel = ReadIntParameter(item, "fixtureDownOutputChannel", 2);
            var upDelayMs = ReadIntParameter(item, "fixtureUpDelayMs", 1000);
            await io.WriteOutputAsync(downOutputChannel, false, cancellationToken).ConfigureAwait(false);
            await io.WriteOutputAsync(upOutputChannel, false, cancellationToken).ConfigureAwait(false);
            if (upDelayMs > 0)
            {
                await Task.Delay(upDelayMs, cancellationToken).ConfigureAwait(false);
            }

            await io.WriteOutputAsync(upOutputChannel, true, cancellationToken).ConfigureAwait(false);

            var result = TestItemResult.Passed(
                item,
                "Fixture released: upOutputChannel=" + upOutputChannel + "; downOutputChannel=" + downOutputChannel);
            result.Value = "FixtureReleased";
            return result;
        }

        private async Task<TestItemResult> ExecuteFunctionalCheckAsync(TestItem item, CancellationToken cancellationToken)
        {
            var template = ReadStringParameter(item, "template");
            if (string.Equals(template, "I2cByteSequence", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteI2cByteSequenceAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(template, "I2cWriteDaqVoltage", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteI2cWriteDaqVoltageAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(template, "I2cSingleByteSwitch", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteI2cSingleByteSwitchAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(template, "I2cWriteReadWordRange", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteI2cWriteReadWordRangeAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(template, "I2cWriteOscilloscopeVavg", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteI2cWriteOscilloscopeVavgAsync(item, cancellationToken).ConfigureAwait(false);
            }

            if (string.Equals(template, "I2cFunctionalGroup", StringComparison.OrdinalIgnoreCase))
            {
                return await ExecuteI2cFunctionalGroupAsync(item, cancellationToken).ConfigureAwait(false);
            }

            return TestItemResult.FromError(
                item,
                new NotSupportedException("FunctionalCheck template is not wired: " + template));
        }

        private async Task<TestItemResult> ExecuteI2cFunctionalGroupAsync(TestItem item, CancellationToken cancellationToken)
        {
            try
            {
                await ExecutePowerListAsync(item, "powerOnBefore", cancellationToken).ConfigureAwait(false);
                await ExecutePowerListAsync(item, "powerOffBefore", cancellationToken).ConfigureAwait(false);

                var groupParameters = item.Parameters;
                var childDefinitions = ReadRequiredArrayParameter(item, "items");
                var passedNames = new List<string>();
                TestItemResult? lastChildResult = null;

                foreach (var token in childDefinitions)
                {
                    if (token.Type != JTokenType.Object)
                    {
                        return TestItemResult.FromError(item, new JsonException("Functional group child item must be an object."));
                    }

                    var childObject = (JObject)token;
                    var childName = ReadRequiredString(childObject, "name");
                    var childParameters = BuildFunctionalGroupChildParameters(groupParameters, childObject);
                    var childTemplate = childParameters["template"] == null ? null : childParameters["template"]!.ToString();
                    if (string.IsNullOrWhiteSpace(childTemplate))
                    {
                        return TestItemResult.FromError(
                            item,
                            new JsonException("Functional group child template is missing: " + childName));
                    }

                    var childItem = new TestItem(item.Id + "." + NormalizeChildId(childName), childName, TestItemKind.FunctionalCheck)
                    {
                        IsRequired = item.IsRequired,
                        StopOnFailure = item.StopOnFailure,
                        Timeout = item.Timeout,
                        SourceReference = item.SourceReference,
                        Parameters = childParameters
                    };
                    lastChildResult = await ExecuteFunctionalCheckAsync(childItem, cancellationToken).ConfigureAwait(false);
                    if (lastChildResult.Status != StepStatus.Passed)
                    {
                        return FromFunctionalGroupChildResult(item, childName, lastChildResult);
                    }

                    passedNames.Add(childName);
                }

                var passed = TestItemResult.Passed(item, "Functional group passed: " + string.Join(", ", passedNames));
                if (lastChildResult != null)
                {
                    passed.Value = lastChildResult.Value;
                    passed.Unit = lastChildResult.Unit;
                    passed.LowLimit = lastChildResult.LowLimit;
                    passed.HighLimit = lastChildResult.HighLimit;
                }

                return passed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
        }

        private async Task<TestItemResult> ExecuteI2cByteSequenceAsync(TestItem item, CancellationToken cancellationToken)
        {
            var relayOutputChannel = ReadNullableIntParameter(item, "relayOutputChannel");
            var io = _adapterRegistry.ModbusIo as IStationIoController;
            var relayWasSet = false;
            if (relayOutputChannel != null && io == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("Modbus IO adapter does not support direct station IO control."));
            }

            try
            {
                await ExecutePowerListAsync(item, "powerOnBefore", cancellationToken).ConfigureAwait(false);
                await ExecutePowerListAsync(item, "powerOffBefore", cancellationToken).ConfigureAwait(false);

                if (relayOutputChannel != null)
                {
                    await io!.WriteOutputAsync(relayOutputChannel.Value, true, cancellationToken).ConfigureAwait(false);
                    relayWasSet = true;
                }

                await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);

                var address = ReadRequiredHexParameter(item, "address");
                var readRegister = ReadRequiredHexParameter(item, "readRegister");
                var readLength = ReadIntParameter(item, "readLength", 1);
                var checks = ReadRequiredArrayParameter(item, "checks");
                var lastActual = string.Empty;

                foreach (var token in checks)
                {
                    var check = (JObject)token;
                    await ExecutePowerArrayAsync(check["powerOn"] as JArray, cancellationToken).ConfigureAwait(false);
                    await ExecutePowerArrayAsync(check["powerOff"] as JArray, cancellationToken).ConfigureAwait(false);
                    await DelayFromTokenAsync(check["settleMs"], cancellationToken).ConfigureAwait(false);

                    var stepResult = await _adapterRegistry.UsbI2c.ExecuteAsync(
                        BuildI2cWriteReadStep(item.Name + " ReadBytes " + ReadString(check, "name"), address, readRegister, readLength),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (stepResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, stepResult);
                    }

                    var actual = ParseBytes(stepResult.Value);
                    var expected = ParseBytes(ReadRequiredString(check, "expectedBytes"));
                    lastActual = ByteText(actual.Take(expected.Length));
                    if (!BytesStartWith(actual, expected))
                    {
                        return FailedFunctionalResult(
                            item,
                            ReadString(check, "name") + " bytes mismatch: expected=" + ByteText(expected) + "; actual=" + ByteText(actual),
                            lastActual);
                    }
                }

                var passed = TestItemResult.Passed(item, "I2C byte sequence matched.");
                passed.Value = lastActual;
                return passed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
            finally
            {
                if (relayWasSet && relayOutputChannel != null && io != null)
                {
                    await io.WriteOutputAsync(relayOutputChannel.Value, false, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<TestItemResult> ExecuteI2cWriteDaqVoltageAsync(TestItem item, CancellationToken cancellationToken)
        {
            try
            {
                await ExecutePowerListAsync(item, "powerOnBefore", cancellationToken).ConfigureAwait(false);
                await ExecutePowerListAsync(item, "powerOffBefore", cancellationToken).ConfigureAwait(false);

                var address = ReadRequiredHexParameter(item, "address");
                var register = ReadRequiredHexParameter(item, "register");
                var daqChannel = ReadIntParameter(item, "daqChannel", 1);
                var checks = ReadRequiredArrayParameter(item, "checks");
                var commandTexts = new List<string>();
                object? lastValue = null;

                foreach (var token in checks)
                {
                    var check = (JObject)token;
                    var writeData = ParseBytes(ReadRequiredString(check, "writeData"));
                    var commandBytes = new[] { (byte)register }.Concat(writeData).ToArray();
                    var commandText = Hex(register) + "," + string.Join(",", writeData.Select(x => Hex(x)));
                    commandTexts.Add(commandText);

                    var writeResult = await _adapterRegistry.UsbI2c.ExecuteAsync(
                        BuildI2cWriteStep(item.Name + " Write " + ReadString(check, "name"), address, commandBytes),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (writeResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, writeResult);
                    }

                    await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);
                    await DelayFromTokenAsync(check["settleMs"], cancellationToken).ConfigureAwait(false);

                    var readResult = await _adapterRegistry.DaqVoltage.ExecuteAsync(
                        BuildDaqReadStep(item.Name + " ReadVoltage " + ReadString(check, "name"), daqChannel),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (readResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, readResult);
                    }

                    var value = Convert.ToDouble(readResult.Value, CultureInfo.InvariantCulture);
                    lastValue = readResult.Value;
                    var low = ReadNullableDouble(check, "low");
                    var high = ReadNullableDouble(check, "high");
                    if ((low != null && value < low.Value) || (high != null && value > high.Value))
                    {
                        var failed = FailedFunctionalResult(
                            item,
                            ReadString(check, "name") + " voltage outside limits: value=" + value.ToString(CultureInfo.InvariantCulture)
                                + "; low=" + LimitText(low)
                                + "; high=" + LimitText(high),
                            readResult.Value);
                        failed.Unit = "V";
                        failed.LowLimit = low;
                        failed.HighLimit = high;
                        return failed;
                    }
                }

                var passed = TestItemResult.Passed(item, "Commands: " + string.Join("; ", commandTexts));
                passed.Value = lastValue;
                passed.Unit = "V";
                return passed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
        }

        private async Task<TestItemResult> ExecuteI2cSingleByteSwitchAsync(TestItem item, CancellationToken cancellationToken)
        {
            var relayOutputChannel = ReadNullableIntParameter(item, "relayOutputChannel");
            var io = _adapterRegistry.ModbusIo as IStationIoController;
            var relayWasSet = false;
            if (relayOutputChannel != null && io == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("Modbus IO adapter does not support direct station IO control."));
            }

            try
            {
                await ExecutePowerListAsync(item, "powerOnBefore", cancellationToken).ConfigureAwait(false);
                await ExecutePowerListAsync(item, "powerOffBefore", cancellationToken).ConfigureAwait(false);

                if (relayOutputChannel != null)
                {
                    await io!.WriteOutputAsync(relayOutputChannel.Value, true, cancellationToken).ConfigureAwait(false);
                    relayWasSet = true;
                }

                await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);

                var address = ReadRequiredHexParameter(item, "address");
                var readRegister = ReadRequiredHexParameter(item, "readRegister");
                var checks = ReadRequiredArrayParameter(item, "checks");
                string? lastActual = null;

                foreach (var token in checks)
                {
                    var check = (JObject)token;
                    await ExecutePowerArrayAsync(check["powerOn"] as JArray, cancellationToken).ConfigureAwait(false);
                    await ExecutePowerArrayAsync(check["powerOff"] as JArray, cancellationToken, "PowerOff").ConfigureAwait(false);
                    await DelayFromTokenAsync(check["settleMs"], cancellationToken).ConfigureAwait(false);

                    var stepResult = await _adapterRegistry.UsbI2c.ExecuteAsync(
                        BuildI2cWriteReadStep(item.Name + " ReadByte " + ReadString(check, "name"), address, readRegister, 1),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (stepResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, stepResult);
                    }

                    var actual = ParseBytes(stepResult.Value);
                    var expected = ParseBytes(ReadRequiredString(check, "expected"));
                    lastActual = actual.Length == 0 ? string.Empty : actual[0].ToString("X2", CultureInfo.InvariantCulture);
                    if (!BytesStartWith(actual, expected))
                    {
                        return FailedFunctionalResult(
                            item,
                            ReadString(check, "name") + " byte mismatch: expected=" + ByteText(expected) + "; actual=" + ByteText(actual),
                            lastActual);
                    }
                }

                var passed = TestItemResult.Passed(item, "I2C single byte switch matched.");
                passed.Value = lastActual;
                return passed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
            finally
            {
                if (relayWasSet && relayOutputChannel != null && io != null)
                {
                    await io.WriteOutputAsync(relayOutputChannel.Value, false, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<TestItemResult> ExecuteI2cWriteReadWordRangeAsync(TestItem item, CancellationToken cancellationToken)
        {
            var relayOutputChannel = ReadNullableIntParameter(item, "relayOutputChannel");
            var io = _adapterRegistry.ModbusIo as IStationIoController;
            var relayWasSet = false;
            if (relayOutputChannel != null && io == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("Modbus IO adapter does not support direct station IO control."));
            }

            try
            {
                await ExecutePowerListAsync(item, "powerOnBefore", cancellationToken).ConfigureAwait(false);
                await ExecutePowerListAsync(item, "powerOffBefore", cancellationToken).ConfigureAwait(false);

                if (relayOutputChannel != null)
                {
                    await io!.WriteOutputAsync(relayOutputChannel.Value, true, cancellationToken).ConfigureAwait(false);
                    relayWasSet = true;
                }

                await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);

                var address = ReadRequiredHexParameter(item, "address");
                var readLength = ReadIntParameter(item, "readLength", 7);
                var wordHighIndex = ReadIntParameter(item, "wordHighIndex", 5);
                var wordLowIndex = ReadIntParameter(item, "wordLowIndex", 4);
                var checks = ReadRequiredArrayParameter(item, "checks");
                int? lastWord = null;

                foreach (var token in checks)
                {
                    var check = (JObject)token;
                    await ExecutePowerArrayAsync(check["powerOn"] as JArray, cancellationToken).ConfigureAwait(false);
                    await ExecutePowerArrayAsync(check["powerOff"] as JArray, cancellationToken, "PowerOff").ConfigureAwait(false);

                    var writeData = ParseBytes(ReadRequiredString(check, "writeData"));
                    var writeResult = await _adapterRegistry.UsbI2c.ExecuteAsync(
                        BuildI2cWriteStep(item.Name + " Write " + ReadString(check, "name"), address, writeData),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (writeResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, writeResult);
                    }

                    await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);
                    await DelayFromTokenAsync(check["settleMs"], cancellationToken).ConfigureAwait(false);

                    var readResult = await _adapterRegistry.UsbI2c.ExecuteAsync(
                        BuildI2cReadStep(item.Name + " ReadBytes " + ReadString(check, "name"), address, readLength),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (readResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, readResult);
                    }

                    var actual = ParseBytes(readResult.Value);
                    if (actual.Length <= wordHighIndex || actual.Length <= wordLowIndex)
                    {
                        return FailedFunctionalResult(
                            item,
                            ReadString(check, "name") + " read length is too short: expected index "
                                + Math.Max(wordHighIndex, wordLowIndex)
                                + "; actualLength=" + actual.Length,
                            ByteText(actual));
                    }

                    var word = (actual[wordHighIndex] << 8) + actual[wordLowIndex];
                    lastWord = word;
                    var low = ReadNullableHexInt(check, "low");
                    var high = ReadNullableHexInt(check, "high");
                    if ((low != null && word < low.Value) || (high != null && word > high.Value))
                    {
                        return FailedFunctionalResult(
                            item,
                            ReadString(check, "name") + " word outside limits: value=" + HexWord(word)
                                + "; low=" + HexLimitText(low)
                                + "; high=" + HexLimitText(high)
                                + "; bytes=" + ByteText(actual),
                            HexWord(word));
                    }
                }

                var passed = TestItemResult.Passed(item, "I2C word ranges matched.");
                passed.Value = lastWord == null ? null : HexWord(lastWord.Value);
                return passed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
            finally
            {
                if (relayWasSet && relayOutputChannel != null && io != null)
                {
                    await io.WriteOutputAsync(relayOutputChannel.Value, false, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<TestItemResult> ExecuteI2cWriteOscilloscopeVavgAsync(TestItem item, CancellationToken cancellationToken)
        {
            var relayOutputChannel = ReadNullableIntParameter(item, "relayOutputChannel");
            var io = _adapterRegistry.ModbusIo as IStationIoController;
            var relayWasSet = false;
            if (relayOutputChannel != null && io == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("Modbus IO adapter does not support direct station IO control."));
            }

            try
            {
                await ExecutePowerListAsync(item, "powerOnBefore", cancellationToken).ConfigureAwait(false);
                await ExecutePowerListAsync(item, "powerOffBefore", cancellationToken).ConfigureAwait(false);

                if (relayOutputChannel != null)
                {
                    await io!.WriteOutputAsync(relayOutputChannel.Value, true, cancellationToken).ConfigureAwait(false);
                    relayWasSet = true;
                }

                await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);

                var address = ReadRequiredHexParameter(item, "address");
                var register = ReadRequiredHexParameter(item, "register");
                var scopeChannel = ReadIntParameter(item, "scopeChannel", 3);
                var checks = ReadRequiredArrayParameter(item, "checks");
                var commandTexts = new List<string>();
                object? lastValue = null;

                foreach (var token in checks)
                {
                    var check = (JObject)token;
                    await ExecutePowerArrayAsync(check["powerOn"] as JArray, cancellationToken).ConfigureAwait(false);
                    await ExecutePowerArrayAsync(check["powerOff"] as JArray, cancellationToken, "PowerOff").ConfigureAwait(false);

                    var writeData = ParseBytes(ReadRequiredString(check, "writeData"));
                    var commandBytes = new[] { (byte)register }.Concat(writeData).ToArray();
                    var commandText = Hex(register) + "," + string.Join(",", writeData.Select(x => Hex(x)));
                    commandTexts.Add(commandText);

                    var writeResult = await _adapterRegistry.UsbI2c.ExecuteAsync(
                        BuildI2cWriteStep(item.Name + " Write " + ReadString(check, "name"), address, commandBytes),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (writeResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, writeResult);
                    }

                    await DelayFromParameterAsync(item, "settleMs", cancellationToken).ConfigureAwait(false);
                    await DelayFromTokenAsync(check["settleMs"], cancellationToken).ConfigureAwait(false);

                    var readResult = await _adapterRegistry.Oscilloscope.ExecuteAsync(
                        BuildOscilloscopeVavgStep(item.Name + " ReadVavg " + ReadString(check, "name"), scopeChannel),
                        new StationExecutionContext(),
                        cancellationToken).ConfigureAwait(false);
                    if (readResult.Status != StepStatus.Passed)
                    {
                        return FromStepResult(item, readResult);
                    }

                    var value = Convert.ToDouble(readResult.Value, CultureInfo.InvariantCulture);
                    lastValue = readResult.Value;
                    var low = ReadNullableDouble(check, "low");
                    var high = ReadNullableDouble(check, "high");
                    if ((low != null && value < low.Value) || (high != null && value > high.Value))
                    {
                        var failed = FailedFunctionalResult(
                            item,
                            ReadString(check, "name") + " Vavg outside limits: value=" + value.ToString(CultureInfo.InvariantCulture)
                                + "; low=" + LimitText(low)
                                + "; high=" + LimitText(high),
                            readResult.Value);
                        failed.Unit = "V";
                        failed.LowLimit = low;
                        failed.HighLimit = high;
                        return failed;
                    }
                }

                var passed = TestItemResult.Passed(item, "Commands: " + string.Join("; ", commandTexts));
                passed.Value = lastValue;
                passed.Unit = "V";
                return passed;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
            finally
            {
                if (relayWasSet && relayOutputChannel != null && io != null)
                {
                    await io.WriteOutputAsync(relayOutputChannel.Value, false, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<TestItemResult> ExecuteMeasurementAsync(TestItem item, CancellationToken cancellationToken)
        {
            var adapterName = ReadStringParameter(item, "adapter");
            var adapter = ResolveMeasurementAdapter(adapterName);
            if (adapter == null)
            {
                return TestItemResult.FromError(
                    item,
                    new NotSupportedException("No hardware adapter is wired for TestPlan item: " + adapterName));
            }

            try
            {
                var step = BuildMeasurementStep(item, adapter.AdapterName);
                var stepResult = await adapter
                    .ExecuteAsync(step, new StationExecutionContext(), cancellationToken)
                    .ConfigureAwait(false);
                return ApplyTestPlanLimits(item, FromStepResult(item, stepResult));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return TestItemResult.FromError(item, ex);
            }
        }

        private IStationStepAdapter? ResolveMeasurementAdapter(string? adapterName)
        {
            if (string.Equals(adapterName, "UsbI2c", StringComparison.OrdinalIgnoreCase))
            {
                return _adapterRegistry.UsbI2c;
            }

            if (string.Equals(adapterName, "Oscilloscope", StringComparison.OrdinalIgnoreCase))
            {
                return _adapterRegistry.Oscilloscope;
            }

            if (string.Equals(adapterName, "DaqVoltage", StringComparison.OrdinalIgnoreCase))
            {
                return _adapterRegistry.DaqVoltage;
            }

            return null;
        }

        private static StepDefinition BuildMeasurementStep(TestItem item, string adapterName)
        {
            var operation = ReadStringParameter(item, "operation") ?? "ReadVoltage";
            var channel = ReadIntParameter(item, "channel", 1);
            var address = ReadStringParameter(item, "address");
            var description = BuildMeasurementDescription(item, operation, channel, address);

            return new StepDefinition
            {
                Name = item.Name,
                StepType = item.Kind == TestItemKind.LimitCheck ? StepType.NumericLimitTest : StepType.Action,
                AdapterName = adapterName,
                DescriptionRaw = description,
                SettingsRaw = description
            };
        }

        private static string BuildMeasurementDescription(
            TestItem item,
            string operation,
            int channel,
            string? address)
        {
            if (string.Equals(operation, "EnterAndCheckDebugMode", StringComparison.OrdinalIgnoreCase))
            {
                return "Locals.I2cDebugMode = EnterAndCheckDebugMode(" + (address ?? string.Empty) + ")";
            }

            if (string.Equals(operation, "ReadVavg", StringComparison.OrdinalIgnoreCase))
            {
                return "Locals.OscilloscopeVavg = ReadVavg(" + channel + ")";
            }

            var daqTarget = "Locals.AC_Input" + channel;
            return daqTarget + " = " + operation + "(" + channel + ")";
        }

        private static TestItemResult ApplyTestPlanLimits(TestItem item, TestItemResult result)
        {
            result.Unit = ReadStringParameter(item, "unit") ?? result.Unit;
            result.LowLimit = ReadNullableDoubleParameter(item, "low") ?? result.LowLimit;
            result.HighLimit = ReadNullableDoubleParameter(item, "high") ?? result.HighLimit;

            if (result.Status != StepStatus.Passed || (result.LowLimit == null && result.HighLimit == null))
            {
                return result;
            }

            double value;
            if (!TryReadDouble(result.Value, out value))
            {
                result.Status = StepStatus.Error;
                result.Message = "Measurement value is not numeric: " + result.Value;
                return result;
            }

            if ((result.LowLimit != null && value < result.LowLimit.Value)
                || (result.HighLimit != null && value > result.HighLimit.Value))
            {
                result.Status = StepStatus.Failed;
                result.Message = "Measurement outside limits: value=" + value.ToString(CultureInfo.InvariantCulture)
                    + "; low=" + LimitText(result.LowLimit)
                    + "; high=" + LimitText(result.HighLimit);
            }

            return result;
        }

        private static bool TryReadDouble(object? value, out double result)
        {
            if (value is double)
            {
                result = (double)value;
                return true;
            }

            if (value is IConvertible)
            {
                try
                {
                    result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    return true;
                }
                catch (FormatException)
                {
                }
                catch (InvalidCastException)
                {
                }
            }

            result = 0.0;
            return false;
        }

        private static string LimitText(double? value)
        {
            return value == null ? string.Empty : value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static bool IsStationFlowItem(TestItemKind kind)
        {
            return kind == TestItemKind.FixturePrepare
                || kind == TestItemKind.SafetyCheck
                || kind == TestItemKind.ResultOutput
                || kind == TestItemKind.Cleanup;
        }

        private static int ReadIntParameter(TestItem item, string name, int fallback)
        {
            var token = item.Parameters[name];
            return token == null || token.Type == JTokenType.Null ? fallback : token.ToObject<int>();
        }

        private static int? ReadNullableIntParameter(TestItem item, string name)
        {
            var token = item.Parameters[name];
            return token == null || token.Type == JTokenType.Null ? (int?)null : token.ToObject<int>();
        }

        private static bool ReadBoolParameter(TestItem item, string name, bool fallback)
        {
            var token = item.Parameters[name];
            return token == null || token.Type == JTokenType.Null ? fallback : token.ToObject<bool>();
        }

        private static bool ReadContextBool(WorkflowRunContext context, string name)
        {
            object? value;
            return context.Values.TryGetValue(name, out value) && value is bool && (bool)value;
        }

        private static TestItemResult MockResult(TestItem item)
        {
            var injected = item.Parameters["mock"] as JObject;
            if (injected != null)
            {
                return InjectedMockResult(item, injected);
            }

            var result = TestItemResult.Passed(item, "Mock " + item.Kind);
            ApplyMockValue(item, result);
            return result;
        }

        private static TestItemResult InjectedMockResult(TestItem item, JObject mock)
        {
            StepStatus status;
            var statusText = ReadString(mock, "status") ?? StepStatus.Passed.ToString();
            if (!Enum.TryParse(statusText, ignoreCase: true, result: out status))
            {
                status = StepStatus.Error;
            }

            var reason = ReadString(mock, "reason") ?? ReadString(mock, "message");
            var result = new TestItemResult
            {
                ItemId = item.Id,
                ItemName = item.Name,
                Kind = item.Kind,
                SourceReference = item.SourceReference,
                Status = status,
                Message = reason ?? "Mock " + status
            };

            var value = mock["value"];
            result.Value = value == null || value.Type == JTokenType.Null ? null : value.ToObject<object>();
            var expected = mock["expected"] ?? mock["expectedValue"];
            result.ExpectedValue = expected == null || expected.Type == JTokenType.Null ? null : expected.ToObject<object>();
            result.CompareType = ReadString(mock, "compareType");
            result.Target = ReadString(mock, "target");
            result.Sent = ReadString(mock, "sent");
            result.Reply = ReadString(mock, "reply");
            result.LowLimit = ReadNullableDouble(mock, "low") ?? ReadNullableDoubleParameter(item, "low");
            result.HighLimit = ReadNullableDouble(mock, "high") ?? ReadNullableDoubleParameter(item, "high");
            if (result.ExpectedValue == null && (result.LowLimit != null || result.HighLimit != null))
            {
                result.ExpectedValue = LimitText(result.LowLimit) + ".." + LimitText(result.HighLimit);
            }

            if (string.IsNullOrWhiteSpace(result.CompareType) && (result.LowLimit != null || result.HighLimit != null))
            {
                result.CompareType = "Range";
            }

            result.Unit = ReadString(mock, "unit") ?? ReadStringParameter(item, "unit");
            result.ExternalLogPath = ReadString(mock, "externalLogPath");

            if (!Enum.IsDefined(typeof(StepStatus), status))
            {
                result.Status = StepStatus.Error;
                result.Message = "Mock status is not supported: " + statusText;
            }

            return result;
        }

        private static void ApplyMockValue(TestItem item, TestItemResult result)
        {
            switch (item.Kind)
            {
                case TestItemKind.Flash:
                    result.Value = "PASS";
                    break;
                case TestItemKind.SafetyCheck:
                    result.Value = true;
                    break;
                case TestItemKind.Measurement:
                    result.Value = item.Id.IndexOf("i2c", StringComparison.OrdinalIgnoreCase) >= 0
                        ? (object)true
                        : ReadDoubleParameter(item, "low", 1.0);
                    result.Unit = ReadStringParameter(item, "unit");
                    break;
                case TestItemKind.FunctionalCheck:
                    result.Value = "PASS";
                    break;
                case TestItemKind.LimitCheck:
                    result.Value = ReadDoubleParameter(item, "low", 0.0);
                    result.LowLimit = ReadNullableDoubleParameter(item, "low");
                    result.HighLimit = ReadNullableDoubleParameter(item, "high");
                    result.Unit = ReadStringParameter(item, "unit");
                    break;
                default:
                    result.Value = item.Kind.ToString();
                    break;
            }
        }

        private static string? ReadStringParameter(TestItem item, string name)
        {
            var token = item.Parameters[name];
            return token == null ? null : token.ToString();
        }

        private static JObject BuildFunctionalGroupChildParameters(JObject groupParameters, JObject childDefinition)
        {
            var merged = new JObject();
            var groupOnlyParameters = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "template",
                "items",
                "powerOnBefore",
                "powerOffBefore"
            };
            var childMetadata = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "id",
                "name"
            };

            foreach (var property in groupParameters.Properties())
            {
                if (!groupOnlyParameters.Contains(property.Name))
                {
                    merged[property.Name] = property.Value.DeepClone();
                }
            }

            foreach (var property in childDefinition.Properties())
            {
                if (!childMetadata.Contains(property.Name))
                {
                    merged[property.Name] = property.Value.DeepClone();
                }
            }

            return merged;
        }

        private static TestItemResult FromFunctionalGroupChildResult(TestItem groupItem, string childName, TestItemResult childResult)
        {
            return new TestItemResult
            {
                ItemId = groupItem.Id,
                ItemName = groupItem.Name,
                Kind = groupItem.Kind,
                SourceReference = groupItem.SourceReference,
                Status = childResult.Status,
                Value = childResult.Value,
                ExpectedValue = childResult.ExpectedValue,
                CompareType = childResult.CompareType,
                Target = childResult.Target,
                Sent = childResult.Sent,
                Reply = childResult.Reply,
                Unit = childResult.Unit,
                LowLimit = childResult.LowLimit,
                HighLimit = childResult.HighLimit,
                Message = childName + ": " + childResult.Message,
                ExternalLogPath = childResult.ExternalLogPath,
                StartTime = childResult.StartTime,
                EndTime = childResult.EndTime,
                Error = childResult.Error
            };
        }

        private static string NormalizeChildId(string value)
        {
            var chars = value
                .Trim()
                .Select(x => char.IsLetterOrDigit(x) ? char.ToLowerInvariant(x) : '-')
                .ToArray();
            var normalized = new string(chars).Trim('-');
            return string.IsNullOrWhiteSpace(normalized) ? "child" : normalized;
        }

        private static int ReadRequiredHexParameter(TestItem item, string name)
        {
            var value = ReadStringParameter(item, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("Required parameter is missing: " + name);
            }

            return ParseInt(value!);
        }

        private static JArray ReadRequiredArrayParameter(TestItem item, string name)
        {
            var token = item.Parameters[name];
            if (token == null || token.Type != JTokenType.Array)
            {
                throw new JsonException("Required array parameter is missing: " + name);
            }

            return (JArray)token;
        }

        private static double ReadDoubleParameter(TestItem item, string name, double fallback)
        {
            var value = ReadNullableDoubleParameter(item, name);
            return value ?? fallback;
        }

        private static double? ReadNullableDoubleParameter(TestItem item, string name)
        {
            var token = item.Parameters[name];
            return token == null ? (double?)null : token.ToObject<double>();
        }

        private async Task ExecutePowerListAsync(TestItem item, string name, CancellationToken cancellationToken)
        {
            var defaultAction = name.IndexOf("Off", StringComparison.OrdinalIgnoreCase) >= 0 ? "PowerOff" : "PowerOn";
            await ExecutePowerArrayAsync(item.Parameters[name] as JArray, cancellationToken, defaultAction).ConfigureAwait(false);
        }

        private async Task ExecutePowerArrayAsync(JArray? items, CancellationToken cancellationToken, string defaultAction = "PowerOn")
        {
            if (items == null)
            {
                return;
            }

            foreach (var token in items)
            {
                var item = (JObject)token;
                var action = ReadString(item, "action") ?? defaultAction;
                var channel = ReadRequiredInt(item, "channel");
                var voltage = ReadNullableDouble(item, "voltage") ?? 0.0;
                var stepName = action + "-Channel" + channel + "-" + voltage.ToString("0.###", CultureInfo.InvariantCulture) + "V";
                var result = await _adapterRegistry.PowerSupply.ExecuteAsync(
                    new StepDefinition
                    {
                        Name = stepName,
                        StepType = StepType.Action,
                        AdapterName = _adapterRegistry.PowerSupply.AdapterName,
                        DescriptionRaw = stepName,
                        SettingsRaw = stepName
                    },
                    new StationExecutionContext(),
                    cancellationToken).ConfigureAwait(false);
                if (result.Status != StepStatus.Passed)
                {
                    throw new InvalidOperationException(result.Message ?? "Power supply step failed: " + stepName);
                }
            }
        }

        private static async Task DelayFromParameterAsync(TestItem item, string name, CancellationToken cancellationToken)
        {
            await DelayFromTokenAsync(item.Parameters[name], cancellationToken).ConfigureAwait(false);
        }

        private static async Task DelayFromTokenAsync(JToken? token, CancellationToken cancellationToken)
        {
            if (token == null || token.Type == JTokenType.Null)
            {
                return;
            }

            var delayMs = token.ToObject<int>();
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }

        private static StepDefinition BuildI2cWriteReadStep(string name, int address, int register, int readLength)
        {
            var expression = "Locals.I2C_ReadValue = WriteReadBytes("
                + Hex(address) + ",{" + Hex(register) + "}," + readLength + ")";
            return BuildAdapterStep(name, "UsbI2c", expression);
        }

        private static StepDefinition BuildI2cWriteStep(string name, int address, IEnumerable<byte> data)
        {
            var expression = "WriteBytes("
                + Hex(address) + ",{" + string.Join(",", data.Select(x => Hex(x))) + "})";
            return BuildAdapterStep(name, "UsbI2c", expression);
        }

        private static StepDefinition BuildI2cReadStep(string name, int address, int readLength)
        {
            var expression = "Locals.I2C_ReadValue = ReadBytes("
                + Hex(address) + "," + readLength + ")";
            return BuildAdapterStep(name, "UsbI2c", expression);
        }

        private static StepDefinition BuildDaqReadStep(string name, int channel)
        {
            return BuildAdapterStep(
                name,
                "DaqVoltage",
                "Locals.AC_Input" + channel + " = ReadVoltage(" + channel + ")");
        }

        private static StepDefinition BuildOscilloscopeVavgStep(string name, int channel)
        {
            return BuildAdapterStep(
                name,
                "Oscilloscope",
                "Locals.CH" + channel + "_VAVG = ReadVavg(" + channel + ")");
        }

        private static StepDefinition BuildAdapterStep(string name, string adapterName, string expression)
        {
            return new StepDefinition
            {
                Name = name,
                StepType = StepType.Action,
                AdapterName = adapterName,
                DescriptionRaw = expression,
                SettingsRaw = expression
            };
        }

        private static TestItemResult FailedFunctionalResult(TestItem item, string message, object? value)
        {
            var result = TestItemResult.Failed(item, message);
            result.Value = value;
            return result;
        }

        private static byte[] ParseBytes(object? value)
        {
            if (value is byte[])
            {
                return (byte[])value;
            }

            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
            return text
                .Split(new[] { ',', ' ', ';', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => (byte)ParseInt(x))
                .ToArray();
        }

        private static int ParseInt(string value)
        {
            var text = value.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return int.Parse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return int.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        private static int? ReadNullableHexInt(JObject item, string name)
        {
            var token = item[name];
            if (token == null || token.Type == JTokenType.Null)
            {
                return null;
            }

            return ParseInt(token.ToString());
        }

        private static bool BytesStartWith(byte[] actual, byte[] expected)
        {
            return actual.Length >= expected.Length && expected.SequenceEqual(actual.Take(expected.Length));
        }

        private static string ByteText(IEnumerable<byte> bytes)
        {
            return string.Join(" ", bytes.Select(x => x.ToString("X2", CultureInfo.InvariantCulture)));
        }

        private static string Hex(int value)
        {
            return "0x" + value.ToString("X2", CultureInfo.InvariantCulture);
        }

        private static string HexWord(int value)
        {
            return "0x" + value.ToString("X4", CultureInfo.InvariantCulture);
        }

        private static string HexLimitText(int? value)
        {
            return value == null ? "<none>" : HexWord(value.Value);
        }

        private static string ReadRequiredString(JObject item, string name)
        {
            var value = ReadString(item, name);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new JsonException("Required parameter is missing: " + name);
            }

            return value!;
        }

        private static string? ReadString(JObject item, string name)
        {
            var token = item[name];
            return token == null || token.Type == JTokenType.Null ? null : token.ToString();
        }

        private static int ReadRequiredInt(JObject item, string name)
        {
            var token = item[name];
            if (token == null || token.Type == JTokenType.Null)
            {
                throw new JsonException("Required parameter is missing: " + name);
            }

            return token.ToObject<int>();
        }

        private static double? ReadNullableDouble(JObject item, string name)
        {
            var token = item[name];
            return token == null || token.Type == JTokenType.Null ? (double?)null : token.ToObject<double>();
        }
    }
}
