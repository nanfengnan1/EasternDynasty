using System;

namespace EasternDynasty
{
    // Geometry from the inspected Inkscape ledger, s1-20261028-193752561.
    internal static class MapHudHeaderLayout
    {
        internal const string Style="youzhou-reference-ledger-v2";
        internal static MapHudNavigationLayout.Box Identity {get{return new MapHudNavigationLayout.Box(16,4,246,62);}}
        internal static MapHudNavigationLayout.Box Crest {get{return new MapHudNavigationLayout.Box(16,4,62,62);}}
        internal static MapHudNavigationLayout.Box IdentityArt {get{return new MapHudNavigationLayout.Box(22,13,50,44);}}
        internal static MapHudNavigationLayout.Box Ruler(bool wrapped){return new MapHudNavigationLayout.Box(84,wrapped?5:7,174,wrapped?38:30);}
        internal static MapHudNavigationLayout.Box IdentityDetail(bool wrapped){return new MapHudNavigationLayout.Box(84,44,174,20);}
        internal static float ProductionPitch(float width){return Math.Min(168,(width-604)/4);}
        internal static MapHudNavigationLayout.Box Production(float width)
        {return new MapHudNavigationLayout.Box(280,5,ProductionPitch(width)*4,58);}
        internal static MapHudNavigationLayout.Box Resource(float width,int index)
        {
            if(index<0||index>5)throw new ArgumentOutOfRangeException("index");
            float pitch=ProductionPitch(width);
            return index<4?new MapHudNavigationLayout.Box(280+index*pitch,5,pitch-6,58)
                :new MapHudNavigationLayout.Box(280+pitch*4+16+(index-4)*150,5,144,58);
        }
        internal static MapHudNavigationLayout.Box Icon(MapHudNavigationLayout.Box b,int index)
        {return index<4?new MapHudNavigationLayout.Box(b.X+6,19,32,26):new MapHudNavigationLayout.Box(b.X+7,19,26,28);}
        internal static MapHudNavigationLayout.Box Stock(MapHudNavigationLayout.Box b,int index)
        {return index<4?new MapHudNavigationLayout.Box(b.X+44,30,b.W-48,26):new MapHudNavigationLayout.Box(b.X+40,30,100,26);}
        internal static MapHudNavigationLayout.Box Detail(MapHudNavigationLayout.Box b,int index)
        {return index<4?new MapHudNavigationLayout.Box(b.X+44,6,b.W-48,23):new MapHudNavigationLayout.Box(b.X+40,6,100,23);}
        internal static MapHudNavigationLayout.Box Camera(float width,float height)
        {return new MapHudNavigationLayout.Box(0,0,width,height-MapHudNavigationLayout.FooterHeight);}
        internal static MapHudNavigationLayout.Box Input(float width,float height)
        {return new MapHudNavigationLayout.Box(0,70,width,height-148);}
    }
}
