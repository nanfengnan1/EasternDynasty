using System;
using System.Drawing;
using System.Linq;
using EasternDynasty;

internal static class CardDossierSkillLayoutChecks
{
    private static int checks, fitting, overflowing;
    private static void Check(bool passed, string message)
    { if (!passed) throw new Exception(message); checks++; }

    private static void Verify(CardDossierSkillLayout layout, float native, float learned, float footer, float viewport, bool hasFooter)
    {
        Check(layout.NativeHeight >= native, "Native description must fit its actual measured text");
        Check(layout.SheetHeight >= 240 && layout.SheetHeight + 4 >= layout.NativeY + layout.NativeHeight + 8, "Image and complete description must fit the shared sheet");
        Check(layout.NativeY >= 80, "Effect text must clear the title and effect heading");
        Check(layout.LearnedY >= layout.SheetHeight + 18, "Learning strip must remain detached from the primary sheet");
        Check(layout.LearnedY >= layout.NativeY + layout.NativeHeight, "Learned section must follow the native paragraph");
        Check(layout.LearnedHeight - 40 >= learned, "Learned text must fit below its actual heading");
        Check(layout.FooterY >= layout.LearnedY + layout.LearnedHeight, "Scope text must not overlap learned text");
        if (hasFooter) Check(layout.FooterHeight >= footer, "Scope text must fit every measured line");
        float last = hasFooter ? layout.FooterY + layout.FooterHeight : layout.LearnedY + layout.LearnedHeight;
        Check(layout.ContentHeight >= last, "Actual last content cannot exceed scrollable content");
        float end = layout.ScrollLimit(viewport);
        if (layout.ContentHeight > viewport)
        {
            overflowing++;
            Check(end > 0, "Actual overflow must retain the positive-scroll requirement");
            Check(Math.Abs(layout.ContentHeight - end - viewport) < .001f, "Scroll end must reach the actual content end");
        }
        else
        {
            fitting++;
            Check(end == 0, "Fully visible content must not be required to scroll");
            Check(last <= viewport, "A fitting viewport must expose all actual text");
        }
        Check(last - end <= viewport + .001f, "Final text bottom must be visible after scrolling");
    }

    private static void Main()
    {
        // Independent font measurements of the actual catalog. GDI+ is not a
        // Unity visual check; the player retains GUIStyle and real scroll checks.
        using (var bitmap = new Bitmap(1, 1))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var format = new StringFormat(StringFormat.GenericTypographic))
        foreach (string familyName in new[] { "Microsoft YaHei", "SimHei", "Arial" })
        using (var body = new Font(familyName, 18, FontStyle.Regular, GraphicsUnit.Pixel))
        using (var caption = new Font(familyName, 16, FontStyle.Regular, GraphicsUnit.Pixel))
        foreach (var dimensions in new[] { new[] { 1440, 900 }, new[] { 1920, 1080 }, new[] { 1600, 720 }, new[] { 1024, 768 } })
        foreach (bool mobile in new[] { false, true })
        {
            float sw = dimensions[0], sh = dimensions[1];
            var screen = MobileScreenLayout.Calculate(sw, sh, sw * .035f, 24, sw * .94f, sh - 36, mobile);
            float panelWidth = Math.Min(1280, screen.LogicalWidth - 64);
            float innerWidth = panelWidth - 20 - panelWidth * .43f - 28 - 24 - 22;
            float viewport = Math.Min(820, screen.LogicalHeight - 28) - 226;
            Check(CardDossierSkillLayout.TextWidth(innerWidth) == innerWidth - 184 && CardDossierSkillLayout.NativeX >= 152, "Text column must remain beyond the complete 96-wide image");
            float maxHeight = 0; string maxId = "";
            foreach (var definition in Catalog.Generals.Where(g => g.s1))
            {
                string text = string.IsNullOrWhiteSpace(definition.nativeSkillText) ? "战法说明尚未收录。" : definition.nativeSkillText;
                float native = graphics.MeasureString(text, body, new SizeF(innerWidth - 184, 100000), format).Height;
                float learned = graphics.MeasureString("请从已拥有卡查看", body, new SizeF(innerWidth - 184, 100000), format).Height;
                float footer = graphics.MeasureString("当前展示资料效果；逐条战法结算与拆解尚未开放。", caption, new SizeF(innerWidth - 24, 100000), format).Height;
                var layout = CardDossierSkillLayout.Calculate(native, learned, footer, true);
                Verify(layout, native, learned, footer, viewport, true);
                if (layout.ContentHeight > maxHeight) { maxHeight = layout.ContentHeight; maxId = definition.id; }
            }
            Console.WriteLine("CARD_SKILL_FONT_LAYOUT: {0}; {1}x{2}; mobile={3}; viewport={4:0.###}; longest={5}; content={6:0.###}; overflow={7}",
                familyName, sw, sh, mobile, viewport, maxId, maxHeight, maxHeight > viewport);
        }
        // A measured seven-line paragraph fitting the failing desktop viewport
        // is valid; requiring positive scroll here is the publication regression.
        var fittingDesktop = CardDossierSkillLayout.Calculate(168, 24, 22, true);
        Verify(fittingDesktop, 168, 24, 22, 538, true);
        Check(fittingDesktop.ScrollLimit(538) == 0, "Recorded fitting desktop case must not trigger the obsolete assumption");
        Console.WriteLine("PUBLICATION_SKILL_ASSUMPTION_REPRODUCED: content={0}; viewport=538; validScroll=0", fittingDesktop.ContentHeight);
        // Exercise genuine layout overflow and multiline footer/learned blocks
        // without adding fake descriptions or changing any production card.
        foreach (float native in new[] { 0f, 24f, 168f, 450f, 1600f })
        foreach (float learned in new[] { 24f, 120f, 700f })
        foreach (float footer in new[] { 22f, 84f, 260f })
        foreach (float viewport in new[] { 510f, 538f, 700f })
        foreach (bool hasFooter in new[] { false, true })
            Verify(CardDossierSkillLayout.Calculate(native, learned, footer, hasFooter), native, learned, footer, viewport, hasFooter);
        foreach (float invalid in new[] { -1f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { CardDossierSkillLayout.Calculate(invalid, 24, 22, true); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "Invalid font metrics must not poison scroll geometry");
        }
        foreach (float invalid in new[] { 0f, 184f, float.NaN, float.PositiveInfinity })
        {
            bool rejected = false;
            try { CardDossierSkillLayout.TextWidth(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "An invalid effect column cannot enter font measurement");
        }
        Check(fitting > 0 && overflowing > 0, "Both fitting and overflowing behavior must be exercised");
        Console.WriteLine("CARD_DOSSIER_SKILL_LAYOUT_CHECKS_OK: {0} assertions; fit={1}; overflow={2}. Pure C# only; publisher must check actual GUIStyle scroll bounds and visible last lines.", checks, fitting, overflowing);
    }
}
