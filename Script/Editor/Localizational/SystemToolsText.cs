using UnityEditor;
using UnityEngine;

namespace Ayla
{
    internal static class SystemToolsText
    {
        private static SystemLanguage s_Language = SystemLanguage.English;

        [InitializeOnLoadMethod]
        private static void StaticAwake()
        {
            s_Language = Application.systemLanguage;
        }

        public static string RequestScriptCompilation => s_Language switch
        {
            SystemLanguage.Korean => "스크립트 컴파일 요청",
            SystemLanguage.Japanese => "スクリプトのコンパイルをリクエスト",
            _ => "Request Script Compilation"
        };

        public static string RequestScriptReload => s_Language switch
        {
            SystemLanguage.Korean => "스크립트 다시 로드",
            SystemLanguage.Japanese => "スクリプトを再読み込み",
            _ => "Request Script Reload"
        };

        public static string RefreshAssetDatabase => s_Language switch
        {
            SystemLanguage.Korean => "애셋 데이터베이스 새로 고침",
            SystemLanguage.Japanese => "アセットデータベースをリフレッシュ",
            _ => "Refresh Asset Database"
        };

        public static string SaveAllAssets => s_Language switch
        {
            SystemLanguage.Korean => "모든 애셋 저장",
            SystemLanguage.Japanese => "すべてのアセットを保存",
            _ => "Save All Assets"
        };

        public static string ClearProgressBar => s_Language switch
        {
            SystemLanguage.Korean => "진행률 표시줄 지우기",
            SystemLanguage.Japanese => "プログレスバーをクリア",
            _ => "Clear Progress Bar"
        };

        public static string CategoryName => s_Language switch
        {
            SystemLanguage.Korean => "시스템",
            SystemLanguage.Japanese => "システム",
            _ => "System"
        };
    }
}
