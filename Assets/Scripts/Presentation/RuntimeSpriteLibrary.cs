using System.Collections.Generic;
using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Presentation
{
    public enum GridOverlayKind
    {
        Move = 0,
        Attack = 1,
        Skill = 2,
        Selected = 3,
    }

    public enum StageBackdropLayer
    {
        Far = 0,
        Mid = 1,
    }

    public static class RuntimeSpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> unitSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> portraitSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> weaponSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> factionMarkerSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> terrainBaseSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> terrainOverlaySprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> terrainPropSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> stageLandmarkSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> stageBackdropSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> ambientOverlaySprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> gradientSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<GridOverlayKind, Sprite> gridOverlaySprites = new Dictionary<GridOverlayKind, Sprite>();

        private static Sprite whiteSprite;
        private static Sprite tileSprite;
        private static Sprite frameSprite;
        private static Sprite bannerSprite;
        private static Sprite mistBandSprite;
        private static Sprite inkPanelSprite;
        private static Sprite slashSprite;
        private static Sprite ringSprite;
        private static Sprite sparkSprite;
        private static Sprite arrowSprite;
        private static Font defaultFont;
        private static Font headingFont;
        private static Font bodyFont;

        public static Sprite WhiteSprite
        {
            get
            {
                if (whiteSprite == null)
                {
                    Texture2D texture = Texture2D.whiteTexture;
                    whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
                }

                return whiteSprite;
            }
        }

        public static Sprite TileSprite => tileSprite ??= CreatePlainTileSprite();

        public static Sprite FrameSprite => frameSprite ??= CreateFrameSprite();

        public static Sprite BannerSprite => bannerSprite ??= CreateBannerSprite();

        public static Sprite MistBandSprite => mistBandSprite ??= CreateMistBandSprite();

        public static Sprite InkPanelSprite => inkPanelSprite ??= CreateInkPanelSprite();

        public static Sprite SlashSprite => slashSprite ??= CreateSlashSprite();

        public static Sprite RingSprite => ringSprite ??= CreateRingSprite();

        public static Sprite SparkSprite => sparkSprite ??= CreateSparkSprite();

        public static Sprite ArrowSprite => arrowSprite ??= CreateArrowSprite();

        public static Font HeadingFont => headingFont ??= CreateHeadingFont();

        public static Font BodyFont => bodyFont ??= CreateBodyFont();

        public static Font DefaultFont => defaultFont ??= BodyFont;

        public static Font GetUiFont(int size, FontStyle fontStyle)
        {
            return fontStyle == FontStyle.Bold && size >= 17 ? HeadingFont : BodyFont;
        }

        public static Sprite GetUnitSprite(string unitId, UnitFaction faction)
        {
            return GetUnitSprite(unitId, faction, UnitRole.Commander);
        }

        public static Sprite GetUnitSprite(string unitId, UnitFaction faction, UnitRole role)
        {
            return GetUnitSprite(UnitVisualCatalog.GetProfile(unitId, faction, role));
        }

        public static Sprite GetUnitSprite(UnitVisualProfile profile)
        {
            if (profile == null)
            {
                return GetUnitSprite(string.Empty, UnitFaction.Player, UnitRole.Commander);
            }

            if (profile.BattleSprite != null)
            {
                return profile.BattleSprite;
            }

            string key = "unit:" + profile.UnitId + ":" + profile.Archetype + ":" + profile.Role + ":" + profile.Faction;
            if (!unitSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CreateUnitSprite(profile);
                unitSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetPortraitSprite(string unitId, UnitFaction faction, UnitRole role)
        {
            return GetPortraitSprite(UnitVisualCatalog.GetProfile(unitId, faction, role));
        }

        public static Sprite GetPortraitSprite(UnitVisualProfile profile)
        {
            if (profile == null)
            {
                return GetPortraitSprite(string.Empty, UnitFaction.Player, UnitRole.Commander);
            }

            if (profile.PortraitSprite != null)
            {
                return profile.PortraitSprite;
            }

            string key = "portrait:" + profile.UnitId + ":" + profile.Archetype + ":" + profile.Role + ":" + profile.Faction;
            if (!portraitSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CreatePortraitSprite(profile);
                portraitSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWeaponSprite(UnitRole role, UnitFaction faction)
        {
            string key = "weapon:" + role + ":" + faction;
            if (!weaponSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CreateWeaponSprite(role, faction);
                weaponSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetWeaponSprite(UnitVisualProfile profile)
        {
            if (profile != null && profile.WeaponIcon != null)
            {
                return profile.WeaponIcon;
            }

            return GetWeaponSprite(profile != null ? profile.Role : UnitRole.Commander, profile != null ? profile.Faction : UnitFaction.Player);
        }

        public static Sprite GetFactionMarkerSprite(UnitVisualProfile profile)
        {
            if (profile != null && profile.FactionMarker != null)
            {
                return profile.FactionMarker;
            }

            string key = "marker:" + (profile != null ? profile.FrameStyle.ToString() : UnitFrameStyle.Common.ToString()) + ":" + (profile != null ? profile.Faction.ToString() : UnitFaction.Player.ToString());
            if (!factionMarkerSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CreateFactionMarkerSprite(profile);
                factionMarkerSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetTerrainBaseSprite(TerrainType terrainType, bool blocked)
        {
            string key = "base:" + GetTerrainResourceKey(terrainType, blocked);
            if (!terrainBaseSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = TryLoadSpriteResource("Terrain/" + GetTerrainResourceKey(terrainType, blocked) + "_base") ?? CreateTerrainBaseSprite(terrainType, blocked);
                terrainBaseSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetTerrainOverlaySprite(TerrainType terrainType, bool blocked)
        {
            string key = "overlay:" + GetTerrainResourceKey(terrainType, blocked);
            if (!terrainOverlaySprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = TryLoadSpriteResource("Terrain/" + GetTerrainResourceKey(terrainType, blocked) + "_overlay") ?? CreateTerrainOverlaySprite(terrainType, blocked);
                terrainOverlaySprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetTerrainPropSprite(TerrainType terrainType, bool blocked)
        {
            string key = "prop:" + GetTerrainResourceKey(terrainType, blocked);
            if (!terrainPropSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = TryLoadSpriteResource("Terrain/" + GetTerrainResourceKey(terrainType, blocked) + "_prop") ?? CreateTerrainPropSprite(terrainType, blocked);
                terrainPropSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStageLandmarkSprite(string stageNameKey)
        {
            string key = string.IsNullOrWhiteSpace(stageNameKey) ? "default" : stageNameKey;
            if (!stageLandmarkSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = TryLoadSpriteResource("StageBackdrops/" + key) ?? CreateStageLandmarkSprite(key);
                stageLandmarkSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetStageBackdropSprite(string stageNameKey, StageBackdropLayer layer)
        {
            string key = (string.IsNullOrWhiteSpace(stageNameKey) ? "default" : stageNameKey) + ":" + layer;
            if (!stageBackdropSprites.TryGetValue(key, out Sprite sprite))
            {
                string resourcePath = "StageBackdrops/" + (string.IsNullOrWhiteSpace(stageNameKey) ? "default" : stageNameKey) + "_" + layer.ToString().ToLowerInvariant();
                sprite = TryLoadSpriteResource(resourcePath) ?? CreateStageBackdropSprite(stageNameKey, layer);
                stageBackdropSprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetAmbientOverlaySprite(string presetId)
        {
            string key = string.IsNullOrWhiteSpace(presetId) ? "default" : presetId;
            if (!ambientOverlaySprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CreateAmbientOverlaySprite(key);
                ambientOverlaySprites[key] = sprite;
            }

            return sprite;
        }

        public static Sprite GetVerticalGradientSprite(Color top, Color bottom)
        {
            string key = "gradient:" + ColorUtility.ToHtmlStringRGBA(top) + ":" + ColorUtility.ToHtmlStringRGBA(bottom);
            if (!gradientSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = CreateVerticalGradientSprite(top, bottom);
                gradientSprites[key] = sprite;
            }

            return sprite;
        }

        public static Color GetTerrainBaseTint(string paletteId, TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return GetPaletteColor(paletteId, "blocked");
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return GetPaletteColor(paletteId, "forest");
                case TerrainType.Fort:
                    return GetPaletteColor(paletteId, "fort");
                case TerrainType.Hazard:
                    return GetPaletteColor(paletteId, "hazard");
                default:
                    return GetPaletteColor(paletteId, "plain");
            }
        }

        public static Color GetTerrainOverlayTint(string paletteId, TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return GetPaletteColor(paletteId, "overlay-blocked");
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return GetPaletteColor(paletteId, "overlay-forest");
                case TerrainType.Fort:
                    return GetPaletteColor(paletteId, "overlay-fort");
                case TerrainType.Hazard:
                    return GetPaletteColor(paletteId, "overlay-hazard");
                default:
                    return GetPaletteColor(paletteId, "overlay-plain");
            }
        }

        public static Color GetTerrainPropTint(string paletteId, TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return GetPaletteColor(paletteId, "prop-blocked");
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return GetPaletteColor(paletteId, "prop-forest");
                case TerrainType.Fort:
                    return GetPaletteColor(paletteId, "prop-fort");
                case TerrainType.Hazard:
                    return GetPaletteColor(paletteId, "prop-hazard");
                default:
                    return GetPaletteColor(paletteId, "prop-plain");
            }
        }

        public static Sprite GetGridOverlaySprite(GridOverlayKind kind)
        {
            if (!gridOverlaySprites.TryGetValue(kind, out Sprite sprite))
            {
                sprite = CreateGridOverlaySprite(kind);
                gridOverlaySprites[kind] = sprite;
            }

            return sprite;
        }

        private static Sprite CreatePlainTileSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 border = new Color32(74, 54, 36, 255);
            Color32 fillA = new Color32(168, 143, 95, 255);
            Color32 fillB = new Color32(151, 129, 84, 255);
            Color32 accent = new Color32(196, 172, 122, 255);
            FillBaseTile(texture, border, fillA, fillB, accent);
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateForestTileSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 border = new Color32(35, 52, 28, 255);
            Color32 fillA = new Color32(88, 104, 60, 255);
            Color32 fillB = new Color32(66, 82, 46, 255);
            Color32 accent = new Color32(138, 162, 90, 255);
            FillBaseTile(texture, border, fillA, fillB, accent);
            FillCircle(texture, 5, 15, 3, new Color32(90, 124, 61, 255));
            FillCircle(texture, 14, 15, 3, new Color32(90, 124, 61, 255));
            FillRect(texture, 8, 4, 11, 10, new Color32(78, 60, 37, 255));
            DrawLine(texture, 3, 16, 8, 12, accent, 1);
            DrawLine(texture, 16, 16, 11, 12, accent, 1);
            DrawLine(texture, 5, 8, 15, 8, new Color32(57, 76, 40, 255), 1);
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateFortTileSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 border = new Color32(66, 61, 56, 255);
            Color32 fillA = new Color32(152, 148, 141, 255);
            Color32 fillB = new Color32(120, 116, 109, 255);
            Color32 accent = new Color32(191, 184, 170, 255);
            FillBaseTile(texture, border, fillA, fillB, accent);
            Color32 mortar = new Color32(91, 85, 78, 255);
            for (int y = 4; y <= 14; y += 5)
            {
                DrawLine(texture, 2, y, 17, y, mortar, 1);
            }

            for (int x = 5; x <= 15; x += 5)
            {
                DrawLine(texture, x, 2, x, 17, mortar, 1);
            }

            FillRect(texture, 2, 2, 5, 5, accent);
            FillRect(texture, 14, 2, 17, 5, accent);
            FillRect(texture, 2, 14, 5, 17, accent);
            FillRect(texture, 14, 14, 17, 17, accent);
            StrokeRect(texture, 3, 3, 16, 16, border);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateHazardTileSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 border = new Color32(74, 30, 17, 255);
            Color32 fillA = new Color32(101, 48, 30, 255);
            Color32 fillB = new Color32(78, 30, 21, 255);
            Color32 accent = new Color32(214, 105, 47, 255);
            Color32 core = new Color32(255, 190, 104, 255);
            FillBaseTile(texture, border, fillA, fillB, accent);
            DrawLine(texture, 4, 4, 8, 11, accent, 1);
            DrawLine(texture, 5, 5, 7, 9, core, 1);
            DrawLine(texture, 13, 4, 16, 10, accent, 1);
            DrawLine(texture, 14, 5, 15, 8, core, 1);
            DrawLine(texture, 6, 15, 11, 11, accent, 1);
            DrawLine(texture, 7, 14, 10, 12, core, 1);
            FillCircle(texture, 10, 10, 2, new Color32(232, 120, 55, 255));
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateBlockedTileSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 border = new Color32(27, 31, 37, 255);
            Color32 fillA = new Color32(67, 72, 80, 255);
            Color32 fillB = new Color32(50, 56, 64, 255);
            Color32 accent = new Color32(104, 112, 123, 255);
            Color32 warning = new Color32(170, 178, 188, 255);
            FillBaseTile(texture, border, fillA, fillB, accent);
            FillRect(texture, 3, 8, 16, 17, accent);
            StrokeRect(texture, 3, 8, 16, 17, border);
            DrawLine(texture, 4, 7, 15, 17, warning, 1);
            DrawLine(texture, 15, 7, 4, 17, warning, 1);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateFrameSprite()
        {
            Texture2D texture = CreateTexture(24, 24);
            Color32 bronze = new Color32(197, 154, 73, 255);
            Color32 innerBronze = new Color32(233, 203, 140, 255);
            StrokeRect(texture, 0, 0, 23, 23, bronze);
            StrokeRect(texture, 2, 2, 21, 21, bronze);
            FillTriangle(texture, new Vector2Int(0, 6), new Vector2Int(6, 0), new Vector2Int(7, 7), innerBronze);
            FillTriangle(texture, new Vector2Int(17, 0), new Vector2Int(23, 6), new Vector2Int(16, 7), innerBronze);
            FillTriangle(texture, new Vector2Int(0, 17), new Vector2Int(6, 23), new Vector2Int(7, 16), innerBronze);
            FillTriangle(texture, new Vector2Int(17, 23), new Vector2Int(23, 17), new Vector2Int(16, 16), innerBronze);
            DrawLine(texture, 4, 1, 9, 1, innerBronze, 1);
            DrawLine(texture, 14, 1, 19, 1, innerBronze, 1);
            DrawLine(texture, 4, 22, 9, 22, innerBronze, 1);
            DrawLine(texture, 14, 22, 19, 22, innerBronze, 1);
            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateBannerSprite()
        {
            Texture2D texture = CreateTexture(72, 22);
            Color32 fill = new Color32(25, 27, 31, 236);
            Color32 accent = new Color32(197, 154, 73, 255);
            FillRect(texture, 6, 3, 65, 18, fill);
            StrokeRect(texture, 6, 3, 65, 18, accent);
            FillTriangle(texture, new Vector2Int(6, 3), new Vector2Int(0, 10), new Vector2Int(6, 18), fill);
            FillTriangle(texture, new Vector2Int(65, 3), new Vector2Int(71, 10), new Vector2Int(65, 18), fill);
            DrawLine(texture, 14, 6, 58, 6, accent, 1);
            DrawLine(texture, 14, 15, 58, 15, new Color32(102, 77, 40, 255), 1);
            texture.Apply();
            return CreateSprite(texture, 36f);
        }

        private static Sprite CreateMistBandSprite()
        {
            Texture2D texture = CreateTexture(160, 48, FilterMode.Bilinear);
            Color baseColor = new Color(1f, 1f, 1f, 0.68f);
            FillEllipse(texture, 38f, 24f, 28f, 10f, new Color(baseColor.r, baseColor.g, baseColor.b, 0.22f));
            FillEllipse(texture, 78f, 30f, 34f, 11f, new Color(baseColor.r, baseColor.g, baseColor.b, 0.3f));
            FillEllipse(texture, 120f, 22f, 26f, 9f, new Color(baseColor.r, baseColor.g, baseColor.b, 0.18f));
            DrawEllipseRing(texture, 78f, 25f, 54f, 14f, new Color(1f, 1f, 1f, 0.08f), 0.08f);
            texture.Apply();
            return CreateSprite(texture, 160f);
        }

        private static Sprite CreateInkPanelSprite()
        {
            Texture2D texture = CreateTexture(48, 48, FilterMode.Bilinear);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float edgeDistance = Mathf.Min(Mathf.Min(x, texture.width - 1 - x), Mathf.Min(y, texture.height - 1 - y));
                    float edgeFade = Mathf.Clamp01(edgeDistance / 6f);
                    float grain = (((x * 13) + (y * 7)) % 17) / 16f;
                    float alpha = Mathf.Lerp(0.82f, 1f, grain * 0.16f) * edgeFade;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateSlashSprite()
        {
            Texture2D texture = CreateTexture(48, 48, FilterMode.Bilinear);
            Color32 bright = new Color32(255, 243, 214, 255);
            Color32 warm = new Color32(229, 159, 88, 255);
            FillTriangle(texture, new Vector2Int(6, 12), new Vector2Int(32, 38), new Vector2Int(14, 40), warm);
            FillTriangle(texture, new Vector2Int(11, 10), new Vector2Int(38, 33), new Vector2Int(20, 37), bright);
            DrawLine(texture, 8, 13, 39, 34, new Color32(255, 255, 255, 164), 1);
            texture.Apply();
            return CreateSprite(texture, 30f);
        }

        private static Sprite CreateRingSprite()
        {
            Texture2D texture = CreateTexture(40, 40, FilterMode.Bilinear);
            Vector2 center = new Vector2(19.5f, 19.5f);
            Color32 ring = new Color32(255, 255, 255, 255);
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance >= 12f && distance <= 15f)
                    {
                        texture.SetPixel(x, y, ring);
                    }
                }
            }

            DrawEllipseRing(texture, 19.5f, 19.5f, 11f, 11f, new Color(1f, 1f, 1f, 0.4f), 0.1f);
            texture.Apply();
            return CreateSprite(texture, 30f);
        }

        private static Sprite CreateSparkSprite()
        {
            Texture2D texture = CreateTexture(40, 40, FilterMode.Bilinear);
            Color32 bright = new Color32(255, 255, 255, 255);
            DrawLine(texture, 20, 6, 20, 34, bright, 1);
            DrawLine(texture, 6, 20, 34, 20, bright, 1);
            DrawLine(texture, 10, 10, 30, 30, bright, 1);
            DrawLine(texture, 10, 30, 30, 10, bright, 1);
            FillRect(texture, 18, 18, 22, 22, bright);
            texture.Apply();
            return CreateSprite(texture, 30f);
        }

        private static Sprite CreateArrowSprite()
        {
            Texture2D texture = CreateTexture(40, 40, FilterMode.Bilinear);
            Color32 bright = new Color32(255, 255, 255, 255);
            FillRect(texture, 18, 6, 21, 23, bright);
            FillRect(texture, 15, 21, 24, 24, bright);
            DrawLine(texture, 11, 19, 20, 34, bright, 1);
            DrawLine(texture, 29, 19, 20, 34, bright, 1);
            texture.Apply();
            return CreateSprite(texture, 30f);
        }

        private static Sprite CreateWeaponSprite(UnitRole role, UnitFaction faction)
        {
            Texture2D texture = CreateTexture(32, 32);
            Color32 outline = new Color32(43, 29, 21, 255);
            Color32 metal = faction == UnitFaction.Player
                ? new Color32(233, 224, 200, 255)
                : new Color32(222, 201, 174, 255);
            Color32 accent = faction == UnitFaction.Player
                ? new Color32(100, 158, 233, 255)
                : new Color32(210, 103, 80, 255);
            Color32 grip = new Color32(120, 76, 44, 255);

            switch (role)
            {
                case UnitRole.Commander:
                    FillRect(texture, 14, 5, 17, 21, metal);
                    FillRect(texture, 12, 21, 19, 23, accent);
                    FillRect(texture, 15, 23, 16, 27, grip);
                    FillRect(texture, 18, 14, 24, 17, accent);
                    StrokeRect(texture, 14, 5, 17, 21, outline);
                    StrokeRect(texture, 12, 21, 19, 23, outline);
                    StrokeRect(texture, 18, 14, 24, 17, outline);
                    break;
                case UnitRole.Guardian:
                    DrawLine(texture, 10, 5, 21, 25, metal, 1);
                    DrawLine(texture, 8, 6, 19, 26, metal, 1);
                    FillRect(texture, 18, 20, 27, 26, accent);
                    FillRect(texture, 7, 4, 13, 10, metal);
                    StrokeRect(texture, 18, 20, 27, 26, outline);
                    StrokeRect(texture, 7, 4, 13, 10, outline);
                    break;
                case UnitRole.Ranger:
                    DrawLine(texture, 11, 5, 11, 26, accent, 1);
                    DrawLine(texture, 20, 5, 20, 26, accent, 1);
                    DrawLine(texture, 11, 5, 20, 15, metal, 1);
                    DrawLine(texture, 11, 26, 20, 15, metal, 1);
                    DrawLine(texture, 15, 8, 15, 23, outline, 1);
                    break;
                case UnitRole.Scout:
                    DrawLine(texture, 9, 6, 16, 23, metal, 1);
                    DrawLine(texture, 16, 23, 20, 27, grip, 1);
                    DrawLine(texture, 23, 6, 16, 23, metal, 1);
                    DrawLine(texture, 16, 23, 12, 27, grip, 1);
                    DrawLine(texture, 8, 7, 15, 24, outline, 1);
                    DrawLine(texture, 24, 7, 17, 24, outline, 1);
                    break;
                case UnitRole.Raider:
                    DrawLine(texture, 7, 6, 24, 26, grip, 1);
                    DrawLine(texture, 9, 4, 26, 24, grip, 1);
                    FillRect(texture, 19, 20, 29, 28, metal);
                    StrokeRect(texture, 19, 20, 29, 28, outline);
                    FillRect(texture, 15, 14, 18, 17, accent);
                    break;
                default:
                    FillRect(texture, 13, 6, 18, 24, metal);
                    FillRect(texture, 12, 24, 19, 27, grip);
                    StrokeRect(texture, 13, 6, 18, 24, outline);
                    break;
            }

            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateFactionMarkerSprite(UnitVisualProfile profile)
        {
            Texture2D texture = CreateTexture(40, 18);
            Color markerColor = profile != null ? profile.MarkerColor : new Color(0.34f, 0.62f, 0.94f, 1f);
            Color shadow = Color.Lerp(markerColor, Color.black, 0.48f);
            FillEllipse(texture, 20f, 8.5f, 18f, 7f, new Color(shadow.r, shadow.g, shadow.b, 0.85f));
            FillEllipse(texture, 20f, 9f, 14f, 4.2f, new Color(markerColor.r, markerColor.g, markerColor.b, 0.98f));
            DrawEllipseRing(texture, 20f, 9f, 14f, 4.2f, Color.white, 0.7f);
            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateTerrainBaseSprite(TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return CreateBlockedTileSprite();
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return CreateForestTileSprite();
                case TerrainType.Fort:
                    return CreateFortTileSprite();
                case TerrainType.Hazard:
                    return CreateHazardTileSprite();
                default:
                    return CreatePlainTileSprite();
            }
        }

        private static Sprite CreateTerrainOverlaySprite(TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return CreateBlockedOverlaySprite();
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return CreateForestOverlaySprite();
                case TerrainType.Fort:
                    return CreateFortOverlaySprite();
                case TerrainType.Hazard:
                    return CreateHazardOverlaySprite();
                default:
                    return CreatePlainOverlaySprite();
            }
        }

        private static Sprite CreateTerrainPropSprite(TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return CreateBoulderPropSprite();
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return CreateTreePropSprite();
                case TerrainType.Fort:
                    return CreateFortPropSprite();
                case TerrainType.Hazard:
                    return CreateFlamePropSprite();
                default:
                    return null;
            }
        }

        private static Sprite CreateStageLandmarkSprite(string stageNameKey)
        {
            Texture2D texture = CreateTexture(160, 72, FilterMode.Bilinear);
            Color silhouette = new Color(1f, 1f, 1f, 0.95f);
            if (stageNameKey == "stage.changban_rearguard")
            {
                FillTriangle(texture, new Vector2Int(18, 20), new Vector2Int(48, 20), new Vector2Int(34, 42), silhouette);
                FillTriangle(texture, new Vector2Int(42, 20), new Vector2Int(80, 20), new Vector2Int(61, 50), silhouette);
                FillTriangle(texture, new Vector2Int(76, 20), new Vector2Int(116, 20), new Vector2Int(97, 44), silhouette);
                FillTriangle(texture, new Vector2Int(110, 20), new Vector2Int(144, 20), new Vector2Int(126, 38), silhouette);
                DrawLine(texture, 20, 20, 144, 20, silhouette, 2);
                for (int x = 28; x <= 138; x += 12)
                {
                    DrawLine(texture, x, 20, x, 30, silhouette, 1);
                }
            }
            else if (stageNameKey == "stage.dingjun_mountain")
            {
                FillTriangle(texture, new Vector2Int(10, 16), new Vector2Int(52, 58), new Vector2Int(90, 16), silhouette);
                FillTriangle(texture, new Vector2Int(66, 16), new Vector2Int(106, 62), new Vector2Int(150, 16), silhouette);
                DrawLine(texture, 14, 16, 150, 16, silhouette, 2);
                for (int x = 24; x <= 140; x += 14)
                {
                    DrawLine(texture, x, 16, x, 30, silhouette, 1);
                }
            }
            else
            {
                FillRect(texture, 16, 18, 146, 26, silhouette);
                for (int x = 28; x <= 136; x += 20)
                {
                    DrawLine(texture, x, 26, x, 52, silhouette, 2);
                    FillTriangle(texture, new Vector2Int(x, 50), new Vector2Int(x + 16, 42), new Vector2Int(x, 32), silhouette);
                }

                DrawLine(texture, 26, 18, 34, 32, silhouette, 1);
                DrawLine(texture, 82, 18, 90, 34, silhouette, 1);
                DrawLine(texture, 120, 18, 128, 34, silhouette, 1);
            }

            texture.Apply();
            return CreateSprite(texture, 160f);
        }

        private static Sprite CreateStageBackdropSprite(string stageNameKey, StageBackdropLayer layer)
        {
            Texture2D texture = CreateTexture(220, 84, FilterMode.Bilinear);
            Color silhouette = new Color(1f, 1f, 1f, layer == StageBackdropLayer.Far ? 0.72f : 0.9f);
            if (stageNameKey == "stage.guangzong")
            {
                if (layer == StageBackdropLayer.Far)
                {
                    FillTriangle(texture, new Vector2Int(0, 20), new Vector2Int(54, 52), new Vector2Int(100, 20), silhouette);
                    FillTriangle(texture, new Vector2Int(74, 20), new Vector2Int(130, 48), new Vector2Int(188, 20), silhouette);
                }
                else
                {
                    FillRect(texture, 8, 18, 206, 24, silhouette);
                    for (int x = 20; x <= 200; x += 24)
                    {
                        DrawLine(texture, x, 24, x, 58, silhouette, 2);
                        FillTriangle(texture, new Vector2Int(x, 56), new Vector2Int(x + 18, 46), new Vector2Int(x, 36), silhouette);
                    }
                }
            }
            else if (stageNameKey == "stage.changban_rearguard")
            {
                if (layer == StageBackdropLayer.Far)
                {
                    DrawLine(texture, 0, 26, 60, 34, silhouette, 2);
                    DrawLine(texture, 60, 34, 132, 24, silhouette, 2);
                    DrawLine(texture, 132, 24, 220, 30, silhouette, 2);
                    FillRect(texture, 0, 0, 220, 18, new Color(1f, 1f, 1f, 0.22f));
                }
                else
                {
                    FillRect(texture, 0, 18, 220, 24, silhouette);
                    for (int x = 18; x <= 196; x += 22)
                    {
                        DrawLine(texture, x, 18, x + 8, 46, silhouette, 1);
                    }
                }
            }
            else
            {
                if (layer == StageBackdropLayer.Far)
                {
                    FillTriangle(texture, new Vector2Int(0, 22), new Vector2Int(52, 60), new Vector2Int(98, 22), silhouette);
                    FillTriangle(texture, new Vector2Int(70, 22), new Vector2Int(126, 66), new Vector2Int(184, 22), silhouette);
                    FillTriangle(texture, new Vector2Int(156, 22), new Vector2Int(196, 54), new Vector2Int(220, 22), silhouette);
                }
                else
                {
                    DrawLine(texture, 0, 20, 220, 20, silhouette, 2);
                    for (int x = 14; x <= 206; x += 18)
                    {
                        DrawLine(texture, x, 20, x + 6, 44, silhouette, 1);
                    }
                }
            }

            texture.Apply();
            return CreateSprite(texture, 220f);
        }

        private static Sprite CreateAmbientOverlaySprite(string presetId)
        {
            Texture2D texture = CreateTexture(180, 52, FilterMode.Bilinear);
            if (presetId == "embers")
            {
                for (int x = 8; x < 172; x += 24)
                {
                    FillCircle(texture, x, 14 + (x % 3), 3, new Color(1f, 1f, 1f, 0.28f));
                    FillCircle(texture, x + 8, 26 + (x % 5), 2, new Color(1f, 1f, 1f, 0.22f));
                }
            }
            else if (presetId == "river-mist")
            {
                FillEllipse(texture, 48f, 26f, 36f, 10f, new Color(1f, 1f, 1f, 0.2f));
                FillEllipse(texture, 112f, 22f, 44f, 12f, new Color(1f, 1f, 1f, 0.24f));
                FillEllipse(texture, 146f, 28f, 28f, 8f, new Color(1f, 1f, 1f, 0.18f));
            }
            else if (presetId == "mountain-wind")
            {
                DrawLine(texture, 8, 12, 52, 34, new Color(1f, 1f, 1f, 0.16f), 1);
                DrawLine(texture, 62, 18, 124, 32, new Color(1f, 1f, 1f, 0.22f), 1);
                DrawLine(texture, 116, 10, 170, 30, new Color(1f, 1f, 1f, 0.15f), 1);
            }
            else
            {
                FillEllipse(texture, 48f, 18f, 18f, 8f, new Color(1f, 1f, 1f, 0.14f));
                FillEllipse(texture, 92f, 26f, 26f, 9f, new Color(1f, 1f, 1f, 0.16f));
                FillEllipse(texture, 136f, 20f, 18f, 7f, new Color(1f, 1f, 1f, 0.12f));
            }

            texture.Apply();
            return CreateSprite(texture, 180f);
        }

        private static Sprite CreateVerticalGradientSprite(Color top, Color bottom)
        {
            Texture2D texture = CreateTexture(64, 64, FilterMode.Bilinear);
            for (int y = 0; y < texture.height; y++)
            {
                float t = texture.height <= 1 ? 0f : (float)y / (texture.height - 1);
                Color color = Color.Lerp(bottom, top, t);
                for (int x = 0; x < texture.width; x++)
                {
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return CreateSprite(texture, 64f);
        }

        private static Sprite CreateGridOverlaySprite(GridOverlayKind kind)
        {
            Texture2D texture = CreateTexture(32, 32);
            Color32 bright = new Color32(255, 255, 255, 255);
            Color32 strong = new Color32(255, 255, 255, 212);
            Color32 soft = new Color32(255, 255, 255, 72);
            switch (kind)
            {
                case GridOverlayKind.Move:
                    FillDiamond(texture, 16, 16, 11, soft);
                    DrawDiamondFrame(texture, 16, 16, 12, bright);
                    DrawDiamondFrame(texture, 16, 16, 8, strong);
                    FillCircle(texture, 16, 16, 2, bright);
                    break;
                case GridOverlayKind.Attack:
                    FillDiamond(texture, 16, 16, 11, soft);
                    DrawDiamondFrame(texture, 16, 16, 12, bright);
                    DrawLine(texture, 16, 5, 16, 27, strong, 1);
                    DrawLine(texture, 5, 16, 27, 16, strong, 1);
                    FillRect(texture, 14, 14, 18, 18, bright);
                    break;
                case GridOverlayKind.Skill:
                    FillCircle(texture, 16, 16, 10, soft);
                    DrawDiamondFrame(texture, 16, 16, 12, bright);
                    DrawLine(texture, 16, 5, 16, 27, strong, 1);
                    DrawLine(texture, 5, 16, 27, 16, strong, 1);
                    DrawLine(texture, 8, 8, 24, 24, strong, 1);
                    DrawLine(texture, 8, 24, 24, 8, strong, 1);
                    FillCircle(texture, 16, 16, 2, bright);
                    break;
                default:
                    FillDiamond(texture, 16, 16, 12, soft);
                    DrawDiamondFrame(texture, 16, 16, 13, bright);
                    DrawDiamondFrame(texture, 16, 16, 10, strong);
                    FillRect(texture, 14, 2, 18, 5, bright);
                    FillRect(texture, 14, 27, 18, 30, bright);
                    FillRect(texture, 2, 14, 5, 18, bright);
                    FillRect(texture, 27, 14, 30, 18, bright);
                    break;
            }

            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateUnitSprite(UnitVisualProfile profile)
        {
            Texture2D texture = CreateTexture(48, 48, FilterMode.Bilinear);
            uint hash = StableHash(profile.UnitId ?? profile.Archetype.ToString());
            int variant = (int)(hash % 3u);

            Color32 outline = new Color32(36, 23, 16, 255);
            Color32 skin = GetSkinColor(profile.Archetype, variant);
            Color32 hair = GetHairColor(profile.Archetype, variant);
            Color32 primary = ToColor32(profile.PrimaryColor);
            Color32 secondary = ToColor32(profile.SecondaryColor);
            Color32 accent = ToColor32(profile.AccentColor);
            Color32 primaryShadow = Darken(primary, 0.22f);
            Color32 cloak = Darken(primary, 0.36f);
            Color32 metal = Blend(secondary, new Color32(225, 220, 206, 255), 0.58f);
            Color32 glow = new Color32(accent.r, accent.g, accent.b, 40);

            FillCircle(texture, 24, 16, 11, glow);
            FillDiamond(texture, 24, 14, 13, new Color32(accent.r, accent.g, accent.b, 22));
            DrawFieldCloak(texture, profile.Role, cloak, outline);
            DrawFieldBody(texture, profile.Role, primary, primaryShadow, accent, metal, outline);
            DrawFieldHead(texture, profile.Role, profile.Archetype, skin, hair, accent, secondary, outline, variant);
            DrawFieldRoleMarks(texture, profile.Role, accent, metal, outline);
            DrawFieldSignature(texture, profile.Archetype, accent, metal, secondary, outline);

            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreatePortraitSprite(UnitVisualProfile profile)
        {
            Texture2D texture = CreateTexture(84, 84, FilterMode.Bilinear);
            uint hash = StableHash(profile.UnitId ?? profile.Archetype.ToString());
            int variant = (int)(hash % 3u);

            Color32 outline = new Color32(34, 21, 16, 255);
            Color32 skin = GetSkinColor(profile.Archetype, variant);
            Color32 hair = GetHairColor(profile.Archetype, variant);
            Color32 primary = ToColor32(profile.PrimaryColor);
            Color32 secondary = ToColor32(profile.SecondaryColor);
            Color32 accent = ToColor32(profile.AccentColor);
            Color32 cloak = Darken(primary, 0.38f);
            Color32 armor = Blend(secondary, new Color32(230, 221, 204, 255), 0.48f);
            Color32 glow = new Color32(accent.r, accent.g, accent.b, 74);

            FillCircle(texture, 42, 48, 24, glow);
            FillDiamond(texture, 42, 44, 28, new Color32(accent.r, accent.g, accent.b, 28));
            DrawEllipseRing(texture, 42f, 42f, 28f, 16f, new Color(1f, 1f, 1f, 0.12f), 0.08f);
            DrawPortraitProp(texture, profile.Role, accent, outline);
            DrawPortraitShoulders(texture, profile.Role, primary, cloak, armor, outline);
            DrawPortraitHead(texture, profile.Role, profile.Archetype, skin, hair, accent, secondary, outline, variant);
            DrawPortraitTrim(texture, profile.Role, accent, armor, outline);
            DrawPortraitSignature(texture, profile.Archetype, accent, armor, secondary, outline);

            texture.Apply();
            return CreateSprite(texture, 42f);
        }

        private static void DrawFieldCloak(Texture2D texture, UnitRole role, Color cloak, Color outline)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    FillRect(texture, 11, 10, 36, 24, cloak);
                    FillTriangle(texture, new Vector2Int(11, 24), new Vector2Int(5, 18), new Vector2Int(10, 9), cloak);
                    FillTriangle(texture, new Vector2Int(36, 24), new Vector2Int(42, 18), new Vector2Int(37, 9), cloak);
                    FillTriangle(texture, new Vector2Int(16, 10), new Vector2Int(11, 0), new Vector2Int(22, 10), cloak);
                    FillTriangle(texture, new Vector2Int(31, 10), new Vector2Int(26, 10), new Vector2Int(36, 0), cloak);
                    StrokeRect(texture, 11, 10, 36, 24, outline);
                    break;
                case UnitRole.Guardian:
                    FillRect(texture, 9, 10, 38, 23, cloak);
                    FillRect(texture, 5, 18, 14, 28, cloak);
                    FillRect(texture, 33, 18, 42, 28, cloak);
                    FillTriangle(texture, new Vector2Int(16, 10), new Vector2Int(12, 0), new Vector2Int(21, 10), cloak);
                    FillTriangle(texture, new Vector2Int(31, 10), new Vector2Int(27, 10), new Vector2Int(36, 0), cloak);
                    StrokeRect(texture, 9, 10, 38, 23, outline);
                    break;
                case UnitRole.Ranger:
                    FillRect(texture, 13, 11, 34, 23, cloak);
                    FillTriangle(texture, new Vector2Int(13, 23), new Vector2Int(8, 17), new Vector2Int(12, 10), cloak);
                    FillTriangle(texture, new Vector2Int(34, 20), new Vector2Int(40, 7), new Vector2Int(34, 9), cloak);
                    FillTriangle(texture, new Vector2Int(17, 11), new Vector2Int(14, 1), new Vector2Int(24, 11), cloak);
                    StrokeRect(texture, 13, 11, 34, 23, outline);
                    break;
                case UnitRole.Scout:
                    FillRect(texture, 14, 12, 31, 22, cloak);
                    FillTriangle(texture, new Vector2Int(14, 22), new Vector2Int(7, 12), new Vector2Int(13, 9), cloak);
                    FillTriangle(texture, new Vector2Int(31, 20), new Vector2Int(38, 8), new Vector2Int(30, 10), cloak);
                    break;
                case UnitRole.Raider:
                    FillRect(texture, 10, 11, 36, 23, cloak);
                    FillTriangle(texture, new Vector2Int(10, 23), new Vector2Int(4, 8), new Vector2Int(12, 12), cloak);
                    FillTriangle(texture, new Vector2Int(36, 23), new Vector2Int(42, 14), new Vector2Int(34, 10), cloak);
                    FillTriangle(texture, new Vector2Int(18, 11), new Vector2Int(10, 0), new Vector2Int(25, 11), cloak);
                    StrokeRect(texture, 10, 11, 36, 23, outline);
                    break;
            }
        }

        private static void DrawFieldBody(Texture2D texture, UnitRole role, Color primary, Color primaryShadow, Color accent, Color metal, Color outline)
        {
            FillRect(texture, 16, 0, 19, 10, primaryShadow);
            FillRect(texture, 28, 0, 31, 10, primaryShadow);
            FillRect(texture, 15, 10, 20, 21, primary);
            FillRect(texture, 27, 10, 32, 21, primary);
            FillRect(texture, 13, 22, 34, 37, primary);
            FillRect(texture, 15, 24, 32, 36, primaryShadow);

            switch (role)
            {
                case UnitRole.Commander:
                    FillRect(texture, 11, 22, 36, 39, primary);
                    FillRect(texture, 15, 24, 32, 26, accent);
                    FillRect(texture, 21, 16, 26, 31, metal);
                    DrawLine(texture, 23, 12, 23, 30, accent, 1);
                    break;
                case UnitRole.Guardian:
                    FillRect(texture, 10, 22, 37, 39, primary);
                    FillRect(texture, 8, 25, 15, 33, metal);
                    FillRect(texture, 32, 25, 39, 33, metal);
                    FillRect(texture, 18, 27, 29, 35, Blend(metal, accent, 0.24f));
                    DrawLine(texture, 14, 35, 33, 35, accent, 1);
                    break;
                case UnitRole.Ranger:
                    FillRect(texture, 14, 22, 31, 35, primary);
                    FillRect(texture, 31, 21, 35, 36, Darken(accent, 0.28f));
                    DrawLine(texture, 15, 33, 31, 26, accent, 1);
                    DrawLine(texture, 20, 22, 28, 38, metal, 1);
                    break;
                case UnitRole.Scout:
                    FillRect(texture, 14, 22, 30, 35, primary);
                    DrawLine(texture, 13, 31, 32, 22, accent, 1);
                    DrawLine(texture, 17, 22, 22, 12, accent, 1);
                    DrawLine(texture, 28, 23, 24, 13, metal, 1);
                    break;
                case UnitRole.Raider:
                    FillRect(texture, 11, 22, 35, 35, primary);
                    FillRect(texture, 12, 20, 35, 22, accent);
                    FillRect(texture, 8, 24, 12, 30, metal);
                    DrawLine(texture, 30, 34, 39, 22, metal, 1);
                    break;
            }

            StrokeRect(texture, 13, 22, 34, 37, outline);
            StrokeRect(texture, 15, 10, 20, 21, outline);
            StrokeRect(texture, 27, 10, 32, 21, outline);
        }

        private static void DrawFieldHead(Texture2D texture, UnitRole role, UnitVisualArchetype archetype, Color skin, Color hair, Color accent, Color secondary, Color outline, int variant)
        {
            FillRect(texture, 17, 36, 30, 45, skin);
            StrokeRect(texture, 17, 36, 30, 45, outline);
            FillRect(texture, 16, 43, 31, 47, hair);
            FillRect(texture, 16, 40, 19, 43, hair);
            FillRect(texture, 28, 40, 31, 43, hair);
            FillRect(texture, 20, 41, 21, 41, outline);
            FillRect(texture, 26, 41, 27, 41, outline);

            if (variant == 1)
            {
                FillRect(texture, 19, 36, 28, 37, secondary);
            }
            else if (variant == 2)
            {
                DrawLine(texture, 22, 38, 25, 35, outline, 1);
            }

            switch (archetype)
            {
                case UnitVisualArchetype.LiuBei:
                    FillRect(texture, 19, 45, 28, 48, accent);
                    FillTriangle(texture, new Vector2Int(21, 48), new Vector2Int(23, 52), new Vector2Int(25, 48), accent);
                    DrawLine(texture, 19, 37, 21, 34, outline, 1);
                    DrawLine(texture, 28, 37, 26, 34, outline, 1);
                    break;
                case UnitVisualArchetype.GuanYu:
                    FillRect(texture, 18, 45, 29, 48, accent);
                    FillRect(texture, 21, 34, 26, 45, outline);
                    FillRect(texture, 22, 30, 25, 34, outline);
                    DrawLine(texture, 21, 45, 19, 39, outline, 1);
                    DrawLine(texture, 26, 45, 28, 39, outline, 1);
                    break;
                case UnitVisualArchetype.ZhangFei:
                    FillRect(texture, 18, 45, 29, 49, hair);
                    DrawLine(texture, 18, 36, 20, 39, outline, 1);
                    DrawLine(texture, 29, 36, 27, 39, outline, 1);
                    DrawLine(texture, 21, 46, 18, 39, outline, 1);
                    DrawLine(texture, 26, 46, 29, 39, outline, 1);
                    DrawLine(texture, 20, 32, 18, 28, outline, 1);
                    DrawLine(texture, 27, 32, 29, 28, outline, 1);
                    break;
                case UnitVisualArchetype.HuangZhong:
                    FillRect(texture, 18, 45, 29, 48, secondary);
                    DrawLine(texture, 20, 44, 18, 38, secondary, 1);
                    DrawLine(texture, 27, 44, 29, 38, secondary, 1);
                    DrawLine(texture, 19, 36, 19, 31, secondary, 1);
                    DrawLine(texture, 28, 36, 28, 31, secondary, 1);
                    break;
                case UnitVisualArchetype.YellowTurban:
                case UnitVisualArchetype.YellowTurbanBoss:
                    FillRect(texture, 15, 44, 32, 48, accent);
                    FillTriangle(texture, new Vector2Int(18, 48), new Vector2Int(24, 52), new Vector2Int(30, 48), accent);
                    if (archetype == UnitVisualArchetype.YellowTurbanBoss)
                    {
                        FillRect(texture, 21, 48, 26, 51, secondary);
                        DrawLine(texture, 23, 51, 23, 54, secondary, 1);
                    }

                    break;
                case UnitVisualArchetype.WeiCommander:
                    FillRect(texture, 15, 44, 32, 47, secondary);
                    FillRect(texture, 19, 47, 28, 49, accent);
                    FillRect(texture, 22, 49, 25, 53, accent);
                    break;
                case UnitVisualArchetype.WeiGuardian:
                    FillRect(texture, 14, 43, 33, 47, secondary);
                    FillRect(texture, 17, 47, 30, 49, accent);
                    FillRect(texture, 14, 40, 17, 43, secondary);
                    FillRect(texture, 30, 40, 33, 43, secondary);
                    break;
                case UnitVisualArchetype.WeiRanger:
                    FillRect(texture, 15, 44, 32, 47, secondary);
                    DrawLine(texture, 17, 47, 28, 49, accent, 1);
                    DrawLine(texture, 29, 45, 32, 50, accent, 1);
                    break;
                case UnitVisualArchetype.WeiRaider:
                    FillRect(texture, 15, 43, 32, 47, secondary);
                    FillRect(texture, 19, 47, 28, 49, accent);
                    DrawLine(texture, 15, 43, 12, 48, secondary, 1);
                    break;
                case UnitVisualArchetype.BossCommander:
                    FillRect(texture, 16, 46, 31, 49, accent);
                    FillRect(texture, 22, 49, 25, 54, accent);
                    FillRect(texture, 17, 44, 19, 49, secondary);
                    FillRect(texture, 28, 44, 30, 49, secondary);
                    DrawLine(texture, 18, 49, 13, 54, accent, 1);
                    DrawLine(texture, 29, 49, 34, 54, accent, 1);
                    break;
                default:
                    if (role == UnitRole.Commander)
                    {
                        FillRect(texture, 18, 46, 29, 49, accent);
                    }

                    break;
            }
        }

        private static void DrawFieldRoleMarks(Texture2D texture, UnitRole role, Color accent, Color metal, Color outline)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    DrawLine(texture, 9, 18, 9, 36, metal, 1);
                    FillTriangle(texture, new Vector2Int(10, 33), new Vector2Int(17, 30), new Vector2Int(10, 26), accent);
                    StrokeRect(texture, 8, 25, 10, 35, outline);
                    break;
                case UnitRole.Guardian:
                    FillRect(texture, 35, 15, 41, 28, metal);
                    FillRect(texture, 36, 16, 40, 26, Darken(metal, 0.2f));
                    StrokeRect(texture, 35, 15, 41, 28, outline);
                    break;
                case UnitRole.Ranger:
                    DrawLine(texture, 36, 12, 41, 25, metal, 1);
                    DrawLine(texture, 31, 16, 40, 16, accent, 1);
                    DrawLine(texture, 31, 16, 37, 24, accent, 1);
                    break;
                case UnitRole.Scout:
                    DrawLine(texture, 11, 14, 16, 28, metal, 1);
                    DrawLine(texture, 35, 14, 30, 28, metal, 1);
                    break;
                case UnitRole.Raider:
                    DrawLine(texture, 8, 12, 40, 38, metal, 1);
                    DrawLine(texture, 11, 10, 43, 34, metal, 1);
                    break;
            }
        }

        private static void DrawFieldSignature(Texture2D texture, UnitVisualArchetype archetype, Color accent, Color metal, Color secondary, Color outline)
        {
            switch (archetype)
            {
                case UnitVisualArchetype.LiuBei:
                    DrawLine(texture, 20, 24, 24, 18, metal, 1);
                    DrawLine(texture, 27, 24, 23, 18, metal, 1);
                    break;
                case UnitVisualArchetype.GuanYu:
                    FillRect(texture, 21, 18, 25, 22, secondary);
                    DrawLine(texture, 23, 18, 23, 13, secondary, 1);
                    break;
                case UnitVisualArchetype.ZhangFei:
                    FillRect(texture, 18, 18, 29, 20, accent);
                    break;
                case UnitVisualArchetype.HuangZhong:
                    DrawLine(texture, 18, 24, 29, 24, metal, 1);
                    DrawLine(texture, 20, 26, 27, 26, secondary, 1);
                    break;
                case UnitVisualArchetype.YellowTurban:
                case UnitVisualArchetype.YellowTurbanBoss:
                    FillRect(texture, 18, 24, 29, 26, accent);
                    if (archetype == UnitVisualArchetype.YellowTurbanBoss)
                    {
                        DrawLine(texture, 23, 24, 23, 18, outline, 1);
                    }

                    break;
                case UnitVisualArchetype.BossCommander:
                    FillRect(texture, 19, 23, 28, 25, metal);
                    FillRect(texture, 22, 19, 25, 23, accent);
                    break;
            }
        }

        private static void DrawPortraitProp(Texture2D texture, UnitRole role, Color accent, Color outline)
        {
            Color weaponTint = Blend(ToColor32(accent), new Color32(240, 234, 221, 255), 0.45f);
            switch (role)
            {
                case UnitRole.Commander:
                    DrawLine(texture, 16, 18, 16, 68, weaponTint, 1);
                    FillTriangle(texture, new Vector2Int(17, 50), new Vector2Int(30, 45), new Vector2Int(17, 38), accent);
                    StrokeRect(texture, 15, 40, 18, 53, outline);
                    break;
                case UnitRole.Guardian:
                    DrawLine(texture, 69, 14, 54, 66, weaponTint, 1);
                    FillRect(texture, 54, 42, 71, 60, accent);
                    FillRect(texture, 57, 45, 68, 57, Darken(accent, 0.2f));
                    StrokeRect(texture, 54, 42, 71, 60, outline);
                    break;
                case UnitRole.Ranger:
                    DrawLine(texture, 67, 18, 67, 65, accent, 1);
                    DrawLine(texture, 55, 18, 55, 65, accent, 1);
                    DrawLine(texture, 55, 18, 67, 41, weaponTint, 1);
                    DrawLine(texture, 55, 65, 67, 41, weaponTint, 1);
                    DrawLine(texture, 60, 26, 60, 54, new Color32(240, 240, 240, 180), 1);
                    break;
                case UnitRole.Scout:
                    DrawLine(texture, 16, 18, 28, 56, weaponTint, 1);
                    DrawLine(texture, 68, 18, 55, 56, weaponTint, 1);
                    break;
                case UnitRole.Raider:
                    DrawLine(texture, 10, 16, 74, 62, weaponTint, 1);
                    DrawLine(texture, 14, 12, 77, 58, weaponTint, 1);
                    break;
            }
        }

        private static void DrawPortraitShoulders(Texture2D texture, UnitRole role, Color primary, Color cloak, Color armor, Color outline)
        {
            FillRect(texture, 18, 11, 65, 31, cloak);
            FillRect(texture, 22, 19, 61, 40, primary);
            FillRect(texture, 24, 31, 59, 46, Darken(primary, 0.18f));

            switch (role)
            {
                case UnitRole.Commander:
                    FillRect(texture, 16, 23, 68, 37, primary);
                    FillRect(texture, 20, 33, 58, 37, Blend(primary, armor, 0.25f));
                    DrawLine(texture, 42, 18, 42, 46, armor, 1);
                    break;
                case UnitRole.Guardian:
                    FillRect(texture, 13, 21, 70, 40, primary);
                    FillRect(texture, 11, 26, 24, 43, armor);
                    FillRect(texture, 59, 26, 72, 43, armor);
                    FillRect(texture, 31, 29, 52, 40, Blend(armor, primary, 0.34f));
                    break;
                case UnitRole.Ranger:
                    FillRect(texture, 22, 22, 59, 35, primary);
                    FillRect(texture, 56, 20, 65, 49, Darken(primary, 0.26f));
                    DrawLine(texture, 29, 35, 54, 21, armor, 1);
                    break;
                case UnitRole.Scout:
                    FillRect(texture, 20, 21, 61, 34, primary);
                    DrawLine(texture, 20, 25, 7, 12, cloak, 1);
                    DrawLine(texture, 60, 25, 76, 14, cloak, 1);
                    break;
                case UnitRole.Raider:
                    FillRect(texture, 18, 21, 64, 34, primary);
                    FillRect(texture, 14, 23, 23, 36, armor);
                    break;
            }

            StrokeRect(texture, 22, 18, 61, 38, outline);
        }

        private static void DrawPortraitHead(Texture2D texture, UnitRole role, UnitVisualArchetype archetype, Color skin, Color hair, Color accent, Color secondary, Color outline, int variant)
        {
            FillRect(texture, 28, 32, 55, 60, skin);
            StrokeRect(texture, 28, 32, 55, 60, outline);
            FillRect(texture, 26, 50, 57, 64, hair);
            FillRect(texture, 26, 43, 31, 51, hair);
            FillRect(texture, 52, 43, 57, 51, hair);
            FillRect(texture, 34, 48, 35, 48, outline);
            FillRect(texture, 46, 48, 47, 48, outline);
            DrawLine(texture, 39, 42, 43, 42, new Color32(156, 116, 90, 255), 1);

            if (variant == 1)
            {
                DrawLine(texture, 37, 35, 44, 35, outline, 1);
                DrawLine(texture, 38, 34, 43, 34, outline, 1);
            }
            else if (variant == 2)
            {
                FillRect(texture, 35, 32, 46, 35, secondary);
            }

            switch (archetype)
            {
                case UnitVisualArchetype.LiuBei:
                    FillRect(texture, 34, 63, 49, 68, accent);
                    FillTriangle(texture, new Vector2Int(38, 68), new Vector2Int(42, 76), new Vector2Int(46, 68), accent);
                    DrawLine(texture, 34, 40, 37, 34, outline, 1);
                    DrawLine(texture, 49, 40, 46, 34, outline, 1);
                    break;
                case UnitVisualArchetype.GuanYu:
                    FillRect(texture, 33, 60, 50, 64, accent);
                    FillRect(texture, 38, 32, 44, 60, outline);
                    FillRect(texture, 39, 24, 43, 32, outline);
                    DrawLine(texture, 37, 60, 33, 48, outline, 1);
                    DrawLine(texture, 46, 60, 50, 48, outline, 1);
                    break;
                case UnitVisualArchetype.ZhangFei:
                    FillRect(texture, 33, 60, 50, 65, hair);
                    DrawLine(texture, 31, 42, 35, 45, outline, 1);
                    DrawLine(texture, 52, 42, 48, 45, outline, 1);
                    DrawLine(texture, 35, 33, 31, 27, outline, 1);
                    DrawLine(texture, 48, 33, 52, 27, outline, 1);
                    DrawLine(texture, 36, 60, 33, 49, outline, 1);
                    DrawLine(texture, 47, 60, 50, 49, outline, 1);
                    break;
                case UnitVisualArchetype.HuangZhong:
                    FillRect(texture, 33, 60, 50, 65, secondary);
                    DrawLine(texture, 34, 43, 32, 31, secondary, 1);
                    DrawLine(texture, 49, 43, 51, 31, secondary, 1);
                    DrawLine(texture, 34, 60, 30, 49, secondary, 1);
                    DrawLine(texture, 49, 60, 53, 49, secondary, 1);
                    break;
                case UnitVisualArchetype.YellowTurban:
                case UnitVisualArchetype.YellowTurbanBoss:
                    FillRect(texture, 26, 57, 57, 63, accent);
                    FillTriangle(texture, new Vector2Int(31, 63), new Vector2Int(42, 69), new Vector2Int(54, 63), accent);
                    if (archetype == UnitVisualArchetype.YellowTurbanBoss)
                    {
                        FillRect(texture, 36, 63, 47, 66, secondary);
                        DrawLine(texture, 41, 66, 41, 74, secondary, 1);
                    }

                    break;
                case UnitVisualArchetype.WeiCommander:
                    FillRect(texture, 25, 57, 58, 63, secondary);
                    FillRect(texture, 31, 63, 52, 67, accent);
                    FillRect(texture, 38, 67, 45, 75, accent);
                    break;
                case UnitVisualArchetype.WeiGuardian:
                    FillRect(texture, 25, 56, 58, 63, secondary);
                    FillRect(texture, 32, 63, 51, 67, accent);
                    FillRect(texture, 24, 49, 29, 58, secondary);
                    FillRect(texture, 54, 49, 59, 58, secondary);
                    break;
                case UnitVisualArchetype.WeiRanger:
                    FillRect(texture, 25, 57, 58, 63, secondary);
                    DrawLine(texture, 31, 63, 52, 67, accent, 1);
                    DrawLine(texture, 55, 59, 61, 68, accent, 1);
                    break;
                case UnitVisualArchetype.WeiRaider:
                    FillRect(texture, 25, 56, 58, 62, secondary);
                    FillRect(texture, 31, 62, 52, 66, accent);
                    DrawLine(texture, 26, 62, 19, 69, secondary, 1);
                    break;
                case UnitVisualArchetype.BossCommander:
                    FillRect(texture, 33, 63, 50, 69, accent);
                    FillRect(texture, 39, 69, 44, 77, accent);
                    FillRect(texture, 31, 61, 34, 66, secondary);
                    FillRect(texture, 49, 61, 52, 66, secondary);
                    DrawLine(texture, 33, 68, 24, 78, accent, 1);
                    DrawLine(texture, 50, 68, 59, 78, accent, 1);
                    break;
            }

            if (role == UnitRole.Scout)
            {
                FillRect(texture, 28, 55, 55, 60, hair);
            }
        }

        private static void DrawPortraitTrim(Texture2D texture, UnitRole role, Color accent, Color armor, Color outline)
        {
            switch (role)
            {
                case UnitRole.Commander:
                    FillRect(texture, 32, 20, 51, 24, accent);
                    FillRect(texture, 37, 17, 45, 20, armor);
                    DrawLine(texture, 42, 20, 42, 36, armor, 1);
                    break;
                case UnitRole.Guardian:
                    FillRect(texture, 31, 19, 52, 24, armor);
                    FillRect(texture, 35, 24, 48, 29, accent);
                    FillRect(texture, 28, 28, 55, 31, armor);
                    break;
                case UnitRole.Ranger:
                    DrawLine(texture, 27, 31, 56, 20, accent, 1);
                    DrawLine(texture, 22, 23, 27, 48, armor, 1);
                    break;
                case UnitRole.Scout:
                    DrawLine(texture, 26, 26, 56, 36, accent, 1);
                    DrawLine(texture, 20, 20, 32, 35, armor, 1);
                    break;
                case UnitRole.Raider:
                    FillRect(texture, 30, 22, 53, 25, accent);
                    FillRect(texture, 26, 18, 32, 31, armor);
                    break;
            }

            DrawLine(texture, 32, 20, 52, 20, outline, 1);
        }

        private static void DrawPortraitSignature(Texture2D texture, UnitVisualArchetype archetype, Color accent, Color armor, Color secondary, Color outline)
        {
            switch (archetype)
            {
                case UnitVisualArchetype.LiuBei:
                    DrawLine(texture, 38, 25, 42, 18, armor, 1);
                    DrawLine(texture, 45, 25, 41, 18, armor, 1);
                    break;
                case UnitVisualArchetype.GuanYu:
                    FillRect(texture, 36, 18, 47, 22, accent);
                    DrawLine(texture, 41, 22, 41, 16, armor, 1);
                    break;
                case UnitVisualArchetype.ZhangFei:
                    FillRect(texture, 31, 24, 52, 27, accent);
                    break;
                case UnitVisualArchetype.HuangZhong:
                    DrawLine(texture, 32, 28, 50, 28, secondary, 1);
                    DrawLine(texture, 36, 31, 47, 31, armor, 1);
                    break;
                case UnitVisualArchetype.YellowTurban:
                    FillRect(texture, 34, 22, 49, 25, accent);
                    break;
                case UnitVisualArchetype.YellowTurbanBoss:
                    FillRect(texture, 34, 22, 49, 25, armor);
                    DrawLine(texture, 41, 22, 41, 17, outline, 1);
                    break;
                case UnitVisualArchetype.WeiCommander:
                case UnitVisualArchetype.WeiGuardian:
                case UnitVisualArchetype.WeiRanger:
                case UnitVisualArchetype.WeiRaider:
                    FillRect(texture, 34, 21, 49, 25, armor);
                    break;
                case UnitVisualArchetype.BossCommander:
                    FillRect(texture, 33, 23, 50, 26, armor);
                    FillRect(texture, 38, 18, 45, 23, accent);
                    break;
            }
        }

        private static void FillBaseTile(Texture2D texture, Color32 border, Color32 fillA, Color32 fillB, Color32 accent)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == texture.width - 1 || y == texture.height - 1;
                    if (isBorder)
                    {
                        texture.SetPixel(x, y, border);
                        continue;
                    }

                    float grain = (((x * 13) + (y * 11) + ((x * y) % 7)) % 23) / 22f;
                    bool brushStroke = ((x + (y * 3)) % 7) < 2;
                    Color32 baseColor = grain > 0.56f ? fillB : fillA;
                    if (brushStroke && y > 2 && y < texture.height - 2)
                    {
                        baseColor = Blend(baseColor, accent, 0.18f);
                    }

                    texture.SetPixel(x, y, baseColor);
                }
            }

            FillRect(texture, 2, texture.height - 5, 4, texture.height - 4, accent);
            FillRect(texture, texture.width - 5, 2, texture.width - 3, 3, accent);
            DrawLine(texture, 2, texture.height - 3, texture.width - 3, texture.height - 4, Blend(fillB, border, 0.28f), 1);
            texture.Apply();
        }

        private static Sprite CreatePlainOverlaySprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 grass = new Color32(215, 202, 150, 255);
            DrawLine(texture, 4, 6, 6, 10, grass, 1);
            DrawLine(texture, 7, 5, 8, 9, grass, 1);
            DrawLine(texture, 13, 10, 15, 14, grass, 1);
            DrawLine(texture, 15, 9, 16, 12, grass, 1);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateForestOverlaySprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 canopy = new Color32(57, 84, 38, 255);
            Color32 highlight = new Color32(101, 135, 67, 255);
            FillCircle(texture, 5, 14, 4, canopy);
            FillCircle(texture, 12, 13, 5, canopy);
            FillCircle(texture, 16, 15, 3, canopy);
            FillCircle(texture, 11, 15, 2, highlight);
            FillRect(texture, 7, 4, 9, 9, new Color32(86, 61, 39, 255));
            FillRect(texture, 11, 5, 13, 10, new Color32(86, 61, 39, 255));
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateFortOverlaySprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 wall = new Color32(124, 88, 57, 255);
            Color32 trim = new Color32(207, 176, 96, 255);
            FillRect(texture, 2, 12, 17, 14, wall);
            FillRect(texture, 3, 14, 6, 17, trim);
            FillRect(texture, 8, 14, 11, 17, trim);
            FillRect(texture, 13, 14, 16, 17, trim);
            for (int x = 2; x <= 17; x += 5)
            {
                DrawLine(texture, x, 14, x, 18, wall, 1);
            }

            DrawLine(texture, 2, 12, 17, 12, trim, 1);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateHazardOverlaySprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 ember = new Color32(244, 132, 50, 255);
            Color32 core = new Color32(255, 216, 144, 255);
            DrawLine(texture, 4, 15, 9, 10, ember, 1);
            DrawLine(texture, 6, 14, 8, 11, core, 1);
            DrawLine(texture, 8, 10, 12, 12, ember, 1);
            DrawLine(texture, 12, 12, 16, 7, ember, 1);
            DrawLine(texture, 13, 11, 15, 8, core, 1);
            DrawLine(texture, 11, 5, 13, 3, ember, 1);
            FillCircle(texture, 10, 10, 2, core);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateBlockedOverlaySprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 ridge = new Color32(44, 48, 55, 255);
            Color32 seal = new Color32(190, 197, 206, 255);
            FillRect(texture, 2, 11, 17, 18, ridge);
            DrawLine(texture, 3, 11, 6, 16, Color.white, 1);
            DrawLine(texture, 9, 12, 12, 18, Color.white, 1);
            DrawLine(texture, 4, 4, 15, 15, seal, 1);
            DrawLine(texture, 15, 4, 4, 15, seal, 1);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateTreePropSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 canopy = new Color32(43, 72, 35, 255);
            Color32 highlight = new Color32(102, 136, 68, 255);
            Color32 trunk = new Color32(93, 63, 36, 255);
            FillCircle(texture, 10, 13, 5, canopy);
            FillCircle(texture, 6, 11, 3, canopy);
            FillCircle(texture, 14, 11, 3, canopy);
            FillCircle(texture, 11, 14, 2, highlight);
            FillRect(texture, 9, 2, 11, 9, trunk);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateFortPropSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 timber = new Color32(109, 74, 42, 255);
            Color32 banner = new Color32(214, 177, 86, 255);
            DrawLine(texture, 6, 4, 6, 16, timber, 1);
            DrawLine(texture, 10, 4, 10, 16, timber, 1);
            DrawLine(texture, 14, 4, 14, 16, timber, 1);
            FillRect(texture, 10, 10, 15, 14, banner);
            DrawLine(texture, 9, 16, 15, 16, timber, 1);
            DrawLine(texture, 6, 7, 10, 7, banner, 1);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateFlamePropSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 flame = new Color32(250, 168, 73, 255);
            Color32 core = new Color32(255, 229, 166, 255);
            FillTriangle(texture, new Vector2Int(10, 18), new Vector2Int(5, 5), new Vector2Int(13, 8), flame);
            FillTriangle(texture, new Vector2Int(12, 16), new Vector2Int(9, 8), new Vector2Int(15, 10), core);
            DrawLine(texture, 7, 9, 10, 16, core, 1);
            FillRect(texture, 8, 2, 12, 4, new Color32(96, 54, 36, 255));
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateBoulderPropSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 rock = new Color32(88, 92, 99, 255);
            Color32 edge = new Color32(124, 131, 141, 255);
            FillCircle(texture, 9, 10, 6, rock);
            FillCircle(texture, 13, 11, 4, rock);
            DrawLine(texture, 6, 12, 10, 15, edge, 1);
            DrawLine(texture, 11, 9, 15, 12, edge, 1);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static string GetTerrainResourceKey(TerrainType terrainType, bool blocked)
        {
            if (blocked)
            {
                return "blocked";
            }

            switch (terrainType)
            {
                case TerrainType.Forest:
                    return "forest";
                case TerrainType.Fort:
                    return "fort";
                case TerrainType.Hazard:
                    return "hazard";
                default:
                    return "plain";
            }
        }

        private static Color GetPaletteColor(string paletteId, string channel)
        {
            switch (string.IsNullOrWhiteSpace(paletteId) ? "frontier-plain" : paletteId)
            {
                case "guangzong-smoke":
                    return GetGuangzongPaletteColor(channel);
                case "changban-river":
                    return GetChangbanPaletteColor(channel);
                case "dingjun-stone":
                    return GetDingjunPaletteColor(channel);
                default:
                    return GetFrontierPaletteColor(channel);
            }
        }

        private static Color GetGuangzongPaletteColor(string channel)
        {
            switch (channel)
            {
                case "forest":
                    return Hex("61543A");
                case "fort":
                    return Hex("6D5843");
                case "hazard":
                    return Hex("6F3D2D");
                case "blocked":
                    return Hex("403532");
                case "overlay-plain":
                    return Hex("E0C68A");
                case "overlay-forest":
                    return Hex("8C7C58");
                case "overlay-fort":
                    return Hex("C8AA7B");
                case "overlay-hazard":
                    return Hex("F0A05D");
                case "overlay-blocked":
                    return Hex("8B837A");
                case "prop-forest":
                    return Hex("7B6A4C");
                case "prop-fort":
                    return Hex("9D7146");
                case "prop-hazard":
                    return Hex("FFCD83");
                case "prop-blocked":
                    return Hex("A39B93");
                default:
                    return Hex("7A654A");
            }
        }

        private static Color GetChangbanPaletteColor(string channel)
        {
            switch (channel)
            {
                case "forest":
                    return Hex("58706F");
                case "fort":
                    return Hex("7B8278");
                case "hazard":
                    return Hex("70574D");
                case "blocked":
                    return Hex("39434B");
                case "overlay-plain":
                    return Hex("C0C6C0");
                case "overlay-forest":
                    return Hex("8CA39B");
                case "overlay-fort":
                    return Hex("D0D2C8");
                case "overlay-hazard":
                    return Hex("D59C7B");
                case "overlay-blocked":
                    return Hex("9DA7AD");
                case "prop-forest":
                    return Hex("7EA19A");
                case "prop-fort":
                    return Hex("A7A89B");
                case "prop-hazard":
                    return Hex("F3D6B6");
                case "prop-blocked":
                    return Hex("A8B2B9");
                default:
                    return Hex("77807C");
            }
        }

        private static Color GetDingjunPaletteColor(string channel)
        {
            switch (channel)
            {
                case "forest":
                    return Hex("596654");
                case "fort":
                    return Hex("827C6D");
                case "hazard":
                    return Hex("6C5A43");
                case "blocked":
                    return Hex("434741");
                case "overlay-plain":
                    return Hex("C8C2AD");
                case "overlay-forest":
                    return Hex("8B9A7A");
                case "overlay-fort":
                    return Hex("D1C5A6");
                case "overlay-hazard":
                    return Hex("D7AA72");
                case "overlay-blocked":
                    return Hex("B0B5AA");
                case "prop-forest":
                    return Hex("7E8A74");
                case "prop-fort":
                    return Hex("A68C61");
                case "prop-hazard":
                    return Hex("EBC98F");
                case "prop-blocked":
                    return Hex("B4B8AD");
                default:
                    return Hex("7E765F");
            }
        }

        private static Color GetFrontierPaletteColor(string channel)
        {
            switch (channel)
            {
                case "forest":
                    return Hex("6D7358");
                case "fort":
                    return Hex("8C7D69");
                case "hazard":
                    return Hex("83594A");
                case "blocked":
                    return Hex("4B4B4A");
                case "overlay-plain":
                    return Hex("D6C28C");
                case "overlay-forest":
                    return Hex("93A07C");
                case "overlay-fort":
                    return Hex("D1BC96");
                case "overlay-hazard":
                    return Hex("DA9A61");
                case "overlay-blocked":
                    return Hex("A2A59D");
                case "prop-forest":
                    return Hex("7A8966");
                case "prop-fort":
                    return Hex("AA875C");
                case "prop-hazard":
                    return Hex("F0C381");
                case "prop-blocked":
                    return Hex("B6BAB4");
                default:
                    return Hex("8E7752");
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

        private static Sprite TryLoadSpriteResource(string path)
        {
            return string.IsNullOrWhiteSpace(path) ? null : Resources.Load<Sprite>(path);
        }

        private static Color32 GetHairColor(UnitVisualArchetype archetype, int variant)
        {
            switch (archetype)
            {
                case UnitVisualArchetype.HuangZhong:
                    return variant == 1 ? new Color32(202, 198, 187, 255) : new Color32(231, 224, 206, 255);
                case UnitVisualArchetype.GuanYu:
                case UnitVisualArchetype.ZhangFei:
                case UnitVisualArchetype.BossCommander:
                    return new Color32(46, 32, 25, 255);
                case UnitVisualArchetype.YellowTurban:
                case UnitVisualArchetype.YellowTurbanBoss:
                    return new Color32(78, 52, 30, 255);
                default:
                    switch (variant)
                    {
                        case 1:
                            return new Color32(52, 36, 28, 255);
                        case 2:
                            return new Color32(76, 52, 35, 255);
                        default:
                            return new Color32(60, 44, 31, 255);
                    }
            }
        }

        private static Color32 GetSkinColor(UnitVisualArchetype archetype, int variant)
        {
            switch (archetype)
            {
                case UnitVisualArchetype.ZhangFei:
                    return new Color32(203, 168, 131, 255);
                case UnitVisualArchetype.HuangZhong:
                    return new Color32(225, 200, 169, 255);
                case UnitVisualArchetype.YellowTurban:
                case UnitVisualArchetype.YellowTurbanBoss:
                    return new Color32(214, 179, 135, 255);
                default:
                    return variant == 2
                        ? new Color32(228, 198, 162, 255)
                        : new Color32(236, 205, 171, 255);
            }
        }

        private static Texture2D CreateTexture(int width, int height)
        {
            return CreateTexture(width, height, FilterMode.Point);
        }

        private static Texture2D CreateTexture(int width, int height, FilterMode filterMode)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = filterMode;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color clear = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = new Color[width * height];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = clear;
            }

            texture.SetPixels(pixels);
            return texture;
        }

        private static Sprite CreateSprite(Texture2D texture, float pixelsPerUnit)
        {
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private static void FillRect(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
                    {
                        continue;
                    }

                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void StrokeRect(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                PlotSlashPixel(texture, x, yMin, color);
                PlotSlashPixel(texture, x, yMax, color);
            }

            for (int y = yMin; y <= yMax; y++)
            {
                PlotSlashPixel(texture, xMin, y, color);
                PlotSlashPixel(texture, xMax, y, color);
            }
        }

        private static void FillCircle(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            int squaredRadius = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    int deltaX = x - centerX;
                    int deltaY = y - centerY;
                    if ((deltaX * deltaX) + (deltaY * deltaY) > squaredRadius)
                    {
                        continue;
                    }

                    PlotSlashPixel(texture, x, y, color);
                }
            }
        }

        private static void FillDiamond(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    int distance = Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY);
                    if (distance <= radius)
                    {
                        PlotSlashPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static void FillEllipse(Texture2D texture, float centerX, float centerY, float radiusX, float radiusY, Color color)
        {
            float inverseRadiusX = 1f / Mathf.Max(0.01f, radiusX);
            float inverseRadiusY = 1f / Mathf.Max(0.01f, radiusY);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float normalizedX = (x - centerX) * inverseRadiusX;
                    float normalizedY = (y - centerY) * inverseRadiusY;
                    if ((normalizedX * normalizedX) + (normalizedY * normalizedY) <= 1f)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void DrawEllipseRing(Texture2D texture, float centerX, float centerY, float radiusX, float radiusY, Color color, float thickness)
        {
            float inverseRadiusX = 1f / Mathf.Max(0.01f, radiusX);
            float inverseRadiusY = 1f / Mathf.Max(0.01f, radiusY);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float normalizedX = (x - centerX) * inverseRadiusX;
                    float normalizedY = (y - centerY) * inverseRadiusY;
                    float distance = (normalizedX * normalizedX) + (normalizedY * normalizedY);
                    if (distance >= 1f - thickness && distance <= 1f + thickness)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void FillTriangle(Texture2D texture, Vector2Int a, Vector2Int b, Vector2Int c, Color color)
        {
            int xMin = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            int xMax = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            int yMin = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            int yMax = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    if (PointInTriangle(new Vector2(x + 0.5f, y + 0.5f), a, b, c))
                    {
                        PlotSlashPixel(texture, x, y, color);
                    }
                }
            }
        }

        private static bool PointInTriangle(Vector2 point, Vector2Int a, Vector2Int b, Vector2Int c)
        {
            float denominator = ((b.y - c.y) * (a.x - c.x)) + ((c.x - b.x) * (a.y - c.y));
            if (Mathf.Approximately(denominator, 0f))
            {
                return false;
            }

            float alpha = (((b.y - c.y) * (point.x - c.x)) + ((c.x - b.x) * (point.y - c.y))) / denominator;
            float beta = (((c.y - a.y) * (point.x - c.x)) + ((a.x - c.x) * (point.y - c.y))) / denominator;
            float gamma = 1f - alpha - beta;
            return alpha >= 0f && beta >= 0f && gamma >= 0f;
        }

        private static void DrawDiamondFrame(Texture2D texture, int centerX, int centerY, int radius, Color color)
        {
            for (int offset = -radius; offset <= radius; offset++)
            {
                int mirrored = radius - Mathf.Abs(offset);
                PlotSlashPixel(texture, centerX + offset, centerY + mirrored, color);
                PlotSlashPixel(texture, centerX + offset, centerY - mirrored, color);
            }
        }

        private static void PlotSlashPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x < 0 || x >= texture.width || y < 0 || y >= texture.height)
            {
                return;
            }

            texture.SetPixel(x, y, color);
        }

        private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, Color color, int thickness)
        {
            int deltaX = Mathf.Abs(x1 - x0);
            int stepX = x0 < x1 ? 1 : -1;
            int deltaY = -Mathf.Abs(y1 - y0);
            int stepY = y0 < y1 ? 1 : -1;
            int error = deltaX + deltaY;

            while (true)
            {
                PlotPixel(texture, x0, y0, color, thickness);
                if (x0 == x1 && y0 == y1)
                {
                    break;
                }

                int errorTimesTwo = error * 2;
                if (errorTimesTwo >= deltaY)
                {
                    error += deltaY;
                    x0 += stepX;
                }

                if (errorTimesTwo <= deltaX)
                {
                    error += deltaX;
                    y0 += stepY;
                }
            }
        }

        private static void PlotPixel(Texture2D texture, int x, int y, Color color, int thickness)
        {
            int radius = Mathf.Max(0, thickness - 1);
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (int offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    PlotSlashPixel(texture, x + offsetX, y + offsetY, color);
                }
            }
        }

        private static Color32 Blend(Color32 a, Color32 b, float t)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
                (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
        }

        private static Color32 Darken(Color32 color, float amount)
        {
            return new Color32(
                (byte)Mathf.RoundToInt(color.r * (1f - amount)),
                (byte)Mathf.RoundToInt(color.g * (1f - amount)),
                (byte)Mathf.RoundToInt(color.b * (1f - amount)),
                color.a);
        }

        private static Color32 ToColor32(Color color)
        {
            return (Color32)color;
        }

        private static uint StableHash(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;
            if (string.IsNullOrEmpty(value))
            {
                return hash;
            }

            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }

            return hash;
        }

        private static Font CreateHeadingFont()
        {
            return Resources.Load<Font>("Fonts/SourceHanSerifTC") ??
                   Resources.Load<Font>("Fonts/NotoSerifTC") ??
                   TryCreateDynamicFont(
                       new[]
                       {
                           "Source Han Serif TC",
                           "Noto Serif CJK TC",
                           "Songti TC",
                           "PingFang TC",
                           "Microsoft JhengHei",
                           "Arial Unicode MS",
                           "Arial",
                       },
                       28) ??
                   Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Font CreateBodyFont()
        {
            return Resources.Load<Font>("Fonts/NotoSansTC") ??
                   Resources.Load<Font>("Fonts/BattleDisplay") ??
                   TryCreateDynamicFont(
                       new[]
                       {
                           "Noto Sans CJK TC",
                           "PingFang TC",
                           "Microsoft JhengHei",
                           "Heiti TC",
                           "Arial Unicode MS",
                           "Segoe UI",
                           "Arial",
                       },
                       26) ??
                   Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Font TryCreateDynamicFont(string[] candidates, int size)
        {
            Font dynamicFont = Font.CreateDynamicFontFromOSFont(candidates, size);
            return dynamicFont;
        }
    }
}
