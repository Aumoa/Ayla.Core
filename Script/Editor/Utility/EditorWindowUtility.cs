#if UNITY_EDITOR
#nullable enable
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Ayla
{
    public static class EditorWindowUtility
    {
        private readonly struct OpenAssetMethod
        {
            public readonly string Metadata;
            public readonly int DelegateType;
            public readonly Delegate Method;
            public readonly int Order;

            public OpenAssetMethod(MethodInfo methodInfo, int delegateType, Delegate method, int order)
            {
                DelegateType = delegateType;
                Method = method;
                Order = order;

                var parameters = methodInfo.GetParameters();
                Metadata = $"{methodInfo.ReturnType.FullName} {methodInfo.DeclaringType.FullName}({string.Join(", ", parameters.Select(p => $"{p.ParameterType.FullName} {p.Name}"))})";
            }

            public override string ToString() => Metadata;
        }

        private static readonly OpenAssetMethod[] Methods;
        private static readonly Func<CallbackOrderAttribute, int> GetCallbackOrder = ExpressionUtility.GetGetProperty<CallbackOrderAttribute, int>("callbackOrder");

        static EditorWindowUtility()
        {
            using (ListPool<OpenAssetMethod>.Get(out var methods))
            {
                foreach (var type in ReflectionUtility.Types)
                {
                    const BindingFlags Flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

                    foreach (var method in type.GetMethods(Flags))
                    {
                        var attribute = method.GetCustomAttribute<OnOpenAssetAttribute>();
                        if (attribute != null)
                        {
                            int order = GetCallbackOrder(attribute);
                            var @delegate = ExpressionUtility.GetMethod(method);
                            if (method.IsFunc<int, bool>())
                            {
                                methods.Add(new OpenAssetMethod(method, 1, @delegate, order));
                            }
                            else if (method.IsFunc<int, int, bool>())
                            {
                                methods.Add(new OpenAssetMethod(method, 2, @delegate, order));
                            }
                            else if (method.IsFunc<int, int, int, bool>())
                            {
                                methods.Add(new OpenAssetMethod(method, 3, @delegate, order));
                            }
                        }
                    }
                }

                methods.Sort((lhs, rhs) => lhs.Order - rhs.Order);
                Methods = methods.ToArray();
            }
        }

        public static bool OpenAssetEditor(Object asset, int lineNumber = 1, int columnIndex = 0)
            => OpenAssetEditor(asset.GetInstanceID(), lineNumber, columnIndex);

        public static bool OpenAssetEditor(int instanceId, int lineNumber = 1, int columnIndex = 0)
        {
            foreach (var method in Methods)
            {
                switch (method.DelegateType)
                {
                    case 1:
                        var method1 = (Func<int, bool>)method.Method;
                        if (method1.Invoke(instanceId))
                        {
                            return true;
                        }
                        break;
                    case 2:
                        var method2 = (Func<int, int, bool>)method.Method;
                        if (method2.Invoke(instanceId, lineNumber))
                        {
                            return true;
                        }
                        break;
                    case 3:
                        var method3 = (Func<int, int, int, bool>)method.Method;
                        if (method3.Invoke(instanceId, lineNumber, columnIndex))
                        {
                            return true;
                        }
                        break;
                }
            }

            return false;
        }
    }
}
#endif