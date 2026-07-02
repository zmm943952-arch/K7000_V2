using System.Threading;
using System.Threading.Tasks;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Running.Executors
{
    public sealed class ActionStepExecutor : IStepExecutor
    {
        public Task<StepResult> ExecuteAsync(StepExecutionContext context, CancellationToken cancellationToken)
        {
            if (ModuleStepClassifier.HasCallableModule(context.Step))
            {
                return context.AdapterRegistry
                    .ResolveActionAdapter(context.Step)
                    .ExecuteAsync(context.Step, context.ExecutionContext, cancellationToken);
            }

            return Task.FromResult(new StepResult
            {
                StepName = context.Step.Name,
                Status = StepStatus.Passed
            });
        }
    }
}
