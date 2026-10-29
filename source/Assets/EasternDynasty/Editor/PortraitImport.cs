using UnityEditor;
using UnityEngine;

namespace EasternDynasty.Editor
{
    public sealed class PortraitImport : AssetPostprocessor
    {
        public override uint GetVersion() { return 3; }
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/EasternDynasty/Resources/Portraits/")) return;
            AssetCatalogTools.ApplyProfile((TextureImporter)assetImporter,"portrait");
        }
    }
}
