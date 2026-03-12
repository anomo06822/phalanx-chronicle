#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PhalanxChronicle.Editor
{
    public sealed class CharacterArtImportPostprocessor : AssetPostprocessor
    {
        private const string PortraitsRoot = "Assets/Art/Characters/Portraits/";
        private const string BattleRoot = "Assets/Art/Characters/Battle/";
        private const string WeaponsRoot = "Assets/Art/UI/Weapons/";
        private const string TerrainRoot = "Assets/Resources/Terrain/";

        private void OnPreprocessTexture()
        {
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (assetPath.StartsWith(PortraitsRoot))
            {
                ConfigurePortrait(importer);
                return;
            }

            if (assetPath.StartsWith(BattleRoot))
            {
                ConfigureBattle(importer);
                return;
            }

            if (assetPath.StartsWith(TerrainRoot))
            {
                ConfigureTerrain(importer);
                return;
            }

            if (assetPath.StartsWith(WeaponsRoot))
            {
                ConfigureWeapon(importer);
            }
        }

        private static void ConfigurePortrait(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
        }

        private static void ConfigureBattle(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 256;
        }

        private static void ConfigureTerrain(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 128;
        }

        private static void ConfigureWeapon(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 256;
        }
    }
}
#endif
