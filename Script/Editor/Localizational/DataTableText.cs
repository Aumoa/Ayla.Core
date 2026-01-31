using UnityEditor;
using UnityEngine;

namespace Ayla
{
    internal static class DataTableText
    {
        private static SystemLanguage s_Language = SystemLanguage.English;

        [InitializeOnLoadMethod]
        private static void StaticAwake()
        {
            s_Language = Application.systemLanguage;
        }

        public static string CreateTitle => s_Language switch
        {
            SystemLanguage.Korean => "데이터 테이블 생성",
            SystemLanguage.Japanese => "データテーブルの作成",
            _ => "Create DataTable"
        };

        public static string SuitableNotFoundMessage => s_Language switch
        {
            SystemLanguage.Korean => "적합한 데이터 테이블 서브클래스를 찾을 수 없습니다. DataTable<TKey, TValue>의 서브클래스를 정의하십시오.",
            SystemLanguage.Japanese => "適切なデータテーブルのサブクラスが見つかりません。DataTable<TKey, TValue>のサブクラスを定義してください。",
            _ => "No suitable DataTable subclass found. Please define a subclass of DataTable<TKey, TValue>."
        };

        public static string TableType => s_Language switch
        {
            SystemLanguage.Korean => "테이블 타입",
            SystemLanguage.Japanese => "テーブルタイプ",
            _ => "Table Type"
        };

        public static string AssetName => s_Language switch
        {
            SystemLanguage.Korean => "에셋 이름",
            SystemLanguage.Japanese => "アセット名",
            _ => "Asset Name"
        };

        public static string Create => s_Language switch
        {
            SystemLanguage.Korean => "생성",
            SystemLanguage.Japanese => "作成",
            _ => "Create"
        };
    }
}
