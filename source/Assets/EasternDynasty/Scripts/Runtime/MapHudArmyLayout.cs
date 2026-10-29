using System;

namespace EasternDynasty
{
    // Geometry of the inspected Inkscape design; no generated illustration code.
    internal static class MapHudArmyLayout
    {
        internal const string Style = "youzhou-army-rows-v3";
        internal static MapHudNavigationLayout.Box Area(float height)
        { return new MapHudNavigationLayout.Box(20,height-396,354,302); }
        internal static MapHudNavigationLayout.Box Header(MapHudNavigationLayout.Box area)
        { return new MapHudNavigationLayout.Box(area.X,area.Y,354,28); }
        internal static MapHudNavigationLayout.Box Card(MapHudNavigationLayout.Box area,int index)
        {
            if(index<0||index>2)throw new ArgumentOutOfRangeException("index");
            return new MapHudNavigationLayout.Box(area.X,area.Y+32+index*68,354,64);
        }
        internal static MapHudNavigationLayout.Box Art(MapHudNavigationLayout.Box card)
        { return new MapHudNavigationLayout.Box(card.X+4,card.Y+1,44,62); }
        internal static MapHudNavigationLayout.Box Name(MapHudNavigationLayout.Box card)
        { return new MapHudNavigationLayout.Box(card.X+62,card.Y+12,164,40); }
        internal static MapHudNavigationLayout.Box Identity(MapHudNavigationLayout.Box card)
        { return new MapHudNavigationLayout.Box(card.X+242,card.Y+12,102,40); }
        internal static MapHudNavigationLayout.Box Status(MapHudNavigationLayout.Box area)
        { return new MapHudNavigationLayout.Box(area.X,area.Y+236,354,20); }
        internal static MapHudNavigationLayout.Box Action(MapHudNavigationLayout.Box area,bool secondary)
        { return new MapHudNavigationLayout.Box(area.X+(secondary?183:0),area.Y+258,171,44); }
    }
}
