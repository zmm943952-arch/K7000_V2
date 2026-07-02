using System;
using System.Threading;
using System.Threading.Tasks;
using PreparedArtifactPaths;
using ReadConfigLib;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;
using RfpTestStation.Adapters.Hardware;

namespace RfpTestStation.Adapters.Config
{
    public sealed class ConfigAdapter : IConfigAdapter
    {
        private readonly string _repoRoot;

        public ConfigAdapter(string repoRoot)
        {
            _repoRoot = repoRoot;
        }

        public string AdapterName
        {
            get { return "Config"; }
        }

        public Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken)
        {
            try
            {
                var text = AdapterStepParser.Text(step);
                object value;

                if (text.IndexOf("ReadConfigLib.JsonConfigReader", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    value = ExecuteJsonConfig(step, executionContext);
                }
                else if (text.IndexOf("PreparedArtifactPaths.PreparedArtifactPathsReader", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    value = ExecutePreparedArtifactReader(step);
                }
                else
                {
                    value = string.Empty;
                }

                AdapterStepParser.SetContextValue(executionContext, AdapterStepParser.AssignmentTarget(step), value);
                return Task.FromResult(HardwareStepResult.Passed(step, AdapterName, value));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HardwareStepResult.Error(step, AdapterName, ex));
            }
        }

        private object ExecuteJsonConfig(StepDefinition step, StationExecutionContext executionContext)
        {
            var quoted = AdapterStepParser.QuotedStrings(step);
            if (quoted.Count < 2)
            {
                return string.Empty;
            }

            var key = quoted[1];
            var defaultValue = quoted.Count > 2 ? quoted[2] : string.Empty;
            var jsonPath = AdapterStepParser.ResolveJsonPath(executionContext, _repoRoot);
            return JsonConfigReader.GetValue(jsonPath, key, defaultValue);
        }

        private object ExecutePreparedArtifactReader(StepDefinition step)
        {
            var quoted = AdapterStepParser.QuotedStrings(step);
            var projectRoot = System.IO.Path.Combine(_repoRoot, "Runtime");

            if (AdapterStepParser.Contains(step, "ReadRfpJoined"))
            {
                var projectName = quoted.Count > 1 ? quoted[1] : string.Empty;
                var separator = quoted.Count > 2 ? quoted[2] : "|";
                return PreparedArtifactPathsReader.ReadRfpJoined(projectRoot, projectName, separator);
            }

            if (AdapterStepParser.Contains(step, "ReadTcon"))
            {
                return PreparedArtifactPathsReader.ReadTcon(projectRoot);
            }

            if (AdapterStepParser.Contains(step, "ReadTddi"))
            {
                return PreparedArtifactPathsReader.ReadTddi(projectRoot);
            }

            return string.Empty;
        }
    }
}
