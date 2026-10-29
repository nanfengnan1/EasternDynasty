namespace EasternDynasty
{
    // Inspected Inkscape layout. Map pixels and camera geometry remain live.
    internal static class MapHudAtlasLayout
    {
        internal const string Style = "youzhou-atlas-sidebar-v3";
        internal const float TerrainOpacity = .88f;
        internal static MapHudNavigationLayout.Box Panel(float width)
        { return new MapHudNavigationLayout.Box(width-302,90,282,204); }
        internal static MapHudNavigationLayout.Box Title(MapHudNavigationLayout.Box panel)
        { return new MapHudNavigationLayout.Box(panel.X+210,panel.Y,72,54); }
        internal static MapHudNavigationLayout.Box Locate(MapHudNavigationLayout.Box panel)
        { return new MapHudNavigationLayout.Box(panel.X+216,panel.Y+64,60,60); }
        internal static MapHudNavigationLayout.Box Pin(MapHudNavigationLayout.Box panel)
        { return new MapHudNavigationLayout.Box(panel.X+236,panel.Y+70,20,28); }
        internal static MapHudNavigationLayout.Box Caption(MapHudNavigationLayout.Box panel)
        { return new MapHudNavigationLayout.Box(panel.X+216,panel.Y+101,60,20); }
        internal static MapHudNavigationLayout.Box Terrain(MapHudNavigationLayout.Box panel)
        { return new MapHudNavigationLayout.Box(panel.X,panel.Y,204,204); }
        internal static MapHudNavigationLayout.Box Legend(MapHudNavigationLayout.Box panel)
        { return new MapHudNavigationLayout.Box(panel.X+210,panel.Y+134,72,70); }
    }
}
