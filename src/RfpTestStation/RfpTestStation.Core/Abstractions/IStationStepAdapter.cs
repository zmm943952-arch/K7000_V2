using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Core.Abstractions
{
    public interface IStationStepAdapter
    {
        string AdapterName { get; }

        Task<StepResult> ExecuteAsync(StepDefinition step, StationExecutionContext executionContext, CancellationToken cancellationToken);
    }
}
