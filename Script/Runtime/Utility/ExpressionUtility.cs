#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.Pool;

namespace Ayla.Core
{
    public static class ExpressionUtility
    {
        public static Delegate GetGetProperty(string propertyName, Type @class, Type memberType)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            PropertyInfo? propertyInfo = null;
            foreach (var type in ClassHierarchy(@class))
            {
                var targetProperty = type.GetProperty(propertyName, Flags);
                if (targetProperty != null && targetProperty.CanRead)
                {
                    propertyInfo = targetProperty;
                    break;
                }
            }

            if (propertyInfo == null)
            {
                throw new KeyNotFoundException($"{memberType.Name} {@class.FullName}.{propertyName} {{ get; }} is not found.");
            }

            var @this = Expression.Parameter(@class, "this");
            var property = Expression.Property(@this, propertyInfo);
            var delegateType = typeof(Func<,>).MakeGenericType(@class, memberType);
            return Expression.Lambda(delegateType, property, @this).Compile();
        }

        public static Func<TClass, TMember> GetGetProperty<TClass, TMember>(string propertyName)
        {
            return (Func<TClass, TMember>)GetGetProperty(propertyName, typeof(TClass), typeof(TMember));
        }

        public static Delegate GetMethod(MethodInfo methodInfo)
        {
            var @class = methodInfo.DeclaringType;
            var parameterTypes = methodInfo.GetParameters();
            var returnType = methodInfo.ReturnType;

            var @this = methodInfo.IsStatic ? null : Expression.Parameter(@class, "this");
            var parameters = new ParameterExpression[parameterTypes.Length];
            var methodParameters = methodInfo.GetParameters();
            for (int i = 0; i < parameters.Length; ++i)
            {
                parameters[i] = Expression.Parameter(methodParameters[i].ParameterType, methodParameters[i].Name);
            }

            var call = Expression.Call(@this, methodInfo, parameters);
            Type? delegateType = FormatDelegateParameters();

            using (ListPool<ParameterExpression>.Get(out var lambdaParameters))
            {
                if (@this != null)
                {
                    lambdaParameters.Add(@this);
                }
                lambdaParameters.AddRange(parameters);
                return Expression.Lambda(delegateType, call, lambdaParameters).Compile();
            }

            Type FormatDelegateParameters()
            {
                if (returnType == typeof(void))
                {
                    var delegateType = parameters.Length switch
                    {
                        0 => typeof(Action),
                        1 => typeof(Action<>),
                        2 => typeof(Action<,>),
                        3 => typeof(Action<,,>),
                        4 => typeof(Action<,,,>),
                        5 => typeof(Action<,,,,>),
                        6 => typeof(Action<,,,,,>),
                        7 => typeof(Action<,,,,,,>),
                        8 => typeof(Action<,,,,,,,>),
                        9 => typeof(Action<,,,,,,,,>),
                        10 => typeof(Action<,,,,,,,,,>),
                        11 => typeof(Action<,,,,,,,,,,>),
                        12 => typeof(Action<,,,,,,,,,,,>),
                        13 => typeof(Action<,,,,,,,,,,,,>),
                        14 => typeof(Action<,,,,,,,,,,,,,>),
                        15 => typeof(Action<,,,,,,,,,,,,,,>),
                        16 => typeof(Action<,,,,,,,,,,,,,,,>),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    return delegateType.MakeGenericType(parameterTypes.Select(p => p.ParameterType).ToArray());
                }
                else
                {
                    var delegateType = parameters.Length switch
                    {
                        0 => typeof(Func<>),
                        1 => typeof(Func<,>),
                        2 => typeof(Func<,,>),
                        3 => typeof(Func<,,,>),
                        4 => typeof(Func<,,,,>),
                        5 => typeof(Func<,,,,,>),
                        6 => typeof(Func<,,,,,,>),
                        7 => typeof(Func<,,,,,,,>),
                        8 => typeof(Func<,,,,,,,,>),
                        9 => typeof(Func<,,,,,,,,,>),
                        10 => typeof(Func<,,,,,,,,,,>),
                        11 => typeof(Func<,,,,,,,,,,,>),
                        12 => typeof(Func<,,,,,,,,,,,,>),
                        13 => typeof(Func<,,,,,,,,,,,,,>),
                        14 => typeof(Func<,,,,,,,,,,,,,,>),
                        15 => typeof(Func<,,,,,,,,,,,,,,,>),
                        16 => typeof(Func<,,,,,,,,,,,,,,,,>),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    using (ListPool<Type>.Get(out var genericTypes))
                    {
                        genericTypes.AddRange(parameterTypes.Select(p => p.ParameterType).ToArray());
                        genericTypes.Add(returnType);
                        return delegateType.MakeGenericType(genericTypes.ToArray());
                    }
                }
            }
        }

        public static Delegate? TryGetMethod(string methodName, Type @class, Type returnType, params Type[] parameterTypes)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            MethodInfo? methodInfo = null;
            foreach (var type in ClassHierarchy(@class))
            {
                var targetMethod = type.GetMethod(methodName, Flags, parameterTypes);
                if (targetMethod != null && targetMethod.ReturnType != returnType)
                {
                    methodInfo = targetMethod;
                    break;
                }
            }

            if (methodInfo == null)
            {
                return null;
            }

            return GetMethod(methodInfo);
        }

        public static Delegate GetMethod(string methodName, Type @class, Type returnType, params Type[] parameterTypes)
        {
            return TryGetMethod(methodName, @class, returnType, parameterTypes) ?? throw new KeyNotFoundException($"{returnType.Name} {@class.FullName}.{methodName}({string.Join(", ", parameterTypes.Select(p => p.FullName))}) is not found.");
        }

        public static Action? TryGetMethodAction<TClass>(string methodName) => (Action?)TryGetMethod(methodName, typeof(TClass), typeof(void));

        public static Action<T1>? TryGetMethodAction<TClass, T1>(string methodName) => (Action<T1>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1));

        public static Action<T1, T2>? TryGetMethodAction<TClass, T1, T2>(string methodName) => (Action<T1, T2>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2));

        public static Action<T1, T2, T3>? TryGetMethodAction<TClass, T1, T2, T3>(string methodName)
            => (Action<T1, T2, T3>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3));

        public static Action<T1, T2, T3, T4>? TryGetMethodAction<TClass, T1, T2, T3, T4>(string methodName)
            => (Action<T1, T2, T3, T4>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        public static Action<T1, T2, T3, T4, T5>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5>(string methodName)
            => (Action<T1, T2, T3, T4, T5>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        public static Action<T1, T2, T3, T4, T5, T6>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        public static Action<T1, T2, T3, T4, T5, T6, T7>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>? TryGetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>?)TryGetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

        public static Func<TReturn>? TryMethodFunc<TClass, TReturn>(string methodName) => (Func<TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn));

        public static Func<T1, TReturn>? TryMethodFunc<TClass, TReturn, T1>(string methodName) => (Func<T1, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1));

        public static Func<T1, T2, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2>(string methodName) => (Func<T1, T2, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2));

        public static Func<T1, T2, T3, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3>(string methodName)
            => (Func<T1, T2, T3, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3));

        public static Func<T1, T2, T3, T4, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4>(string methodName)
            => (Func<T1, T2, T3, T4, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        public static Func<T1, T2, T3, T4, T5, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5>(string methodName)
            => (Func<T1, T2, T3, T4, T5, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        public static Func<T1, T2, T3, T4, T5, T6, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        public static Func<T1, T2, T3, T4, T5, T6, T7, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>? TryMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>?)TryGetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

        public static Action GetMethodAction<TClass>(string methodName) => (Action)GetMethod(methodName, typeof(TClass), typeof(void));

        public static Action<T1> GetMethodAction<TClass, T1>(string methodName) => (Action<T1>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1));

        public static Action<T1, T2> GetMethodAction<TClass, T1, T2>(string methodName) => (Action<T1, T2>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2));

        public static Action<T1, T2, T3> GetMethodAction<TClass, T1, T2, T3>(string methodName)
            => (Action<T1, T2, T3>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3));

        public static Action<T1, T2, T3, T4> GetMethodAction<TClass, T1, T2, T3, T4>(string methodName)
            => (Action<T1, T2, T3, T4>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        public static Action<T1, T2, T3, T4, T5> GetMethodAction<TClass, T1, T2, T3, T4, T5>(string methodName)
            => (Action<T1, T2, T3, T4, T5>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        public static Action<T1, T2, T3, T4, T5, T6> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        public static Action<T1, T2, T3, T4, T5, T6, T7> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

        public static Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> GetMethodAction<TClass, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName)
            => (Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>)GetMethod(methodName, typeof(TClass), typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

        public static Func<TReturn> GetMethodFunc<TClass, TReturn>(string methodName) => (Func<TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn));

        public static Func<T1, TReturn> GetMethodFunc<TClass, TReturn, T1>(string methodName) => (Func<T1, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1));

        public static Func<T1, T2, TReturn> GetMethodFunc<TClass, TReturn, T1, T2>(string methodName) => (Func<T1, T2, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2));

        public static Func<T1, T2, T3, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3>(string methodName)
            => (Func<T1, T2, T3, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3));

        public static Func<T1, T2, T3, T4, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4>(string methodName)
            => (Func<T1, T2, T3, T4, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        public static Func<T1, T2, T3, T4, T5, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5>(string methodName)
            => (Func<T1, T2, T3, T4, T5, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        public static Func<T1, T2, T3, T4, T5, T6, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        public static Func<T1, T2, T3, T4, T5, T6, T7, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

        public static Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn> GetMethodFunc<TClass, TReturn, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName)
            => (Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>)GetMethod(methodName, typeof(TClass), typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

        private static IEnumerable<Type> ClassHierarchy(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }
    }
}