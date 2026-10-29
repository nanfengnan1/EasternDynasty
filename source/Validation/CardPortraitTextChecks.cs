using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using EasternDynasty;

internal static class CardPortraitTextChecks
{
    private static int checks, namesChecked;
    private static void Check(bool passed, string description)
    { if (!passed) throw new Exception(description); checks++; }

    private static void Main(string[] args)
    {
        CardPortraitDesignChecks.Run(args.Length == 1 ? args[0] : Directory.GetCurrentDirectory());
        // These are font-file/GDI+ metrics, not Unity execution. They independently
        // reproduce the publication font-height mismatch; the player keeps its
        // real GUIStyle assertions and remains the final authority for rendering.
        using (var bitmap = new Bitmap(1, 1))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var format = new StringFormat(StringFormat.GenericTypographic))
        foreach (string requested in new[] { "Microsoft YaHei", "SimHei", "Arial" })
        using (var family = new FontFamily(requested))
        {
            var cache = new Dictionary<string, CardPortraitGlyphMetrics>();
            Func<char, int, CardPortraitGlyphMetrics> measure = (glyph, size) =>
            {
                string key = glyph + ":" + size;
                CardPortraitGlyphMetrics result;
                if (cache.TryGetValue(key, out result)) return result;
                using (var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var actual = graphics.MeasureString(glyph.ToString(), font, new SizeF(1000, 1000), format);
                    result = new CardPortraitGlyphMetrics { Width = actual.Width,
                        Height = Math.Max(actual.Height, size * family.GetLineSpacing(FontStyle.Regular)
                            / (float)family.GetEmHeight(FontStyle.Regular)) };
                }
                cache[key] = result;
                return result;
            };
            if (requested == "Microsoft YaHei")
            {
                Check(measure('曹', 30).Height > 38, "Recorded desktop failure must reproduce: size 30 exceeds a 38-high cell");
                int fitted = CardPortraitTextLayout.FitName("曹操", 44, 38, 30, measure);
                Check(fitted < 30 && fitted >= 18, "Measured desktop font becomes smaller and remains readable");
                Console.WriteLine("PUBLICATION_FONT_FAILURE_REPRODUCED: Microsoft YaHei size30 height={0:0.####}, cell=38; fittedSize={1}, fittedHeight={2:0.####}",
                    measure('曹', 30).Height, fitted, measure('曹', fitted).Height);
            }
            int starSize = CardPortraitTextLayout.FitName("★", 48, 30, 24, measure);
            var star = measure('★', starSize);
            Check(starSize >= 14 && starSize <= 24 && star.Width <= 48 && star.Height <= 30,
                "Actual single rarity glyph fits its independent vertical cell: " + requested);
            if (starSize < 24)
                Check(measure('★', starSize + 1).Width > 48 || measure('★', starSize + 1).Height > 30,
                    "Vertical star uses the largest measured fitting font size: " + requested);
            Console.WriteLine("CARD_STAR_FONT_METADATA_OK: {0}; fittedSize={1}; glyph={2:0.####}x{3:0.####}; cell=48x30",
                requested, starSize, star.Width, star.Height);
            foreach (var dimensions in new[] { new[] { 1440, 900 }, new[] { 1920, 1080 }, new[] { 1600, 720 }, new[] { 1280, 720 } })
            foreach (bool mobile in new[] { false, true })
            {
                float sw = dimensions[0], sh = dimensions[1];
                var layout = MobileScreenLayout.Calculate(sw, sh, sw * .035f, 24, sw * .94f, sh - 36, mobile);
                float artHeight = Math.Min(820, layout.LogicalHeight - 28) - 28;
                foreach (var definition in Catalog.Generals)
                {
                    float step = CardPortraitDesignLayout.NameStep(artHeight, definition.name.Length);
                    int fitted = CardPortraitTextLayout.FitName(definition.name, 44, step, 30, measure);
                    Check(fitted >= 18 && fitted <= 30, "Actual catalog name retains a readable size: " + definition.id);
                    foreach (char glyph in definition.name)
                    {
                        var actual = measure(glyph, fitted);
                        Check(actual.Width <= 44 && actual.Height <= step, "Actual name glyph fits its complete cell: " + definition.id);
                    }
                    if (fitted < 30)
                        Check(definition.name.Any(g => measure(g, fitted + 1).Width > 44 || measure(g, fitted + 1).Height > step),
                            "Name uses the largest measured fitting font size: " + definition.id);
                    namesChecked++;
                }
            }
            Console.WriteLine("CARD_NAME_FONT_METADATA_OK: {0}; em={1}; lineSpacing={2}; {3} names at 4 resolutions and desktop/mobile safe areas",
                requested, family.GetEmHeight(FontStyle.Regular), family.GetLineSpacing(FontStyle.Regular), Catalog.Generals.Count);
        }
        // A later glyph can be wider than the first; the entire name participates.
        Func<char, int, CardPortraitGlyphMetrics> wide = (glyph, size) => new CardPortraitGlyphMetrics
            { Width = glyph == '宽' ? size * 2 : size, Height = size };
        int wideFit = CardPortraitTextLayout.FitName("曹宽", 44, 38, 30, wide);
        Check(wideFit == 22, "A wide later glyph constrains the real rendered width");
        Func<char, int, CardPortraitGlyphMetrics> tall = (glyph, size) => new CardPortraitGlyphMetrics
            { Width = size, Height = size * 1.6f };
        Check(CardPortraitTextLayout.FitName("公孙瓒", 44, 38, 30, tall) == 23,
            "Tall CJK fallback metrics fit without relying on a fixed font-size margin");
        Check(CardPortraitTextLayout.FitName("曹操", 44, 22, 30, tall) == 13,
            "Constrained cells still use measured line height");
        Console.WriteLine("CARD_PORTRAIT_TEXT_CHECKS_OK: {0} assertions; {1} actual-name/font/layout cases. Pure C# font metrics only; publisher must rerun unchanged Unity geometry assertions and inspect new screenshots.", checks, namesChecked);
    }
}
