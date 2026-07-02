using RfpTestStation.Core.Expressions;
using RfpTestStation.Core.Model;
using Xunit;

namespace RfpTestStation.Tests.Expressions
{
    public sealed class TestStandExpressionEvaluatorTests
    {
        [Fact]
        public void EvaluateBooleanSupportsLocalsAndNotOperator()
        {
            var context = new ExecutionContext();
            context.Locals["X2气缸到位"] = false;

            var evaluator = new TestStandExpressionEvaluator();

            Assert.True(evaluator.EvaluateBoolean("!Locals.X2气缸到位", context));
        }

        [Fact]
        public void EvaluateBooleanSupportsRunStateSequenceFailed()
        {
            var context = new ExecutionContext { SequenceFailed = true };
            var evaluator = new TestStandExpressionEvaluator();

            Assert.True(evaluator.EvaluateBoolean("RunState.SequenceFailed", context));
        }

        [Fact]
        public void EvaluateBooleanSupportsComparisonAndLogicalOperators()
        {
            var context = new ExecutionContext();
            context.Locals["Value"] = 5;
            context.FileGlobals["Enabled"] = true;

            var evaluator = new TestStandExpressionEvaluator();

            Assert.True(evaluator.EvaluateBoolean("Locals.Value >= 3 && FileGlobals.Enabled", context));
            Assert.True(evaluator.EvaluateBoolean("Locals.Value < 3 || FileGlobals.Enabled", context));
        }

        [Fact]
        public void EvaluateNumberSupportsTimeInterval()
        {
            var evaluator = new TestStandExpressionEvaluator();

            Assert.Equal(1.0, evaluator.EvaluateNumber("TimeInterval(1)", new ExecutionContext()));
        }

        [Fact]
        public void EvaluateNumberSupportsArrayIndexShiftAndAddition()
        {
            var context = new ExecutionContext();
            context.Locals["I2C_ReadValue"] = new byte[] { 0, 0, 0, 0, 0x34, 0x12 };
            var evaluator = new TestStandExpressionEvaluator();

            Assert.Equal(0x1234, evaluator.EvaluateNumber("((Locals.I2C_ReadValue[5]<<8)+Locals.I2C_ReadValue[4])", context));
        }

        [Fact]
        public void EvaluateNumberSupportsMultiplication()
        {
            var context = new ExecutionContext();
            context.Locals["CH1_VAVG"] = 1.2;
            var evaluator = new TestStandExpressionEvaluator();

            Assert.Equal(12.0, evaluator.EvaluateNumber("Locals.CH1_VAVG*10", context));
        }

        [Fact]
        public void EvaluateThrowsForUnsupportedExpressions()
        {
            var evaluator = new TestStandExpressionEvaluator();

            Assert.Throws<ExpressionEvaluationException>(
                () => evaluator.EvaluateBoolean("Locals.Value + 1", new ExecutionContext()));
        }
    }
}
