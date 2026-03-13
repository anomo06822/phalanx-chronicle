#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PhalanxChronicle.Editor
{
    public sealed class ItemIconImportPostprocessor : AssetPostprocessor
    {
        private const string UiItemsRoot = "Assets/Art/UI/Items/";
        private const string ResourceItemsRoot = "Assets/Resources/ItemIcons/";
        private const string SourceItemsRoot = "Assets/ArtSource/Items/";

        private void OnPreprocessTexture()
        {
            TextureImporter importer = assetImporter as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (assetPath.StartsWith(UiItemsRoot) || assetPath.StartsWith(ResourceItemsRoot) || assetPath.StartsWith(SourceItemsRoot))
            {
                ConfigureItemIcon(importer);
            }
        }

        private static void ConfigureItemIcon(TextureImporter importer)
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
