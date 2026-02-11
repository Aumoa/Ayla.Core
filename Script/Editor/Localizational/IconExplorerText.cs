using UnityEditor;
using UnityEngine;

namespace Ayla;

internal static class IconExplorerText
{
    private static SystemLanguage s_Language = SystemLanguage.English;

    [InitializeOnLoadMethod]
    private static void StaticAwake()
    {
        s_Language = Application.systemLanguage;
    }

    public static string Title => s_Language switch
    {
        SystemLanguage.Korean => "아이콘 탐색기",
        SystemLanguage.Japanese => "アイコンエクスプローラー",
        _ => "Icon Explorer"
    };

    public static string Search => s_Language switch
    {
        SystemLanguage.Korean => "검색",
        SystemLanguage.Japanese => "検索",
        _ => "Search"
    };

    public static string Name => s_Language switch
    {
        SystemLanguage.Korean => "이름",
        SystemLanguage.Japanese => "名前",
        _ => "Name"
    };

    public static string AssetPath => s_Language switch
    {
        SystemLanguage.Korean => "에셋 경로",
        SystemLanguage.Japanese => "アセットパス",
        _ => "Asset Path"
    };

    public static string Size => s_Language switch
    {
        SystemLanguage.Korean => "크기",
        SystemLanguage.Japanese => "サイズ",
        _ => "Size"
    };

    public static string GraphicsFormat => s_Language switch
    {
        SystemLanguage.Korean => "포맷",
        SystemLanguage.Japanese => "フォーマット",
        _ => "Graphics"
    };

    public static string SelectedIcon => s_Language switch
    {
        SystemLanguage.Korean => "선택된 아이콘",
        SystemLanguage.Japanese => "選択されたアイコン",
        _ => "Selected Icon"
    };
}
