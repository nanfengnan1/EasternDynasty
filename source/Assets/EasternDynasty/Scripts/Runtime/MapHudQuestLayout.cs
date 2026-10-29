namespace EasternDynasty
{
    // Exact geometry of the inspected Inkscape design. No artwork is generated here.
    internal static class MapHudQuestLayout
    {
        internal const string Style = "youzhou-quest-sleeve-v4";
        internal static MapHudNavigationLayout.Box Panel { get { return new MapHudNavigationLayout.Box(20,90,354,118); } }
        internal static MapHudNavigationLayout.Box Banner { get { return new MapHudNavigationLayout.Box(20,90,80,68); } }
        internal static MapHudNavigationLayout.Box Open { get { return new MapHudNavigationLayout.Box(20,90,354,68); } }
        internal static MapHudNavigationLayout.Box Body { get { return new MapHudNavigationLayout.Box(110,90,264,68); } }
        internal static MapHudNavigationLayout.Box Chapter { get { return new MapHudNavigationLayout.Box(120,90,244,18); } }
        internal static MapHudNavigationLayout.Box Title { get { return new MapHudNavigationLayout.Box(120,108,244,24); } }
        internal static MapHudNavigationLayout.Box Objective { get { return new MapHudNavigationLayout.Box(120,132,244,26); } }
        internal static MapHudNavigationLayout.Box Progress { get { return new MapHudNavigationLayout.Box(20,164,212,44); } }
        internal static MapHudNavigationLayout.Box Action { get { return new MapHudNavigationLayout.Box(242,164,132,44); } }
    }
}
