using System;

namespace EasternDynasty
{
    // Inspected Inkscape layout: Docs/DesignUI/s1-20261028-172751912/design-spec.json.
    // Pure geometry shared with checks; the artwork is exported by Inkscape.
    internal static class MapHudNavigationLayout
    {
        internal const string Style = "youzhou-reference-image-captions-v2";
        internal const string RecruitStyle = "youzhou-recruit-pedestal-v3";
        internal const float FooterHeight = 78;
        internal struct Box
        {
            internal float X,Y,W,H;
            internal Box(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;}
        }
        internal static Box Entry(float width,float height,int index)
        {
            if(index<0||index>5)throw new ArgumentOutOfRangeException("index");
            return index==5?new Box(width-152,height-74,132,70):new Box(20+116*index,height-74,108,70);
        }
        internal static Box Art(Box b,bool recruit){return recruit?new Box(b.X+46,b.Y+7,40,38):new Box(b.X+4,b.Y+3,100,42);}
        internal static Box Caption(Box b,bool recruit){return recruit?new Box(b.X+8,b.Y+47,116,22):new Box(b.X+4,b.Y+46,100,22);}
        // Inspected map-only recruitment pedestal: DesignUI/s1-20261028-225251901.
        // City pages and an open land menu retain the compact footer entry.
        internal static Box RecruitEntry(float width,float height){return new Box(width-180,height-144,160,140);}
        internal static Box RecruitArt(Box b){return new Box(b.X+50,b.Y+10,60,88);}
        internal static Box RecruitCaption(Box b){return new Box(b.X+10,b.Y+114,140,24);}
        internal static Box Utility(float width,float height,bool clock)
        {
            // The inspected s1-20261028-064751954 revision separates the real
            // controls by eight units, including their pixel-scaled review bounds.
            float x=Math.Max(620,(width-270)/2);
            return new Box(x+(clock?90:0),height-61,clock?180:82,49);
        }
    }
}
