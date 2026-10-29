using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace EasternDynasty
{
    public static class AssetCatalogTools
    {
        public static void ApplyProfile(TextureImporter importer, string kind)
        {
            bool map = kind == "map-tile";
            bool ui = kind == "ui-sprite";
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = map || ui;
            importer.isReadable = map;
            importer.mipmapEnabled = map;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = map ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.maxTextureSize = ui ? 8192 : 1024;
            importer.textureCompression = map || ui ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        }

        [MenuItem("Eastern Dynasty/Assets/Validate catalog")]
        public static void ValidateForBuild()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (Directory.Exists(Path.Combine(root, "AssetInbox/.asset-transaction"))) throw new InvalidOperationException("Asset transaction unfinished. Close Unity and run Tools/Import-Asset.ps1 -Recover before building.");
            string path = AssetCatalogRules.AssetRoot + "asset-manifest.json";
            var entries = AssetCatalogRules.Validate(JsonUtility.FromJson<AssetManifest>(File.ReadAllText(Path.Combine(root, path))));
            string[] required = { "map.youzhou.grain", "map.youzhou.pasture", "map.youzhou.stone", "map.youzhou.iron", "map.youzhou.city", "map.youzhou.mountain", "map.youzhou.forest", "portrait.han_scout", "portrait.han_officer", "portrait.huangfu_song" };
            foreach (string id in required)
            {
                AssetRecord entry;
                if (!entries.TryGetValue(id, out entry) || !entry.required || entry.status != "ready") throw new InvalidOperationException("Prototype baseline asset must be registered ready and required: " + id);
            }
            AssetRecord s1Reference;
            if(!entries.TryGetValue("reference.youzhou.resources",out s1Reference)||s1Reference.status!="ready")
                throw new InvalidOperationException("S1 map reference texture is required for this build.");
            var resourceFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string asset in AssetDatabase.GetAllAssetPaths())
            {
                int marker = asset.LastIndexOf("/Resources/", StringComparison.Ordinal);
                if (marker < 0 || AssetDatabase.IsValidFolder(asset)) continue;
                string key = asset.Substring(marker + 11); key = key.Substring(0, key.Length - Path.GetExtension(key).Length);
                if (resourceFiles.ContainsKey(key)) throw new InvalidOperationException("Ambiguous global Resources path: " + key + " (" + resourceFiles[key] + ", " + asset + ")");
                resourceFiles.Add(key, asset);
            }
            int ready = 0, missing = 0;
            var registered = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries.Values)
            {
                string assetPath = AssetCatalogRules.AssetRoot + entry.resourcePath + ".png";
                registered.Add(assetPath);
                string absolute = Path.Combine(root, assetPath);
                if (entry.status == "missing")
                {
                    missing++;
                    if (File.Exists(absolute)) throw new InvalidOperationException("Unregistered PNG for missing asset: " + entry.id);
                    continue;
                }
                ready++;
                if (!File.Exists(absolute) || !File.Exists(absolute + ".meta")) throw new InvalidOperationException("Missing PNG/.meta: " + entry.id);
                RejectReparsePath(absolute);
                string hash;
                using (var sha = SHA256.Create()) using (var file = File.OpenRead(absolute)) hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "").ToLowerInvariant();
                if (hash != entry.sha256) throw new InvalidOperationException("Source PNG SHA256 mismatch: " + entry.id);
                using (var stream = File.OpenRead(absolute))
                {
                    var header = new byte[24];
                    if (stream.Read(header, 0, header.Length) != header.Length || header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71 || ReadInt(header, 16) != entry.width || ReadInt(header, 20) != entry.height) throw new InvalidOperationException("Source PNG dimensions/signature mismatch: " + entry.id);
                }
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Texture has not been imported: " + entry.id);
                AssertProfile(importer, entry);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (texture == null || texture.width == 0 || Math.Abs(texture.width / (float)texture.height - entry.width / (float)entry.height) > .01f) throw new InvalidOperationException("Imported texture missing or aspect changed: " + entry.id);
                string resourceAsset;
                if (!resourceFiles.TryGetValue(entry.resourcePath, out resourceAsset) || resourceAsset != assetPath) throw new InvalidOperationException("Resources path does not resolve to registered PNG: " + entry.id);
            }
            foreach (string folder in new[] { "Youzhou", "YouzhouReferences", "YouzhouUI", "Portraits" })
            {
                string managedFolder = Path.Combine(root, AssetCatalogRules.AssetRoot + folder);
                if (!Directory.Exists(managedFolder)) continue;
                foreach (string file in Directory.GetFiles(managedFolder, "*.png", SearchOption.AllDirectories))
                {
                    string relative = file.Substring(root.Length + 1).Replace('\\', '/');
                    if (!registered.Contains(relative)) throw new InvalidOperationException("Unregistered managed PNG: " + relative);
                }
            }
            GameAssets.ClearCache();
            Debug.Log("ASSET_CATALOG_VALID: ready=" + ready + ", missing=" + missing + "; missing portraits remain UI placeholders, not complete art coverage.");
        }

        private static void AssertProfile(TextureImporter i, AssetRecord e)
        {
            bool map = e.kind == "map-tile";
            bool ui = e.kind == "ui-sprite";
            if (i.textureType != TextureImporterType.Default || !i.sRGBTexture || i.npotScale != TextureImporterNPOTScale.None || i.alphaSource != TextureImporterAlphaSource.FromInput || i.alphaIsTransparency != (map || ui) || i.isReadable != map || i.mipmapEnabled != map || i.wrapMode != TextureWrapMode.Clamp || i.filterMode != (map ? FilterMode.Trilinear : FilterMode.Bilinear) || i.maxTextureSize != (ui ? 8192 : 1024) || i.textureCompression != (map || ui ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ) || i.GetPlatformTextureSettings("Standalone").overridden || (ui && i.GetPlatformTextureSettings("Android").overridden))
                throw new InvalidOperationException("Texture import profile differs for " + e.id + ". Reimport the asset; remove unexpected Standalone overrides and UI Android overrides. GUID need not change.");
        }
        private static int ReadInt(byte[] b, int p) { return (b[p] << 24) | (b[p + 1] << 16) | (b[p + 2] << 8) | b[p + 3]; }
        private static void RejectReparsePath(string path)
        {
            for (string probe = path; !string.IsNullOrEmpty(probe); probe = Path.GetDirectoryName(probe))
                if ((File.Exists(probe) || Directory.Exists(probe)) && (File.GetAttributes(probe) & FileAttributes.ReparsePoint) != 0) throw new InvalidOperationException("Reparse asset paths are not allowed: " + probe);
        }
    }
}
