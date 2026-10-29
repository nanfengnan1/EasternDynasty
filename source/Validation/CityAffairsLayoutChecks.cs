using System;
using EasternDynasty;

internal static class CityAffairsLayoutChecks
{
    private static int checks;
    private static void Check(bool pass, string message)
    { if (!pass) throw new Exception(message); checks++; }
    private static void Near(float actual, float expected, string message)
    { Check(Math.Abs(actual - expected) < .002f, message); }
    private static bool Contains(CityPanoramaBounds outer, CityPanoramaBounds inner)
    {
        return inner.Width > 0 && inner.Height > 0 && inner.X >= outer.X - .002f && inner.Y >= outer.Y - .002f
            && inner.X + inner.Width <= outer.X + outer.Width + .002f
            && inner.Y + inner.Height <= outer.Y + outer.Height + .002f;
    }
    private static bool Separated(CityPanoramaBounds a, CityPanoramaBounds b)
    {
        return a.X + a.Width <= b.X + .002f || b.X + b.Width <= a.X + .002f
            || a.Y + a.Height <= b.Y + .002f || b.Y + b.Height <= a.Y + .002f;
    }
    private static bool At(CityPanoramaBounds a, float x, float y)
    { return x >= a.X && x < a.X + a.Width && y >= a.Y && y < a.Y + a.Height; }
    private static int Command(float width, float x, float y)
    {
        var controls = new[] { CityAffairsLayout.Seal(width), CityAffairsLayout.Action(width, 0),
            CityAffairsLayout.Action(width, 1), CityAffairsLayout.Quest(width) };
        for (int i = 0; i < controls.Length; i++) if (At(controls[i], x, y)) return i;
        return -1;
    }
    private static void Layout(float width)
    {
        var bounds = new CityPanoramaBounds { Width = width, Height = 218 };
        var seal = CityAffairsLayout.Seal(width);
        var art = CityAffairsLayout.Building(width);
        var title = CityAffairsLayout.SealCaption(width);
        var summary = CityAffairsLayout.Summary(width);
        var quest = CityAffairsLayout.Quest(width);
        var questArt = CityAffairsLayout.QuestArt(width);
        var questTitle = CityAffairsLayout.QuestCaption(width);
        Check(Contains(bounds, seal) && Contains(seal, art) && Contains(seal, title), "complete building and caption stay in the seal");
        Check(Contains(bounds, summary) && Separated(seal, summary) && Separated(art, title), "independent building, title and summary never overlap");
        Near(seal.Width, 112, "reviewed seal width"); Near(seal.Height, 112, "reviewed seal height");
        Near(art.Width, 104, "reviewed building window width"); Near(art.Height, 78, "reviewed building window height");
        Check(Contains(quest, questArt) && Contains(quest, questTitle) && Separated(questArt, questTitle), "task image and caption fit separate visible regions");
        Near(quest.Height, 44, "task entry keeps 44-unit touch height");
        var controls = new[] { seal, CityAffairsLayout.Action(width, 0), CityAffairsLayout.Action(width, 1), quest };
        for (int i = 0; i < controls.Length; i++)
        {
            Check(Contains(bounds, controls[i]) && controls[i].Width >= 44 && controls[i].Height >= 44,
                "every complete touch control stays inside original affairs panel");
            for (int j = i + 1; j < controls.Length; j++) Check(Separated(controls[i], controls[j]), "commands cannot overlap");
            Check(Command(width, controls[i].X + controls[i].Width / 2, controls[i].Y + controls[i].Height / 2) == i,
                "visible command routes to its own parent");
        }
        foreach (var region in new[] { art, title })
            Check(Command(width, region.X + region.Width / 2, region.Y + region.Height / 2) == 0,
                "building and caption share the same facility command");
        foreach (var region in new[] { questArt, questTitle })
            Check(Command(width, region.X + region.Width / 2, region.Y + region.Height / 2) == 3,
                "task image and caption share the same chapter command");
        Check(Command(width, summary.X + summary.Width / 2, summary.Y + summary.Height / 2) == -1,
            "factual summary is not a hidden command");
        Check(Command(width, width / 2, 164) == -1 && Command(width, width / 2, 216) == -1,
            "inter-row and bottom gaps do not trigger a command");
        Near(controls[1].Height, 44, "manage facility remains touchable");
        Near(controls[2].Height, 44, "construction remains touchable");
        Near(quest.Y + quest.Height, 212, "task clears original 218-unit footprint");
    }
    private static void Screen(float[] screen)
    {
        var layout = MobileScreenLayout.Calculate(screen[0], screen[1], screen[2], screen[3], screen[4], screen[5], screen[6] > 0);
        var content = CityOverviewLayout.Content(layout.LogicalWidth, layout.LogicalHeight);
        float width = CityOverviewLayout.ArmyRailWidth(content.Height);
        Layout(width);
        var panel = new CityPanoramaBounds { X = content.X + content.Width - width, Y = content.Y, Width = width, Height = 218 };
        Check(Contains(content, panel), "affairs fits actual city content");
        var safe = new CityPanoramaBounds { X = layout.X, Y = layout.Y, Width = layout.Width, Height = layout.Height };
        var pixels = new CityPanoramaBounds { X = layout.X + panel.X * layout.Scale, Y = layout.Y + panel.Y * layout.Scale,
            Width = width * layout.Scale, Height = panel.Height * layout.Scale };
        Check(Contains(safe, pixels), "complete affairs panel stays in device safe area");
        Console.WriteLine("CITY_AFFAIRS_LAYOUT screen={0}x{1} mobile={2} width={3:0.##}", screen[0], screen[1], screen[6] > 0, width);
    }
    private static void Main()
    {
        Check(CityAffairsLayout.Style == "inkscape-city-affairs-v1", "professional design version");
        for (float width = 342; width <= 406; width += .5f) Layout(width);
        foreach (var screen in new[] {
            new[] {1440f,900f,0f,0f,1440f,900f,0f}, new[] {1280f,720f,0f,0f,1280f,720f,0f},
            new[] {1920f,1080f,0f,0f,1920f,1080f,0f}, new[] {1600f,720f,56f,18f,1488f,684f,1f},
            new[] {1280f,720f,0f,0f,1280f,720f,1f}, new[] {2400f,1080f,80f,20f,2240f,1040f,1f},
            new[] {1920f,1200f,0f,0f,1920f,1200f,1f}, new[] {1440f,1080f,0f,0f,1440f,1080f,0f},
            new[] {2048f,1536f,0f,0f,2048f,1536f,1f} }) Screen(screen);
        foreach (int index in new[] { -1, 2 })
        {
            bool rejected = false;
            try { CityAffairsLayout.Action(342, index); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid action index cannot spill into the chapter entry");
        }
        Console.WriteLine("City affairs layout checks passed: " + checks + "; geometry only; real font, input and screenshots remain publisher checks.");
    }
}
