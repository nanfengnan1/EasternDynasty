using System;

namespace EasternDynasty
{
    // Inspected city ledger, s1-20261028-204251299. The city backdrop is opaque;
    // all live resource positions intentionally match the shared map ledger.
    internal static class CityHeaderLayout
    {
        internal const string Style = "inkscape-city-ledger-v2";
        internal static MapHudNavigationLayout.Box Backdrop(float width) { return new MapHudNavigationLayout.Box(0, 0, width, 70); }
        internal static MapHudNavigationLayout.Box Identity { get { return MapHudHeaderLayout.Identity; } }
        internal static MapHudNavigationLayout.Box Crest { get { return MapHudHeaderLayout.Crest; } }
        internal static MapHudNavigationLayout.Box Art { get { return MapHudHeaderLayout.IdentityArt; } }
        internal static MapHudNavigationLayout.Box Name(bool wrapped)
        { return MapHudHeaderLayout.Ruler(wrapped); }
        internal static MapHudNavigationLayout.Box IdentityDetail { get { return MapHudHeaderLayout.IdentityDetail(false); } }
        internal static MapHudNavigationLayout.Box Production(float width) { return MapHudHeaderLayout.Production(width); }
        internal static MapHudNavigationLayout.Box Resource(float width, int index)
        {
            return MapHudHeaderLayout.Resource(width, index);
        }
        internal static MapHudNavigationLayout.Box Icon(MapHudNavigationLayout.Box cell, int index)
        { return MapHudHeaderLayout.Icon(cell, index); }
        internal static MapHudNavigationLayout.Box Stock(MapHudNavigationLayout.Box cell, int index)
        { return MapHudHeaderLayout.Stock(cell, index); }
        internal static MapHudNavigationLayout.Box Detail(MapHudNavigationLayout.Box cell, int index)
        { return MapHudHeaderLayout.Detail(cell, index); }
    }
}
