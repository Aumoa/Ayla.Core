using UnityEditor;
using UnityEngine;

namespace Ayla
{
    internal class DevelopmentWindowText
    {
        private static SystemLanguage s_Language = SystemLanguage.English;

        [InitializeOnLoadMethod]
        private static void StaticAwake()
        {
            s_Language = Application.systemLanguage;
        }

        public static string Title => s_Language switch
        {
            SystemLanguage.Korean => "개발자 도구",
            SystemLanguage.Japanese => "開発者ツール",
            _ => "Developer Tools"
        };
    }
}
