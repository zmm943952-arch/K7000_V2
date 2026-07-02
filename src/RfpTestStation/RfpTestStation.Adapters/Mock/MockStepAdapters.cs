using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Adapters.Hardware;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.Mock
{
    public sealed class MockStationStepAdapter :
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
        private readonly IDictionary<string, int> _readCounts = new Dictionary<string, int>();

        public MockStationStepAdapter(string adapterName)
        {
            AdapterName = adapterName;
        }

        public string AdapterName { get; }

        public IList<string> Calls { get; } = new List<string>();

        public ISet<string> FailingStepNames { get; } = new HashSet<string>();

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            Calls.Add(step.Name);
            var target = AdapterStepParser.AssignmentTarget(step) ?? MockAssignmentTargetFor(step);
            var value = MockValueFor(step, target);
            if (value != null)
            {
                AdapterStepParser.SetContextValue(executionContext, target, value);
            }

            return Task.FromResult(new StepResult
            {
                StepName = step.Name,
                Status = FailingStepNames.Contains(step.Name) ? StepStatus.Failed : StepStatus.Passed,
                Message = AdapterName,
                Value = value
            });
        }

        private object? MockValueFor(StepDefinition step, string? target)
        {
            var text = AdapterStepParser.Text(step);
            if (text.IndexOf("ReadInputChannel", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return SafeInputDefault(target);
            }

            if (text.IndexOf("EnterAndCheckDebugMode", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (text.IndexOf("ReadBytes", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var args = AdapterStepParser.ArgumentsFor(step, "ReadBytes");
                var length = args.Count > 1 ? AdapterStepParser.ResolveInt(args[1], new StationExecutionContext(), 1) : 1;
                return new byte[Math.Max(0, length)];
            }

            if (text.IndexOf("ReadVavg", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return 1.2;
            }

            if (text.IndexOf("ReadVolt", StringComparison.OrdinalIgnoreCase) >= 0
                || ((target ?? string.Empty).IndexOf("AC_Input", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return NextAcInputValue(target);
            }

            return null;
        }

        private double NextAcInputValue(string? target)
        {
            var key = target ?? "AC_Input";
            _readCounts.TryGetValue(key, out var count);
            count++;
            _readCounts[key] = count;
            return count % 2 == 1 ? 3.3 : 0.0;
        }

        private static string? MockAssignmentTargetFor(StepDefinition step)
        {
            var match = Regex.Match(step.Name + " " + AdapterStepParser.Text(step), @"AC_Input(?<channel>\d+)", RegexOptions.IgnoreCase);
            return match.Success ? "Locals.AC_Input" + match.Groups["channel"].Value : null;
        }

        private static bool SafeInputDefault(string? target)
        {
            var text = target ?? string.Empty;
            if (text.IndexOf("急停", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (text.IndexOf("原点", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return false;
            }

            if (text.IndexOf("光栅", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (text.IndexOf("到位", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (text.IndexOf("按下", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return false;
        }
    }
}
