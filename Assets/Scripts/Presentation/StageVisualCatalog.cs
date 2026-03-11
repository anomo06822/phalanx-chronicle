using System.Collections.Generic;
using PhalanxChronicle.Data;
using UnityEngine;

namespace PhalanxChronicle.Presentation
{
    public static class StageVisualCatalog
    {
        private const string ResourcePath = "StageVisualDefinitions";

        private static readonly Dictionary<string, StageVisualDefinition> definitionsByKey = new Dictionary<string, StageVisualDefinition>();
        private static readonly Dictionary<string, StageVisualDefinition> fallbackDefinitions = new Dictionary<string, StageVisualDefinition>();
        private static bool definitionsLoaded;

        public static StageVisualDefinition GetDefinition(string stageNameKey)
        {
            EnsureDefinitionsLoaded();
            string normalizedKey = string.IsNullOrWhiteSpace(stageNameKey) ? "stage.frontier_pass" : stageNameKey;
            if (definitionsByKey.TryGetValue(normalizedKey, out StageVisualDefinition definition))
            {
                return definition;
            }

            if (!fallbackDefinitions.TryGetValue(normalizedKey, out definition))
            {
                definition = CreateFallbackDefinition(normalizedKey);
                fallbackDefinitions[normalizedKey] = definition;
            }

            return definition;
        }

        private static void EnsureDefinitionsLoaded()
        {
            if (definitionsLoaded)
            {
                return;
            }

            definitionsLoaded = true;
            definitionsByKey.Clear();
            StageVisualDefinition[] definitions = Resources.LoadAll<StageVisualDefinition>(ResourcePath);
            foreach (StageVisualDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.StageNameKey) || definitionsByKey.ContainsKey(definition.StageNameKey))
                {
                    continue;
                }

                definitionsByKey.Add(definition.StageNameKey, definition);
            }
        }

        private static StageVisualDefinition CreateFallbackDefinition(string stageNameKey)
        {
            switch (stageNameKey)
            {
                case "stage.guangzong":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("705B3D"),
                        Hex("241A16"),
                        Hex("A88A5C", 0.30f),
                        Hex("1C1413"),
                        Hex("C59A49"),
                        "embers",
                        "guangzong-smoke");
                case "stage.changban_rearguard":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("55697A"),
                        Hex("181E24"),
                        Hex("C2D0D6", 0.24f),
                        Hex("15181C"),
                        Hex("B9B7A8"),
                        "river-mist",
                        "changban-river");
                case "stage.dingjun_mountain":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("5C6757"),
                        Hex("161A17"),
                        Hex("C6CBB8", 0.22f),
                        Hex("161815"),
                        Hex("BEA166"),
                        "mountain-wind",
                        "dingjun-stone");
                default:
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("5A594E"),
                        Hex("1B1815"),
                        Hex("C5B798", 0.22f),
                        Hex("171411"),
                        Hex("B18A4A"),
                        "dust",
                        "frontier-plain");
            }
        }

        private static Color Hex(string html, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString("#" + html, out Color color))
            {
                color = Color.white;
            }

            color.a = alpha;
            return color;
        }
    }
}
