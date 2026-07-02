using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Workflow;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Adapters.TestPlans
{
    public interface ITestPlanFlashAdapter
    {
        Task<StepResult> ExecuteAsync(TestItem item, StationExecutionContext executionContext, CancellationToken cancellationToken);
    }
}
