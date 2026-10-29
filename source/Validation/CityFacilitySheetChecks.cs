using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EasternDynasty;
using Box = EasternDynasty.CityFacilitySheetLayout.Box;

internal static class CityFacilitySheetChecks
{
    private static int checks, fittingViews, scrollingViews;
    private static float narrowestInner = float.MaxValue;

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

    private static void Pairwise(Box[] parts, string context)
    {
        for (int i = 0; i < parts.Length; i++)
        for (int j = 0; j < i; j++)
            Check(Separate(parts[i], parts[j]), context + " overlap: " + i + "/" + j);
    }

    private static void Screen(float[] dimensions)
    {
        var screen = MobileScreenLayout.Calculate(dimensions[0], dimensions[1], dimensions[2], dimensions[3], dimensions[4], dimensions[5], dimensions[6] > 0);
        // The real CityPresentation parent uses this 66% detail panel beside
        // the district portrait/grid; do not substitute an isolated full screen.
        float panelWidth = (screen.LogicalWidth - 48) * .66f;
        float panelHeight = screen.LogicalHeight - 240;
        float panelX = 24 + screen.LogicalWidth - 48 - panelWidth, panelY = 142;
        var panel = new Box(0, 0, panelWidth, panelHeight);
        float inner = CityFacilitySheetLayout.InnerWidth(panelWidth);
        narrowestInner = Math.Min(narrowestInner, inner);
        var content = new Box(0, 0, inner, CityFacilitySheetLayout.ContentHeight);

        foreach (string state in new[] { "empty-eight-options", "built-upgrade", "construction", "maximum-level" })
        {
            bool options = state == "empty-eight-options";
            bool maximum = state == "maximum-level";
            var view = CityFacilitySheetLayout.View(panelWidth, panelHeight, options);
            var action = CityFacilitySheetLayout.Action(panelWidth, panelHeight);
            var reason = new Box(20, panelHeight - 104, panelWidth - 40, 34);
            Check(Contains(panel, view) && Contains(panel, action) && Contains(panel, reason), state + " view, action and reason stay in their actual panel");
            Check(Separate(view, reason) && Separate(view, action) && Separate(reason, action), state + " scrolling body and fixed controls do not overlap");
            Check(action.W >= 44 && action.H >= 44, state + " fixed action retains at least 44 logical units");
            Check(inner + 20 <= view.W, state + " body retains space for the actual scroll gutter");

            var physical = new Box(screen.X + (panelX + action.X) * screen.Scale, screen.Y + (panelY + action.Y) * screen.Scale,
                action.W * screen.Scale, action.H * screen.Scale);
            Check(Contains(new Box(screen.X, screen.Y, screen.Width, screen.Height), physical), state + " fixed action remains inside physical desktop/mobile safe area");

            if (options)
            {
                var selectors = Enumerable.Range(0, 8).Select(i => CityFacilitySheetLayout.Options(panelWidth, i)).ToArray();
                Pairwise(selectors, "eight actual facility selectors");
                foreach (var option in selectors)
                {
                    Check(Contains(panel, option) && Separate(option, view) && Separate(option, reason) && Separate(option, action), "facility selector clears the scroll body and fixed command");
                    Check(option.W >= 44 && option.H >= 44, "every facility selector remains a 44-unit logical touch target");
                    var safeOption = new Box(screen.X + (panelX + option.X) * screen.Scale, screen.Y + (panelY + option.Y) * screen.Scale,
                        option.W * screen.Scale, option.H * screen.Scale);
                    Check(Contains(new Box(screen.X, screen.Y, screen.Width, screen.Height), safeOption), "every facility selector remains in the physical safe area");
                }
            }

            var description = CityFacilitySheetLayout.Description(inner);
            var effect = CityFacilitySheetLayout.Effect(inner);
            var costs = CityFacilitySheetLayout.Costs(inner);
            var requirement = CityFacilitySheetLayout.Requirement(inner);
            var duration = CityFacilitySheetLayout.Duration(inner);
            var note = CityFacilitySheetLayout.Note(inner);
            var sections = new[] { description, effect, costs, requirement, duration, note };
            foreach (var section in sections) Check(Contains(content, section), state + " every content block has real scrollable extent");
            Pairwise(sections, state + " description/effect/cost/requirement/duration/note");
            var parts = new[] { CityFacilitySheetLayout.Unit(inner), CityFacilitySheetLayout.Current(inner), CityFacilitySheetLayout.Arrow(inner), CityFacilitySheetLayout.Next(inner) };
            foreach (var part in parts) Check(Contains(effect, part), state + " effect unit, current value, arrow and next value stay inside the horizontal effect band");
            Pairwise(parts, state + " horizontal effect fields");
            Check(parts[0].W >= 240 && parts[1].W >= 140 && parts[3].W >= 170, "long project effect unit and both numeric columns retain text width");
            Check(parts[2].W >= 36 && parts[2].H >= 24, "exported directional symbol retains its dedicated region");
            // The two value columns each need an independent heading and value
            // row; the Runtime uses these within Current/Next's 64-unit boxes.
            foreach (var column in new[] { parts[1], parts[3] })
            {
                var caption = new Box(column.X, column.Y, column.W, 22);
                var number = new Box(column.X, column.Y + 24, column.W, 40);
                Check(Contains(column, caption) && Contains(column, number) && Separate(caption, number), "effect level caption does not overlap its actual value");
            }

            if (!maximum)
            {
                var columns = Enumerable.Range(0, 5).Select(i => CityFacilitySheetLayout.Cost(inner, i)).ToArray();
                Pairwise(columns, state + " all five project resource columns");
                foreach (var cost in columns)
                {
                    Check(Contains(costs, cost), "each project cost is inside the costs band");
                    Check(Contains(new Box(0, 0, inner, view.H), cost), state + " all five costs are completely visible before scrolling");
                    Check(cost.W >= 160 && cost.H >= 88, "cost icon, amount, stock/paid and deficit/refund retain distinct vertical space");
                }
            }

            float scrollLimit = Math.Max(0, CityFacilitySheetLayout.ContentHeight - view.H);
            if (!options) Check(CityFacilitySheetLayout.ContentHeight <= view.H + .002f, state + " complete built/working/maximum sheet fits its first viewport");
            if (scrollLimit > 0) scrollingViews++; else fittingViews++;
            Check(note.Y + note.H - scrollLimit <= view.H + .002f, state + " last project note is fully reachable by scrolling");
            Check(Separate(Offset(new Box(0, 0, inner, Math.Min(view.H, CityFacilitySheetLayout.ContentHeight)), view.X, view.Y), action), "visible body cannot intrude into the fixed action at either scroll end");
            Check(requirement.Y + requirement.H - scrollLimit <= view.H + .002f, state + " prerequisite information remains reachable");
        }
        Console.WriteLine("CITY_FACILITY_SHEET_SCREEN: {0}x{1}; mobile={2}; logical={3:0.##}x{4:0.##}; inner={5:0.##}; emptyViewport={6:0.##}; builtViewport={7:0.##}",
            dimensions[0], dimensions[1], dimensions[6] > 0, screen.LogicalWidth, screen.LogicalHeight, inner,
            CityFacilitySheetLayout.View(panelWidth, panelHeight, true).H, CityFacilitySheetLayout.View(panelWidth, panelHeight, false).H);
    }

    private static void TextFits(Graphics graphics, StringFormat format, string family, string text, float width, float height, int fontSize, string context, int minimum = -1, bool wrap = false)
    {
        if (minimum < 0) minimum = fontSize;
        SizeF measured = SizeF.Empty;
        using (var actualFormat = new StringFormat(format))
        {
            if (!wrap) actualFormat.FormatFlags |= StringFormatFlags.NoWrap;
            for (int size = fontSize; size >= minimum; size--)
            using (var font = new Font(family, size, FontStyle.Regular, GraphicsUnit.Pixel))
            {
                measured = graphics.MeasureString(text, font, new SizeF(width, 10000), actualFormat);
                if (measured.Width <= width + .5f && measured.Height <= height + .5f) break;
            }
            Check(measured.Width <= width + .5f && measured.Height <= height + .5f,
                context + " exceeds " + width + "x" + height + " with " + family + ": " + text + " = " + measured);
        }
    }

    private static void TextBudgets(string root)
    {
        string catalog = File.ReadAllText(Path.Combine(root, "Assets/EasternDynasty/Scripts/Core/CityModels.cs"));
        var definitions = Regex.Matches(catalog, "new CityBuildingDefinition\\(CityBuilding\\.\\w+,\"([^\"]+)\",\"([^\"]+)\"").Cast<Match>().ToArray();
        Check(definitions.Length == 9, "text budget reads all nine actual facility catalog descriptions");
        var description = CityFacilitySheetLayout.Description(narrowestInner);
        var unit = CityFacilitySheetLayout.Unit(narrowestInner);
        var current = CityFacilitySheetLayout.Current(narrowestInner);
        var next = CityFacilitySheetLayout.Next(narrowestInner);
        var cost = CityFacilitySheetLayout.Cost(narrowestInner, 0);
        using (var bitmap = new Bitmap(1, 1))
        using (var graphics = Graphics.FromImage(bitmap))
        using (var format = new StringFormat(StringFormat.GenericTypographic))
        foreach (string family in new[] { "Microsoft YaHei", "SimHei", "Arial" })
        {
            foreach (var definition in definitions)
            {
                TextFits(graphics, format, family, definition.Groups[2].Value, description.W, description.H, 18, "actual description", 13, true);
                TextFits(graphics, format, family, definition.Groups[1].Value, 98, 23, 15, "actual selector name");
            }
            TextFits(graphics, format, family, "项目校场提供兵力加成；本城仍为一支部队。", description.W, description.H, 18, "actual drill-ground override", 13, true);
            foreach (string text in new[] { "级 · 设施等级上限", "粮食 / 小时", "木材 / 小时", "石料 / 小时", "铁矿 / 小时", "金币 / 小时", "% · 新建设工期缩短", "· 部队带兵上限加成" })
                TextFits(graphics, format, family, text, unit.W, unit.H, 18, "effect unit", 13);
            foreach (string text in new[] { "当前 Lv. 10", "完工 Lv. 10", "最高等级" })
                TextFits(graphics, format, family, text, Math.Min(current.W, next.W), 22, 14, "effect level caption", 13);
            foreach (string text in new[] { "0", "40", "2500", "999999999", "—" })
                TextFits(graphics, format, family, text, Math.Min(current.W, next.W), 40, 28, "effect fitted numeric budget", 13);
            // Runtime displays cost / stock in one line. Current valid facility
            // prices are at most five digits; keep a 12-digit stock stress case.
            foreach (string text in new[] { "999999999999.99", "99999 / 999999999999", "0 / 0", "99999 / 99999" })
                TextFits(graphics, format, family, text, cost.W - 16, 28, 17, "actual combined cost/stock numeric budget", 12);
            foreach (string text in new[] { "已付 · 返 99999", "已付 · 返 999999999999", "缺 999999999999", "缺 99999", "充足" })
                TextFits(graphics, format, family, text, cost.W - 16, 24, 14, "actual paid/refund/status budget", 12);
            foreach (string text in new[] { "中断施工 · 查看返还明细", "前往城主府 · 需要 8 级", "前往伐木场 · Lv. 10", "已达最高等级", "4分00秒 / 开始建设", "4分00秒 / 升级设施" })
                TextFits(graphics, format, family, text, 344, 42, 18, "fixed action title", 13);
        }
        Console.WriteLine("CITY_FACILITY_SHEET_FONT_BUDGETS: 9 actual catalog names/descriptions; effect units; 12-digit resources and fixed actions; 3 GDI+ font families. Runtime GUIStyle font fit remains a publisher check.");
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
        Check(fittingViews > 0 && scrollingViews > 0, "exercise genuinely fitting and scrolling bodies without demanding fake overflow");
        TextBudgets(args[0]);
        Console.WriteLine("CITY_FACILITY_SHEET_CHECKS_OK: {0} assertions; 12 desktop/mobile layouts x 4 states; fitting={1}; scrolling={2}. Pure layout/font-budget checks only; actual screenshots, Chinese GUIStyle metrics and image/text clicks require the publisher.", checks, fittingViews, scrollingViews);
    }
}
