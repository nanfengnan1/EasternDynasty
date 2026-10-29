using System;
using System.Collections.Generic;
using UnityEngine;

namespace EasternDynasty
{
    public interface IGameAssetProvider
    {
        string LoadManifestJson();
        Texture2D LoadTexture(AssetRecord entry);
    }

    public sealed class ResourcesGameAssetProvider : IGameAssetProvider
    {
        public string LoadManifestJson()
        {
            var text = Resources.Load<TextAsset>(AssetCatalogRules.ManifestResourcePath);
            if (text == null) throw new InvalidOperationException("Required Resources/asset-manifest.json is missing.");
            return text.text;
        }
        public Texture2D LoadTexture(AssetRecord entry) { return Resources.Load<Texture2D>(entry.resourcePath); }
    }

    // Unity main thread API. Providers may change while rendering code keeps stable IDs.
    // This is a packaged-asset loader, not a downloader or a player hot-install facility.
    public static class GameAssets
    {
        private static IGameAssetProvider provider = new ResourcesGameAssetProvider();
        private static Dictionary<string, AssetRecord> entries;
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private static readonly HashSet<string> warned = new HashSet<string>(StringComparer.Ordinal);
        public static int ReadyCount { get { EnsureCatalog(); int n = 0; foreach (var e in entries.Values) if (e.status == "ready") n++; return n; } }
        public static int MissingCount { get { EnsureCatalog(); return entries.Count - ReadyCount; } }

        public static void ConfigureProvider(IGameAssetProvider value)
        {
            if (value == null) throw new ArgumentNullException("value");
            provider = value; ClearCache();
        }
        // Drops our references only: never destroys a texture that a live UI/atlas uses.
        public static void ClearCache() { entries = null; cache.Clear(); warned.Clear(); }

        public static Texture2D LoadTexture(string id, bool required = true)
        {
            EnsureCatalog();
            AssetRecord entry;
            if (string.IsNullOrEmpty(id) || !entries.TryGetValue(id, out entry)) return Unavailable(id, "ID is not registered", required);
            if (entry.status != "ready") return Unavailable(id, "art is explicitly missing; use the UI placeholder", required || entry.required);
            Texture2D texture;
            if (cache.TryGetValue(id, out texture) && texture != null) return texture;
            texture = provider.LoadTexture(entry);
            if (texture == null) return Unavailable(id, "registered texture could not load from '" + entry.resourcePath + "'", required || entry.required);
            cache[id] = texture; return texture;
        }
        private static Texture2D Unavailable(string id, string reason, bool required)
        {
            string message = "GameAssets [" + (id ?? "<null>") + "]: " + reason + ".";
            if (required) throw new InvalidOperationException(message);
            if (warned.Add(id ?? "<null>")) Debug.LogWarning(message);
            return null;
        }
        private static void EnsureCatalog()
        {
            if (entries != null) return;
            try { entries = AssetCatalogRules.Validate(JsonUtility.FromJson<AssetManifest>(provider.LoadManifestJson())); }
            catch (Exception error) { throw new InvalidOperationException("GameAssets catalog could not initialize: " + error.Message, error); }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { provider = new ResourcesGameAssetProvider(); ClearCache(); }
    }
}
