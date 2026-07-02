using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running.Executors
{
    public sealed class UnsupportedStepExecutor : IStepExecutor
    {
        public Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(new StepResult
            {
                StepName = context.Step.Name,
                Status = StepStatus.Error,
                Message = "Unsupported step type: " + context.Step.StepTypeRaw
            });
        }
    }
}
