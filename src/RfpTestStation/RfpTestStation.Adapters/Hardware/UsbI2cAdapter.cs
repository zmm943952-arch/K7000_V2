using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using USB2IICDll;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Hardware
{
    public sealed class UsbI2cAdapter : IUsbI2cAdapter
    {
        public string AdapterName
        {
            get { return "UsbI2c"; }
        }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            try
            {
                var text = AdapterStepParser.Text(step);
                if (text.IndexOf("EnterAndCheckDebugMode", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var passed = TestStandApi.EnterAndCheckDebugMode();
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), passed);
                    return Task.FromResult(passed
                        ? HardwareStepResult.Passed(step, AdapterName, passed)
                        : HardwareStepResult.Failed(step, AdapterName, passed));
                }

                if (text.IndexOf("WriteReadBytes", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var args = AdapterStepParser.ArgumentsFor(step, "WriteReadBytes");
                    var data = TestStandApi.WriteReadBytes(
                        AdapterStepParser.ResolveInt(args[0], executionContext),
                        AdapterStepParser.ByteArrayArgument(args[1]),
                        AdapterStepParser.ResolveInt(args[2], executionContext));
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), data);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, string.Join(",", data.Select(x => x.ToString("X2")))));
                }

                if (text.IndexOf("WriteBytes", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var args = AdapterStepParser.ArgumentsFor(step, "WriteBytes");
                    TestStandApi.WriteBytes(
                        AdapterStepParser.ResolveInt(args[0], executionContext),
                        AdapterStepParser.ByteArrayArgument(args[1]));
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName));
                }

                if (text.IndexOf("ReadBytes", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var args = AdapterStepParser.ArgumentsFor(step, "ReadBytes");
                    var data = TestStandApi.ReadBytes(
                        AdapterStepParser.ResolveInt(args[0], executionContext),
                        AdapterStepParser.ResolveInt(args[1], executionContext));
                    AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), data);
                    return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, string.Join(",", data.Select(x => x.ToString("X2")))));
                }

                return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, null, "No I2C action matched."));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HardwareStepResult.Error(step, AdapterName, ex));
            }
        }
    }
}
