namespace RfpTestStation.Core.Expressions
{
    public sealed class ExpressionResult
    {
        public ExpressionResult(string expression, object? value)
        {
            Expression = expression;
            Value = value;
        }

        public string Expression { get; }

        public object? Value { get; }
    }
}
