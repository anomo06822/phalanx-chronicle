#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using PhalanxChronicle.Data;
using PhalanxChronicle.Presentation;
using UnityEditor;
using UnityEngine;

namespace PhalanxChronicle.Editor
{
    public static class CharacterVisualPipelineBuilder
    {
        private const string ManifestPath = "Docs/character-asset-manifest.csv";
        private const string DefinitionsFolder = "Assets/Resources/UnitVisualDefinitions";
        private const string PortraitsRoot = "Assets/Art/Characters/Portraits";
        private const string BattleRoot = "Assets/Art/Characters/Battle";
        private const string WeaponsRoot = "Assets/Art/UI/Weapons";
        private const string AutoSyncSessionKey = "PhalanxChronicle.CharacterVisualPipeline.AutoSyncOnce";

        private static readonly string[] RequiredFolders =
        {
            "Assets/Art",
            "Assets/Art/Characters",
            PortraitsRoot,
            PortraitsRoot + "/Heroes",
            PortraitsRoot + "/Bosses",
            PortraitsRoot + "/Enemies",
            BattleRoot,
            BattleRoot + "/Heroes",
            BattleRoot + "/Bosses",
            BattleRoot + "/Enemies",
            "Assets/Art/UI",
            WeaponsRoot,
            "Assets/Resources",
            DefinitionsFolder,
        };

        [MenuItem("Phalanx Chronicle/Visuals/Prepare Character Art Pipeline")]
        public static void PrepareCharacterArtPipeline()
        {
            EnsureRequiredFolders();
            ManifestSyncResult result = SyncUnitVisualDefinitionsFromManifest();
            ReimportArtAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[CharacterVisualPipeline] Prepared folders and synced definitions. " +
                $"Created={result.CreatedDefinitions}, Updated={result.UpdatedDefinitions}, " +
                $"PortraitsMissing={result.MissingPortraits.Count}, BattleMissing={result.MissingBattles.Count}, IconsMissing={result.MissingIcons.Count}.");
        }

        [MenuItem("Phalanx Chronicle/Visuals/Sync Unit Visual Definitions")]
        public static void SyncUnitVisualDefinitionsMenu()
        {
            ManifestSyncResult result = SyncUnitVisualDefinitionsFromManifest();
            ReimportArtAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[CharacterVisualPipeline] Synced definitions. " +
                $"Created={result.CreatedDefinitions}, Updated={result.UpdatedDefinitions}, " +
                $"PortraitsMissing={result.MissingPortraits.Count}, BattleMissing={result.MissingBattles.Count}, IconsMissing={result.MissingIcons.Count}.");
        }

        [MenuItem("Phalanx Chronicle/Visuals/Open Character Visual Guide")]
        public static void OpenCharacterVisualGuide()
        {
            UnityEngine.Object guide = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Docs/character-visual-production-guide.md");
            if (guide != null)
            {
                Selection.activeObject = guide;
                EditorGUIUtility.PingObject(guide);
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterAutoSync()
        {
            EditorApplication.delayCall += AutoSyncOnceAfterLoad;
        }

        private static void AutoSyncOnceAfterLoad()
        {
            if (SessionState.GetBool(AutoSyncSessionKey, false))
            {
                return;
            }

            SessionState.SetBool(AutoSyncSessionKey, true);
            if (!File.Exists(Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory(), ManifestPath)))
            {
                return;
            }

            try
            {
                EnsureRequiredFolders();
                ManifestSyncResult result = SyncUnitVisualDefinitionsFromManifest();
                ReimportArtAssets();
                AssetDatabase.SaveAssets();
                Debug.Log(
                    $"[CharacterVisualPipeline] Auto-sync complete. " +
                    $"Created={result.CreatedDefinitions}, Updated={result.UpdatedDefinitions}, " +
                    $"PortraitsMissing={result.MissingPortraits.Count}, BattleMissing={result.MissingBattles.Count}, IconsMissing={result.MissingIcons.Count}.");
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[CharacterVisualPipeline] Auto-sync skipped: " + exception.Message);
            }
        }

        private static ManifestSyncResult SyncUnitVisualDefinitionsFromManifest()
        {
            EnsureRequiredFolders();
            List<ManifestRow> rows = LoadManifestRows();
            ManifestSyncResult result = new ManifestSyncResult();

            foreach (ManifestRow row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.UnitId))
                {
                    continue;
                }

                string assetPath = $"{DefinitionsFolder}/{row.UnitId}.asset";
                bool created = false;
                UnitVisualDefinition definition = AssetDatabase.LoadAssetAtPath<UnitVisualDefinition>(assetPath);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<UnitVisualDefinition>();
                    AssetDatabase.CreateAsset(definition, assetPath);
                    created = true;
                    result.CreatedDefinitions++;
                }

                SerializedObject serializedObject = new SerializedObject(definition);
                bool changed = ApplyDefinition(serializedObject, row, created, result);
                if (changed)
                {
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(definition);
                    result.UpdatedDefinitions++;
                }
            }

            return result;
        }

        private static bool ApplyDefinition(SerializedObject serializedObject, ManifestRow row, bool created, ManifestSyncResult result)
        {
            bool changed = false;
            changed |= SetString(serializedObject, "unitId", row.UnitId);

            if (created)
            {
                changed |= SetBool(serializedObject, "heroProfile", row.DefaultHeroProfile);
                changed |= SetEnum(serializedObject, "archetype", UnitVisualArchetype.Default);
                changed |= SetEnum(serializedObject, "frameStyle", row.DefaultFrameStyle);
                changed |= SetBool(serializedObject, "useCustomPalette", false);
                changed |= SetFloat(serializedObject, "battleScale", 0f);
            }

            Sprite portraitSprite = FindSpriteByBaseName(PortraitsRoot, row.PortraitAssetKey);
            if (portraitSprite == null)
            {
                result.MissingPortraits.Add($"{row.UnitId}:{row.PortraitAssetKey}");
            }
            else
            {
                changed |= SetObjectIfMissing(serializedObject, "portraitSprite", portraitSprite);
            }

            Sprite battleSprite = FindSpriteByBaseName(BattleRoot, row.BattleAssetKey);
            if (battleSprite == null)
            {
                result.MissingBattles.Add($"{row.UnitId}:{row.BattleAssetKey}");
            }
            else
            {
                changed |= SetObjectIfMissing(serializedObject, "battleSprite", battleSprite);
                SerializedProperty battleScaleProperty = serializedObject.FindProperty("battleScale");
                if (battleScaleProperty != null && battleScaleProperty.floatValue <= 0f)
                {
                    battleScaleProperty.floatValue = 1f;
                    changed = true;
                }
            }

            Sprite weaponIcon = FindSpriteByBaseName(WeaponsRoot, row.WeaponIconKey);
            if (weaponIcon == null)
            {
                result.MissingIcons.Add($"{row.UnitId}:{row.WeaponIconKey}");
            }
            else
            {
                changed |= SetObjectIfMissing(serializedObject, "weaponIcon", weaponIcon);
            }

            return changed;
        }

        private static void EnsureRequiredFolders()
        {
            foreach (string folder in RequiredFolders)
            {
                EnsureFolder(folder);
            }
        }

        private static void ReimportArtAssets()
        {
            ReimportFolderSprites(PortraitsRoot);
            ReimportFolderSprites(BattleRoot);
            ReimportFolderSprites(WeaponsRoot);
        }

        private static void EnsureFolder(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            string normalizedPath = assetPath.Replace('\\', '/').TrimEnd('/');
            int separatorIndex = normalizedPath.LastIndexOf('/');
            if (separatorIndex <= 0)
            {
                return;
            }

            string parent = normalizedPath.Substring(0, separatorIndex);
            string folderName = normalizedPath.Substring(separatorIndex + 1);
            EnsureFolder(parent);

            if (!AssetDatabase.IsValidFolder(normalizedPath))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void ReimportFolderSprites(string rootFolder)
        {
            if (!AssetDatabase.IsValidFolder(rootFolder))
            {
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { rootFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static List<ManifestRow> LoadManifestRows()
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            string fullPath = Path.Combine(projectRoot, ManifestPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Character asset manifest not found at {fullPath}");
            }

            string[] lines = File.ReadAllLines(fullPath);
            List<ManifestRow> rows = new List<ManifestRow>();
            for (int index = 1; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                {
                    continue;
                }

                string[] values = ParseCsvLine(lines[index]);
                if (values.Length < 10)
                {
                    continue;
                }

                rows.Add(new ManifestRow(
                    values[0],
                    values[1],
                    values[2],
                    values[3],
                    values[4],
                    values[5],
                    values[6],
                    values[7],
                    values[8],
                    values[9]));
            }

            return rows;
        }

        private static string[] ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            bool inQuotes = false;
            int startIndex = 0;

            for (int index = 0; index < line.Length; index++)
            {
                char current = line[index];
                if (current == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (current == ',' && !inQuotes)
                {
                    values.Add(UnescapeCsv(line.Substring(startIndex, index - startIndex)));
                    startIndex = index + 1;
                }
            }

            values.Add(UnescapeCsv(line.Substring(startIndex)));
            return values.ToArray();
        }

        private static string UnescapeCsv(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
            {
                trimmed = trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed.Replace("\"\"", "\"");
        }

        private static Sprite FindSpriteByBaseName(string rootFolder, string baseName)
        {
            if (string.IsNullOrWhiteSpace(baseName) || !AssetDatabase.IsValidFolder(rootFolder))
            {
                return null;
            }

            string[] guids = AssetDatabase.FindAssets(baseName + " t:Sprite", new[] { rootFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), baseName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static bool SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.stringValue == value)
            {
                return false;
            }

            property.stringValue = value;
            return true;
        }

        private static bool SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.boolValue == value)
            {
                return false;
            }

            property.boolValue = value;
            return true;
        }

        private static bool SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || Mathf.Approximately(property.floatValue, value))
            {
                return false;
            }

            property.floatValue = value;
            return true;
        }

        private static bool SetEnum(SerializedObject serializedObject, string propertyName, Enum value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            int intValue = Convert.ToInt32(value);
            if (property == null || property.intValue == intValue)
            {
                return false;
            }

            property.intValue = intValue;
            return true;
        }

        private static bool SetObjectIfMissing(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            if (value == null)
            {
                return false;
            }

            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue != null)
            {
                return false;
            }

            property.objectReferenceValue = value;
            return true;
        }

        private sealed class ManifestRow
        {
            public ManifestRow(
                string unitId,
                string displayName,
                string faction,
                string role,
                string deliveryTier,
                string portraitAssetKey,
                string battleAssetKey,
                string weaponIconKey,
                string visualFamily,
                string notes)
            {
                UnitId = unitId?.Trim() ?? string.Empty;
                DisplayName = displayName?.Trim() ?? string.Empty;
                Faction = faction?.Trim() ?? string.Empty;
                Role = role?.Trim() ?? string.Empty;
                DeliveryTier = deliveryTier?.Trim() ?? string.Empty;
                PortraitAssetKey = portraitAssetKey?.Trim() ?? string.Empty;
                BattleAssetKey = battleAssetKey?.Trim() ?? string.Empty;
                WeaponIconKey = weaponIconKey?.Trim() ?? string.Empty;
                VisualFamily = visualFamily?.Trim() ?? string.Empty;
                Notes = notes?.Trim() ?? string.Empty;
            }

            public string UnitId { get; }

            public string DisplayName { get; }

            public string Faction { get; }

            public string Role { get; }

            public string DeliveryTier { get; }

            public string PortraitAssetKey { get; }

            public string BattleAssetKey { get; }

            public string WeaponIconKey { get; }

            public string VisualFamily { get; }

            public string Notes { get; }

            public bool DefaultHeroProfile =>
                Faction.Equals("Player", StringComparison.OrdinalIgnoreCase) ||
                DeliveryTier.StartsWith("P0", StringComparison.OrdinalIgnoreCase);

            public UnitFrameStyle DefaultFrameStyle =>
                Faction.Equals("Player", StringComparison.OrdinalIgnoreCase)
                    ? UnitFrameStyle.Hero
                    : DeliveryTier.StartsWith("P0", StringComparison.OrdinalIgnoreCase)
                        ? UnitFrameStyle.Boss
                        : UnitFrameStyle.Common;
        }

        private sealed class ManifestSyncResult
        {
            public int CreatedDefinitions { get; set; }

            public int UpdatedDefinitions { get; set; }

            public List<string> MissingPortraits { get; } = new List<string>();

            public List<string> MissingBattles { get; } = new List<string>();

            public List<string> MissingIcons { get; } = new List<string>();
        }
    }
}
#endif
