#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DevKombat.Editor
{
    /// <summary>
    /// Configura automaticamente os assets extraídos para Assets/Resources/Kombat:
    /// sprites pixelados com o pivô nos pés para lutadores, centro para efeitos e
    /// música em streaming.
    /// </summary>
    public class KombatAssetImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/Kombat/";
        private const float FighterPixelsPerUnit = 28f;

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root))
            {
                return;
            }

            TextureImporter importer = (TextureImporter)assetImporter;
            bool photo = assetPath.Contains("/Stages/") || assetPath.Contains("/FX/portal/") ||
                         assetPath.EndsWith("logo_wide.png") || assetPath.EndsWith("fatality_big.png");
            bool world = assetPath.Contains("/Fighters/") || assetPath.Contains("/FX/");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = world ? FighterPixelsPerUnit : 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = photo ? FilterMode.Bilinear : FilterMode.Point;
            importer.textureCompression = photo ? TextureImporterCompression.Compressed : TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = assetPath.Contains("/Fighters/")
                ? (int)SpriteAlignment.BottomCenter
                : (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Root))
            {
                return;
            }

            AudioImporter importer = (AudioImporter)assetImporter;
            if (assetPath.Contains("/Music"))
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                importer.defaultSampleSettings = settings;
            }
        }
    }
}
#endif
