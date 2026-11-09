using UnityEditor;

namespace FlowerEscape.EditorTools
{
    /// <summary>
    /// Ảnh PNG kéo vào Assets/FlowerEscape/Art được tự đặt kiểu Sprite (2D and UI),
    /// tắt mipmap, giữ alpha. Chỉ áp dụng lần import đầu (sau đó bạn tự chỉnh trong Inspector).
    /// </summary>
    public class FlowerEscapeSpriteImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/FlowerEscape/Art")) return;
            if (!assetImporter.importSettingsMissing) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.maxTextureSize = 2048;
            ti.spritePixelsPerUnit = 100;
        }
    }
}
