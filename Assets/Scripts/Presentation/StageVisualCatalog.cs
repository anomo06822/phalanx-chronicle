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
                case "stage.jiangxia_ferry":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("4E6176"),
                        Hex("17202A"),
                        Hex("BDD1DF", 0.22f),
                        Hex("151A20"),
                        Hex("B6A16D"),
                        "river-smoke",
                        "jiangxia-bridges");
                case "stage.jiameng_pass":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("62624F"),
                        Hex("1A1A14"),
                        Hex("D2C79C", 0.22f),
                        Hex("17150F"),
                        Hex("C09A55"),
                        "ridge-wind",
                        "jiameng-ridge");
                case "stage.luocheng_siege":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("6B5448"),
                        Hex("221714"),
                        Hex("D7B39A", 0.22f),
                        Hex("1C1310"),
                        Hex("C48A58"),
                        "city-embers",
                        "luocheng-gate");
                case "stage.yangping_pass":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("556150"),
                        Hex("171B16"),
                        Hex("CAD1BF", 0.2f),
                        Hex("141613"),
                        Hex("AF945E"),
                        "stone-dust",
                        "yangping-slide");
                case "stage.hanshui":
                    return StageVisualDefinition.CreateRuntime(
                        stageNameKey,
                        Hex("5A6B6E"),
                        Hex("181E1E"),
                        Hex("C6D7D2", 0.18f),
                        Hex("151818"),
                        Hex("B59C69"),
                        "river-wind",
                        "hanshui-bank");
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
