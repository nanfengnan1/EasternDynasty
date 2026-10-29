using System;
using EasternDynasty;

internal static class CityDockLayoutChecks
{
    private static int checks;
    private static void Check(bool pass, string reason)
    { checks++; if (!pass) throw new Exception("City dock: " + reason); }
    private static void Near(float value, float expected, string reason)
    { Check(Math.Abs(value - expected) < .02f, reason); }
    private static bool Contains(CityPanoramaBounds parent, CityPanoramaBounds child)
    { return child.Width > 0 && child.Height > 0 && child.X >= parent.X - .02f && child.Y >= parent.Y - .02f
        && child.X + child.Width <= parent.X + parent.Width + .02f && child.Y + child.Height <= parent.Y + parent.Height + .02f; }
    private static bool Separate(CityPanoramaBounds a, CityPanoramaBounds b)
    { return a.X + a.Width <= b.X || b.X + b.Width <= a.X || a.Y + a.Height <= b.Y || b.Y + b.Height <= a.Y; }
    private static bool At(CityPanoramaBounds area, float x, float y)
    { return x >= area.X && y >= area.Y && x < area.X + area.Width && y < area.Y + area.Height; }
    private static int Command(float width, float height, float x, float y)
    {
        for (int i = 0; i < 5; i++) if (At(CityOverviewLayout.Navigation(width, height, i), x, y)) return i;
        return At(CityOverviewLayout.Back(width, height), x, y) ? 5 : -1;
    }
    private static void RegionRoutes(float width, float height, CityPanoramaBounds region, int command)
    {
        // All four visible region corners and its centre must reach the parent
        // command. This is pure hit geometry, not a claim of pointer execution.
        foreach (float x in new[] { region.X + .1f, region.X + region.Width / 2, region.X + region.Width - .1f })
            foreach (float y in new[] { region.Y + .1f, region.Y + region.Height / 2, region.Y + region.Height - .1f })
                Check(Command(width, height, x, y) == command, "visible image/caption cannot route into a neighbouring page");
    }
    private static void Layout(float width, float height)
    {
        var screen = new CityPanoramaBounds { Width = width, Height = height };
        var dock = CityOverviewLayout.Dock(width, height);
        var content = CityOverviewLayout.Content(width, height);
        Check(Contains(screen, dock) && Contains(screen, content) && Separate(dock, content), "dock and city content stay separate inside the screen");
        Near(dock.X, 24, "original dock left edge"); Near(dock.Y, 82, "original dock top");
        Near(dock.Width, 92, "original dock width"); Near(dock.Height, height - 180, "original dock height");
        Near(content.X, 128, "panorama left edge unchanged"); Near(content.Y, dock.Y, "panorama top unchanged");
        Near(content.Width, width - 152, "panorama width unchanged"); Near(content.Height, dock.Height, "panorama full height unchanged");
        var controls = new CityPanoramaBounds[6];
        for (int i = 0; i < 5; i++)
        {
            var parent = CityOverviewLayout.Navigation(width, height, i); controls[i] = parent;
            var art = CityDockLayout.Art(parent); var caption = CityDockLayout.Caption(parent);
            Check(Contains(dock, parent) && Contains(parent, art) && Contains(parent, caption), "complete floating seal and caption fit their parent");
            Check(Separate(art, caption) && parent.Width >= 44 && parent.Height >= 44, "separate visible image/text keep a full touch action");
            Near(parent.X, dock.X + 4, "navigation dock inset"); Near(parent.Y, dock.Y + 8 + i * 90, "reviewed vertical rhythm");
            Near(parent.Width, 84, "reviewed touch width"); Near(parent.Height, 86, "reviewed touch height");
            Near(art.X, parent.X + 6, "art horizontal inset"); Near(art.Y, parent.Y + 2, "art top inset");
            Near(art.Width, 72, "complete illustration width"); Near(art.Height, 54, "complete illustration height");
            Near(caption.X, parent.X + 2, "caption horizontal inset"); Near(caption.Y, parent.Y + 60, "caption independent baseline");
            Near(caption.Width, 80, "readable caption width"); Near(caption.Height, 24, "readable caption height");
            RegionRoutes(width, height, art, i); RegionRoutes(width, height, caption, i);
        }
        var back = CityOverviewLayout.Back(width, height); controls[5] = back;
        var backArt = CityDockLayout.BackArt(back); var backCaption = CityDockLayout.BackCaption(back);
        Check(Contains(dock, back) && Contains(back, backArt) && Contains(back, backCaption) && Separate(backArt, backCaption), "bottom return art and text remain separate inside the touch parent");
        Near(back.X, dock.X + 4, "original return left inset"); Near(back.Y, dock.Y + dock.Height - 72, "original return bottom anchor");
        Near(back.Width, 84, "original return width"); Near(back.Height, 64, "original return height");
        Near(backArt.X, back.X + back.Width / 2 - 16, "return icon centred"); Near(backArt.Y, back.Y + 6, "return icon top inset");
        Near(backArt.Width, 32, "return icon width"); Near(backArt.Height, 32, "return icon height");
        Near(backCaption.X, back.X + 2, "return caption inset"); Near(backCaption.Y, back.Y + 42, "return caption baseline");
        Near(backCaption.Width, back.Width - 4, "return caption width"); Near(backCaption.Height, 22, "return caption height");
        RegionRoutes(width, height, backArt, 5); RegionRoutes(width, height, backCaption, 5);
        Check(back.Y > controls[4].Y + controls[4].Height, "last page action does not crowd the bottom return");
        for (int i = 0; i < controls.Length; i++)
        {
            for (int j = i + 1; j < controls.Length; j++) Check(Separate(controls[i], controls[j]), "commands never overlap");
            Check(Command(width, height, dock.X + 1, controls[i].Y + 1) == -1, "outer dock gutter is not a hidden command");
        }
        for (int i = 0; i < 4; i++)
            Check(Command(width, height, controls[i].X + controls[i].Width / 2, controls[i].Y + controls[i].Height + 2) == -1,
                "inter-seal spacing does not select a neighbour");
        Check(Command(width, height, content.X + 1, content.Y + 1) == -1, "city scene is outside dock navigation");
    }
    private static void Screen(float[] screen)
    {
        var layout = MobileScreenLayout.Calculate(screen[0], screen[1], screen[2], screen[3], screen[4], screen[5], screen[6] > 0);
        Layout(layout.LogicalWidth, layout.LogicalHeight);
        var safe = new CityPanoramaBounds { X = layout.X, Y = layout.Y, Width = layout.Width, Height = layout.Height };
        for (int i = 0; i < 6; i++)
        {
            var action = i == 5 ? CityOverviewLayout.Back(layout.LogicalWidth, layout.LogicalHeight) : CityOverviewLayout.Navigation(layout.LogicalWidth, layout.LogicalHeight, i);
            var pixels = new CityPanoramaBounds { X = layout.X + action.X * layout.Scale, Y = layout.Y + action.Y * layout.Scale,
                Width = action.Width * layout.Scale, Height = action.Height * layout.Scale };
            Check(Contains(safe, pixels), "entire touch action lies in actual desktop/phone safe area");
        }
        Console.WriteLine("CITY_DOCK_LAYOUT screen={0}x{1} mobile={2} logical={3:0.##}x{4:0.##}", screen[0], screen[1], screen[6] > 0, layout.LogicalWidth, layout.LogicalHeight);
    }
    private static void Main()
    {
        Check(CityDockLayout.Style == "inkscape-city-dock-v1", "professional design identity");
        // Include the design's 540-high dock (720 logical screen) as well as
        // the supported phone layout's 820 minimum logical screen height.
        for (float height = 720; height <= 1000; height += 2) Layout(1440, height);
        foreach (var screen in new[] {
            new[] {1440f,900f,0f,0f,1440f,900f,0f}, new[] {1280f,720f,0f,0f,1280f,720f,0f},
            new[] {1920f,1080f,0f,0f,1920f,1080f,0f}, new[] {640f,480f,0f,0f,640f,480f,0f},
            new[] {1600f,720f,56f,18f,1488f,684f,1f}, new[] {1280f,720f,0f,0f,1280f,720f,1f},
            new[] {2400f,1080f,80f,20f,2240f,1040f,1f}, new[] {1920f,1200f,0f,0f,1920f,1200f,1f},
            new[] {1440f,1080f,0f,0f,1440f,1080f,0f}, new[] {2048f,1536f,0f,0f,2048f,1536f,1f},
            new[] {2960f,1440f,120f,36f,2720f,1368f,1f} }) Screen(screen);
        foreach (int index in new[] { -1, 5 })
        {
            bool rejected = false;
            try { CityOverviewLayout.Navigation(1440, 820, index); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "invalid navigation cannot become a return command or spill into scenery");
        }
        Console.WriteLine("City dock layout checks passed: " + checks + "; pure geometry only; actual fonts, images, input and visual quality remain publisher checks.");
    }
}
