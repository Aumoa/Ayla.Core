#nullable enable

using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Ayla.Core
{
    public static partial class ExpressionSerializer
    {
        private static class RegexPatterns
        {
            public static readonly Regex ExpressionTypeCode = new("^\"TypeName\"(?:[ ]+)?:(?:[ ]+)?\"([A-Za-z_][A-Za-z0-9_.]+)\"");
            public static readonly Regex JsonMember = new(@"^""([A-Za-z_][A-Za-z0-9_]+)""(?:[ \r\n\t]+)?:(?:[ \r\n\t]+)?");

            public static readonly char[] TrimNextMember = new[] { ' ', '\t', '\r', '\n', ',' };
            public static readonly char[] TrimNext = new[] { ' ', '\t', '\r', '\n' };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Expression Deserialize(string expression)
        {
            return Deserialize(expression, expression.AsSpan());
        }

        private static Expression Deserialize(string ss, ReadOnlySpan<char> expression)
        {
            if (expression[0] != '{')
            {
                throw new FormatException("Expression must start with '{'");
            }

            expression = expression[1..].TrimStart(RegexPatterns.TrimNext);
            var match = RegexPatterns.ExpressionTypeCode.Match(ss, StringDistance(ss, expression), expression.Length);
            if (match.Success == false)
            {
                throw new FormatException("Invalid expression format: The first JSON member of the Expression item must start with TypeName.");
            }

            switch (match.Groups[1].Value)
            {
                case "System.Linq.Expressions.BinaryExpression":
                    return DeserializeBinaryExpression(ss, expression[match.Groups[0].Value.Length..]);
                default:
                    throw new NotSupportedException($"Unsupported expression type: {match.Groups[1].Value}");
            }
        }

        private static BinaryExpression DeserializeBinaryExpression(string ss, ReadOnlySpan<char> expression)
        {
            expression = expression.TrimStart(RegexPatterns.TrimNextMember);

            ExpressionType? nodeType = null;
            Expression? left = null;
            Expression? right = null;
            while (nodeType.HasValue == false || left == null || right == null)
            {
                var match = RegexPatterns.JsonMember.Match(ss, StringDistance(ss, expression), expression.Length);
                if (match.Success == false)
                {
                    throw new FormatException("Invalid binary expression format: missing member");
                }

                var memberName = match.Groups[1].Value;
                expression = expression[match.Groups[0].Value.Length..];
                switch (memberName)
                {
                    case "NodeType":
                        if (nodeType.HasValue)
                        {
                            throw new FormatException("Duplicate 'NodeType' member in binary expression");
                        }
                        nodeType = DeserializeEnum<ExpressionType>(ss, expression);
                        expression = expression[match.Groups[0].Value.Length..].TrimStart(RegexPatterns.TrimNextMember);
                        break;
                    case "Left":
                        if (left != null)
                        {
                            throw new FormatException("Duplicate 'Left' member in binary expression");
                        }
                        left = Deserialize(ss, expression);
                        break;
                    case "Right":
                        if (right != null)
                        {
                            throw new FormatException("Duplicate 'Right' member in binary expression");
                        }
                        right = Deserialize(ss, expression);
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported member '{memberName}' in binary expression");
                }
            }

            return Expression.MakeBinary(nodeType.Value, left, right);
        }

        private static T DeserializeEnum<T>(string ss, ReadOnlySpan<char> expression) where T : struct, Enum
        {
            for (int i = 1; i < expression.Length; ++i)
            {
                if (expression[i] == '\"')
                {
                    return (T)Enum.Parse(typeof(T), expression[1..i].ToString());
                }
            }

            throw new Exception($"Failed to deserialize enum of type {typeof(T).Name} from expression: {ss}");
        }

        private static unsafe int StringDistance(string ss, ReadOnlySpan<char> ds)
        {
            ref var ssr = ref MemoryMarshal.GetReference(ss.AsSpan());
            ref var dsr = ref MemoryMarshal.GetReference(ds);
            int distance = (int)((char*)Unsafe.AsPointer(ref dsr) - (char*)Unsafe.AsPointer(ref ssr));
            return distance;
        }
    }
}
