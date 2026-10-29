using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EasternDynasty
{
    [Serializable] public sealed class AssetManifest { public int schemaVersion; public AssetRecord[] entries; }
    [Serializable] public sealed class AssetSource
    {
        public string kind, reference, rights, createdUtc;
    }
    [Serializable] public sealed class AssetRecord
    {
        public string id, kind, status, resourcePath, purpose, sha256, importedUtc, placeholderPolicy;
        public bool required, hasAlpha, hasTransparency;
        public int width, height;
        public AssetSource source;
    }

    // Plain serializable arrays on disk; dictionaries are only an in-memory index.
    public static class AssetCatalogRules
    {
        public const string ManifestResourcePath = "asset-manifest";
        public const string AssetRoot = "Assets/EasternDynasty/Resources/";
        public static string ExpectedResourcePath(string id, string kind)
        {
            if (id == null) return null;
            if (kind == "ui-sprite" && Regex.IsMatch(id, @"\Aui\.youzhou\.[a-z][a-z0-9_]{0,63}\z")) return "YouzhouUI/" + id.Substring(11);
            if (kind == "map-reference" && Regex.IsMatch(id, @"\Areference\.youzhou\.[a-z][a-z0-9_]{0,63}\z")) return "YouzhouReferences/" + id.Substring(18);
            if (kind == "map-tile" && Regex.IsMatch(id, @"\Amap\.youzhou\.[a-z][a-z0-9_]{0,63}\z")) return "Youzhou/" + id.Substring(12);
            if (kind == "portrait" && Regex.IsMatch(id, @"\Aportrait\.[a-z][a-z0-9_]{0,63}\z")) return "Portraits/" + id.Substring(9);
            return null;
        }

        public static Dictionary<string, AssetRecord> Validate(AssetManifest manifest)
        {
            if (manifest == null || manifest.schemaVersion != 1 || manifest.entries == null) throw new InvalidOperationException("Asset manifest: expected schemaVersion=1 and entries array.");
            var entries = new Dictionary<string, AssetRecord>(StringComparer.Ordinal);
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in manifest.entries)
            {
                if (entry == null) throw new InvalidOperationException("Asset manifest contains a null record.");
                string expected = ExpectedResourcePath(entry.id, entry.kind);
                if (expected == null || entry.resourcePath != expected) Fail(entry, "unknown kind/ID or noncanonical Resources path");
                if (entries.ContainsKey(entry.id) || !paths.Add(entry.resourcePath)) Fail(entry, "duplicate ID or Resources path");
                if (string.IsNullOrWhiteSpace(entry.purpose)) Fail(entry, "purpose is required");
                if (entry.status == "ready")
                {
                    if (entry.width < 1 || entry.height < 1 || entry.width > 8192 || entry.height > 8192 || (long)entry.width * entry.height > 16777216) Fail(entry, "invalid dimensions");
                    if (entry.sha256 == null || !Regex.IsMatch(entry.sha256, @"\A[a-f0-9]{64}\z")) Fail(entry, "invalid SHA-256");
                    if (entry.source == null || (entry.source.kind != "imagegen" && entry.source.kind != "user-provided" && entry.source.kind != "licensed-local") || string.IsNullOrWhiteSpace(entry.source.reference) || string.IsNullOrWhiteSpace(entry.source.rights)) Fail(entry, "source and rights statement are required");
                    DateTime date;
                    if (!DateTime.TryParse(entry.importedUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out date)) Fail(entry, "invalid importedUtc");
                    if (!string.IsNullOrEmpty(entry.source.createdUtc) && !DateTime.TryParse(entry.source.createdUtc, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind, out date)) Fail(entry, "invalid source.createdUtc");
                    if (entry.placeholderPolicy != "none") Fail(entry, "ready asset must use placeholderPolicy=none");
                    float aspect = entry.width / (float)entry.height;
                    if (entry.kind == "map-tile" && (!entry.hasAlpha || !entry.hasTransparency || Math.Abs(aspect - 1.5f) > .08f)) Fail(entry, "map art requires transparent PNG near 3:2");
                    if (entry.kind == "portrait" && Math.Abs(aspect - 2f / 3f) > .05f) Fail(entry, "portrait requires a canvas near 2:3, including native 240x340 references");
                    if (entry.hasTransparency && !entry.hasAlpha) Fail(entry, "transparency requires alpha");
                }
                else if (entry.status == "missing")
                {
                    if (entry.kind != "portrait" || entry.required || entry.placeholderPolicy != "ui-placeholder" || entry.width != 0 || entry.height != 0 || !string.IsNullOrEmpty(entry.sha256) || entry.hasAlpha || entry.hasTransparency) Fail(entry, "missing portraits must be optional, without invented image metadata");
                }
                else Fail(entry, "unknown status");
                entries.Add(entry.id, entry);
            }
            return entries;
        }
        private static void Fail(AssetRecord entry, string reason) { throw new InvalidOperationException("Asset '" + entry.id + "': " + reason + "."); }
    }
}
