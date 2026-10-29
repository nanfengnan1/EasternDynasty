using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EasternDynasty;
using Box = EasternDynasty.CityFacilityGalleryLayout.Box;

internal static class CityFacilityGalleryChecks
{
    private static int checks;
    private static float minimumOptionWidth = float.MaxValue, minimumCellWidth = float.MaxValue;
    private static float minimumStageWidth = float.MaxValue;
    private static void Check(bool pass, string message)
    { if (!pass) throw new Exception(message); checks++; }

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
    private static Box Offset(Box value, float x, float y)
    { return new Box(value.X + x, value.Y + y, value.W, value.H); }
    private static Box Sheet(CityFacilitySheetLayout.Box value)
    { return new Box(value.X, value.Y, value.W, value.H); }
    private static void Pairwise(Box[] parts, string context)
    {
        for (int i = 0; i < parts.Length; i++)
        for (int j = 0; j < i; j++)
            Check(Separate(parts[i], parts[j]), context + " overlap: " + i + "/" + j);
    }
    private static Box FitCompleteArt(Box viewport)
    {
        // Match DrawCityFacilityArt's ScaleToFit for the existing 240x180 image.
        float scale = Math.Min(viewport.W / 240, viewport.H / 180);
        var fitted = new Box(viewport.X + (viewport.W - 240 * scale) / 2,
            viewport.Y + (viewport.H - 180 * scale) / 2, 240 * scale, 180 * scale);
        Check(Contains(viewport, fitted), "complete building image fits without cropping the roof or yard");
        Check(Math.Abs(fitted.W / fitted.H - 4f / 3) < .0001f, "original 240x180 image keeps its aspect ratio");
        Check(Math.Abs(fitted.W - viewport.W) < .002f || Math.Abs(fitted.H - viewport.H) < .002f,
            "ScaleToFit uses the available image viewport");
        return fitted;
    }
    private static void Safe(Box rect, MobileScreenLayout screen, string context)
    {
        var physical = new Box(screen.X + rect.X * screen.Scale, screen.Y + rect.Y * screen.Scale,
            rect.W * screen.Scale, rect.H * screen.Scale);
        Check(Contains(new Box(screen.X, screen.Y, screen.Width, screen.Height), physical), context + " stays inside the physical safe area");
    }

    private static void Screen(float[] dimensions)
    {
        var screen = MobileScreenLayout.Calculate(dimensions[0], dimensions[1], dimensions[2], dimensions[3], dimensions[4], dimensions[5], dimensions[6] > 0);
        // Actual CityPresentation side-by-side parents: 66% ledger, 18-unit gap,
        // 24-unit screen margins and shared y=142. This is not a full-screen mock.
        float contentWidth = screen.LogicalWidth - 48, height = screen.LogicalHeight - 240;
        float detailWidth = contentWidth * .66f, districtWidth = contentWidth - detailWidth - 18;
        var district = new Box(24, 142, districtWidth, height);
        var detail = new Box(district.X + district.W + 18, district.Y, detailWidth, height);
        var parent = new Box(0, 0, districtWidth, height);
        var stage = CityFacilityGalleryLayout.Stage(districtWidth, height);
        var heading = CityFacilityGalleryLayout.LocatorHeading(districtWidth, height);
        var grid = CityFacilityGalleryLayout.Grid(districtWidth, height);
        var queue = new Box(16, height - 100, districtWidth - 32, 88);
        minimumStageWidth = Math.Min(minimumStageWidth, stage.W);
        foreach (var part in new[] { stage, heading, grid, queue })
        {
            Check(Contains(parent, part), "stage, locator, district grid and queue remain in their real parent");
            var actual = Offset(part, district.X, district.Y);
            Check(Separate(actual, detail), "gallery never overlays the adjacent ledger");
            Safe(actual, screen, "district section");
        }
        Pairwise(new[] { stage, heading, grid, queue }, "gallery / locator / grid / queue");
        Check(Separate(district, detail), "district and ledger parents remain separate");
        Safe(district, screen, "district parent"); Safe(detail, screen, "ledger parent");

        var image = CityFacilityGalleryLayout.Image(stage.W, stage.H);
        var footer = CityFacilityGalleryLayout.Footer(stage.W, stage.H);
        var stageLocal = new Box(0, 0, stage.W, stage.H);
        Check(Contains(stageLocal, image) && Contains(stageLocal, footer), "large image and footer have their own complete stage regions");
        Check(Separate(image, footer), "large building image does not overlap its name/level footer");
        var illustration = new Box(10, 2, stage.W - 20, 18);
        var facilityName = new Box(footer.X + 14, footer.Y + 1, footer.W - 154, 32);
        var facilityLevel = new Box(footer.X + footer.W - 134, footer.Y + 1, 120, 32);
        Check(Contains(stageLocal, illustration) && Separate(illustration, image), "illustration label stays above the full building image");
        Check(Contains(footer, facilityName) && Contains(footer, facilityLevel) && Separate(facilityName, facilityLevel),
            "large facility name/construction state and level progression keep independent footer regions");
        var completeArt = FitCompleteArt(image);
        Check(completeArt.H >= 208 && completeArt.W >= 277, "short mobile layout retains a substantial complete building image");

        float cellWidth = (grid.W - 12) / 3, cellHeight = (grid.H - 12) / 3;
        minimumCellWidth = Math.Min(minimumCellWidth, cellWidth);
        var cells = Enumerable.Range(0, 9).Select(i => new Box(grid.X + i % 3 * (cellWidth + 6), grid.Y + i / 3 * (cellHeight + 6), cellWidth, cellHeight)).ToArray();
        Pairwise(cells, "nine district location controls");
        Check(Math.Abs(cellHeight - 48) < .002f, "compact district controls keep the reviewed 48-unit height");
        foreach (var cell in cells)
        {
            Check(Contains(grid, cell) && cell.W >= 44 && cell.H >= 44, "every district control retains a 44-unit logical touch target");
            var number = new Box(8, 2, 18, 22);
            var name = new Box(29, 1, cell.W - 34, 23);
            var state = new Box(6, 26, cell.W - 12, 20);
            var local = new Box(0, 0, cell.W, cell.H);
            foreach (var text in new[] { number, name, state }) Check(Contains(local, text), "district number/name/state remain fully inside 48-unit cell");
            Pairwise(new[] { number, name, state }, "district number / name / state");
            Safe(Offset(cell, district.X, district.Y), screen, "district touch target");
        }

        var options = Enumerable.Range(0, 8).Select(i => Sheet(CityFacilitySheetLayout.Options(detailWidth, i))).ToArray();
        Pairwise(options, "eight facility selectors");
        var ledger = Sheet(CityFacilitySheetLayout.View(detailWidth, height, true));
        var action = Sheet(CityFacilitySheetLayout.Action(detailWidth, height));
        foreach (var option in options)
        {
            minimumOptionWidth = Math.Min(minimumOptionWidth, option.W);
            Check(Contains(new Box(0, 0, detailWidth, height), option), "facility selector stays in its real detail parent");
            Check(option.W >= 44 && option.H >= 44, "facility selector preserves a 44-unit logical touch target");
            Check(Separate(option, ledger) && Separate(option, action), "facility thumbnails do not cover scrollable ledger or command");
            Safe(Offset(option, detail.X, detail.Y), screen, "facility selector");
            var local = new Box(0, 0, option.W, option.H);
            var art = CityFacilityGalleryLayout.OptionImage(option.W);
            var caption = CityFacilityGalleryLayout.OptionFooter(option.W);
            var name = CityFacilityGalleryLayout.OptionName(option.W);
            var state = CityFacilityGalleryLayout.OptionState(option.W);
            foreach (var part in new[] { art, caption, name, state }) Check(Contains(local, part), "thumbnail image/footer/text remain inside the clickable parent");
            Check(Separate(art, caption) && Separate(name, state), "thumbnail image does not overlap footer; name and status do not overlap");
            Check(Contains(caption, name) && Contains(caption, state), "both live labels remain inside the separate thumbnail footer");
            var thumb = FitCompleteArt(art);
            Check(thumb.W >= 77 && thumb.H >= 58, "selector retains complete building thumbnail instead of a small circular crop");
        }
        Console.WriteLine("CITY_FACILITY_GALLERY_SCREEN: {0}x{1}; mobile={2}; logical={3:0.##}x{4:0.##}; stage={5:0.##}x{6:0.##}; districtCell={7:0.##}x{8:0.##}; selectorWidth={9:0.##}",
            dimensions[0], dimensions[1], dimensions[6] > 0, screen.LogicalWidth, screen.LogicalHeight, stage.W, stage.H, cellWidth, cellHeight, options[0].W);
    }

    private static void TextFits(Graphics graphics, StringFormat format, string family, string text, float width, float height, int fontSize, int minimum, string context)
    {
        SizeF measured = SizeF.Empty;
        for (int size = fontSize; size >= minimum; size--)
        using (var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel))
        {
            measured = graphics.MeasureString(text, font, new SizeF(width, 10000), format);
            if (measured.Width <= width + .5f && measured.Height <= height + .5f) break;
        }
        Check(measured.Width <= width + .5f && measured.Height <= height + .5f,
            context + " exceeds " + width + "x" + height + " in " + family + ": " + text + " = " + measured);
    }
    private static void TextBudgets(string root)
    {
        string catalog = File.ReadAllText(Path.Combine(root, "Assets/EasternDynasty/Scripts/Core/CityModels.cs"));
        var names = Regex.Matches(catalog, "new CityBuildingDefinition\\(CityBuilding\\.\\w+,\\\"([^\\\"]+)\\\",\\\"").Cast<Match>().Select(m => m.Groups[1].Value).ToArray();
        Check(names.Length == 9, "font budgets read all nine actual facility names from CityCatalog");
        using (var bitmap = new Bitmap(1, 1))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var format = new StringFormat(StringFormat.GenericTypographic))
        {
            format.FormatFlags |= StringFormatFlags.NoWrap;
            foreach (string family in new[] { "Microsoft YaHei", "SimHei", "Arial" })
            {
                foreach (var name in names)
                {
                    TextFits(graphics, format, family, name, minimumOptionWidth, 20, 15, 13, "thumbnail facility name");
                    TextFits(graphics, format, family, name, minimumCellWidth - 34, 23, 15, 13, "district facility name");
                    foreach (string state in new[] { " · 施工中", " · 未建", "" })
                        TextFits(graphics, format, family, name + state, minimumStageWidth - 154, 32, 18, 13, "large stage name/construction footer");
                }
                foreach (string level in new[] { "0 / 10", "10 / 10", "0 → 1 / 10", "9 → 10 / 10" })
                    TextFits(graphics, format, family, level, 120, 32, 16, 13, "large stage independent level progression");
                foreach (string state in new[] { "Lv. 10", "府 10 解锁", "可建" })
                    TextFits(graphics, format, family, state, minimumOptionWidth, CityFacilityGalleryLayout.OptionState(minimumOptionWidth).H, 12, 12, "thumbnail status");
                foreach (string state in new[] { "选择设施", "不可建设", "Lv. 10", "施工中  4分00秒", "施工中  999分59秒" })
                    TextFits(graphics, format, family, state, minimumCellWidth - 12, 20, 13, 12, "district status and construction timer");
                foreach (string name in new[] { "地形保留", "待建设" })
                    TextFits(graphics, format, family, name, minimumCellWidth - 34, 23, 15, 13, "district empty/unbuildable name");
            }
        }
        Console.WriteLine("CITY_FACILITY_GALLERY_FONT_BUDGETS: 9 actual catalog names; separate stage/thumbnail labels; 48-high district name and construction text; 3 GDI+ font families. Actual Unity fonts remain a publisher check.");
    }
    private static int U16(byte[] data,int offset)
    {return (data[offset]<<8)|data[offset+1];}
    private static int S16(byte[] data,int offset)
    {return unchecked((short)U16(data,offset));}
    private static int U32(byte[] data,int offset)
    {return checked((int)(((uint)data[offset]<<24)|((uint)data[offset+1]<<16)|((uint)data[offset+2]<<8)|data[offset+3]));}
    private static int FontTable(byte[] data,string tag)
    {
        for(int i=0;i<U16(data,4);i++)
        {
            int entry=12+i*16;
            if(System.Text.Encoding.ASCII.GetString(data,entry,4)==tag)return U32(data,entry+8);
        }
        throw new Exception("Bundled CJK font table missing: "+tag);
    }
    private static void BundledFontBudgets(string root)
    {
        string fontPath=Path.Combine(root,"Assets/EasternDynasty/Resources/Fonts/CJK.otf");
        var data=File.ReadAllBytes(fontPath);
        int head=FontTable(data,"head"),hhea=FontTable(data,"hhea");
        int unitsPerEm=U16(data,head+18),ascent=S16(data,hhea+4),descent=S16(data,hhea+6),gap=S16(data,hhea+8);
        Check(unitsPerEm>0&&ascent>descent,"bundled CJK font has valid horizontal line metrics");
        float lineAt12=(ascent-descent+gap)*12f/unitsPerEm;
        var status=CityFacilityGalleryLayout.OptionState(minimumOptionWidth);
        var name=CityFacilityGalleryLayout.OptionName(minimumOptionWidth);
        var footer=CityFacilityGalleryLayout.OptionFooter(minimumOptionWidth);
        Check(lineAt12>16+.1f,"actual bundled CJK metadata reproduces the previous 16-high status failure");
        Check(status.H>=lineAt12&&status.H>=Math.Ceiling(lineAt12),"complete bundled 12px Chinese status line fits with rounding room");
        Check(name.H>=(ascent-descent+gap)*13f/unitsPerEm,"facility name preserves the readable 13px minimum with the bundled font");
        Check(Contains(footer,status)&&Contains(footer,name)&&Separate(name,status),"actual font accommodation leaves name and state separate inside the footer");
        Check(CityFacilitySheetLayout.OptionHeight==100&&footer.H==40&&status.H==20,
            "option/footer/status geometry matches the exported 110251322 font-fit amendment");
        var option=CityFacilitySheetLayout.Options(960,0);var view=CityFacilitySheetLayout.View(960,580,true);
        Check(view.Y-(option.Y+option.H)>=8,"font correction preserves the existing first-screen ledger and eight-unit row gap");
        using(var sha=System.Security.Cryptography.SHA256.Create())
            Console.WriteLine("CITY_FACILITY_GALLERY_BUNDLED_FONT: sha256={0}; unitsPerEm={1}; ascent={2}; descent={3}; lineGap={4}; 12pxLine={5:0.###}; oldStatus=16; designedStatus={6:0.###}; actual Unity GUIStyle remains the publication gate.",
                BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant(),unitsPerEm,ascent,descent,gap,lineAt12,status.H);
    }
    private static void Main(string[] args)
    {
        foreach (var dimensions in new[] {
            new[] { 1440f,900f,0f,0f,1440f,900f,0f }, new[] { 1920f,1080f,0f,0f,1920f,1080f,0f },
            new[] { 1280f,720f,0f,0f,1280f,720f,0f }, new[] { 1600f,720f,0f,0f,1600f,720f,0f },
            new[] { 1440f,1080f,0f,0f,1440f,1080f,0f }, new[] { 1600f,720f,56f,18f,1488f,684f,1f },
            new[] { 1600f,720f,56f,24f,1504f,684f,1f }, new[] { 1600f,720f,0f,0f,1600f,720f,1f },
            new[] { 1280f,720f,0f,0f,1280f,720f,1f }, new[] { 2400f,1080f,80f,20f,2240f,1040f,1f },
            new[] { 1920f,1200f,0f,0f,1920f,1200f,1f }, new[] { 2048f,1536f,0f,0f,2048f,1536f,1f } }) Screen(dimensions);
        TextBudgets(args[0]);
        BundledFontBudgets(args[0]);
        Console.WriteLine("CITY_FACILITY_GALLERY_CHECKS_OK: {0} assertions; 12 actual desktop/mobile layouts; full 240x180 image fit; nine 48-high districts; eight independent thumbnail selectors. Pure geometry/font budgets only; runtime appearance and image/text clicks require publisher evidence.", checks);
    }
}
