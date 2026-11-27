using UnityEditor;

namespace Propulsor.EditorTools
{
    /// <summary>
    /// Configura a importação da arte gerada (Resources/Art): sprite, sem redimensionar para
    /// potência de 2 (preserva os retângulos dos quadros), sem mipmaps, transparência correta
    /// e compressão sem perda visível. Roda automaticamente ao importar.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = UnityEngine.FilterMode.Bilinear;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.maxTextureSize = 4096;
        }
    }
}
