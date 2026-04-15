#nullable enable

using UnityEngine;

namespace Ayla
{
    internal class ProjectGenerationToolsText
    {
        public static string ProjectGenerationToolsTitle => ApplicationMisc.EditorLanguage switch
        {
            SystemLanguage.Korean => "프로젝트 생성",
            SystemLanguage.Japanese => "プロジェクト生成",
            _ => "Project Generation"
        };

        public static string MarkdownHeader => ApplicationMisc.EditorLanguage switch
        {
            SystemLanguage.Korean => "마크다운",
            SystemLanguage.Japanese => "マークダウン",
            _ => "Markdown"
        };

        public static string PackagesHeader => ApplicationMisc.EditorLanguage switch
        {
            SystemLanguage.Korean => "패키지",
            SystemLanguage.Japanese => "パッケージ",
            _ => "Packages"
        };

        public static string Enabled => ApplicationMisc.EditorLanguage switch
        {
            SystemLanguage.Korean => "활성화",
            SystemLanguage.Japanese => "有効化",
            _ => "Enabled"
        };
    }
}
