using System;
using EasternDynasty;

internal static class LandCommandTextChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static LandCommandTextMetrics Metrics(float width, float height, float lineHeight)
    { return new LandCommandTextMetrics { Width = width, Height = height, LineHeight = lineHeight }; }
    public static void Main()
    {
        // The previous 56-wide box was exactly four nominal 14px characters.
        // A font advance only slightly wider must fail, never get silently clipped.
        var narrow = LandCommandTextLayout.Fit(56, 36, 14, 14,
            size => Metrics(size * 4.05f, size, size * 1.45f));
        Check(!narrow.Fits && narrow.FontSize == 14 && narrow.Width > 56,
            "Fractional CJK advances cannot be accepted using nominal font size.");
        var wider = LandCommandTextLayout.Fit(84, 36, 18, 14,
            size => Metrics(size * 4.05f, size, size * 1.45f));
        Check(wider.Fits && wider.FontSize == 18, "A wider designed caption retains its preferred readable type.");
        // Observed CJK.otf hmtx: 屯/田/·/令 each advance 1000, 3 advances 555,
        // unitsPerEm=1000. The middle dot is a full cell in the bundled font.
        Func<int, LandCommandTextMetrics> farmCaption = size => Metrics(size * 4.555f, size, size * 1.45f);
        var oldFarm = LandCommandTextLayout.Fit(56, 36, 14, 14, farmCaption);
        Check(!oldFarm.Fits && Math.Abs(oldFarm.Width - 63.77f) < .001f,
            "Recorded bundled-font advances reproduce the old farming-caption budget failure.");
        var fullFarm = LandCommandTextLayout.Fit(140, 36, 18, 14, farmCaption);
        Check(fullFarm.Fits && fullFarm.FontSize == 18,
            "The complete farming label and command cost fit without deleting characters or reducing readability.");

        foreach (float advance in new[] { .8f, 1f, 1.05f, 1.2f, 1.5f })
        foreach (float line in new[] { 1f, 1.3f, 1.7f, 2.1f, 2.7f })
        foreach (int count in new[] { 2, 4, 5, 6 })
        foreach (float width in new[] { 56f, 84f, 104f, 144f, 260f })
        {
            Func<int, LandCommandTextMetrics> measure = size => Metrics(size * count * advance, size * 1.1f, size * line);
            var fit = LandCommandTextLayout.Fit(width, 36, 18, 14, measure);
            Check(fit.FontSize >= 14 && fit.FontSize <= 18, "Fitting must preserve the readable range.");
            Check(fit.Fits == (fit.Width <= width && fit.Height <= 36), "Result must agree with actual full-caption width and line height.");
            if (fit.Fits && fit.FontSize < 18)
            {
                var larger = measure(fit.FontSize + 1);
                Check(larger.Width > width || Math.Max(larger.Height, larger.LineHeight) > 36,
                    "Fitting must choose the largest actually fitting size.");
            }
            if (!fit.Fits) Check(fit.FontSize == 14, "An impossible caption reports failure at the minimum, without shrinking or hiding it.");
        }
        var tall = LandCommandTextLayout.Fit(260, 36, 18, 14, size => Metrics(50, 15, size * 2.1f));
        Check(tall.Fits && tall.FontSize == 17 && tall.Height == tall.LineHeight,
            "The font line height constrains fitting even when measured ink is short.");
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f, 0f })
        {
            Check(!LandCommandTextLayout.Fit(84, 36, 18, 14, size => Metrics(invalid, 20, 20)).Fits,
                "Invalid measured width cannot pass.");
            Check(!LandCommandTextLayout.Fit(84, 36, 18, 14, size => Metrics(50, invalid, 20)).Fits,
                "Invalid measured height cannot pass.");
            Check(!LandCommandTextLayout.Fit(84, 36, 18, 14, size => Metrics(50, 20, invalid)).Fits,
                "Invalid line height cannot pass.");
        }
        Console.WriteLine("LAND_COMMAND_TEXT_CHECKS_OK: {0} assertions; complete measured captions, readable minimum, line height and invalid metrics. Synthetic font metrics only; Unity CJK rendering remains the publisher gate.", checks);
    }
}
