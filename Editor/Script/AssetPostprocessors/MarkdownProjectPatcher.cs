#nullable enable

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;

namespace Ayla
{
    public class MarkdownProjectPatcher : AssetPostprocessor
    {
        public static bool Enabled
        {
            get => EditorPrefs.GetBool("Ayla.MarkdownProjectPatcher.Markdown.Enabled", true);
            set => EditorPrefs.SetBool("Ayla.MarkdownProjectPatcher.Markdown.Enabled", value);
        }

        private static readonly Regex s_AssemblyDefinitionMatch = new(@"<None Include=""([\w\\.-]+).asmdef"" \/>", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex s_UnityProjectTypeMatch = new(@"<UnityProjectType>([\w]+):\d<\/UnityProjectType>", RegexOptions.Multiline);  // Not RegexOptions.Compiled

        public static string OnGeneratedCSProject(string path, string content)
        {
            if (!Enabled)
            {
                return content;
            }

            var match = s_AssemblyDefinitionMatch.Match(content);
            if (match.Success)
            {
                var assemblyFileName = match.Groups[1].Value;
                return GenerateForExternalAssembly(assemblyFileName, content);
            }
            else
            {
                return GenerateForRootAssembly(path, content);
            }
        }

        private static string GenerateForExternalAssembly(string assemblyFileName, string content)
        {
            var assemblyDirectory = Path.GetDirectoryName(assemblyFileName);
            var mdFiles = Directory.GetFiles(assemblyDirectory, "*.md", SearchOption.AllDirectories);
            if (mdFiles.Length == 0)
            {
                return content;
            }

            return AppendMarkdownSection(mdFiles, content);
        }

        private static string GenerateForRootAssembly(string path, string content)
        {
            var assetsDirectory = Path.Combine(Path.GetDirectoryName(path), "Assets");
            Debug.Assert(Directory.Exists(assetsDirectory));

            var match = s_UnityProjectTypeMatch.Match(content);
            bool isEditor;
            if (match.Success)
            {
                isEditor = match.Groups[1].Value == "Editor";
            }
            else
            {
                isEditor = false;
            }

            using var scope1 = ListPool<string>.Get(out var mdFiles);
            CollectMarkdownFiles(assetsDirectory, isEditor, false, mdFiles);

            return AppendMarkdownSection(mdFiles, content);

            static void CollectMarkdownFiles(string current, bool isEditor, bool isInEditor, List<string> output)
            {
                if (!isEditor || isInEditor)
                {
                    var fileNames = Directory.GetFiles(current);
                    if (fileNames.Any(f => f.EndsWith(".asmdef", System.StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }

                    int removeAt = output.Count;
                    foreach (var fileName in fileNames)
                    {
                        if (fileName.EndsWith(".asmdef", System.StringComparison.OrdinalIgnoreCase))
                        {
                            if (removeAt > output.Count)
                            {
                                output.RemoveRange(removeAt, output.Count - removeAt);
                            }

                            return;
                        }
                        else if (fileName.EndsWith(".md", System.StringComparison.OrdinalIgnoreCase))
                        {
                            output.Add(fileName);
                        }
                    }
                }

                foreach (var subDir in Directory.GetDirectories(current))
                {
                    isInEditor = isInEditor || subDir.EndsWith("Editor");
                    if (!isEditor && isInEditor)
                    {
                        continue;
                    }

                    CollectMarkdownFiles(subDir, isEditor, isInEditor, output);
                }
            }
        }

        private static string AppendMarkdownSection(IList<string> fileNames, string content)
        {
            var sb = new StringBuilder();
            sb.AppendLine("  <ItemGroup>");
            foreach (var mdFile in fileNames)
            {
                sb.AppendLine($"    <None Include=\"{mdFile.Replace('/', '\\')}\" />");
            }
            sb.AppendLine("  </ItemGroup>");

            return content.Replace("</Project>", sb + "</Project>");
        }
    }
}
