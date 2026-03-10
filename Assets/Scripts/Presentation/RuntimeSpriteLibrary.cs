using System.Collections.Generic;
using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Presentation
{
    public static class RuntimeSpriteLibrary
    {
        private static readonly Dictionary<string, Sprite> unitSprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> weaponSprites = new Dictionary<string, Sprite>();

        private static Sprite whiteSprite;
        private static Sprite tileSprite;
        private static Sprite frameSprite;
        private static Sprite bannerSprite;
        private static Sprite slashSprite;
        private static Sprite ringSprite;
        private static Sprite sparkSprite;
        private static Sprite arrowSprite;
        private static Font defaultFont;

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

        public static Sprite TileSprite => tileSprite ??= CreateTileSprite();

        public static Sprite FrameSprite => frameSprite ??= CreateFrameSprite();

        public static Sprite BannerSprite => bannerSprite ??= CreateBannerSprite();

        public static Sprite SlashSprite => slashSprite ??= CreateSlashSprite();

        public static Sprite RingSprite => ringSprite ??= CreateRingSprite();

        public static Sprite SparkSprite => sparkSprite ??= CreateSparkSprite();

        public static Sprite ArrowSprite => arrowSprite ??= CreateArrowSprite();

        public static Font DefaultFont => defaultFont ??= CreateDefaultFont();

        public static Sprite GetUnitSprite(string unitId, UnitFaction faction)
        {
            string key = faction + ":" + unitId;
            if (unitSprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            sprite = CreateUnitSprite(unitId, faction);
            unitSprites[key] = sprite;
            return sprite;
        }

        public static Sprite GetWeaponSprite(UnitRole role, UnitFaction faction)
        {
            string key = role + ":" + faction;
            if (weaponSprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            sprite = CreateWeaponSprite(role, faction);
            weaponSprites[key] = sprite;
            return sprite;
        }

        private static Sprite CreateTileSprite()
        {
            Texture2D texture = CreateTexture(16, 16);
            Color32 border = new Color32(56, 39, 25, 255);
            Color32 fillA = new Color32(188, 162, 106, 255);
            Color32 fillB = new Color32(171, 146, 96, 255);
            Color32 accent = new Color32(208, 188, 138, 255);

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == 15 || y == 15;
                    if (isBorder)
                    {
                        texture.SetPixel(x, y, border);
                        continue;
                    }

                    bool diagonal = (x + y) % 5 == 0;
                    texture.SetPixel(x, y, diagonal ? fillB : fillA);
                }
            }

            FillRect(texture, 2, 12, 4, 13, accent);
            FillRect(texture, 11, 2, 13, 3, accent);
            texture.Apply();
            return CreateSprite(texture, 16f);
        }

        private static Sprite CreateFrameSprite()
        {
            Texture2D texture = CreateTexture(20, 20);
            Color32 gold = new Color32(240, 209, 96, 255);

            for (int y = 0; y < 20; y++)
            {
                for (int x = 0; x < 20; x++)
                {
                    bool outer = x == 0 || y == 0 || x == 19 || y == 19;
                    bool corner = (x <= 4 || x >= 15) && (y <= 4 || y >= 15);
                    if (outer || corner)
                    {
                        texture.SetPixel(x, y, gold);
                    }
                }
            }

            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateBannerSprite()
        {
            Texture2D texture = CreateTexture(48, 16);
            Color32 fill = new Color32(28, 31, 45, 240);
            Color32 border = new Color32(199, 168, 86, 255);

            FillRect(texture, 0, 0, 47, 15, fill);
            StrokeRect(texture, 0, 0, 47, 15, border);
            FillRect(texture, 2, 2, 45, 13, fill);
            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateSlashSprite()
        {
            Texture2D texture = CreateTexture(32, 32);
            Color32 bright = new Color32(255, 246, 210, 255);
            Color32 warm = new Color32(255, 173, 84, 255);

            for (int x = 4; x < 28; x++)
            {
                int y = x - 2;
                PlotSlashPixel(texture, x, y, bright);
                PlotSlashPixel(texture, x, y - 1, warm);
                PlotSlashPixel(texture, x, y + 1, warm);
            }

            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static Sprite CreateRingSprite()
        {
            Texture2D texture = CreateTexture(32, 32);
            Vector2 center = new Vector2(15.5f, 15.5f);
            Color32 ring = new Color32(255, 255, 255, 255);

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    if (distance >= 10.5f && distance <= 13.2f)
                    {
                        texture.SetPixel(x, y, ring);
                    }
                }
            }

            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateSparkSprite()
        {
            Texture2D texture = CreateTexture(32, 32);
            Color32 bright = new Color32(255, 255, 255, 255);

            DrawLine(texture, 16, 4, 16, 28, bright, 1);
            DrawLine(texture, 4, 16, 28, 16, bright, 1);
            DrawLine(texture, 8, 8, 24, 24, bright, 1);
            DrawLine(texture, 8, 24, 24, 8, bright, 1);
            FillRect(texture, 14, 14, 18, 18, bright);
            texture.Apply();
            return CreateSprite(texture, 24f);
        }

        private static Sprite CreateArrowSprite()
        {
            Texture2D texture = CreateTexture(32, 32);
            Color32 bright = new Color32(255, 255, 255, 255);

            FillRect(texture, 14, 5, 17, 20, bright);
            FillRect(texture, 12, 18, 19, 21, bright);
            DrawLine(texture, 8, 16, 16, 27, bright, 1);
            DrawLine(texture, 24, 16, 16, 27, bright, 1);
            texture.Apply();
            return CreateSprite(texture, 24f);
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

        private static Sprite CreateUnitSprite(string unitId, UnitFaction faction)
        {
            Texture2D texture = CreateTexture(32, 32);
            uint hash = StableHash(unitId);
            int variant = (int)(hash % 3u);

            Color32 outline = new Color32(43, 29, 21, 255);
            Color32 skin = new Color32(233, 202, 166, 255);
            Color32 hair = new Color32(58, 42, 30, 255);
            Color32 primary = GetPaletteColor(
                faction,
                (int)((hash / 7u) % 3u),
                new[]
                {
                    new Color32(63, 98, 173, 255),
                    new Color32(47, 122, 118, 255),
                    new Color32(86, 90, 162, 255),
                },
                new[]
                {
                    new Color32(174, 62, 49, 255),
                    new Color32(145, 88, 34, 255),
                    new Color32(132, 55, 72, 255),
                });
            Color32 accent = GetPaletteColor(
                faction,
                (int)((hash / 19u) % 3u),
                new[]
                {
                    new Color32(233, 209, 99, 255),
                    new Color32(189, 219, 122, 255),
                    new Color32(133, 212, 216, 255),
                },
                new[]
                {
                    new Color32(242, 197, 86, 255),
                    new Color32(233, 162, 78, 255),
                    new Color32(220, 122, 107, 255),
                });

            FillRect(texture, 12, 2, 15, 5, outline);
            FillRect(texture, 17, 2, 20, 5, outline);

            FillRect(texture, 8, 6, 23, 17, primary);
            StrokeRect(texture, 8, 6, 23, 17, outline);

            FillRect(texture, 10, 10, 21, 12, accent);
            FillRect(texture, 7, 8, 8, 15, primary);
            FillRect(texture, 23, 8, 24, 15, primary);
            StrokeRect(texture, 7, 8, 8, 15, outline);
            StrokeRect(texture, 23, 8, 24, 15, outline);

            FillRect(texture, 11, 18, 20, 25, skin);
            StrokeRect(texture, 11, 18, 20, 25, outline);
            FillRect(texture, 10, 24, 21, 28, hair);
            FillRect(texture, 14, 21, 14, 21, outline);
            FillRect(texture, 17, 21, 17, 21, outline);

            DrawUnitVariant(texture, variant, outline, accent, primary, faction);
            texture.Apply();
            return CreateSprite(texture, 20f);
        }

        private static void DrawUnitVariant(Texture2D texture, int variant, Color outline, Color accent, Color primary, UnitFaction faction)
        {
            switch (variant)
            {
                case 0:
                    FillRect(texture, 14, 29, 17, 31, accent);
                    FillRect(texture, 13, 27, 18, 28, accent);
                    StrokeRect(texture, 13, 27, 18, 31, outline);
                    break;
                case 1:
                    FillRect(texture, 9, 26, 12, 27, accent);
                    FillRect(texture, 19, 26, 22, 27, accent);
                    FillRect(texture, 14, 28, 17, 31, accent);
                    StrokeRect(texture, 9, 26, 12, 27, outline);
                    StrokeRect(texture, 19, 26, 22, 27, outline);
                    StrokeRect(texture, 14, 28, 17, 31, outline);
                    break;
                default:
                    FillRect(texture, 8, 20, 10, 23, primary);
                    FillRect(texture, 21, 20, 23, 23, primary);
                    StrokeRect(texture, 8, 20, 10, 23, outline);
                    StrokeRect(texture, 21, 20, 23, 23, outline);
                    break;
            }

            if (faction == UnitFaction.Enemy)
            {
                FillRect(texture, 10, 13, 21, 14, outline);
            }
        }

        private static Texture2D CreateTexture(int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
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
                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void StrokeRect(Texture2D texture, int xMin, int yMin, int xMax, int yMax, Color color)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                texture.SetPixel(x, yMin, color);
                texture.SetPixel(x, yMax, color);
            }

            for (int y = yMin; y <= yMax; y++)
            {
                texture.SetPixel(xMin, y, color);
                texture.SetPixel(xMax, y, color);
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
                    int pixelX = x + offsetX;
                    int pixelY = y + offsetY;
                    if (pixelX < 0 || pixelX >= texture.width || pixelY < 0 || pixelY >= texture.height)
                    {
                        continue;
                    }

                    texture.SetPixel(pixelX, pixelY, color);
                }
            }
        }

        private static Color32 GetPaletteColor(UnitFaction faction, int index, Color32[] playerPalette, Color32[] enemyPalette)
        {
            Color32[] palette = faction == UnitFaction.Player ? playerPalette : enemyPalette;
            return palette[index % palette.Length];
        }

        private static uint StableHash(string value)
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;

            uint hash = offset;
            for (int index = 0; index < value.Length; index++)
            {
                hash ^= value[index];
                hash *= prime;
            }

            return hash;
        }

        private static Font CreateDefaultFont()
        {
            Font dynamicFont = Font.CreateDynamicFontFromOSFont(
                new[]
                {
                    "PingFang TC",
                    "Noto Sans CJK TC",
                    "Heiti TC",
                    "Arial Unicode MS",
                    "Microsoft JhengHei",
                    "Segoe UI",
                    "Arial",
                },
                28);

            return dynamicFont != null ? dynamicFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
    }
}
