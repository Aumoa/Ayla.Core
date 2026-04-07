#nullable enable

using System.IO;
using UnityEditor;

namespace Ayla
{
    internal static class CreateDataTableMenu
    {
        [MenuItem("Assets/Create/Ayla/Data Table/New Table")]
        public static void CreateAssetMenu()
        {
            var activeObject = Selection.activeObject;
            var assetPath = AssetDatabase.GetAssetPath(activeObject);
            string directoryName;
            
            if (string.IsNullOrEmpty(assetPath))
            {
                directoryName = "Assets";
            }
            else if (AssetDatabase.IsValidFolder(assetPath))
            {
                // If directory, use current path
                directoryName = assetPath;
            }
            else
            {
                // If asset, use parent directory
                directoryName = Path.GetDirectoryName(assetPath).Replace('\\', '/');
            }

            CreateDataTableWindow.Create(directoryName).ShowModal();
        }
    }
}