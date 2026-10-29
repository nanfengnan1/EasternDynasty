using System;

namespace EasternDynasty
{
    // Geometry of the inspected city-marker SVG. Artwork is exported by Inkscape.
    internal static class MapCityLabelLayout
    {
        internal const string Style="youzhou-city-marker-v1";
        internal static MapHudNavigationLayout.Box Panel(float anchorX,float anchorY,float measuredNameWidth)
        {
            float width=Math.Max(184,Math.Min(304,measuredNameWidth+70));
            float height=measuredNameWidth>width-70?72:52;
            return new MapHudNavigationLayout.Box(anchorX-width/2,anchorY-height-12,width,height);
        }
        internal static MapHudNavigationLayout.Box Crest(MapHudNavigationLayout.Box p)
        {return new MapHudNavigationLayout.Box(p.X,p.Y+(p.H-52)/2,52,52);}
        internal static MapHudNavigationLayout.Box Image(MapHudNavigationLayout.Box p)
        {return new MapHudNavigationLayout.Box(p.X+6,p.Y+(p.H-52)/2+12,40,28);}
        internal static MapHudNavigationLayout.Box Name(MapHudNavigationLayout.Box p)
        {return new MapHudNavigationLayout.Box(p.X+60,p.Y+2,p.W-70,p.H==72?44:24);}
        internal static MapHudNavigationLayout.Box Detail(MapHudNavigationLayout.Box p)
        {return new MapHudNavigationLayout.Box(p.X+60,p.Y+p.H-24,p.W-70,22);}
        internal static MapHudNavigationLayout.Box Ribbon(MapHudNavigationLayout.Box p)
        {return new MapHudNavigationLayout.Box(p.X+44,p.Y,p.W-44,p.H);}
    }
}
