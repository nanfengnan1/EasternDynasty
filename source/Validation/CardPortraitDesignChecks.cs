using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using EasternDynasty;
using Box = EasternDynasty.CardPortraitDesignLayout.Box;

internal static class CardPortraitDesignChecks
{
    private static int checks, cases;
    [DataContract] private sealed class Manifest
    { [DataMember] public Asset[] entries { get; set; } }
    [DataContract] private sealed class Asset
    {
        [DataMember] public string id { get; set; }
        [DataMember] public string status { get; set; }
        [DataMember] public string resourcePath { get; set; }
        [DataMember] public int width { get; set; }
        [DataMember] public int height { get; set; }
    }

    private static void Check(bool pass, string message)
    { if (!pass) throw new Exception(message); checks++; }

    private static void Near(float actual, float expected, string message)
    { Check(Math.Abs(actual - expected) < .002f, message + ": " + actual + " != " + expected); }

    private static bool Contains(Box outer, Box inner)
    {
        return inner.W > 0 && inner.H > 0 && inner.X >= outer.X - .002f && inner.Y >= outer.Y - .002f
            && inner.X + inner.W <= outer.X + outer.W + .002f && inner.Y + inner.H <= outer.Y + outer.H + .002f;
    }

    private static bool Separate(Box a, Box b)
    {
        return a.X + a.W <= b.X + .002f || b.X + b.W <= a.X + .002f
            || a.Y + a.H <= b.Y + .002f || b.Y + b.H <= a.Y + .002f;
    }

    private static void Screen(float[] screen, Dictionary<string, Asset> sources)
    {
        var layout = MobileScreenLayout.Calculate(screen[0], screen[1], screen[2], screen[3], screen[4], screen[5], screen[6] > 0);
        float width = Math.Min(1280, layout.LogicalWidth - 64), height = Math.Min(820, layout.LogicalHeight - 28);
        var panel = new Box((layout.LogicalWidth - width) / 2, (layout.LogicalHeight - height) / 2, width, height);
        var art = new Box(panel.X + 20, panel.Y + 14, panel.W * .43f, panel.H - 28);
        var safe = new Box(layout.X, layout.Y, layout.Width, layout.Height);
        var physical = new Box(layout.X + art.X * layout.Scale, layout.Y + art.Y * layout.Scale,
            art.W * layout.Scale, art.H * layout.Scale);
        Check(Contains(safe, physical), "actual production card frame stays within physical safe area");
        Check(Contains(panel, art), "portrait frame stays within the existing card modal");
        foreach (var definition in Catalog.Generals.Where(g => g.s1))
        {
            string context = screen[0] + "x" + screen[1] + " mobile=" + (screen[6] > 0) + " " + definition.id;
            var source = sources["portrait." + definition.id];
            var area = CardPortraitDesignLayout.ImageArea(art);
            var image = CardPortraitDesignLayout.FitImage(area, source.width, source.height);
            var banner = CardPortraitDesignLayout.Banner(art, definition.name.Length);
            var stars = CardPortraitDesignLayout.Stars(art, definition.name.Length, definition.stars);
            var basis = CardPortraitDesignLayout.Base(art);
            var regions = new[] { image, banner, stars, basis };
            Check(Contains(area, image) && Contains(art, area), context + " full painting fits the designed image window");
            for (int i = 0; i < regions.Length; i++)
            {
                Check(Contains(art, regions[i]), context + " complete region inside portrait " + i);
                for (int j = 0; j < i; j++)
                    Check(Separate(regions[i], regions[j]), context + " painting and identity regions never overlap " + i + "/" + j);
            }
            Near(image.W / image.H, source.width / (float)source.height, context + " complete source aspect");
            Near(image.X + image.W / 2, area.X + area.W / 2, context + " painting horizontally centered");
            Near(image.Y, banner.Y, context + " painting shares the pennant top anchor");
            Check(image.Y + image.H <= basis.Y - 11.99f, context + " painting leaves a visible gap above the factual base");
            float previousScale = Math.Min((art.W - 82) / source.width, (art.H - 137) / source.height);
            Check(image.W + .002f >= source.width * previousScale && image.H + .002f >= source.height * previousScale,
                context + " complete painting is not reduced by the design change");
            Near(area.H - (art.H - 137), 33, context + " removed horizontal crown returns height to the artwork");
            Near(stars.X, banner.X, context + " vertical rarity shares the identity column");
            Near(stars.Y - banner.Y - banner.H, 8, context + " rarity starts below the complete name pennant");
            Near(stars.W, 48, context + " individual rarity cell width");
            Near(stars.H, 30 * definition.stars, context + " one 30-high cell for each real star");
            float step = CardPortraitDesignLayout.NameStep(art.H, definition.name.Length);
            for (int i = 0; i < definition.name.Length; i++)
            {
                var nameCell = new Box(banner.X + 2, banner.Y + 55 + i * step, 44, step);
                Check(Contains(banner, nameCell), context + " full name glyph cell stays in pennant " + i);
            }
            for (int i = 0; i < definition.stars; i++)
                Check(Contains(stars, new Box(stars.X, stars.Y + 30 * i, 48, 30)),
                    context + " actual star cell stays in separate rarity column " + i);
            Check(stars.Y + stars.H < basis.Y, context + " longest name and actual rarity clear the base");
            cases++;
        }
        Console.WriteLine("CARD_PORTRAIT_DESIGN_SCREEN: {0}x{1}; mobile={2}; art={3:0.##}x{4:0.##}; safe={5:0.##}x{6:0.##}",
            screen[0], screen[1], screen[6] > 0, art.W, art.H, layout.Width, layout.Height);
    }

    internal static void Run(string root)
    {
        Manifest manifest;
        using (var stream = File.OpenRead(Path.Combine(root, "Assets/EasternDynasty/Resources/asset-manifest.json")))
            manifest = (Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
        var sources = manifest.entries.Where(a => a.id.StartsWith("portrait.s1_", StringComparison.Ordinal) && a.status == "ready")
            .ToDictionary(a => a.id);
        var definitions = Catalog.Generals.Where(g => g.s1).ToArray();
        Check(definitions.Length >= 245 && sources.Count == definitions.Length, "all current 245 S1 catalog identities have one controlled painting");
        foreach (var definition in definitions)
        {
            Asset source;
            Check(sources.TryGetValue("portrait." + definition.id, out source), "stable portrait identity " + definition.id);
            Check(source.width > 0 && source.height > 0, "positive source dimensions " + source.id);
            using (var image = Image.FromFile(Path.Combine(root, "Assets/EasternDynasty/Resources", source.resourcePath + ".png")))
                Check(image.Width == source.width && image.Height == source.height, "actual controlled PNG dimensions match manifest " + source.id);
        }
        Check(definitions.Any(g => g.name.Length >= 4), "actual longest source names participate");
        Check(definitions.Any(g => g.stars <= 3) && definitions.Any(g => g.stars == 5), "actual low and high rarity source identities participate");
        Check(CardPortraitDesignLayout.Style == "inkscape-portrait-column-v2", "runtime and inspected design share the expected revision");
        foreach (var screen in new[] {
            new[] {1440f,900f,0f,0f,1440f,900f,0f}, new[] {1920f,1080f,0f,0f,1920f,1080f,0f},
            new[] {1280f,720f,0f,0f,1280f,720f,0f}, new[] {1600f,720f,0f,0f,1600f,720f,0f},
            new[] {1440f,1080f,0f,0f,1440f,1080f,0f}, new[] {1600f,720f,56f,18f,1488f,684f,1f},
            new[] {1600f,720f,56f,24f,1504f,684f,1f}, // Exact -edMobileReview safe area and inspected mobile draft.
            new[] {1600f,720f,0f,0f,1600f,720f,1f}, new[] {1280f,720f,0f,0f,1280f,720f,1f},
            new[] {2400f,1080f,80f,20f,2240f,1040f,1f}, new[] {1920f,1200f,0f,0f,1920f,1200f,1f},
            new[] {2048f,1536f,0f,0f,2048f,1536f,1f} }) Screen(screen, sources);
        Console.WriteLine("CARD_PORTRAIT_DESIGN_CHECKS_OK: {0} assertions; {1} actual portrait/name/rarity/layout cases; {2} controlled PNGs. Pure geometry only; new Unity screenshots and real clicks remain publisher checks.", checks, cases, sources.Count);
    }
}
