using UnityEditor;
using UnityEngine;

namespace Ayla
{
    internal static class OrderedDictionaryText
    {
        private static SystemLanguage s_Language = SystemLanguage.English;

        [InitializeOnLoadMethod]
        private static void StaticAwake()
        {
            s_Language = Application.systemLanguage;
        }

        public static string Title => s_Language switch
        {
            SystemLanguage.Korean => "데이터 테이블 에디터",
            SystemLanguage.Japanese => "データテーブルエディター",
            _ => "Data Table Editor"
        };

        public static string OpenEditor => s_Language switch
        {
            SystemLanguage.Korean => "에디터 열기",
            SystemLanguage.Japanese => "エディターを開く",
            _ => "Open Editor"
        };

        public static string TitleAppend => s_Language switch
        {
            SystemLanguage.Korean => "{0} 및 {1}개 오브젝트",
            SystemLanguage.Japanese => "{0} と {1} 個のオブジェクト",
            _ => "{0} and {1} more"
        };

        public static string InsertHereTooltip => s_Language switch
        {
            SystemLanguage.Korean => "여기에 삽입",
            SystemLanguage.Japanese => "ここに挿入",
            _ => "Insert Here"
        };

        public static string AddLastTooltip => s_Language switch
        {
            SystemLanguage.Korean => "마지막에 추가",
            SystemLanguage.Japanese => "最後に追加",
            _ => "Add Last"
        };

        public static string RemoveTooltip => s_Language switch
        {
            SystemLanguage.Korean => "제거",
            SystemLanguage.Japanese => "削除",
            _ => "Remove"
        };

        public static string MoveUpTooltip => s_Language switch
        {
            SystemLanguage.Korean => "위로 이동",
            SystemLanguage.Japanese => "上に移動",
            _ => "Move Up"
        };

        public static string MoveDownTooltip => s_Language switch
        {
            SystemLanguage.Korean => "아래로 이동",
            SystemLanguage.Japanese => "下に移動",
            _ => "Move Down"
        };
    }
}
