using System;

namespace RfpTestStation.Core.Expressions
{
    public sealed class ExpressionEvaluationException : Exception
    {
        public ExpressionEvaluationException(string expression, string message)
            : base("Cannot evaluate expression '" + expression + "': " + message)
        {
            Expression = expression;
        }

        public string Expression { get; }
    }
}
