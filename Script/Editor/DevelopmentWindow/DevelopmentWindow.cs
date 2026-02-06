#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Ayla;

public class DevelopmentWindow : EditorWindow, ISerializationCallbackReceiver
{
    private const string IconName_FavoriteOff = "d_Favorite@2x";
    private const string IconName_FavoriteOn = "d_Favorite_colored@2x";
    private const string IconName_Expand = "PreviewExpand@2x";
    private const string IconName_Collapse = "PreviewCollapse@2x";
    private const string IconName_Upper = "uibuilderpackageresources/icons/selected/inspector/text alignment/upper@2x.png";
    private const string IconName_Lower = "uibuilderpackageresources/icons/selected/inspector/text alignment/lower@2x.png";

    private const float TitleMargin = 2;
    private const float TitleLabelHeight = 18;
    private const float TitleLayoutHeight = TitleMargin + TitleMargin + TitleLabelHeight;
    private const float ContentPadding = 4;
    private const float BottomMargin = 4;

    [SerializeField]
    private List<string> m_ClassName = new();

    [SerializeField]
    private List<string> m_Serialized = new();

    [SerializeField]
    private float m_ScrollValue;

    [SerializeField]
    private int m_SelectedCategory = 0;

    private string[] m_Categories = null!;  // from Awake()
    private readonly Dictionary<string, DevelopmentTools[]> m_DevTools = new();
    private readonly List<DevelopmentTools> m_FavoriteTools = new();
    private bool m_Initialized;

    private void Awake()
    {
        var titleContent_ = titleContent ??= new GUIContent();
        titleContent_.text = DevelopmentWindowText.Title;
    }

    private async void OnEnable()
    {
        if (m_ClassName.Count != m_Serialized.Count)
        {
            m_ClassName.Clear();
            m_Serialized.Clear();
            Debug.LogErrorFormat("Invalid serialized value.");
        }

        try
        {
            await ReflectionUtility.WaitForInitializeAsync();
            m_Initialized = true;

            using (ListPool<Type>.Get(out var outputTypes))
            using (DictionaryPool<string, List<DevelopmentTools>>.Get(out var output))
            using (HashSetPool<string>.Get(out var categories))
            {
                ReflectionUtility.GetTypes(type => type.IsAssignableTo(typeof(DevelopmentTools)) && type.IsAbstract == false && type.IsAssignableTo(typeof(FavoriteDevTool)) == false, outputTypes);
                foreach (var targetType in outputTypes)
                {
                    using var scope1 = DevelopmentTools.InternalConstructorArgs.Ready(this, targetType);
                    var instanced = (DevelopmentTools)Activator.CreateInstance(targetType);
                    int serializedIndex = m_ClassName.FindIndex(p => p == targetType.FullName);
                    if (serializedIndex != -1)
                    {
                        var serializedValue = m_Serialized[serializedIndex];
                        instanced.OnDeserialize(serializedValue);
                    }

                    var categoryAttribute = targetType.GetCustomAttribute<CategoryAttribute>();
                    string category = categoryAttribute?.Category ?? "Misc";

                    if (output.TryGetValue(category, out var list) == false)
                    {
                        list = new List<DevelopmentTools>();
                        output.Add(category, list);
                    }

                    list.Add(instanced);
                    categories.Add(category);
                }

                foreach (var (key, list) in output)
                {
                    m_DevTools.Add(key, list.ToArray());
                }

                m_Categories = categories.ToArray();
            }

            ReorderAndPopulateFavorite();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDisable()
    {
        m_DevTools.Clear();
        m_Initialized = false;
    }

    private void OnGUI()
    {
        if (!m_Initialized)
        {
            EditorGUILayout.LabelField("Initializing...");
            return;
        }

        var drawingArgs = DrawingArgs.MakeRoot(this);
        var current = Event.current;

        drawingArgs = drawingArgs.MarginTop(DrawToolbars(drawingArgs));

        HorizontalBorder.Draw(drawingArgs);
        drawingArgs = drawingArgs.MarginTop(1);

        const float VerticalScrollWidth = 14;

        string? categoryName;
        if (m_SelectedCategory == -1)
        {
            categoryName = null;
        }
        else if (m_SelectedCategory < m_Categories.Length)
        {
            categoryName = m_Categories[m_SelectedCategory];
        }
        else
        {
            m_SelectedCategory = -1;
            categoryName = null;
        }

        var tools = categoryName is null ? (IList<DevelopmentTools>)m_FavoriteTools : m_DevTools[categoryName];
        var helpBox = EditorStyles.helpBox;
        float defaultHeight = TitleLayoutHeight + helpBox.margin.vertical + helpBox.padding.vertical + BottomMargin;
        float categoryViewHeight = tools.Sum(p => p.ViewHeight + defaultHeight);
        var verticalScrollRect = drawingArgs.FillRight(VerticalScrollWidth);
        using (GUIScope.Disabled(drawingArgs.DrawingRect.height >= categoryViewHeight))
        {
            float scrollViewHeight = Math.Min(drawingArgs.DrawingRect.height, categoryViewHeight);
            m_ScrollValue = GUI.VerticalScrollbar(verticalScrollRect.DrawingRect, m_ScrollValue, scrollViewHeight, 0, categoryViewHeight);
        }
        drawingArgs = drawingArgs.MarginRight(VerticalScrollWidth);

        using (GUIScope.Area(drawingArgs.DrawingRect))
        {
            drawingArgs = drawingArgs.WithArea();

            drawingArgs = drawingArgs.ScrollVertical(m_ScrollValue);
            foreach (var tool in tools)
            {
                var height = defaultHeight + tool.ViewHeight;
                var toolBox = drawingArgs.FillTop(height);
                // Cannot skip drawing previous tool on scroll because, evaluate drawing rect must begin and end drawing calls.

                var contentHeight = DrawDevelopmentTool(current, tools, tool, toolBox);
                drawingArgs = drawingArgs.MarginTop(height);

                if (current.rawType == EventType.Repaint)
                {
                    tool.CachedHeight = contentHeight;
                    if (toolBox.DrawingRect.yMin > toolBox.ClippingRect.yMax)
                    {
                        break;
                    }
                }
            }
        }
    }

    private float DrawToolbars(DrawingArgs drawingArgs)
    {
        const float Height = 22.0f;
        drawingArgs = drawingArgs.FillTop(Height);

        var favBtn = drawingArgs.FillLeft(24);
        if (GUI.Toggle(favBtn.DrawingRect, m_SelectedCategory == -1, EditorGUIHelper.TempIconContent(IconName_FavoriteOn), EditorStyles.toolbarButton))
        {
            m_SelectedCategory = -1;
        }
        drawingArgs = drawingArgs.MarginLeft(24);

        float widthPerItem = (int)(drawingArgs.DrawingRect.width / m_Categories.Length);
        for (int i = 0; i < m_Categories.Length; ++i)
        {
            DrawingArgs currentDrawingArgs;
            if (i == m_Categories.Length - 1)
            {
                currentDrawingArgs = drawingArgs;
            }
            else
            {
                currentDrawingArgs = drawingArgs.FillLeft(widthPerItem);
            }

            if (GUI.Toggle(currentDrawingArgs.DrawingRect, m_SelectedCategory == i, EditorGUIHelper.TempContent(m_Categories[i]), EditorStyles.toolbarButton))
            {
                m_SelectedCategory = i;
            }

            drawingArgs = drawingArgs.MarginLeft(currentDrawingArgs.DrawingRect.width);
        }

        return Height;
    }

    private float DrawDevelopmentTool(Event current, IList<DevelopmentTools> sourceList, DevelopmentTools tool, DrawingArgs drawingArgs)
    {
        int index = sourceList.IndexOf(tool);

        var helpBox = EditorStyles.helpBox;
        drawingArgs = drawingArgs.Margin(helpBox.margin);

        if (current.rawType == EventType.Repaint)
        {
            helpBox.Draw(drawingArgs.DrawingRect, GUIContent.none, 0);
        }

        drawingArgs = drawingArgs.Margin(helpBox.padding);
        drawingArgs = drawingArgs.MarginBottom(BottomMargin);

        const float ToolbarButtonWidth = 24;
        drawingArgs = drawingArgs.MarginTop(TitleMargin);
        var titleLayout = drawingArgs.FillTop(TitleLabelHeight);

        var currentIconName = tool.IsFavorite ? IconName_FavoriteOn : IconName_FavoriteOff;
        bool changedBool = GUI.Toggle(titleLayout.DrawingRect.FillRight(ToolbarButtonWidth), tool.IsFavorite, EditorGUIHelper.TempIconContent(currentIconName), EditorStyles.iconButton);
        titleLayout = titleLayout.MarginRight(ToolbarButtonWidth);
        if (GUI.changed)
        {
            tool.IsFavorite = changedBool;
            GUI.changed = false;
        }

        using (GUIScope.Disabled(index == 0))
        {
            bool isClicked = GUI.Button(titleLayout.DrawingRect.FillRight(ToolbarButtonWidth), EditorGUIHelper.TempIconContent(IconName_Upper), EditorStyles.miniButtonMid);
            titleLayout = titleLayout.MarginRight(ToolbarButtonWidth);
            if (isClicked)
            {
                GUI.changed = false;
                EditorApplication.delayCall += () =>
                {
                    (sourceList[index], sourceList[index - 1]) = (sourceList[index - 1], sourceList[index]);
                    UpdateSourceListOrders();
                    ReorderAndPopulateFavorite();
                };
            }
        }

        using (GUIScope.Disabled(index == sourceList.Count - 1))
        {
            bool isClicked = GUI.Button(titleLayout.DrawingRect.FillRight(ToolbarButtonWidth), EditorGUIHelper.TempIconContent(IconName_Lower), EditorStyles.miniButtonMid);
            titleLayout = titleLayout.MarginRight(ToolbarButtonWidth);
            if (isClicked)
            {
                GUI.changed = false;
                EditorApplication.delayCall += () =>
                {
                    (sourceList[index], sourceList[index + 1]) = (sourceList[index + 1], sourceList[index]);
                    UpdateSourceListOrders();
                    ReorderAndPopulateFavorite();
                };
            }
        }

        {
            const float ExpandButtonWidth = 24;
            bool isClicked = GUI.Button(titleLayout.FillLeft(ExpandButtonWidth).DrawingRect, EditorGUIHelper.TempIconContent(tool.IsExpanded ? IconName_Collapse : IconName_Expand), EditorStyles.iconButton);
            titleLayout = titleLayout.MarginLeft(ExpandButtonWidth);
            if (isClicked)
            {
                tool.IsExpanded = !tool.IsExpanded;
            }
        }

        GUI.Label(titleLayout.DrawingRect, tool.Title, EditorStyles.boldLabel);
        drawingArgs = drawingArgs.MarginTop(TitleMargin + TitleLabelHeight);

        HorizontalBorder.Draw(drawingArgs);
        drawingArgs = drawingArgs.MarginTop(1 + ContentPadding);

        using var scope1 = GUIScope.Area(drawingArgs.DrawingRect);
        using var scope2 = EditorGUIScope.Vertical(out var outputRect);

        if (tool.IsExpanded || current.rawType == EventType.Layout)
        {
            tool.OnGUI(drawingArgs.WithArea());
        }

        return outputRect.height + ContentPadding;

        void UpdateSourceListOrders()
        {
            for (int i = 0; i < sourceList.Count; ++i)
            {
                using (sourceList[i].SuppressCallDelayUpdate())
                {
                    sourceList[i].Order = i;
                }
            }
        }
    }

    internal void ReorderAndPopulateFavorite()
    {
        m_FavoriteTools.Clear();

        foreach (var item in m_DevTools.Values.SelectMany(p => p))
        {
            if (item.IsFavorite)
            {
                using (DevelopmentTools.InternalConstructorArgs.Ready(this, item.GetType()))
                {
                    var favItem = new FavoriteDevTool(item);
                    m_FavoriteTools.Add(favItem);
                }
            }
        }

        m_FavoriteTools.Sort((l, r) => l.Order - r.Order);
        int favIndex = 0;
        foreach (var item in m_FavoriteTools)
        {
            using (item.SuppressCallDelayUpdate())
            {
                item.Order = favIndex++;
            }
        }

        foreach (var list in m_DevTools.Values)
        {
            Array.Sort(list, (l, r) => l.Order - r.Order);
        }

        Repaint();
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        DevelopmentTools.InternalConstructorArgs.OnAfterDeserialize();
    }

    [MenuItem("Window/Ayla/Development Window")]
    public static void OpenWindow()
    {
        GetWindow<DevelopmentWindow>().Show();
    }
}