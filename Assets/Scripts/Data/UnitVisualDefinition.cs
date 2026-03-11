using PhalanxChronicle.Presentation;
using UnityEngine;

namespace PhalanxChronicle.Data
{
    [CreateAssetMenu(menuName = "Phalanx Chronicle/Unit Visual Definition", fileName = "UnitVisualDefinition")]
    public sealed class UnitVisualDefinition : ScriptableObject
    {
        [SerializeField] private string unitId = string.Empty;
        [SerializeField] private bool heroProfile = true;
        [SerializeField] private UnitVisualArchetype archetype = UnitVisualArchetype.Default;
        [SerializeField] private UnitFrameStyle frameStyle = UnitFrameStyle.Common;
        [SerializeField] private bool useCustomPalette;
        [SerializeField] private Color primaryColor = Color.white;
        [SerializeField] private Color secondaryColor = Color.white;
        [SerializeField] private Color accentColor = Color.white;
        [SerializeField] private Color frameColor = Color.white;
        [SerializeField] private Color markerColor = Color.white;
        [SerializeField] private Color portraitBackdropColor = Color.white;
        [SerializeField] private float battleScale = 1f;
        [SerializeField] private Sprite portraitSprite;
        [SerializeField] private Sprite battleSprite;
        [SerializeField] private Sprite weaponIcon;
        [SerializeField] private Sprite factionMarker;
        [SerializeField] private Sprite selectionFrame;
        [SerializeField] private RuntimeAnimatorController idleAnimationController;

        public string UnitId => unitId;

        public bool HeroProfile => heroProfile;

        public UnitVisualArchetype Archetype => archetype;

        public UnitFrameStyle FrameStyle => frameStyle;

        public bool UseCustomPalette => useCustomPalette;

        public Color PrimaryColor => primaryColor;

        public Color SecondaryColor => secondaryColor;

        public Color AccentColor => accentColor;

        public Color FrameColor => frameColor;

        public Color MarkerColor => markerColor;

        public Color PortraitBackdropColor => portraitBackdropColor;

        public float BattleScale => battleScale;

        public Sprite PortraitSprite => portraitSprite;

        public Sprite BattleSprite => battleSprite;

        public Sprite WeaponIcon => weaponIcon;

        public Sprite FactionMarker => factionMarker;

        public Sprite SelectionFrame => selectionFrame;

        public RuntimeAnimatorController IdleAnimationController => idleAnimationController;
    }
}
