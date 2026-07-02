using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using RfpTestStation.Core.Model;

namespace RfpTestStation.Core.Expressions
{
    public sealed class TestStandExpressionEvaluator
    {
        public ExpressionResult Evaluate(string expression, ExecutionContext context)
        {
            try
            {
                var parser = new Parser(expression, context);
                return new ExpressionResult(expression, parser.Parse());
            }
            catch (ExpressionEvaluationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ExpressionEvaluationException(expression, ex.Message);
            }
        }

        public bool EvaluateBoolean(string expression, ExecutionContext context)
        {
            return ToBoolean(Evaluate(expression, context).Value);
        }

        public double EvaluateNumber(string expression, ExecutionContext context)
        {
            return ToNumber(Evaluate(expression, context).Value);
        }

        private static bool ToBoolean(object? value)
        {
            if (value is bool boolean)
            {
                return boolean;
            }

            if (value is double number)
            {
                return Math.Abs(number) > double.Epsilon;
            }

            if (value is int integer)
            {
                return integer != 0;
            }

            if (value is string text && bool.TryParse(text, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException("Value cannot be converted to Boolean.");
        }

        private static double ToNumber(object? value)
        {
            if (value is double number)
            {
                return number;
            }

            if (value is int integer)
            {
                return integer;
            }

            if (value is byte byteValue)
            {
                return byteValue;
            }

            if (value is decimal decimalValue)
            {
                return (double)decimalValue;
            }

            if (value is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            throw new InvalidOperationException("Value cannot be converted to Number.");
        }

        private sealed class Parser
        {
            private readonly string _expression;
            private readonly ExecutionContext _context;
            private readonly Tokenizer _tokenizer;
            private Token _current;

            public Parser(string expression, ExecutionContext context)
            {
                _expression = expression;
                _context = context;
                _tokenizer = new Tokenizer(expression);
                _current = _tokenizer.Next();
            }

            public object? Parse()
            {
                var value = ParseOr();
                Expect(TokenType.End);
                return value;
            }

            private object? ParseOr()
            {
                var left = ParseAnd();
                while (_current.Type == TokenType.OrOr)
                {
                    Advance();
                    var right = ParseAnd();
                    left = ToBoolean(left) || ToBoolean(right);
                }

                return left;
            }

            private object? ParseAnd()
            {
                var left = ParseEquality();
                while (_current.Type == TokenType.AndAnd)
                {
                    Advance();
                    var right = ParseEquality();
                    left = ToBoolean(left) && ToBoolean(right);
                }

                return left;
            }

            private object? ParseEquality()
            {
                var left = ParseComparison();
                while (_current.Type == TokenType.EqualEqual || _current.Type == TokenType.BangEqual)
                {
                    var type = _current.Type;
                    Advance();
                    var right = ParseComparison();
                    var equal = AreEqual(left, right);
                    left = type == TokenType.EqualEqual ? equal : !equal;
                }

                return left;
            }

            private object? ParseComparison()
            {
                var left = ParseShift();
                while (_current.Type == TokenType.Less
                    || _current.Type == TokenType.LessEqual
                    || _current.Type == TokenType.Greater
                    || _current.Type == TokenType.GreaterEqual)
                {
                    var type = _current.Type;
                    Advance();
                    var right = ParseShift();
                    var comparison = Compare(left, right);

                    switch (type)
                    {
                        case TokenType.Less:
                            left = comparison < 0;
                            break;
                        case TokenType.LessEqual:
                            left = comparison <= 0;
                            break;
                        case TokenType.Greater:
                            left = comparison > 0;
                            break;
                        case TokenType.GreaterEqual:
                            left = comparison >= 0;
                            break;
                    }
                }

                return left;
            }

            private object? ParseShift()
            {
                var left = ParseAdditive();
                while (_current.Type == TokenType.ShiftLeft)
                {
                    Advance();
                    var right = ParseAdditive();
                    left = ToInt32(left) << ToInt32(right);
                }

                return left;
            }

            private object? ParseAdditive()
            {
                var left = ParseMultiplicative();
                while (_current.Type == TokenType.Plus)
                {
                    Advance();
                    var right = ParseMultiplicative();
                    left = Add(left, right);
                }

                return left;
            }

            private object? ParseMultiplicative()
            {
                var left = ParseUnary();
                while (_current.Type == TokenType.Star)
                {
                    Advance();
                    var right = ParseUnary();
                    left = ToNumber(left) * ToNumber(right);
                }

                return left;
            }

            private object? ParseUnary()
            {
                if (_current.Type == TokenType.Bang)
                {
                    Advance();
                    return !ToBoolean(ParseUnary());
                }

                return ParsePrimary();
            }

            private object? ParsePrimary()
            {
                switch (_current.Type)
                {
                    case TokenType.Number:
                        var number = _current.NumberValue;
                        Advance();
                        return number;
                    case TokenType.String:
                        var text = _current.Text;
                        Advance();
                        return text;
                    case TokenType.Identifier:
                        return ParseIdentifierOrFunction();
                    case TokenType.OpenParen:
                        Advance();
                        var value = ParseOr();
                        Expect(TokenType.CloseParen);
                        return value;
                    default:
                        throw Error("Unexpected token: " + _current.Text);
                }
            }

            private object? ParseIdentifierOrFunction()
            {
                var identifier = _current.Text;
                Advance();

                if (_current.Type == TokenType.OpenParen)
                {
                    return ParseFunctionCall(identifier);
                }

                if (string.Equals(identifier, "True", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                if (string.Equals(identifier, "False", StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                return ResolveIdentifier(identifier);
            }

            private object? ParseFunctionCall(string functionName)
            {
                Advance();
                var args = new List<object?>();
                if (_current.Type != TokenType.CloseParen)
                {
                    args.Add(ParseOr());
                    while (_current.Type == TokenType.Comma)
                    {
                        Advance();
                        args.Add(ParseOr());
                    }
                }

                Expect(TokenType.CloseParen);

                if (string.Equals(functionName, "TimeInterval", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Count != 1)
                    {
                        throw Error("TimeInterval expects one argument.");
                    }

                    return ToNumber(args[0]);
                }

                throw Error("Unsupported function: " + functionName);
            }

            private object? ResolveIdentifier(string identifier)
            {
                if (identifier.StartsWith("Locals.", StringComparison.OrdinalIgnoreCase))
                {
                    return GetValue(_context.Locals, identifier.Substring("Locals.".Length), identifier);
                }

                if (identifier.StartsWith("FileGlobals.", StringComparison.OrdinalIgnoreCase))
                {
                    return GetValue(_context.FileGlobals, identifier.Substring("FileGlobals.".Length), identifier);
                }

                if (identifier.StartsWith("Parameters.", StringComparison.OrdinalIgnoreCase))
                {
                    return GetValue(_context.Parameters, identifier.Substring("Parameters.".Length), identifier);
                }

                if (identifier.StartsWith("RunState.", StringComparison.OrdinalIgnoreCase))
                {
                    var key = identifier.Substring("RunState.".Length);
                    if (string.Equals(key, "SequenceFailed", StringComparison.OrdinalIgnoreCase))
                    {
                        return _context.SequenceFailed;
                    }

                    return GetValue(_context.RunState, key, identifier);
                }

                throw Error("Unsupported identifier: " + identifier);
            }

            private object? GetValue(IDictionary<string, object> values, string key, string identifier)
            {
                if (values.TryGetValue(key, out var value))
                {
                    return value;
                }

                var indexedValue = TryGetIndexedValue(values, key);
                if (indexedValue.Found)
                {
                    return indexedValue.Value;
                }

                throw Error("Identifier not found: " + identifier);
            }

            private static IndexedValue TryGetIndexedValue(IDictionary<string, object> values, string key)
            {
                var openBracket = key.LastIndexOf('[');
                if (openBracket <= 0 || !key.EndsWith("]", StringComparison.Ordinal))
                {
                    return IndexedValue.NotFound;
                }

                var baseKey = key.Substring(0, openBracket);
                var indexText = key.Substring(openBracket + 1, key.Length - openBracket - 2);
                if (!int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var index)
                    || !values.TryGetValue(baseKey, out var value))
                {
                    return IndexedValue.NotFound;
                }

                if (value is byte[] bytes && index >= 0 && index < bytes.Length)
                {
                    return new IndexedValue(true, bytes[index]);
                }

                if (value is Array array && index >= 0 && index < array.Length)
                {
                    return new IndexedValue(true, array.GetValue(index));
                }

                if (value is IList list && index >= 0 && index < list.Count)
                {
                    return new IndexedValue(true, list[index]);
                }

                return IndexedValue.NotFound;
            }

            private static bool AreEqual(object? left, object? right)
            {
                if (TryConvertToNumber(left, out var leftNumber) && TryConvertToNumber(right, out var rightNumber))
                {
                    return Math.Abs(leftNumber - rightNumber) < double.Epsilon;
                }

                return string.Equals(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            }

            private static int Compare(object? left, object? right)
            {
                if (TryConvertToNumber(left, out var leftNumber) && TryConvertToNumber(right, out var rightNumber))
                {
                    return leftNumber.CompareTo(rightNumber);
                }

                return string.Compare(Convert.ToString(left, CultureInfo.InvariantCulture), Convert.ToString(right, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            }

            private static object Add(object? left, object? right)
            {
                if (left is string || right is string)
                {
                    return Convert.ToString(left, CultureInfo.InvariantCulture) + Convert.ToString(right, CultureInfo.InvariantCulture);
                }

                return ToNumber(left) + ToNumber(right);
            }

            private static int ToInt32(object? value)
            {
                return Convert.ToInt32(ToNumber(value), CultureInfo.InvariantCulture);
            }

            private static bool TryConvertToNumber(object? value, out double number)
            {
                if (value is double doubleValue)
                {
                    number = doubleValue;
                    return true;
                }

                if (value is int intValue)
                {
                    number = intValue;
                    return true;
                }

                if (value is byte byteValue)
                {
                    number = byteValue;
                    return true;
                }

                if (value is string text && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                {
                    number = parsed;
                    return true;
                }

                number = 0;
                return false;
            }

            private void Expect(TokenType type)
            {
                if (_current.Type != type)
                {
                    throw Error("Expected " + type + " but found " + _current.Text);
                }

                Advance();
            }

            private void Advance()
            {
                _current = _tokenizer.Next();
            }

            private ExpressionEvaluationException Error(string message)
            {
                return new ExpressionEvaluationException(_expression, message);
            }
        }

            private sealed class Tokenizer
        {
            private readonly string _expression;
            private int _position;

            public Tokenizer(string expression)
            {
                _expression = expression ?? string.Empty;
            }

            public Token Next()
            {
                SkipWhitespace();

                if (_position >= _expression.Length)
                {
                    return new Token(TokenType.End, string.Empty);
                }

                var ch = _expression[_position];
                if (char.IsDigit(ch))
                {
                    return ReadNumber();
                }

                if (ch == '"')
                {
                    return ReadString();
                }

                if (IsIdentifierStart(ch))
                {
                    return ReadIdentifier();
                }

                _position++;
                switch (ch)
                {
                    case '!':
                        if (TryConsume('='))
                        {
                            return new Token(TokenType.BangEqual, "!=");
                        }

                        return new Token(TokenType.Bang, "!");
                    case '&':
                        if (TryConsume('&'))
                        {
                            return new Token(TokenType.AndAnd, "&&");
                        }
                        break;
                    case '|':
                        if (TryConsume('|'))
                        {
                            return new Token(TokenType.OrOr, "||");
                        }
                        break;
                    case '=':
                        if (TryConsume('='))
                        {
                            return new Token(TokenType.EqualEqual, "==");
                        }
                        break;
                    case '<':
                        if (TryConsume('<'))
                        {
                            return new Token(TokenType.ShiftLeft, "<<");
                        }

                        if (TryConsume('='))
                        {
                            return new Token(TokenType.LessEqual, "<=");
                        }

                        return new Token(TokenType.Less, "<");
                    case '>':
                        if (TryConsume('='))
                        {
                            return new Token(TokenType.GreaterEqual, ">=");
                        }

                        return new Token(TokenType.Greater, ">");
                    case '(':
                        return new Token(TokenType.OpenParen, "(");
                    case ')':
                        return new Token(TokenType.CloseParen, ")");
                    case ',':
                        return new Token(TokenType.Comma, ",");
                    case '+':
                        return new Token(TokenType.Plus, "+");
                    case '*':
                        return new Token(TokenType.Star, "*");
                }

                throw new InvalidOperationException("Unsupported token: " + ch);
            }

            private Token ReadNumber()
            {
                var start = _position;
                if (_position + 1 < _expression.Length
                    && _expression[_position] == '0'
                    && (_expression[_position + 1] == 'x' || _expression[_position + 1] == 'X'))
                {
                    _position += 2;
                    while (_position < _expression.Length && Uri.IsHexDigit(_expression[_position]))
                    {
                        _position++;
                    }

                    var hexText = _expression.Substring(start + 2, _position - start - 2);
                    return new Token(TokenType.Number, _expression.Substring(start, _position - start), Convert.ToInt32(hexText, 16));
                }

                while (_position < _expression.Length && (char.IsDigit(_expression[_position]) || _expression[_position] == '.'))
                {
                    _position++;
                }

                var text = _expression.Substring(start, _position - start);
                return new Token(TokenType.Number, text, double.Parse(text, CultureInfo.InvariantCulture));
            }

            private Token ReadString()
            {
                _position++;
                var start = _position;
                while (_position < _expression.Length && _expression[_position] != '"')
                {
                    _position++;
                }

                if (_position >= _expression.Length)
                {
                    throw new InvalidOperationException("Unterminated string literal.");
                }

                var text = _expression.Substring(start, _position - start);
                _position++;
                return new Token(TokenType.String, text);
            }

            private Token ReadIdentifier()
            {
                var start = _position;
                while (_position < _expression.Length && IsIdentifierPart(_expression[_position]))
                {
                    _position++;
                }

                return new Token(TokenType.Identifier, _expression.Substring(start, _position - start));
            }

            private void SkipWhitespace()
            {
                while (_position < _expression.Length && char.IsWhiteSpace(_expression[_position]))
                {
                    _position++;
                }
            }

            private bool TryConsume(char expected)
            {
                if (_position < _expression.Length && _expression[_position] == expected)
                {
                    _position++;
                    return true;
                }

                return false;
            }

            private static bool IsIdentifierStart(char ch)
            {
                return char.IsLetter(ch) || ch == '_';
            }

            private static bool IsIdentifierPart(char ch)
            {
                return !char.IsWhiteSpace(ch)
                    && ch != '!'
                    && ch != '&'
                    && ch != '|'
                    && ch != '='
                    && ch != '<'
                    && ch != '>'
                    && ch != '('
                    && ch != ')'
                    && ch != ','
                    && ch != '+'
                    && ch != '*';
            }
        }

        private sealed class Token
        {
            public Token(TokenType type, string text, double numberValue = 0)
            {
                Type = type;
                Text = text;
                NumberValue = numberValue;
            }

            public TokenType Type { get; }

            public string Text { get; }

            public double NumberValue { get; }
        }

        private enum TokenType
        {
            End,
            Identifier,
            Number,
            String,
            Bang,
            AndAnd,
            OrOr,
            EqualEqual,
            BangEqual,
            Less,
            LessEqual,
            Greater,
            GreaterEqual,
            OpenParen,
            CloseParen,
            Comma,
            Plus,
            ShiftLeft,
            Star
        }

        private readonly struct IndexedValue
        {
            public IndexedValue(bool found, object? value)
            {
                Found = found;
                Value = value;
            }

            public bool Found { get; }

            public object? Value { get; }

            public static IndexedValue NotFound
            {
                get { return new IndexedValue(false, null); }
            }
        }
    }
}
