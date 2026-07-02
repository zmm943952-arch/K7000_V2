using System.Collections.Generic;
using RfpTestStation.Core.Abstractions;
using RfpTestStation.Core.Model;
using RfpTestStation.Core.Running.Executors;

namespace RfpTestStation.Core.Running
{
    public sealed class StepExecutorRegistry
    {
        private readonly IDictionary<StepType, IStepExecutor> _executors = new Dictionary<StepType, IStepExecutor>();
        private readonly IStepExecutor _unsupportedExecutor = new UnsupportedStepExecutor();

        public StepExecutorRegistry()
        {
            RegisterDefaults();
        }

        public static StepExecutorRegistry CreateDefault()
        {
            return new StepExecutorRegistry();
        }

        public void Register(StepType stepType, IStepExecutor executor)
        {
            _executors[stepType] = executor;
        }

        public IStepExecutor Resolve(StepDefinition step)
        {
            return _executors.TryGetValue(step.StepType, out var executor) ? executor : _unsupportedExecutor;
        }

        private void RegisterDefaults()
        {
            var action = new ActionStepExecutor();
            var limit = new LimitStepExecutors();

            Register(StepType.Action, action);
            Register(StepType.Statement, action);
            Register(StepType.Label, action);
            Register(StepType.Goto, action);
            Register(StepType.MessagePopup, action);

            Register(StepType.Wait, new WaitStepExecutor());
            Register(StepType.If, new IfStepExecutor());
            Register(StepType.Else, new ElseStepExecutor());
            Register(StepType.ElseIf, new IfStepExecutor());
            Register(StepType.While, new WhileStepExecutor());
            Register(StepType.For, new ForStepExecutor());
            Register(StepType.ForEach, new ForStepExecutor());
            Register(StepType.SequenceCall, new SequenceCallStepExecutor());

            Register(StepType.NumericLimitTest, limit);
            Register(StepType.StringValueTest, limit);
            Register(StepType.PassFailTest, limit);
            Register(StepType.MultipleNumericLimitTest, limit);
        }
    }
}
