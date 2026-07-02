using System;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running
{
    public static class ModuleStepClassifier
    {
        public static bool HasCallableModule(StepDefinition step)
        {
            var text = ModuleText(step);
            var adapterName = step.AdapterName ?? step.ModuleCall?.AdapterName ?? string.Empty;

            return text.IndexOf(".vi", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("Assembly:", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf(".dll", StringComparison.OrdinalIgnoreCase) >= 0
                || (!string.IsNullOrWhiteSpace(adapterName) && !string.Equals(adapterName, "<None>", StringComparison.OrdinalIgnoreCase));
        }

        public static string ModuleText(StepDefinition step)
        {
            return string.Join(" ", new[]
            {
                step.Name,
                step.AdapterName,
                step.DescriptionRaw,
                step.ModuleCall?.RawText,
                step.ModuleCall?.ViPath,
                step.ModuleCall?.TypeName,
                step.ModuleCall?.MethodName,
                step.ModuleCall?.AssemblyName
            });
        }
    }
}
