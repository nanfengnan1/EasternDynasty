using UnityEditor;

namespace EasternDynasty.Editor
{
    public sealed class YouzhouTextureImport : AssetPostprocessor
    {
        public override uint GetVersion() { return 4; }
        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/EasternDynasty/Resources/YouzhouUI/"))
            { AssetCatalogTools.ApplyProfile((TextureImporter)assetImporter,"ui-sprite"); return; }
            if (assetPath.StartsWith("Assets/EasternDynasty/Resources/YouzhouReferences/"))
            { AssetCatalogTools.ApplyProfile((TextureImporter)assetImporter,"map-reference"); return; }
            if (!assetPath.StartsWith("Assets/EasternDynasty/Resources/Youzhou/")) return;
            AssetCatalogTools.ApplyProfile((TextureImporter)assetImporter,"map-tile");
        }
    }
}
