using UnityEngine;

namespace PhalanxChronicle.Data
{
    [CreateAssetMenu(menuName = "Phalanx Chronicle/Stage Visual Definition", fileName = "StageVisualDefinition")]
    public sealed class StageVisualDefinition : ScriptableObject
    {
        [SerializeField] private string stageNameKey = "stage.frontier_pass";
        [SerializeField] private Color skyTopColor = new Color(0.25f, 0.25f, 0.28f, 1f);
        [SerializeField] private Color skyBottomColor = new Color(0.11f, 0.11f, 0.12f, 1f);
        [SerializeField] private Color hazeColor = new Color(0.56f, 0.52f, 0.46f, 0.28f);
        [SerializeField] private Color boardBackdropColor = new Color(0.14f, 0.11f, 0.09f, 1f);
        [SerializeField] private Color boardFrameColor = new Color(0.77f, 0.61f, 0.29f, 1f);
        [SerializeField] private Sprite landmarkSprite;
        [SerializeField] private Sprite farBackdropSprite;
        [SerializeField] private Sprite midBackdropSprite;
        [SerializeField] private string ambientParticlePresetId = "dust";
        [SerializeField] private string terrainPaletteId = "frontier-plain";

        public string StageNameKey => stageNameKey;

        public Color SkyTopColor => skyTopColor;

        public Color SkyBottomColor => skyBottomColor;

        public Color HazeColor => hazeColor;

        public Color BoardBackdropColor => boardBackdropColor;

        public Color BoardFrameColor => boardFrameColor;

        public Sprite LandmarkSprite => landmarkSprite;

        public Sprite FarBackdropSprite => farBackdropSprite;

        public Sprite MidBackdropSprite => midBackdropSprite;

        public string AmbientParticlePresetId => ambientParticlePresetId;

        public string TerrainPaletteId => terrainPaletteId;

        public static StageVisualDefinition CreateRuntime(
            string key,
            Color skyTop,
            Color skyBottom,
            Color haze,
            Color boardBackdrop,
            Color boardFrame,
            string ambientPresetId,
            string paletteId,
            Sprite farSprite = null,
            Sprite midSprite = null,
            Sprite landmark = null)
        {
            StageVisualDefinition definition = CreateInstance<StageVisualDefinition>();
            definition.stageNameKey = string.IsNullOrWhiteSpace(key) ? "stage.frontier_pass" : key;
            definition.skyTopColor = skyTop;
            definition.skyBottomColor = skyBottom;
            definition.hazeColor = haze;
            definition.boardBackdropColor = boardBackdrop;
            definition.boardFrameColor = boardFrame;
            definition.ambientParticlePresetId = string.IsNullOrWhiteSpace(ambientPresetId) ? "dust" : ambientPresetId;
            definition.terrainPaletteId = string.IsNullOrWhiteSpace(paletteId) ? "frontier-plain" : paletteId;
            definition.farBackdropSprite = farSprite;
            definition.midBackdropSprite = midSprite;
            definition.landmarkSprite = landmark;
            return definition;
        }
    }
}
