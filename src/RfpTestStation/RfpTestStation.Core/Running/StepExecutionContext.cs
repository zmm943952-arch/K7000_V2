using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Expressions;
using RfpTestStation.Core.Model;
using StationExecutionContext = RfpTestStation.Core.Model.ExecutionContext;

namespace RfpTestStation.Core.Running
{
    public sealed class StepExecutionContext
    {
        public StepExecutionContext(
            StepDefinition step,
            StationExecutionContext executionContext,
            SequenceRunner runner,
            IClock clock,
            TestStandExpressionEvaluator expressionEvaluator,
            IStationAdapterRegistry adapterRegistry)
        {
            Step = step;
            ExecutionContext = executionContext;
            Runner = runner;
            Clock = clock;
            ExpressionEvaluator = expressionEvaluator;
            AdapterRegistry = adapterRegistry;
        }

        public StepDefinition Step { get; }

        public StationExecutionContext ExecutionContext { get; }

        public SequenceRunner Runner { get; }

        public IClock Clock { get; }

        public TestStandExpressionEvaluator ExpressionEvaluator { get; }

        public IStationAdapterRegistry AdapterRegistry { get; }
    }
}
