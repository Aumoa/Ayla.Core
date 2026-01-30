using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Ayla
{
    public class OrderedDictionaryEditor : EditorWindow
    {
        private readonly struct ColumnDefinition
        {
            public readonly string Name;
            public readonly string TypeName;
            public readonly int Width;

            public ColumnDefinition(string name, string typeName)
            {
                Name = name;
                TypeName = typeName;
                Width = 100;
            }
        }

        [SerializeField]
        private Object[] m_TargetObjects = Array.Empty<Object>();
        [SerializeField]
        private string m_PropertyPath = "";

        private SerializedProperty m_Property;
        private Object m_ClassDefaultObject;
        private SerializedProperty m_ClassDefaultObjectProperty;

        private ColumnDefinition[] m_KeyColumns = Array.Empty<ColumnDefinition>();
        private ColumnDefinition[] m_ValueColumns = Array.Empty<ColumnDefinition>();

        private void OnEnable()
        {
            if (m_Property == null && m_TargetObjects.Length != 0)
            {
                var serializedObject = new SerializedObject(m_TargetObjects);
                InternalSelectProperty(serializedObject.FindProperty(m_PropertyPath));
            }
            else
            {
                Refresh();
            }
        }

        private void OnGUI()
        {
            if (m_Property == null)
            {
                EditorGUILayout.LabelField("No property selected.");
                return;
            }

            var layout = position.ZeroPosition();
            var headerLayout = layout.FillTop(EditorGUIUtility.singleLineHeight);
            {
                var headerLayoutAdv = headerLayout;
                for (int i = 0; i < m_KeyColumns.Length; ++i)
                {
                    ref var c = ref m_KeyColumns[i];
                    var r = headerLayoutAdv.FillLeft(c.Width);
                    DrawColumnName(ref c, r);
                    headerLayoutAdv = headerLayoutAdv.MarginLeft(c.Width);
                }
                headerLayoutAdv = headerLayoutAdv.MarginLeft(VerticalBorder.ShadowPixels);
                for (int i = 0; i < m_ValueColumns.Length; ++i)
                {
                    ref var c = ref m_ValueColumns[i];
                    var r = headerLayoutAdv.FillLeft(c.Width);
                    DrawColumnName(ref c, r);
                    headerLayoutAdv = headerLayoutAdv.MarginLeft(c.Width);
                }

                static void DrawColumnName(ref ColumnDefinition c, Rect r)
                {
                    var content = EditorGUIHelper.TempContent(c.Name);
                    GUI.Label(r, content, EditorStyles.boldLabel);
                    var size = EditorStyles.boldLabel.CalcSize(content);
                    r = r.MarginLeft(size.x + EditorGUIUtility.standardVerticalSpacing);
                    GUI.Label(r, $"[{c.TypeName}]");
                }
            }

            layout = layout.MarginTop(EditorGUIUtility.singleLineHeight);
            var headerBorder = layout;
            HorizontalBorder.Draw(new DrawingArgs(headerBorder));
            layout = layout.MarginTop(HorizontalBorder.Height);

            var keyBorder = layout.MarginLeft(m_KeyColumns.Sum(c => c.Width));
            VerticalBorder.Draw(new DrawingArgs(keyBorder));
        }

        private void OnDestroy()
        {
            TryDeleteClassDefaultObject();
        }

        public void SelectProperty(SerializedProperty property)
        {
            m_TargetObjects = property.serializedObject.targetObjects;
            m_PropertyPath = property.propertyPath;
            InternalSelectProperty(property);
        }

        private void InternalSelectProperty(SerializedProperty property)
        {
            m_Property = property;
            TryDeleteClassDefaultObject();
            if (m_Property == null)
            {
                return;
            }

            var propertyType = property.boxedValue.GetType();
            if (propertyType.GetGenericTypeDefinition() != typeof(OrderedDictionary<,>))
            {
                return;
            }

            var ga = propertyType.GetGenericArguments();
            var keyType = ga[0];
            var valueType = ga[1];
            var classDefaultObjectType = GetClassDefaultType(keyType, valueType);
            m_ClassDefaultObject = CreateInstance(classDefaultObjectType);
            m_ClassDefaultObjectProperty = new SerializedObject(m_ClassDefaultObject).FindProperty("m_Dict");
            m_ClassDefaultObjectProperty.Next(true);

            m_ClassDefaultObjectProperty.serializedObject.Update();
            m_ClassDefaultObjectProperty.arraySize = 1;
            m_ClassDefaultObjectProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Refresh();
        }

        private void Refresh()
        {
            titleContent = new GUIContent(OrderedDictionaryText.kTitle + " - " + FormatTargetObjects());

            if (m_ClassDefaultObjectProperty == null)
            {
                m_KeyColumns = Array.Empty<ColumnDefinition>();
                m_ValueColumns = Array.Empty<ColumnDefinition>();
            }
            else
            {
                var copy = m_ClassDefaultObjectProperty.Copy(); // m_Rows
                copy.Next(true); // m_Rows.Array
                copy.Next(true); // m_Rows.Array.size
                copy.Next(false); // m_Rows.Array.data[0]
                copy.Next(true); // Key
                using var scope1 = ListPool<ColumnDefinition>.Get(out var columns);
                VisitChildren(copy, p =>
                {
                    columns.Add(new ColumnDefinition(p.name, p.type));
                });
                if (columns.Count > 1)
                {
                    columns.RemoveAt(0);
                }
                m_KeyColumns = columns.ToArray();
                columns.Clear();
                VisitChildren(copy, p =>
                {
                    columns.Add(new ColumnDefinition(p.name, p.type));
                });
                if (columns.Count > 1)
                {
                    columns.RemoveAt(0);
                }
                m_ValueColumns = columns.ToArray();
            }

            return;

            static void VisitChildren(SerializedProperty prop, Action<SerializedProperty> body)
            {
                int depth = prop.depth;
                bool first = true;

                while (true)
                {
                    body(prop);
                    if (!prop.Next(first))
                    {
                        break;
                    }

                    if (depth < prop.depth)
                    {
                        first = false;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            string FormatTargetObjects()
            {
                if (m_TargetObjects.Length == 1)
                {
                    return m_TargetObjects[0].name;
                }
                else if (m_TargetObjects.Length != 0)
                {
                    return string.Format(OrderedDictionaryText.kTitleAppend, m_TargetObjects[0].name, m_TargetObjects.Length - 1);
                }
                else
                {
                    return "<error>";
                }
            }
        }

        private void TryDeleteClassDefaultObject()
        {
            if (m_ClassDefaultObject)
            {
                DestroyImmediate(m_ClassDefaultObject);
                m_ClassDefaultObject = null;
            }
        }

        private static readonly ModuleBuilder s_CDOModuleBuilder;
        private static readonly ConcurrentDictionary<(Type KeyType, Type ValueType), Type> s_CDOTypes = new();

        static OrderedDictionaryEditor()
        {
            var assemblyName = new AssemblyName("DynamicDataTableAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            s_CDOModuleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        private static Type GetClassDefaultType(Type keyType, Type valueType)
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
    }
}
