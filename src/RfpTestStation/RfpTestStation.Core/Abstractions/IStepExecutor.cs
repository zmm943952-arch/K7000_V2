using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running;

namespace RfpTestStation.Core.Abstractions
{
    public interface IStepExecutor
    {
        Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken);
    }
}
