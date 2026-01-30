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

        public static string kTitle => s_Language switch
        {
            SystemLanguage.Korean => "데이터 테이블 에디터",
            SystemLanguage.Japanese => "データテーブルエディター",
            _ => "Data Table Editor"
        };

        public static string kOpenEditor => s_Language switch
        {
            SystemLanguage.Korean => "에디터 열기",
            SystemLanguage.Japanese => "エディターを開く",
            _ => "Open Editor"
        };

        public static string kTitleAppend => s_Language switch
        {
            SystemLanguage.Korean => "{0} 및 {1}개 오브젝트",
            SystemLanguage.Japanese => "{0} と {1} 個のオブジェクト",
            _ => "{0} and {1} more"
        };
    }
}
