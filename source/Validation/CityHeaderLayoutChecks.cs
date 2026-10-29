using System;
using EasternDynasty;

internal static class CityHeaderLayoutChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }
    private static bool Contains(MapHudNavigationLayout.Box parent, MapHudNavigationLayout.Box child)
    { return child.W > 0 && child.H > 0 && child.X >= parent.X - .01f && child.Y >= parent.Y - .01f && child.X + child.W <= parent.X + parent.W + .01f && child.Y + child.H <= parent.Y + parent.H + .01f; }
    private static bool Overlaps(MapHudNavigationLayout.Box a, MapHudNavigationLayout.Box b)
    { return a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H; }
    private static bool Equal(MapHudNavigationLayout.Box a, MapHudNavigationLayout.Box b)
    { return Math.Abs(a.X-b.X)<.01f && Math.Abs(a.Y-b.Y)<.01f && Math.Abs(a.W-b.W)<.01f && Math.Abs(a.H-b.H)<.01f; }
    private static void Exact(MapHudNavigationLayout.Box actual,float x,float y,float w,float h,string message)
    { Check(Equal(actual,new MapHudNavigationLayout.Box(x,y,w,h)),message); }
    private static void Layout(float sw, float sh, float sx, float sy, float safeW, float safeH, bool mobile)
    {
        var screen = MobileScreenLayout.Calculate(sw, sh, sx, sy, safeW, safeH, mobile);
        var header = new MapHudNavigationLayout.Box(0, 0, screen.LogicalWidth, 70);
        Check(Equal(CityHeaderLayout.Backdrop(screen.LogicalWidth),header),"Opaque city backdrop spans every header pixel at the actual logical width.");
        var identity = CityHeaderLayout.Identity;
        Exact(identity,16,4,246,62,"City must use the inspected compact identity, rejecting the obsolete 252-wide plate.");
        Exact(CityHeaderLayout.Crest,16,4,62,62,"City crest is a complete square seal.");
        Exact(CityHeaderLayout.Art,22,13,50,44,"Complete town image uses the inspected inset, rejecting the oversized old art.");
        Exact(CityHeaderLayout.IdentityDetail,84,44,174,20,"Province and actual town level remain below the full name.");
        Check(Equal(identity,MapHudHeaderLayout.Identity) && Equal(CityHeaderLayout.Crest,MapHudHeaderLayout.Crest)
            && Equal(CityHeaderLayout.Art,MapHudHeaderLayout.IdentityArt),"Map and city share the inspected home-seal hierarchy.");
        Check(Contains(header, identity) && identity.H >= 44, "Actual home action must remain reachable inside the header.");
        Check(Contains(identity,CityHeaderLayout.Crest) && Contains(CityHeaderLayout.Crest,CityHeaderLayout.Art),"The complete town artwork must remain inside its seal.");
        Check(Contains(identity, CityHeaderLayout.Art), "Home image must remain inside its actual action.");
        foreach (bool wrapped in new[] { false, true })
        {
            var name = CityHeaderLayout.Name(wrapped);
            Exact(name,84,wrapped?5:7,174,wrapped?38:30,"Home title retains its measured short/long-name region.");
            Check(Equal(name,MapHudHeaderLayout.Ruler(wrapped)),"City and map titles use the same wrapped and single-line geometry.");
            Check(Contains(identity, name) && Contains(identity, CityHeaderLayout.IdentityDetail), "Complete title and province/level must fit the action.");
            Check(!Overlaps(name, CityHeaderLayout.Art) && !Overlaps(name, CityHeaderLayout.IdentityDetail), "Wrapped title must not obscure the image or level.");
            Check(name.W >= 160 && (!wrapped || name.H >= 36), "Long name must retain a real two-line area.");
        }
        var previous = identity;
        float pitch=Math.Min(168,(screen.LogicalWidth-604)/4);
        var productionLedger=CityHeaderLayout.Production(screen.LogicalWidth);
        Exact(productionLedger,280,5,pitch*4,58,"Four production columns share one continuous ledger.");
        Check(Contains(header,productionLedger) && !Overlaps(identity,productionLedger),"Continuous ledger and home identity remain separate in the safe header.");
        for (int i = 0; i < 6; i++)
        {
            var cell = CityHeaderLayout.Resource(screen.LogicalWidth, i);
            float x=i<4?280+i*pitch:280+4*pitch+16+(i-4)*150;
            Exact(cell,x,5,i<4?pitch-6:144,58,"Currency stays next to the production ledger, including wide screens.");
            Check(Equal(cell,MapHudHeaderLayout.Resource(screen.LogicalWidth,i)),"Map and city actual resource columns must align.");
            if(i<4) Check(Contains(productionLedger,cell),"Every production cell belongs to the same painted ledger.");
            Check(Contains(header, cell) && !Overlaps(previous, cell), "Stocks must not obscure the home or another stock.");
            var parts = new[] { CityHeaderLayout.Icon(cell, i), CityHeaderLayout.Stock(cell, i), CityHeaderLayout.Detail(cell, i) };
            Exact(parts[0],cell.X+(i<4?6:7),19,i<4?32:26,i<4?26:28,"Resource artwork retains its complete inspected proportions.");
            Exact(parts[1],cell.X+(i<4?44:40),30,i<4?cell.W-48:100,26,"All six stock values share the lower numeric baseline.");
            Exact(parts[2],cell.X+(i<4?44:40),6,i<4?cell.W-48:100,23,"All resource names and income share the upper detail row.");
            for (int p = 0; p < parts.Length; p++)
            {
                Check(Contains(cell, parts[p]), "Drawn icon/stock/income must remain visible inside the registered read-only cell.");
                for (int q = 0; q < p; q++) Check(!Overlaps(parts[p], parts[q]), "Icon and both text rows must be separate.");
            }
            Check(parts[1].W >= 100 && parts[2].W >= 100, "Stock/income text needs room at supported logical widths.");
            float px = screen.X + cell.X * screen.Scale, py = screen.Y + cell.Y * screen.Scale;
            Check(px >= screen.X && py >= screen.Y && px + cell.W * screen.Scale <= screen.X + screen.Width + .1f
                && py + cell.H * screen.Scale <= screen.Y + screen.Height + .1f, "Visible resource must stay inside the physical safe area.");
            previous = cell;
        }
        var production = CityHeaderLayout.Resource(screen.LogicalWidth, 3);
        var currency = CityHeaderLayout.Resource(screen.LogicalWidth, 4);
        Check(Math.Abs(currency.X-production.X-production.W-22)<.01f,"Production and currency use the inspected 22-unit gap, without a wide-screen detached currency group.");
    }
    public static void Main()
    {
        Check(CityHeaderLayout.Style=="inkscape-city-ledger-v2","City header must identify the inspected second ledger design.");
        Layout(1280, 720, 0, 0, 1280, 720, false);
        Layout(1440, 900, 0, 0, 1440, 900, false);
        Layout(1920, 1080, 0, 0, 1920, 1080, false);
        Layout(1024, 768, 0, 0, 1024, 768, false);
        Layout(3440, 1440, 0, 0, 3440, 1440, false);
        Layout(1600, 720, 56, 12, 1504, 696, true);
        Layout(2340, 1080, 84, 24, 2172, 1032, true);
        Layout(1280, 720, 40, 10, 1200, 700, true);
        Layout(1024, 768, 0, 20, 1024, 728, true);
        bool low = false, high = false;
        try { CityHeaderLayout.Resource(1440, -1); } catch (ArgumentOutOfRangeException) { low = true; }
        try { CityHeaderLayout.Resource(1440, 6); } catch (ArgumentOutOfRangeException) { high = true; }
        Check(low && high, "Only the six actual resource types may be drawn.");
        Console.WriteLine("PASS: " + checks + " city ledger exact geometry, map alignment, safe-area, complete-title and resource-layer checks across nine desktop/mobile layouts. Pixel clearing, runtime fonts and clicks remain publisher checks.");
    }
}
