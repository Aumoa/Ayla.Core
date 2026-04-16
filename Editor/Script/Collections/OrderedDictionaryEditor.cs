#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Ayla
{
    public class OrderedDictionaryEditor : EditorWindow
    {
        private const int kMinimumWidth = 50;
        private const int kMaximumWidth = 500;
        private const int kDefaultWidth = 100;

        private readonly struct ColumnDefinition
        {
            public readonly string Name;
            public readonly string TypeName;
            public readonly SerializedProperty Property;

            private readonly string m_PropertyUniqueKey;

            public ColumnDefinition(string name, string typeName, string propertyUniqueKey, SerializedProperty property)
            {
                Name = name;
                TypeName = typeName;
                Property = property;
                m_PropertyUniqueKey = propertyUniqueKey;
            }

            public int Width
            {
                get => EditorPrefs.GetInt(m_PropertyUniqueKey, kDefaultWidth);
                set => EditorPrefs.SetInt(m_PropertyUniqueKey, Math.Clamp(value, kMinimumWidth, kMaximumWidth));
            }
        }

        private sealed class DragHandle : IDisposable
        {
            private readonly int m_Button;
            private readonly Vector2 m_Initial;
            private Vector2 m_MousePosition;

            public DragHandle(Event current, int button)
            {
                m_Button = button;
                m_Initial = current.mousePosition;
                m_MousePosition = m_Initial;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            public event Action<Vector2, Vector2>? MouseMove;

            public bool Update(Event current)
            {
                if (current.button != m_Button)
                {
                    return true;
                }

                switch (current.rawType)
                {
                    case EventType.MouseMove or EventType.MouseDrag:
                        m_MousePosition += current.delta;
                        MouseMove?.Invoke(m_MousePosition - m_Initial, current.delta);
                        current.Use();
                        break;
                    case EventType.MouseUp:
                        current.Use();
                        return false;
                }

                return true;
            }
        }

        private const int kButtonWidth = 18;
        private const int kScrollSize = 14;
        private const int kLeftSelector = 14;

        [SerializeField]
        private Object[] m_TargetObjects = Array.Empty<Object>();
        [SerializeField]
        private string m_PropertyPath = "";
        [SerializeField]
        private Vector2 m_Scroll;

        private GUIContent? m_DefaultLabelContent;
        private SerializedProperty? m_Property;
        private SerializedProperty? m_Rows;
        private SerializedProperty? m_Selector;
        private SerializedProperty? m_ClassDefaultObjectProperty;
        private SerializedProperty? m_CDOCopySource;
        private SerializedProperty? m_CDOCopySourceKey;

        private ColumnDefinition[] m_KeyColumns = Array.Empty<ColumnDefinition>();
        private ColumnDefinition[] m_ValueColumns = Array.Empty<ColumnDefinition>();
        private event Action? UpdateQueue;
        private readonly HashSet<uint> m_KeyCollection = new();

        private GUIContent? m_InsertHereContent;
        private GUIContent? m_AddLastContent;
        private GUIContent? m_RemoveContent;
        private GUIContent? m_MoveUpContent;
        private GUIContent? m_MoveDownContent;

        private DragHandle? m_DragHandle;

        private void OnEnable()
        {
            m_DefaultLabelContent = new GUIContent { text = "O" };

            m_InsertHereContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_addmore").image,
                tooltip = OrderedDictionaryText.InsertHereTooltip
            };

            m_AddLastContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_addmore").image,
                tooltip = OrderedDictionaryText.AddLastTooltip
            };

            m_RemoveContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_remove").image,
                tooltip = OrderedDictionaryText.RemoveTooltip
            };

            m_MoveUpContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_scrollup@2x").image,
                tooltip = OrderedDictionaryText.MoveUpTooltip
            };

            m_MoveDownContent = new GUIContent
            {
                image = EditorGUIUtility.IconContent("d_scrolldown@2x").image,
                tooltip = OrderedDictionaryText.MoveDownTooltip
            };

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

            m_Property.serializedObject.Update();

            var layout = position.ZeroPosition().MarginTop(EditorGUIUtility.standardVerticalSpacing);
            float toolsWidth = (kButtonWidth + EditorGUIUtility.standardVerticalSpacing) * 4;
            DrawContents(layout, toolsWidth);
            DrawMainBorders(layout, toolsWidth);

            UpdateQueue?.Invoke();
            UpdateQueue = null;

            m_Property.serializedObject.ApplyModifiedProperties();

            const int kMinimumRowCount = 5;
            minSize = new Vector2(
                m_KeyColumns.Sum(c => c.Width) + kDefaultWidth + toolsWidth + kScrollSize,
                (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + kScrollSize + (EditorGUIUtility.singleLineHeight + 1) * kMinimumRowCount + EditorGUIUtility.standardVerticalSpacing
                );

            var current = Event.current;
            if (current == null)
            {
                return;
            }

            if (m_DragHandle != null)
            {
                try
                {
                    if (m_DragHandle.Update(current) == false)
                    {
                        m_DragHandle.Dispose();
                        m_DragHandle = null;
                    }
                }
                catch
                {
                    m_DragHandle!.Dispose();
                    throw;
                }
            }
        }

        private void OnFocus()
        {
            if (m_Property != null)
            {
                var serializedObject = m_Property.serializedObject;
                if (serializedObject != null)
                {
                    var targetObjects = serializedObject.targetObjects;
                    using var scope1 = ListPool<GameObject>.Get(out var gameObjects);
                    foreach (var targetObject in targetObjects)
                    {
                        if (targetObject is Component component && component.gameObject != null)
                        {
                            gameObjects.Add(component.gameObject);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (gameObjects.Count == targetObjects.Length)
                    {
                        Selection.objects = gameObjects.ToArray();
                    }
                    else
                    {
                        Selection.objects = targetObjects;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (m_Selector != null)
            {
                OrderedDictionarySelection.OnDestroy(this, m_Selector);
            }
        }

        private void DrawContents(Rect rect, float toolsWidth)
        {
            var bottomScroll = rect.FillBottom(kScrollSize);

            var rightScroll = rect.FillRight(kScrollSize);
            rect = rect.MarginBottom(bottomScroll.height).MarginRight(kScrollSize).MarginLeft(kLeftSelector);

            var headerLayout = rect.FillTop(EditorGUIUtility.singleLineHeight);
            rect = rect.MarginTop(headerLayout.height + EditorGUIUtility.standardVerticalSpacing);
            var inputLayout = rect.FillBottom(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            rect = rect.MarginBottom(inputLayout.height);
            var rowsLayout = rect.MarginTop(EditorGUIUtility.standardVerticalSpacing);

            bool keyAdd = m_KeyCollection.Add(m_CDOCopySourceKey!.contentHash);
            m_KeyCollection.Clear();

            DrawColumns(headerLayout, toolsWidth);
            DrawRows(rowsLayout, toolsWidth, m_KeyCollection, keyAdd);
            DrawInputBar(inputLayout.MarginTop(EditorGUIUtility.standardVerticalSpacing), toolsWidth, keyAdd);

            var keyWidth = m_KeyColumns.Sum(c => c.Width);
            var valueWidth = m_ValueColumns.Sum(c => c.Width);
            var valueViewWidth = rect.width - keyWidth - toolsWidth - (EditorGUIUtility.standardVerticalSpacing - 1);
            using (GUIScope.Disabled(valueWidth <= valueViewWidth))
            {
                var horizontalScrollRect = bottomScroll.MarginLeft(keyWidth).MarginRight(toolsWidth + kScrollSize);
                m_Scroll.x = GUI.HorizontalScrollbar(
                    horizontalScrollRect,
                    m_Scroll.x,
                    valueViewWidth,
                    0,
                    Mathf.Max(valueWidth, valueViewWidth)
                    );
            }
            EditorGUI.DrawRect(bottomScroll.FillTop(1), Color.black);

            var rowsHeight = m_Rows!.arraySize * (EditorGUIUtility.singleLineHeight + 1) - 1;
            var rowsViewHeight = rowsLayout.height;
            using (GUIScope.Disabled(rowsHeight <= rowsViewHeight))
            {
                m_Scroll.y = GUI.VerticalScrollbar(
                    rightScroll.MarginTop(headerLayout.height).MarginBottom(inputLayout.height + kScrollSize),
                    m_Scroll.y,
                    rowsViewHeight,
                    0,
                    Mathf.Max(rowsHeight, rowsViewHeight)
                    );
            }
            EditorGUI.DrawRect(rightScroll.FillLeft(1), Color.black);
        }

        private void DrawRows(Rect rect, float toolsWidth, HashSet<uint> keyCollection, bool keyAdd)
        {
            int arraySize = m_Rows!.arraySize;
            var current = Event.current;
            int currentSelector = m_Selector!.intValue;

            var outerArea = rect;
            rect = rect.MarginLeft(-kLeftSelector);
            using (GUIScope.Area(rect))
            {
                rect = rect.ZeroPosition()
                    .MarginTop(-m_Scroll.y)
                    .MarginLeft(kLeftSelector);

                for (int i = 0; i < arraySize; ++i)
                {
                    var element = m_Rows.GetArrayElementAtIndex(i);
                    var rowRect = rect.FillTop(EditorGUIUtility.singleLineHeight);
                    var expandedArea = rowRect.MarginLeft(-kLeftSelector);

                    if (i == currentSelector)
                    {
                        EditorGUI.DrawRect(expandedArea, Color.green.WithAlpha(0.2f));
                    }

                    element.Next(true);  // Key
                    if (!keyCollection.Add(element.contentHash))
                    {
                        EditorGUI.DrawRect(rowRect, Color.red.WithAlpha(0.2f));
                    }
                    int index = 0;
                    VisitChildren(element, child =>
                    {
                        ref var c = ref m_KeyColumns[index++];
                        var r = rowRect.FillLeft(c.Width);
                        DrawPropertyField(r, child, index != m_KeyColumns.Length);
                        rowRect = rowRect.MarginLeft(c.Width);
                    });

                    var area = rowRect.MarginRight(toolsWidth);
                    using (GUIScope.Area(area))
                    {
                        var zp = rowRect.ZeroPosition().MarginLeft(EditorGUIUtility.standardVerticalSpacing - m_Scroll.x);
                        index = 0;
                        VisitChildren(element, child =>
                        {
                            ref var c = ref m_ValueColumns[index++];
                            var r = zp.FillLeft(c.Width);
                            if (r.x <= area.width && r.xMax > 0 && rowRect.y <= outerArea.height && rowRect.yMax > 0)
                            {
                                DrawPropertyField(r, child, true);
                            }
                            zp = zp.MarginLeft(c.Width);
                        });
                    }

                    var toolbarRect = rowRect.FillRight(toolsWidth).MarginLeft(EditorGUIUtility.standardVerticalSpacing);
                    using (GUIScope.Disabled(!keyAdd))
                    {
                        if (GUI.Button(toolbarRect.FillLeft(kButtonWidth), m_InsertHereContent, EditorStyles.iconButton))
                        {
                            int ii = i;
                            UpdateQueue += () => InsertNewElementAt(ii);
                        }
                    }
                    toolbarRect = toolbarRect.MarginLeft(kButtonWidth + EditorGUIUtility.standardVerticalSpacing);
                    if (GUI.Button(toolbarRect.FillLeft(kButtonWidth), m_RemoveContent, EditorStyles.iconButton))
                    {
                        int ii = i;
                        UpdateQueue += () => m_Rows.DeleteArrayElementAtIndex(ii);
                        if (m_Selector.intValue == i)
                        {
                            SetSelectorIndex(-1);
                        }
                    }
                    toolbarRect = toolbarRect.MarginLeft(kButtonWidth + EditorGUIUtility.standardVerticalSpacing);
                    using (GUIScope.Disabled(i == 0))
                    {
                        if (GUI.Button(toolbarRect.FillLeft(kButtonWidth), m_MoveUpContent, EditorStyles.iconButton))
                        {
                            int ii = i;
                            UpdateQueue += () => m_Rows.MoveArrayElement(ii, ii - 1);
                            SetSelectorIndex(i - 1);
                        }
                    }
                    toolbarRect = toolbarRect.MarginLeft(kButtonWidth + EditorGUIUtility.standardVerticalSpacing);
                    using (GUIScope.Disabled(i == arraySize - 1))
                    {
                        if (GUI.Button(toolbarRect.FillLeft(kButtonWidth), m_MoveDownContent, EditorStyles.iconButton))
                        {
                            int ii = i;
                            UpdateQueue += () => m_Rows.MoveArrayElement(ii, ii + 1);
                            SetSelectorIndex(i + 1);
                        }
                    }

                    if (current != null)
                    {
                        if (current.rawType == EventType.MouseDown && current.button == 0 && expandedArea.Contains(current.mousePosition))
                        {
                            SetSelectorIndex(i);
                            GUI.FocusControl("");
                            Repaint();
                            current.Use();
                        }
                    }

                    rect = rect.MarginTop(rowRect.height);
                    EditorGUI.DrawRect(rect.FillTop(1).MarginLeft(-kLeftSelector), Color.black);
                    rect = rect.MarginTop(1);
                }
            }

            if (current != null)
            {
                if (current.rawType == EventType.ScrollWheel && outerArea.Contains(current.mousePosition))
                {
                    float scale = EditorGUIUtility.singleLineHeight * 0.5f;
                    if (current.control)
                    {
                        scale *= 3.0f;
                    }

                    m_Scroll += current.delta * scale;
                    Repaint();
                    current.Use();
                }
                else if (current.rawType == EventType.MouseDown && current.button == 0 && outerArea.Contains(current.mousePosition))
                {
                    SetSelectorIndex(-1);
                    GUI.FocusControl("");
                    Repaint();
                    current.Use();
                }
            }
        }

        private void DrawMainBorders(Rect rect, float toolsWidth)
        {
            HorizontalBorder.Draw(new DrawingArgs(rect.MarginTop(EditorGUIUtility.singleLineHeight)));
            HorizontalBorder.Draw(new DrawingArgs(rect.FillBottom(EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing + kScrollSize)));
            VerticalBorder.Draw(new DrawingArgs(rect.MarginLeft(kLeftSelector)));
            VerticalBorder.Draw(new DrawingArgs(rect.MarginLeft(kLeftSelector + m_KeyColumns.Sum(c => c.Width))));
            VerticalBorder.Draw(new DrawingArgs(rect.MarginLeft(kLeftSelector).FillRight(toolsWidth + kScrollSize)));
        }

        private void DrawInputBar(Rect rect, float toolsWidth, bool keyAdd)
        {
            m_ClassDefaultObjectProperty!.serializedObject.Update();
            int currentSelector = m_Selector!.intValue;
            var expandedArea = rect.MarginLeft(-kLeftSelector);
            var current = Event.current;

            if (currentSelector == OrderedDictionary.kSelectorIndex_NewElement)
            {
                EditorGUI.DrawRect(expandedArea, Color.green.WithAlpha(0.2f));
            }

            for (int i = 0; i < m_KeyColumns.Length; ++i)
            {
                ref var c = ref m_KeyColumns[i];
                var r = rect.FillLeft(c.Width);
                DrawPropertyField(r, c.Property, i != m_KeyColumns.Length - 1);
                rect = rect.MarginLeft(c.Width);
            }
            var area = rect.MarginRight(toolsWidth);
            using (GUIScope.Area(area))
            {
                var zp = rect.ZeroPosition().MarginLeft(EditorGUIUtility.standardVerticalSpacing - m_Scroll.x);
                for (int i = 0; i < m_ValueColumns.Length; ++i)
                {
                    ref var c = ref m_ValueColumns[i];
                    var r = zp.FillLeft(c.Width);
                    if (r.x <= area.width && r.xMax > 0)
                    {
                        DrawPropertyField(r, c.Property, true);
                    }
                    zp = zp.MarginLeft(c.Width);
                }
            }

            m_ClassDefaultObjectProperty.serializedObject.ApplyModifiedProperties();

            var toolbarRect = rect.FillRight(toolsWidth).MarginLeft(EditorGUIUtility.standardVerticalSpacing);
            using (GUIScope.Disabled(!keyAdd))
            {
                if (GUI.Button(toolbarRect.FillLeft(kButtonWidth), m_AddLastContent, EditorStyles.iconButton))
                {
                    InsertNewElementAt(null);
                }
            }

            if (current != null)
            {
                if (current.rawType == EventType.MouseDown && current.button == 0 && expandedArea.Contains(current.mousePosition))
                {
                    SetSelectorIndex(OrderedDictionary.kSelectorIndex_NewElement);
                    GUI.FocusControl("");
                    Repaint();
                    current.Use();
                }
            }
        }

        private void DrawColumns(Rect rect, float toolsWidth)
        {
            for (int i = 0; i < m_KeyColumns.Length; ++i)
            {
                ref var c = ref m_KeyColumns[i];
                var r = rect.FillLeft(c.Width);
                DrawColumnName(ref c, r, i != m_KeyColumns.Length - 1);
                rect = rect.MarginLeft(c.Width);
            }
            rect = rect.MarginLeft(EditorGUIUtility.standardVerticalSpacing);
            var area = rect.MarginRight(toolsWidth);
            using (GUIScope.Area(area))
            {
                rect = rect.ZeroPosition().MarginLeft(-m_Scroll.x);
                for (int i = 0; i < m_ValueColumns.Length; ++i)
                {
                    ref var c = ref m_ValueColumns[i];
                    var r = rect.FillLeft(c.Width);
                    if (r.x > area.width)
                    {
                        break;
                    }
                    if (r.xMax > 0)
                    {
                        DrawColumnName(ref c, r, true);
                    }
                    rect = rect.MarginLeft(c.Width);
                }
            }

            return;

            void DrawColumnName(ref ColumnDefinition c, Rect r, bool drawBorder)
            {
                var content = EditorGUIHelper.TempContent(c.Name);
                GUI.Label(r.MarginLeft(EditorGUIUtility.standardVerticalSpacing), content, EditorStyles.boldLabel);
                var size = EditorStyles.boldLabel.CalcSize(content);
                r = r.MarginLeft(size.x + EditorGUIUtility.standardVerticalSpacing);
                GUI.Label(r, $"[{c.TypeName}]");
                if (drawBorder)
                {
                    EditorGUI.DrawRect(r.FillRight(1), Color.black);
                }

                var current = Event.current;
                var handleRect = r.FillRight(2);
                EditorGUIUtility.AddCursorRect(handleRect, MouseCursor.ResizeHorizontal);
                if (current.rawType == EventType.MouseDown && handleRect.Contains(current.mousePosition))
                {
                    m_DragHandle?.Dispose();
                    m_DragHandle = new DragHandle(current, 0);
                    var cc = c;
                    var initial = c.Width;
                    m_DragHandle.MouseMove += (distance, _) =>
                    {
                        var nx = initial + distance.x;
                        cc.Width = (int)nx;
                        Repaint();
                    };
                }
            }
        }

        private void InsertNewElementAt(int? index)
        {
            var copyDest = m_Property!.Copy();
            copyDest.Next(true);  // m_Rows
            index ??= copyDest.arraySize;
            copyDest.InsertArrayElementAtIndex(index.Value);
            var newElement = copyDest.GetArrayElementAtIndex(index.Value);
            newElement.boxedValue = m_CDOCopySource!.boxedValue;
            SetSelectorIndex(index.Value);
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
            if (m_Property == null)
            {
                return;
            }
            var propertyType = property.boxedValue.GetType();
            if (propertyType.GetGenericTypeDefinition() != typeof(OrderedDictionary<,>))
            {
                m_Property = null;
                return;
            }

            try
            {
                m_Rows = m_Property.Copy();
                m_Rows.Next(true);
                m_Selector = m_Rows.Copy();
                m_Selector.Next(false);

                var ga = propertyType.GetGenericArguments();
                var keyType = ga[0];
                var valueType = ga[1];
                m_ClassDefaultObjectProperty = ClassDefaultObjectBuilder.NewClassDefaultObjectProperty(keyType, valueType);
                m_CDOCopySource = m_ClassDefaultObjectProperty.GetArrayElementAtIndex(0);
                m_CDOCopySourceKey = m_CDOCopySource.Copy();
                m_CDOCopySourceKey.Next(true);
                Refresh();
            }
            catch
            {
                m_Property = null;
                m_Rows = null;
                m_ClassDefaultObjectProperty = null;
                m_CDOCopySource = null;
                m_CDOCopySourceKey = null;
                throw;
            }
        }

        private void Refresh()
        {
            titleContent = new GUIContent(OrderedDictionaryText.Title + " - " + FormatTargetObjects());

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
                string assemblyQualifiedName = m_Property!.serializedObject.targetObject.GetType().AssemblyQualifiedName;
                VisitChildren(copy, p =>
                {
                    columns.Add(new ColumnDefinition(p.name, p.type, assemblyQualifiedName + "$" + p.propertyPath, p.Copy()));
                });
                m_KeyColumns = columns.ToArray();
                columns.Clear();
                VisitChildren(copy, p =>
                {
                    columns.Add(new ColumnDefinition(p.name, p.type, assemblyQualifiedName + "$" + p.propertyPath, p.Copy()));
                });
                m_ValueColumns = columns.ToArray();
            }

            return;

            string FormatTargetObjects()
            {
                var targetObjects = m_Property?.serializedObject.targetObjects ?? Array.Empty<Object>();
                if (targetObjects.Length == 1)
                {
                    return targetObjects[0].name;
                }
                else if (targetObjects.Length != 0)
                {
                    return string.Format(OrderedDictionaryText.TitleAppend, targetObjects[0].name, targetObjects.Length - 1);
                }
                else
                {
                    return "<error>";
                }
            }
        }

        private void DrawPropertyField(Rect r, SerializedProperty p, bool drawBorder)
        {
            using (EditorGUIScope.LabelWidth(EditorGUIUtility.singleLineHeight * 0.7f))
            using (EditorGUIScope.WideMode(true))
            {
                var labelContent = !p.isArray && p.hasVisibleChildren ? GUIContent.none : m_DefaultLabelContent;
                var fieldRect = r.Margin(EditorGUIUtility.standardVerticalSpacing, 0);

                if (IsStruct(p) && EditorGUI.GetPropertyHeight(p, true) > EditorGUIUtility.singleLineHeight)
                {
                    string summaryText = EditorJsonUtility.ToJson(p.boxedValue);
                    EditorGUI.LabelField(fieldRect, labelContent, EditorGUIUtility.TrTempContent(summaryText));
                }
                else
                {
                    EditorGUI.PropertyField(fieldRect, p, labelContent);
                }

                if (drawBorder)
                {
                    EditorGUI.DrawRect(r.FillRight(1), Color.black);
                }
            }
        }

        private void SetSelectorIndex(int value)
        {
            ThrowHelper.ThrowIfNull(m_Selector, nameof(m_Selector));
            OrderedDictionarySelection.Choose(this, m_Selector);
            m_Selector.intValue = value;
        }

        private static bool IsStruct(SerializedProperty p)
        {
            return p.propertyType is not (SerializedPropertyType.String or SerializedPropertyType.ObjectReference) && p.hasChildren;
        }

        private static void VisitChildren(SerializedProperty prop, Action<SerializedProperty> body)
        {
            if (prop.hasChildren && !prop.isArray)
            {
                int depth = prop.depth;
                prop.Next(true);
                while (depth < prop.depth)
                {
                    body(prop);
                    if (!prop.Next(false))
                    {
                        break;
                    }
                }
            }
            else
            {
                body(prop);
                prop.Next(false);
            }
        }
    }
}
