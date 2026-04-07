#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Debug = UnityEngine.Debug;

namespace Ayla
{
    /// <summary>
    /// Provides a set of utility methods for performing reflection-based operations within the current application
    /// domain.
    /// </summary>
    /// <remarks>The ReflectionUtility class includes methods for retrieving types, checking type
    /// assignability and interface implementation, and determining method signatures such as Action and Func patterns.
    /// These utilities are designed to simplify and standardize common reflection tasks, making it easier to work with
    /// types and methods in a type-safe manner. All operations are performed against the assemblies loaded in the
    /// current AppDomain.</remarks>
    public static class ReflectionUtility
    {
        private static class Nested
        {
            private static readonly Task<ReadOnlyCollection<Type>> s_AllTask;
            private static ReadOnlyCollection<Type>? s_All;
            private static bool s_Blocked;

            public static Task WaitTask => s_AllTask;

            public static ReadOnlyCollection<Type> All
            {
                get
                {
                    if (s_All is null)
                    {
                        if (!s_Blocked)
                        {
                            s_Blocked = true;
                            Debug.LogFormat("Waiting for type collection task to complete...");
                        }
                        return s_AllTask.Result;
                    }

                    return s_All;
                }
            }

            static Nested()
            {
                s_AllTask = Task.Run(() =>
                {
                    var timer = Stopwatch.StartNew();

                    try
                    {
                        using var scope1 = ListPool<Type>.Get(out var types);
                        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            foreach (var type in assembly.GetTypes())
                            {
                                types.Add(type);
                            }
                        }

                        return types.ToArray();
                    }
                    finally
                    {
                        timer.Stop();
                        Debug.LogFormat("Collect all types for ReflectionUtility tooks {0}ms in thread-pool", timer.ElapsedMilliseconds);
                    }
                }).ContinueWith(r =>
                {
                    s_All = Array.AsReadOnly(r.Result);
                    return s_All;
                });
            }

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
            private static void InitializeCall()
            {
            }

#if UNITY_EDITOR
            [InitializeOnLoadMethod]
            private static void InitializeEditorCall()
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    _ = s_AllTask;
                }
            }
#endif
        }

        /// <summary>
        /// Waits asynchronously until the initialization process is complete.
        /// </summary>
        /// <param name="cancellationToken"> A token to monitor for cancellation requests. </param>
        /// <remarks>Call this method to ensure that initialization is complete before performing
        /// operations that depend on it. This is typically used in scenarios where subsequent actions require the
        /// initialization to be finished.</remarks>
        /// <returns>A task that represents the asynchronous wait operation. The task completes when initialization has finished.</returns>
        public static async ValueTask WaitForInitializeAsync(CancellationToken cancellationToken = default)
        {
            await Nested.WaitTask.WaitAsync(cancellationToken);
        }

        /// <summary>
        /// Waits asynchronously until the initialization process is complete.
        /// </summary>
        /// <remarks>Call this method to ensure that initialization is complete before performing
        /// operations that depend on it. This is typically used in scenarios where subsequent actions require the
        /// initialization to be finished.</remarks>
        /// <returns>A task that represents the asynchronous wait operation. The task completes when initialization has finished.</returns>
        public static async ValueTask WaitForInitializeAsync()
        {
            await Nested.WaitTask;
        }

        /// <summary>
        /// Gets all types in the current AppDomain.
        /// </summary>
        public static ReadOnlyCollection<Type> Types => Nested.All;

        /// <summary>
        /// Checks if the current type is assignable to the target type.
        /// </summary>
        /// <param name="this"> The type to check for assignability. </param>
        /// <param name="targetType"> The target type to check against. Can be <c>null</c>. </param>
        /// <returns>
        /// <c>true</c> if the current type can be assigned to the <paramref name="targetType"/>; 
        /// otherwise, <c>false</c>. Returns <c>false</c> if <paramref name="targetType"/> is <c>null</c>.
        /// </returns>
        /// <remarks>
        /// This method uses <see cref="Type.IsAssignableFrom"/> to determine if the current type 
        /// can be assigned to the specified <paramref name="targetType"/>.
        /// </remarks>
        public static bool IsAssignableTo(this Type @this, [NotNullWhen(true)] Type? targetType)
            => targetType?.IsAssignableFrom(@this) ?? false;

        /// <summary>
        /// Determines whether the current type implements the specified interface or is a constructed generic type of
        /// the specified generic type definition.
        /// </summary>
        /// <remarks>This method checks both direct and inherited implementations of interfaces, including
        /// generic interfaces, as well as whether the type or any of its base types is a constructed generic type of
        /// the specified generic type definition. If the target type is not an interface or a generic type definition,
        /// the method falls back to checking assignability.</remarks>
        /// <param name="this">The type to examine for implementation of the specified interface or generic type.</param>
        /// <param name="targetType">The type representing the interface or generic type definition to check against. This parameter must not be
        /// null.</param>
        /// <returns>true if the current type implements the specified interface or is a constructed generic type of the
        /// specified generic type definition; otherwise, false.</returns>
        public static bool IsImplements(this Type @this, [NotNullWhen(true)] Type? targetType)
        {
            if (targetType == null)
            {
                return false;
            }

            if (targetType.IsInterface)
            {
                // Check if @this implements the interface (including generic interfaces)
                foreach (var i in @this.GetInterfaces())
                {
                    if (i == targetType || (targetType.IsGenericTypeDefinition && i.IsGenericType && i.GetGenericTypeDefinition() == targetType))
                    {
                        return true;
                    }
                }

                return false;
            }
            else if (targetType.IsGenericTypeDefinition)
            {
                // Check if @this or any base type is a constructed generic of targetType
                for (var t = @this; t != null && t != typeof(object); t = t.BaseType)
                {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == targetType)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                // Fallback to assignable check
                return @this.IsAssignableTo(targetType);
            }
        }

        /// <summary>
        /// Gets all types from the current AppDomain that match the specified predicate.
        /// </summary>
        /// <param name="predicate"> A function that defines the condition each type must satisfy. </param>
        /// <param name="outputTypes"> A list to which the matching types will be added. </param>
        public static void GetTypes(Predicate<Type> predicate, IList<Type> outputTypes)
        {
            foreach (var type in Nested.All)
            {
                if (predicate(type))
                {
                    outputTypes.Add(type);
                }
            }
        }

        /// <summary>
        /// Gets the method with the specified name and parameter types from the current type.
        /// </summary>
        /// <param name="type"> The type to search for the method. </param>
        /// <param name="name"> The name of the method to find. </param>
        /// <param name="bindingAttr"> A bitmask that specifies how the search is conducted. This value is a combination of one or more <see cref="BindingFlags"/>. </param>
        /// <param name="types"> An array of <see cref="Type"/> objects representing the number, order, and type of the parameters for the method to find. </param>
        /// <returns>
        /// A <see cref="MethodInfo"/> object representing the method that matches the specified criteria, or <c>null</c> if no such method is found.
        /// </returns>
        public static MethodInfo? GetMethod(this Type type, string name, BindingFlags bindingAttr, Type[] types)
            => type.GetMethod(name, bindingAttr, binder: null, types, modifiers: null);

        /// <summary>
        /// Gets the method with the specified name, generic parameter count, and parameter types from the current type.
        /// </summary>
        /// <param name="type"> The type to search for the method. </param>
        /// <param name="name"> The name of the method to find. </param>
        /// <param name="genericParameterCount"> The number of generic parameters the method should have. </param>
        /// <param name="bindingAttr"> A bitmask that specifies how the search is conducted. This value is a combination of one or more <see cref="BindingFlags"/>. </param>
        /// <param name="types"> An array of <see cref="Type"/> objects representing the number, order, and type of the parameters for the method to find. </param>
        /// <returns>
        /// A <see cref="MethodInfo"/> object representing the method that matches the specified criteria, or <c>null</c> if no such method is found.
        /// </returns>
        public static MethodInfo? GetMethod(this Type type, string name, int genericParameterCount, BindingFlags bindingAttr, Type[] types)
            => type.GetMethod(name, genericParameterCount, bindingAttr, null, types, null);

        /// <summary>
        /// Gets the method with the specified name, generic parameter count, and parameter types from the current type.
        /// </summary>
        /// <param name="type"> The type to search for the method. </param>
        /// <param name="name"> The name of the method to find. </param>
        /// <param name="genericParameterCount"> The number of generic parameters the method should have. </param>
        /// <param name="bindingAttr"> A bitmask that specifies how the search is conducted. This value is a combination of one or more <see cref="BindingFlags"/>. </param>
        /// <param name="binder"> An object that enables binding, coercion of argument types, and invocation of members through reflection. Can be <c>null</c>. </param>
        /// <param name="types"> An array of <see cref="Type"/> objects representing the number, order, and type of the parameters for the method to find. </param>
        /// <param name="modifiers"> An array of <see cref="ParameterModifier"/> objects representing the attributes associated with the parameters of the method to find. Can be <c>null</c>. </param>
        /// <returns>
        /// A <see cref="MethodInfo"/> object representing the method that matches the specified criteria, or <c>null</c> if no such method is found.
        /// </returns>
        public static MethodInfo? GetMethod(this Type type, string name, int genericParameterCount, BindingFlags bindingAttr, Binder? binder, Type[] types, ParameterModifier[]? modifiers)
            => type.GetMethod(name, genericParameterCount, bindingAttr, binder, CallingConventions.Any, types, modifiers);

        /// <summary>
        /// Checks if the specified method matches the given return type and parameter types.
        /// </summary>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <param name="returnType"> The expected return type of the method. </param>
        /// <param name="parameters"> An array of <see cref="Type"/> objects representing the expected parameter types of the method. </param>
        /// <returns>
        /// <c>true</c> if the method's return type and parameter types match the specified criteria; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsActionOrFunc(this MethodInfo method, Type returnType, params Type[] parameters)
        {
            if (method.ReturnType != returnType)
            {
                return false;
            }

            var @params = method.GetParameters();
            if (@params.Length != parameters.Length)
            {
                return false;
            }

            for (int i = 0; i < parameters.Length; ++i)
            {
                if (@params[i].ParameterType != parameters[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the method is an action or a function with the specified return type and parameter types.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsAction(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and one parameter of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the single parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and one parameter of type <typeparamref name="T1"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and two parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and two parameters of type <typeparamref name="T1"/> and <typeparamref name="T2"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and three parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and three parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/> and <typeparamref name="T3"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and four parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and four parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/> and <typeparamref name="T4"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and five parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and five parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/> and <typeparamref name="T5"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and six parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and six parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/> and <typeparamref name="T6"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and seven parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and seven parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/> and <typeparamref name="T7"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and eight parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and eight parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, and <typeparamref name="T8"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and nine parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and nine parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/> and <typeparamref name="T9"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and ten parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and ten parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and eleven parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T11"> The type of the eleventh parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and eleven parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/> and <typeparamref name="T11"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and twelve parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T11"> The type of the eleventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T12"> The type of the twelfth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and twelve parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and thirteen parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T11"> The type of the eleventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T12"> The type of the twelfth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T13"> The type of the thirteenth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and thirteen parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/> and <typeparamref name="T13"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and fourteen parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T11"> The type of the eleventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T12"> The type of the twelfth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T13"> The type of the thirteenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T14"> The type of the fourteenth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and fourteen parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, <typeparamref name="T13"/> and <typeparamref name="T14"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and fifteen parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T11"> The type of the eleventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T12"> The type of the twelfth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T13"> The type of the thirteenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T14"> The type of the fifteenth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and fifteen parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, <typeparamref name="T13"/>, <typeparamref name="T14"/> and <typeparamref name="T15"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

        /// <summary>
        /// Checks if the method is an Action with a return type of <c>void</c> and sixteen parameters of the specified type.
        /// </summary>
        /// <typeparam name="T1"> The type of the first parameter that the Action should accept. </typeparam>
        /// <typeparam name="T2"> The type of the second parameter that the Action should accept. </typeparam>
        /// <typeparam name="T3"> The type of the third parameter that the Action should accept. </typeparam>
        /// <typeparam name="T4"> The type of the fourth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T5"> The type of the fifth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T6"> The type of the sixth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T7"> The type of the seventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T8"> The type of the eighth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T9"> The type of the ninth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T10"> The type of the tenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T11"> The type of the eleventh parameter that the Action should accept. </typeparam>
        /// <typeparam name="T12"> The type of the twelfth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T13"> The type of the thirteenth parameter that the Action should accept. </typeparam>
        /// <typeparam name="T14"> The type of the sixteenth parameter that the Action should accept. </typeparam>
        /// <param name="method"> The <see cref="MethodInfo"/> instance representing the method to check. </param>
        /// <returns>
        /// <c>true</c> if the method is an Action with a return type of <c>void</c> and sixteen parameters of type <typeparamref name="T1"/>, <typeparamref name="T2"/>, <typeparamref name="T3"/>, <typeparamref name="T4"/>, <typeparamref name="T5"/>, <typeparamref name="T6"/>, <typeparamref name="T7"/>, <typeparamref name="T8"/>, <typeparamref name="T9"/>, <typeparamref name="T10"/>, <typeparamref name="T11"/>, <typeparamref name="T12"/>, <typeparamref name="T13"/>, <typeparamref name="T14"/>, <typeparamref name="T15"/> and <typeparamref name="T16"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsAction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(void), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsFunc<TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and a single parameter of type <typeparamref name="T1"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T2"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T3"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T4"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T5"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T6"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T7"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T8"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T9"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T10"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T11">The type of the eleventh parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T11"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T11">The type of the eleventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T12">The type of the twelfth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T12"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T11">The type of the eleventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T12">The type of the twelfth parameter that the function should accept.</typeparam>
        /// <typeparam name="T13">The type of the thirteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T13"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T11">The type of the eleventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T12">The type of the twelfth parameter that the function should accept.</typeparam>
        /// <typeparam name="T13">The type of the thirteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T14">The type of the fourteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T14"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T11">The type of the eleventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T12">The type of the twelfth parameter that the function should accept.</typeparam>
        /// <typeparam name="T13">The type of the thirteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T14">The type of the fourteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T15">The type of the fifteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T15"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15));

        /// <summary>
        /// Checks if the method is a function with the specified return type and parameter types.
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter that the function should accept.</typeparam>
        /// <typeparam name="T2">The type of the second parameter that the function should accept.</typeparam>
        /// <typeparam name="T3">The type of the third parameter that the function should accept.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter that the function should accept.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter that the function should accept.</typeparam>
        /// <typeparam name="T6">The type of the sixth parameter that the function should accept.</typeparam>
        /// <typeparam name="T7">The type of the seventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T8">The type of the eighth parameter that the function should accept.</typeparam>
        /// <typeparam name="T9">The type of the ninth parameter that the function should accept.</typeparam>
        /// <typeparam name="T10">The type of the tenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T11">The type of the eleventh parameter that the function should accept.</typeparam>
        /// <typeparam name="T12">The type of the twelfth parameter that the function should accept.</typeparam>
        /// <typeparam name="T13">The type of the thirteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T14">The type of the fourteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T15">The type of the fifteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="T16">The type of the sixteenth parameter that the function should accept.</typeparam>
        /// <typeparam name="TReturn">The return type of the function.</typeparam>
        /// <param name="method">The <see cref="MethodInfo"/> instance representing the method to check.</param>
        /// <returns>
        /// <c>true</c> if the method is a function with the specified return type <typeparamref name="TReturn"/> 
        /// and parameters of type <typeparamref name="T1"/> through <typeparamref name="T16"/>; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsFunc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TReturn>(this MethodInfo method)
            => IsActionOrFunc(method, typeof(TReturn), typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16));
    }
}