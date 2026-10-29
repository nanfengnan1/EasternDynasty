using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EasternDynasty;
using Box = EasternDynasty.CityFacilityAtlasLayout.Box;

internal static class CityFacilityAtlasChecks
{
    private static int checks, textCases;
    private static float minimumTextWidth = float.MaxValue, minimumViewHeight = float.MaxValue;
    private static float minimumTextSlack = float.MaxValue;
    private static string tightestText;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static bool Contains(Box parent, Box child)
    { return child.W > 0 && child.H > 0 && child.X >= parent.X - .002f && child.Y >= parent.Y - .002f
        && child.X + child.W <= parent.X + parent.W + .002f && child.Y + child.H <= parent.Y + parent.H + .002f; }
    private static bool Separate(Box a, Box b)
    { return a.X + a.W <= b.X + .002f || b.X + b.W <= a.X + .002f
        || a.Y + a.H <= b.Y + .002f || b.Y + b.H <= a.Y + .002f; }
    private static Box Offset(Box r, float x, float y) { return new Box(r.X + x, r.Y + y, r.W, r.H); }
    private static void Safe(Box r, MobileScreenLayout screen, string context)
    {
        var physical = new Box(screen.X + r.X * screen.Scale, screen.Y + r.Y * screen.Scale, r.W * screen.Scale, r.H * screen.Scale);
        Check(Contains(new Box(screen.X, screen.Y, screen.Width, screen.Height), physical), context + " exceeds physical safe area");
    }

    private static void Nodes(float viewWidth, float viewHeight, int count, string context)
    {
        int columns = CityFacilityAtlasLayout.Columns(viewWidth), rows = (count + columns - 1) / columns;
        float width = CityFacilityAtlasLayout.Width(viewWidth), height = CityFacilityAtlasLayout.Height, gap = CityFacilityAtlasLayout.Gap;
        minimumTextWidth = Math.Min(minimumTextWidth, CityFacilityAtlasLayout.Effect(width).W);
        minimumViewHeight = Math.Min(minimumViewHeight, viewHeight);
        Check(width > 0 && columns >= 2 && columns <= 3, context + " uses valid node dimensions");
        Check(columns * width + (columns - 1) * gap <= viewWidth - 20, context + " leaves the actual scrollbar reservation clear");
        Check(2 * height + gap <= viewHeight, context + " cannot show two complete rows");
        var local = new Box(0, 0, width, height);
        var art = CityFacilityAtlasLayout.Art(width);
        var parts = new[] { art, CityFacilityAtlasLayout.Name(width), CityFacilityAtlasLayout.State(width),
            CityFacilityAtlasLayout.Effect(width), CityFacilityAtlasLayout.Condition(width), CityFacilityAtlasLayout.Action(width) };
        Check(art.W == 216 && art.H == 162 && Math.Abs(art.W / art.H - 240f / 180) < .0001f,
            context + " preserves the inspected 216x162 whole-building viewport and original aspect ratio");
        for (int i = 0; i < parts.Length; i++)
        {
            Check(Contains(local, parts[i]), context + " node part outside card: " + i);
            for (int j = 0; j < i; j++) Check(Separate(parts[i], parts[j]), context + " image/text/action overlap: " + i + "/" + j);
        }
        var action = parts[5];
        Check(action.W >= 44 && action.H >= 44 && art.W >= 44 && art.H >= 44,
            context + " image and action retain logical touch targets");
        float contentHeight = Math.Max(viewHeight, rows * (height + gap) - gap);
        float maxScroll = contentHeight - viewHeight;
        int fullyVisibleAtTop = 0;
        for (int i = 0; i < count; i++)
        {
            float x = i % columns * (width + gap), y = i / columns * (height + gap);
            var card = new Box(x, y, width, height);
            if (Contains(new Box(0, 0, viewWidth, viewHeight), card)) fullyVisibleAtTop++;
            float scroll = Math.Max(0, Math.Min(y, maxScroll));
            Check(Contains(new Box(0, 0, viewWidth, viewHeight), Offset(card, 0, -scroll)),
                context + " scrolling cannot fully expose facility " + i + " for enabled input");
        }
        Check(fullyVisibleAtTop >= Math.Min(count, columns * 2), context + " first two rows are not fully actionable");
        var last = new Box((count - 1) % columns * (width + gap), (rows - 1) * (height + gap) - maxScroll, width, height);
        Check(Contains(new Box(0, 0, viewWidth, viewHeight), last), context + " final row not reachable at maximum scroll");
    }

    private static void Screen(float[] d)
    {
        var screen = MobileScreenLayout.Calculate(d[0], d[1], d[2], d[3], d[4], d[5], d[6] > 0);
        // The actual CityPresentation content and catalog parents, including sidebar and scroll margins.
        var content = new Box(24, 142, screen.LogicalWidth - 48, screen.LogicalHeight - 240);
        var sidebar = new Box(content.X, content.Y, 208, content.H);
        var panel = new Box(sidebar.X + sidebar.W + 16, content.Y, content.W - 224, content.H);
        var view = new Box(panel.X + 18, panel.Y + 80, panel.W - 36, panel.H - 94);
        Check(Contains(content, sidebar) && Contains(content, panel) && Separate(sidebar, panel), "catalog sidebar and panel remain separate inside content");
        Check(Contains(panel, view), "catalog scroll canvas stays inside its parent");
        Safe(content, screen, "catalog content"); Safe(view, screen, "catalog scroll canvas");
        var hall = new Box(12, 56, 184, 138);
        var title = new Box(12, 197, 184, 26);
        var progress = new Box(12, 227, 184, 24);
        var footer = new Box(18, sidebar.H - 64, sidebar.W - 36, 58);
        var categories = Enumerable.Range(0, 4).Select(i => new Box(12, 276 + i * 50, 184, 44)).ToArray();
        Check(Separate(hall, title) && Separate(title, progress) && Separate(progress, categories[0]), "sidebar building identity and category sections overlap");
        Check(Separate(categories[3], footer), "sidebar final category overlaps provenance footer");
        foreach (var category in categories)
        { Check(Contains(new Box(0, 0, sidebar.W, sidebar.H), category), "sidebar category clips at short safe-area height"); Safe(Offset(category, sidebar.X, sidebar.Y), screen, "category touch target"); }
        var heading = new Box(panel.X + 22, panel.Y + 10, panel.W - 220, 28);
        var queue = new Box(panel.X + panel.W - 184, panel.Y + 14, 162, 44);
        Check(Separate(heading, queue) && Separate(queue, view), "construction location command overlaps heading or first node row");
        foreach (int count in new[] { 9, 2, 5 }) Nodes(view.W, view.H, count, "screen " + d[0] + "x" + d[1] + " count=" + count);
        Console.WriteLine("CITY_FACILITY_ATLAS_SCREEN: {0}x{1}; mobile={2}; logical={3:0.##}x{4:0.##}; view={5:0.##}x{6:0.##}; columns={7}; cardWidth={8:0.##}",
            d[0], d[1], d[6] > 0, screen.LogicalWidth, screen.LogicalHeight, view.W, view.H,
            CityFacilityAtlasLayout.Columns(view.W), CityFacilityAtlasLayout.Width(view.W));
    }

    private static void TextFits(Graphics graphics, StringFormat format, FontFamily family, string text, float width, float height, int startSize, int minimumSize, string context)
    {
        SizeF measured = SizeF.Empty; int chosen = minimumSize;
        for (int size = startSize; size >= minimumSize; size--)
        using (var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel))
        {
            // Measure unbounded text to prevent the measurement API clipping or wrapping away an overflow.
            measured = graphics.MeasureString(text, font, new SizeF(10000, 10000), format); chosen = size;
            if (measured.Width + 4 <= width && measured.Height <= height) break;
        }
        Check(measured.Width + 4 <= width + .01f && measured.Height <= height + .01f,
            context + " lacks 4px width reserve: " + text + " = " + measured + " in " + width + "x" + height);
        textCases++;
        if (width - measured.Width < minimumTextSlack)
        { minimumTextSlack = width - measured.Width; tightestText = context + ": " + text + " @" + chosen; }
    }
    private static void Rich(World world)
    { world.resources = new Supplies { grain=10000000,grass=10000000,stone=10000000,iron=10000000,gold=10000000 }; }
    private static void Complete(World world, int plot, CityBuilding building)
    {
        Rich(world); Check(world.CityBuildProblem(plot, building) == "", "valid actual construction fixture: " + building);
        world.StartCityConstruction(plot, building); Check(world.CityJob != null, "actual construction starts");
        world.Tick(world.CityJob.remaining); Check(world.CityJob == null, "actual construction completes");
    }
    private static void TextBudgets(string root)
    {
        string source = File.ReadAllText(Path.Combine(root, "Assets/EasternDynasty/Scripts/Runtime/CityFacilityPresentation.cs"));
        var units = new Dictionary<CityBuilding, string>();
        foreach (Match m in Regex.Matches(source, "case CityBuilding\\.(\\w+):return \"([^\"]+)\";"))
            units[(CityBuilding)Enum.Parse(typeof(CityBuilding), m.Groups[1].Value)] = m.Groups[2].Value;
        var fallback = Regex.Match(source, "default:return \"( · 部队[^\"]+)\";");
        Check(units.Count == 7 && fallback.Success, "read live effect suffixes rather than inventing short test-only labels");
        var worlds = new List<World>();
        var fresh = World.CreateYouzhou(); worlds.Add(fresh);
        var working = World.CreateYouzhou(); Rich(working); working.StartCityConstruction(0, CityBuilding.Lumberyard);
        Check(working.CityJob != null, "long named work site uses actual Core construction"); worlds.Add(working);
        var maximum = World.CreateYouzhou();
        while (maximum.CityLevel(CityBuilding.TownHall) < CityCatalog.Get(CityBuilding.TownHall).maximumLevel) Complete(maximum, 4, CityBuilding.TownHall);
        while (maximum.CityLevel(CityBuilding.Barracks) < CityCatalog.Get(CityBuilding.Barracks).maximumLevel) Complete(maximum, 0, CityBuilding.Barracks);
        worlds.Add(maximum);
        var routes = worlds.SelectMany(w => CityCatalog.Buildings.Select(d => CityFacilityNavigation.Resolve(w, d.kind))).ToArray();
        Check(routes.Any(r=>r.state==CityFacilityRouteState.HallRequired) && routes.Any(r=>r.state==CityFacilityRouteState.QueueBlocked)
            && routes.Any(r=>r.state==CityFacilityRouteState.MaximumLevel), "font fixtures include actual locked, queue and maximum-level routes");
        using (var bitmap = new Bitmap(1, 1))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var family = new FontFamily("Microsoft YaHei"))
        using (var format = new StringFormat(StringFormat.GenericTypographic))
        {
            format.FormatFlags |= StringFormatFlags.NoWrap;
            foreach (var definition in CityCatalog.Buildings)
            {
                TextFits(graphics, format, family, definition.name, minimumTextWidth, 32, 22, 13, "facility name");
                foreach (int level in new[] { 0, 1, definition.maximumLevel-1, definition.maximumLevel })
                {
                    int next = Math.Min(definition.maximumLevel, level+1), required = CityCatalog.RequiredHallLevel(definition.kind, next);
                    string unit = units.ContainsKey(definition.kind) ? units[definition.kind] : fallback.Groups[1].Value;
                    string effect = CityCatalog.EffectAtLevel(definition.kind, level)+" → "+CityCatalog.EffectAtLevel(definition.kind, next)+unit;
                    TextFits(graphics, format, family, effect, minimumTextWidth, 24, 14, 13, "live Core effect");
                    TextFits(graphics, format, family, "Lv. "+level+" / "+definition.maximumLevel, minimumTextWidth, 22, 14, 13, "live Core level");
                    TextFits(graphics, format, family, "城主府 Lv. "+required+"  ·  已满足", minimumTextWidth, 28, 14, 13, "live Core prerequisite");
                }
                TextFits(graphics, format, family, "等待 "+definition.name+"完工", minimumTextWidth, 28, 14, 13, "named queue condition");
                int seconds=(int)Math.Ceiling(fresh.CityBuildSeconds(definition.kind,definition.maximumLevel));
                string duration=seconds>=3600?(seconds/3600)+"时"+(seconds%3600/60)+"分":seconds>=60?(seconds/60)+"分"+(seconds%60).ToString("00")+"秒":seconds+"秒";
                TextFits(graphics, format, family, "完工剩余  "+duration, minimumTextWidth, 28, 14, 13, "live maximum Core construction time");
            }
            foreach (var label in routes.Select(r=>r.label).Distinct())
                TextFits(graphics, format, family, label, minimumTextWidth-16, 42, 16, 13, "actual route action with 8px side insets");
            foreach (string text in new[] { "设施已达最高等级", "主城发展核心", "尚未建成" })
                TextFits(graphics, format, family, text, minimumTextWidth, 28, 14, 13, "facility state");
            foreach (var definition in CityCatalog.Buildings)
                TextFits(graphics, format, family, "施工至 Lv. "+definition.maximumLevel, minimumTextWidth, 22, 14, 13, "maximum construction target");
            foreach (string caption in new[] { "全部设施", "城务发展", "资源生产", "军备建设" })
                TextFits(graphics, format, family, caption, 168, 42, 17, 13, "sidebar category");
        }
        Console.WriteLine("CITY_FACILITY_ATLAS_FONT_BUDGETS: {0} actual-name/Core-level/effect/route cases; textWidth={1:0.###}; minimumWidthSlack={2:0.###}; tightest={3}; Microsoft YaHei GDI+ budgets only. Unity and Android actual fonts require publisher verification.", textCases, minimumTextWidth, minimumTextSlack, tightestText);
    }
    private static int Main(string[] args)
    {
        try
        {
            foreach (var d in new[] {
                new[] {1440f,900f,0f,0f,1440f,900f,0f}, new[] {1280f,720f,0f,0f,1280f,720f,0f},
                new[] {1920f,1080f,0f,0f,1920f,1080f,0f}, new[] {1440f,1080f,0f,0f,1440f,1080f,0f},
                new[] {1600f,720f,56f,18f,1488f,684f,1f}, new[] {1600f,720f,56f,24f,1504f,684f,1f},
                new[] {1280f,720f,0f,0f,1280f,720f,1f}, new[] {2400f,1080f,80f,20f,2240f,1040f,1f},
                new[] {1920f,1200f,0f,0f,1920f,1200f,1f}, new[] {2048f,1536f,0f,0f,2048f,1536f,1f}
            }) Screen(d);
            Check(CityFacilityAtlasLayout.Columns(1459)==2 && CityFacilityAtlasLayout.Columns(1460)==3,
                "three-column breakpoint preserves the inspected minimum text budget");
            Nodes(1459,486,CityCatalog.Buildings.Length,"below three-column boundary");
            Nodes(1460,486,CityCatalog.Buildings.Length,"at three-column boundary");
            TextBudgets(args[0]);
            Console.WriteLine("CITY_FACILITY_ATLAS_CHECKS_OK: {0} assertions; 10 desktop/mobile layouts plus column boundaries; complete 216x162 art, independent labels/actions, two rows, final-row reachability and live Core text budgets. Pure checks only; no Unity, desktop, input or visual acceptance.", checks);
            return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
