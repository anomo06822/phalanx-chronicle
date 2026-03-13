#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PhalanxChronicle.Core;
using UnityEditor;
using UnityEngine;

namespace PhalanxChronicle.Editor
{
    public static class ItemIconCoverageAudit
    {
        private const string SourceRoot = "Assets/ArtSource/Items/WarReportBadges";
        private const string UiRoot = "Assets/Art/UI/Items";
        private const string ResourcesRoot = "Assets/Resources/ItemIcons";

        [MenuItem("Phalanx Chronicle/Visuals/Generate Item Icons")]
        public static void GenerateItemIcons()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                UnityEngine.Debug.LogError("[ItemIconCoverageAudit] 無法定位專案根目錄。");
                return;
            }

            string scriptPath = Path.Combine(projectRoot, "scripts", "generate_item_icons.py");
            if (!File.Exists(scriptPath))
            {
                UnityEngine.Debug.LogError($"[ItemIconCoverageAudit] 找不到腳本：{scriptPath}");
                return;
            }

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/bin/python3",
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = projectRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using System.Diagnostics.Process process = System.Diagnostics.Process.Start(startInfo);
            string stdout = process?.StandardOutput.ReadToEnd() ?? string.Empty;
            string stderr = process?.StandardError.ReadToEnd() ?? string.Empty;
            process?.WaitForExit();

            if (process == null || process.ExitCode != 0)
            {
                UnityEngine.Debug.LogError("[ItemIconCoverageAudit] item icon 生成失敗。\n" + stdout + "\n" + stderr);
                return;
            }

            AssetDatabase.Refresh();
            UnityEngine.Debug.Log("[ItemIconCoverageAudit] item icon 已重新生成。\n" + stdout.Trim());
        }

        [MenuItem("Phalanx Chronicle/Visuals/Audit Item Icon Coverage")]
        public static void AuditItemIconCoverage()
        {
            List<string> issues = RunAudit();
            if (issues.Count == 0)
            {
                UnityEngine.Debug.Log("[ItemIconCoverageAudit] 36 個 item icon 覆蓋檢查通過。");
                return;
            }

            UnityEngine.Debug.LogError("[ItemIconCoverageAudit] 發現 item icon 缺口：\n - " + string.Join("\n - ", issues));
        }

        private static List<string> RunAudit()
        {
            List<string> issues = new List<string>();
            HashSet<string> expectedFileNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (ItemDefinition item in ItemCatalog.All)
            {
                string svgPath = $"{SourceRoot}/{item.ItemId}.svg";
                string uiPngPath = $"{UiRoot}/{item.ItemId}__icon.png";
                string resourcePngPath = $"{ResourcesRoot}/{item.ItemId}__icon.png";
                expectedFileNames.Add($"{item.ItemId}__icon.png");

                if (!File.Exists(svgPath))
                {
                    issues.Add($"缺少 source SVG：{svgPath}");
                }

                ValidateSprite(uiPngPath, $"{item.ItemId} UI icon", issues);
                ValidateSprite(resourcePngPath, $"{item.ItemId} resource icon", issues);
            }

            CollectOrphans(UiRoot, expectedFileNames, issues, "UI");
            CollectOrphans(ResourcesRoot, expectedFileNames, issues, "Resources");
            return issues;
        }

        private static void ValidateSprite(string assetPath, string label, List<string> issues)
        {
            if (!File.Exists(assetPath))
            {
                issues.Add($"缺少 {label}：{assetPath}");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                issues.Add($"無法讀取 importer：{assetPath}");
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single)
            {
                issues.Add($"必須是單張 Sprite：{assetPath}");
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                issues.Add($"filter 必須為 Bilinear：{assetPath}");
            }

            if (Math.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f)
            {
                issues.Add($"PPU 必須為 100：{assetPath}");
            }
        }

        private static void CollectOrphans(string root, HashSet<string> expectedFileNames, List<string> issues, string label)
        {
            if (!Directory.Exists(root))
            {
                return;
            }

            foreach (string path in Directory.GetFiles(root, "*__icon.png", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(path);
                if (!expectedFileNames.Contains(fileName))
                {
                    issues.Add($"{label} 孤兒 icon：{path}");
                }
            }
        }
    }
}
#endif
