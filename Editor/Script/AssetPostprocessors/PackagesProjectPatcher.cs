#nullable enable

using System.IO;
using UnityEditor;

namespace Ayla
{
    public class PackagesProjectPatcher : AssetPostprocessor
    {
        public static bool Enabled
        {
            get => EditorPrefs.GetBool("Ayla.PackagesProjectPatcher.Enabled", true);
            set => EditorPrefs.SetBool("Ayla.PackagesProjectPatcher.Enabled", value);
        }

        public static string OnGeneratedCSProject(string path, string content)
        {
            if (!Enabled)
            {
                return content;
            }

            if (!Path.GetFileName(path).Equals("Assembly-CSharp.csproj", System.StringComparison.OrdinalIgnoreCase))
            {
                return content;
            }

            const string kDefaultSection = @"  <ItemGroup>
    <None Include=""Packages\manifest.json"" />
    <None Include=""Packages\packages-lock.json"" />
  </ItemGroup>
";

            return content.Replace("</Project>", kDefaultSection + "</Project>");
        }
    }
}
