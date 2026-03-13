#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PhalanxChronicle.Data;
using UnityEditor;
using UnityEngine;

namespace PhalanxChronicle.Editor
{
    public static class SampleVisualCoverageAudit
    {
        private const string DefinitionsRoot = "Assets/Resources/UnitVisualDefinitions";
        private const string StageDefinitionsRoot = "Assets/Resources/StageVisualDefinitions";
        private const string TerrainRoot = "Assets/Resources/Terrain";

        private static readonly string[] RequiredBattleUnits =
        {
            "player-liu-bei",
            "player-guan-yu",
            "player-zhang-fei",
            "player-huang-zhong",
            "player-zhuge-liang",
            "player-zhao-yun",
            "player-ma-chao",
            "enemy-zhang-bao",
            "enemy-zhang-liang",
            "enemy-yellow_turban_raider",
            "enemy-armored_zealot",
            "enemy-yellow_turban_archer",
            "enemy-yellow_turban_hunter",
            "enemy-fervent_spearman",
            "enemy-jiangxia-bridge-captain",
            "enemy-jiangxia-bow-chief",
            "enemy-jiangxia-outer-warden-a",
            "enemy-jiangxia-outer-warden-b",
            "enemy-jiangxia-ferry-captain",
            "enemy-jiangxia-river-rider",
            "enemy-jiangxia-river-rider-b",
            "enemy-luocheng-gate-captain",
            "enemy-luocheng-wall-bow",
            "enemy-luocheng-outer-guard",
            "enemy-wei_shieldman",
            "enemy-wei_deadeye",
            "enemy-luocheng-commandant",
            "enemy-luocheng-street-guard-a",
            "enemy-luocheng-street-guard-b",
        };

        private static readonly string[] RequiredPortraitUnits =
        {
            "player-liu-bei",
            "player-guan-yu",
            "player-zhang-fei",
            "player-huang-zhong",
            "player-zhuge-liang",
            "player-zhao-yun",
            "player-ma-chao",
            "enemy-zhang-bao",
            "enemy-zhang-liang",
            "enemy-jiangxia-ferry-captain",
            "enemy-luocheng-commandant",
        };

        private static readonly (string StageKey, string PaletteId)[] RequiredStages =
        {
            ("stage.guangzong", "guangzong-smoke"),
            ("stage.jiangxia_ferry", "jiangxia-bridges"),
            ("stage.luocheng_siege", "luocheng-gate"),
        };

        private static readonly string[] TerrainKeys = { "plain", "forest", "fort", "hazard", "blocked" };
        private static readonly string[] TerrainVariants = { "a", "b", "c" };
        private static readonly string[] TerrainLayers = { "base", "overlay", "prop" };

        [MenuItem("Phalanx Chronicle/Visuals/Prepare Sample Slice")]
        public static void PrepareSampleSlice()
        {
            CharacterVisualPipelineBuilder.SyncUnitVisualDefinitionsMenu();
            AuditSampleSliceCoverage();
        }

        [MenuItem("Phalanx Chronicle/Visuals/Audit Sample Slice Coverage")]
        public static void AuditSampleSliceCoverage()
        {
            List<string> issues = RunAudit();
            if (issues.Count == 0)
            {
                Debug.Log("[SampleVisualCoverageAudit] 三關樣板視覺覆蓋檢查通過。");
                return;
            }

            Debug.LogError("[SampleVisualCoverageAudit] 發現樣板視覺覆蓋缺口：\n - " + string.Join("\n - ", issues));
        }

        private static List<string> RunAudit()
        {
            List<string> issues = new List<string>();

            foreach (string unitId in RequiredBattleUnits)
            {
                ValidateUnitDefinition(unitId, false, issues);
            }

            foreach (string unitId in RequiredPortraitUnits)
            {
                ValidateUnitDefinition(unitId, true, issues);
            }

            foreach ((string stageKey, string paletteId) in RequiredStages)
            {
                ValidateStageDefinition(stageKey, paletteId, issues);
                ValidateTerrainPalette(paletteId, issues);
            }

            return issues;
        }

        private static void ValidateUnitDefinition(string unitId, bool requirePortrait, List<string> issues)
        {
            string assetPath = $"{DefinitionsRoot}/{unitId}.asset";
            UnitVisualDefinition definition = AssetDatabase.LoadAssetAtPath<UnitVisualDefinition>(assetPath);
            if (definition == null)
            {
                issues.Add($"缺少 UnitVisualDefinition：{unitId}");
                return;
            }

            if (definition.BattleSprite == null)
            {
                issues.Add($"缺少正式 battle sprite：{unitId}");
            }
            else
            {
                ValidateSpriteImport(definition.BattleSprite, 128f, FilterMode.Point, $"{unitId} battle", issues);
            }

            if (!requirePortrait)
            {
                return;
            }

            if (definition.PortraitSprite == null)
            {
                issues.Add($"缺少正式 portrait：{unitId}");
                return;
            }

            ValidateSpriteImport(definition.PortraitSprite, null, null, $"{unitId} portrait", issues);
        }

        private static void ValidateStageDefinition(string stageKey, string expectedPaletteId, List<string> issues)
        {
            string[] guids = AssetDatabase.FindAssets("t:StageVisualDefinition", new[] { StageDefinitionsRoot });
            StageVisualDefinition matchedDefinition = null;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                StageVisualDefinition definition = AssetDatabase.LoadAssetAtPath<StageVisualDefinition>(path);
                if (definition != null && string.Equals(definition.StageNameKey, stageKey, StringComparison.OrdinalIgnoreCase))
                {
                    matchedDefinition = definition;
                    break;
                }
            }

            if (matchedDefinition == null)
            {
                issues.Add($"缺少 StageVisualDefinition：{stageKey}");
                return;
            }

            if (!string.Equals(matchedDefinition.TerrainPaletteId, expectedPaletteId, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add($"StageVisualDefinition palette 不符：{stageKey} -> {matchedDefinition.TerrainPaletteId}，預期 {expectedPaletteId}");
            }
        }

        private static void ValidateTerrainPalette(string paletteId, List<string> issues)
        {
            foreach (string terrainKey in TerrainKeys)
            {
                foreach (string variant in TerrainVariants)
                {
                    foreach (string layer in TerrainLayers)
                    {
                        string assetPath = $"{TerrainRoot}/{paletteId}/{terrainKey}_{variant}_{layer}.png";
                        if (!File.Exists(assetPath))
                        {
                            issues.Add($"缺少 terrain 資產：{assetPath}");
                            continue;
                        }

                        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (importer == null)
                        {
                            issues.Add($"無法讀取 terrain importer：{assetPath}");
                            continue;
                        }

                        if (importer.filterMode != FilterMode.Point)
                        {
                            issues.Add($"terrain filter 必須為 Point：{assetPath}");
                        }

                        if (Mathf.Abs(importer.spritePixelsPerUnit - 64f) > 0.01f)
                        {
                            issues.Add($"terrain PPU 必須為 64：{assetPath}");
                        }
                    }
                }
            }
        }

        private static void ValidateSpriteImport(Sprite sprite, float? expectedPpu, FilterMode? expectedFilterMode, string label, List<string> issues)
        {
            string assetPath = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                issues.Add($"找不到 sprite 路徑：{label}");
                return;
            }

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                issues.Add($"無法讀取 sprite importer：{label}");
                return;
            }

            if (expectedPpu.HasValue && Mathf.Abs(importer.spritePixelsPerUnit - expectedPpu.Value) > 0.01f)
            {
                issues.Add($"{label} PPU 不符：{assetPath}");
            }

            if (expectedFilterMode.HasValue && importer.filterMode != expectedFilterMode.Value)
            {
                issues.Add($"{label} filter 不符：{assetPath}");
            }
        }
    }
}
#endif
