#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ayla
{
    internal static class ClassDefaultObjectBuilder
    {
        private static readonly ModuleBuilder s_CDOModuleBuilder;
        private static readonly ConcurrentDictionary<(Type KeyType, Type ValueType), Type> s_CDOTypes = new();
        private static readonly Dictionary<(Type KeyType, Type ValueType), Object> s_CDOs = new();

        static ClassDefaultObjectBuilder()
        {
            var assemblyName = new AssemblyName("DynamicDataTableAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            s_CDOModuleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        public static Type GetClassDefaultType(Type keyType, Type valueType)
        {
            var pair = (keyType, valueType);

            return s_CDOTypes.GetOrAdd(pair, static pair2 =>
            {
                string typeName = $"DynamicDataTable_{GetSafeName(pair2.KeyType.FullName)}_{GetSafeName(pair2.ValueType.FullName)}";
                var typeBuilder = s_CDOModuleBuilder.DefineType(
                    typeName,
                    TypeAttributes.Public | TypeAttributes.Class,
                    typeof(DataTable<,>).MakeGenericType(pair2.KeyType, pair2.ValueType)
                );
                typeBuilder.SetCustomAttribute(ClassDefaultObjectAttribute.Builder);
                var type = typeBuilder.CreateType();
                return type;
            });

            static string GetSafeName(string fullName)
            {
                return fullName.Replace('.', '_').Replace("+", "__");
            }
        }

        public static Object GetClassDefaultObject(Type keyType, Type valueType)
        {
            var pair = (keyType, valueType);
            if (!s_CDOs.TryGetValue(pair, out var cdo))
            {
                var cdoType = GetClassDefaultType(keyType, valueType);
                cdo = ScriptableObject.CreateInstance(cdoType);
            }

            return cdo;
        }

        public static SerializedProperty NewClassDefaultObjectProperty(Type keyType, Type valueType)
        {
            var cdo = GetClassDefaultObject(keyType, valueType);
            var rootProperty = new SerializedObject(cdo).FindProperty("m_Dict");
            rootProperty.Next(true);

            if (rootProperty.arraySize != 1)
            {
                rootProperty.serializedObject.Update();
                rootProperty.arraySize = 1;
                rootProperty.serializedObject.ApplyModifiedProperties();
            }

            return rootProperty;
        }
    }
}
