#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine.Pool;

namespace Ayla.Core
{
    public static partial class ExpressionSerializer
    {
        private record SerializationContext
        {
            public readonly struct IndentScope : IDisposable
            {
                private readonly SerializationContext m_Context;
                private readonly int m_Level;

                public IndentScope(SerializationContext context)
                {
                    m_Context = context;
                    m_Level = context.IndentLevel++;
                }

                public void Dispose()
                {
                    m_Context.IndentLevel = m_Level;
                }
            }

            public StringBuilder Builder { get; } = new();

            public int IndentLevel { get; set; } = 0;

            public string IndentPrefix => new(' ', IndentLevel * 2);

            public IndentScope Indent()
            {
                return new IndentScope(this);
            }

            public string AddIndentNewLine(Func<string> body)
            {
                using var scope1 = Indent();
                return body() + '\n';
            }

            public string Indented(string format)
            {
                return IndentPrefix + format;
            }

            public string IndentedJson(string name, object valueFormat)
            {
                return Indented($"\"{name}\": {valueFormat}");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string Serialize(this Expression value)
        {
            var context = new SerializationContext();
            return Serialize(value, context);
        }

        private static string Serialize(Expression value, SerializationContext context)
        {
            switch (value)
            {
                case BinaryExpression binaryExpression:
                    return Serialize(binaryExpression, context);
                case LambdaExpression lambdaExpression:
                    return Serialize(lambdaExpression, context);
                case MethodCallExpression methodCallExpression:
                    return Serialize(methodCallExpression, context);
                case ConstantExpression constantExpression:
                    return Serialize(constantExpression, context);
                case NewArrayExpression newArrayExpression:
                    return Serialize(newArrayExpression, context);
                case null:
                    return "null";
                default:
                    throw new Exception($"Unsupported expression type: {value.GetType()}");
            }
        }

        private static string Serialize(NewArrayExpression value, SerializationContext context)
        {
            return "{\n" +
                context.AddIndentNewLine(() =>
                {
                    return string.Join(",\n",
                        context.IndentedJson("TypeName", typeof(NewArrayExpression).FullName.Quotes()),
                        context.IndentedJson("Type", value.Type.FullName.Quotes()),
                        context.IndentedJson("Expressions", Serialize(value.Expressions, context))
                        );
                }) +
                context.Indented("}");
        }

        private static string Serialize(ConstantExpression value, SerializationContext context)
        {
            if (value.Value is null)
            {
                return "null";
            }

            return "{\n" +
                context.AddIndentNewLine(() =>
                {
                    return string.Join(",\n",
                        context.IndentedJson("TypeName", typeof(ConstantExpression).FullName.Quotes()),
                        context.IndentedJson("Value", value.Value?.ToString().Quotes() ?? "null"),
                        context.IndentedJson("Type", value.Type.FullName.Quotes())
                        );
                }) +
                context.Indented("}");
        }

        private static string Serialize(MethodCallExpression value, SerializationContext context)
        {
            return "{\n" +
                context.AddIndentNewLine(() =>
                {
                    return string.Join(",\n",
                        context.IndentedJson("TypeName", typeof(MethodCallExpression).FullName.Quotes()),
                        context.IndentedJson("Object", Serialize(value.Object, context)),
                        context.IndentedJson("Method", Serialize(value.Method, context)),
                        context.IndentedJson("Arguments", Serialize(value.Arguments, context))
                        );
                }) +
                context.Indented("}");
        }

        private static string Serialize(LambdaExpression value, SerializationContext context)
        {
            return "{\n" +
                context.AddIndentNewLine(() =>
                {
                    return string.Join(",\n",
                        context.IndentedJson("TypeName", typeof(LambdaExpression).FullName.Quotes()),
                        context.IndentedJson("Parameters", Serialize(value.Parameters, context)),
                        context.IndentedJson("Body", Serialize(value.Body, context))
                        );
                }) +
                context.Indented("}");
        }

        private static string Serialize(BinaryExpression value, SerializationContext context)
        {
            return "{\n" +
                context.AddIndentNewLine(() =>
                {
                    return string.Join(",\n",
                        context.IndentedJson("TypeName", typeof(BinaryExpression).FullName.Quotes()) +
                        context.IndentedJson("NodeType", value.NodeType),
                        context.IndentedJson("Left", Serialize(value.Left, context)),
                        context.IndentedJson("Right", Serialize(value.Right, context))
                        );
                }) +
                context.Indented("}");
        }

        private static string Serialize<T>(IReadOnlyCollection<T> expressions, SerializationContext context) where T : Expression
            => Serialize(expressions, context, Serialize);

        private static string Serialize<T>(IEnumerable<T> values, SerializationContext context, Func<T, SerializationContext, string> serializer)
        {
            using var scope1 = ListPool<T>.Get(out var temp);
            temp.AddRange(values);

            if (temp.Count == 0)
            {
                return "[]";
            }
            else
            {
                return "[\n" +
                    context.AddIndentNewLine(() => string.Join(",\n", temp.Select(p => context.Indented(serializer(p, context))))) +
                    context.Indented("]");
            }
        }

        private static string Serialize(MethodInfo value, SerializationContext context)
        {
            return "{\n" +
                context.AddIndentNewLine(() =>
                {
                    return string.Join(",\n",
                        context.IndentedJson("DeclaringType", value.DeclaringType.FullName.Quotes()),
                        context.IndentedJson("ReturnType", value.ReturnType.FullName.Quotes()),
                        context.IndentedJson("Name", value.Name.Quotes()),
                        context.IndentedJson("Parameters", Serialize(value.GetParameters(), context, Serialize))
                        );
                }) +
                context.Indented("}");
        }

        private static string Serialize(ParameterInfo value, SerializationContext context)
        {
            using var scope1 = ListPool<string>.Get(out var values);
            if (value.IsDefined(typeof(ParamArrayAttribute)))
            {
                values.Add("params");
            }

            var fullName = value.ParameterType.FullName;
            values.Add(fullName);
            return string.Join(' ', values).Quotes();
        }
    }
}
