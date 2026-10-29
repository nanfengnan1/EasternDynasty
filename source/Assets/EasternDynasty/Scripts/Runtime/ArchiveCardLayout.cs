using System;
namespace EasternDynasty
{
    // Geometry from the inspected Inkscape archive design; no Unity dependency.
    internal static class ArchiveCardLayout
    {
        internal struct Box
        {
            internal float X,Y,W,H;
            internal Box(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;}
        }
        internal const float Gap=12;
        internal static float Height(float viewHeight){return Math.Min(252,(viewHeight-Gap)/2);}
        internal static int Columns(float viewWidth,float viewHeight)
        {return Math.Max(1,(int)((viewWidth-24+Gap)/((Height(viewHeight)-52)/1.45f+38+Gap)));}
        internal static float Width(float viewWidth,float viewHeight)
        {int c=Columns(viewWidth,viewHeight);return Math.Min((Height(viewHeight)-52)/1.45f+38,(viewWidth-24-Gap*(c-1))/c);}
        internal static float Left(float viewWidth,float viewHeight)
        {int c=Columns(viewWidth,viewHeight);return Math.Max(0,(viewWidth-24-c*Width(viewWidth,viewHeight)-Gap*(c-1))/2);}
        internal static float ContentHeight(float viewHeight,int count,int columns)
        {return Math.Max(viewHeight,Math.Max(0,(count+columns-1)/columns)*(Height(viewHeight)+Gap)-Gap);}
        internal static float NameStep(float height,int length){return Math.Min(24,(height-112)/Math.Max(1,length));}
        internal static Box Art(float w,float h){return new Box(36,26,w-40,h-70);}
        internal static Box Name(float w,float h,int length){return new Box(4,30,28,NameStep(h,length)*length+8);}
        internal static Box Footer(float w,float h){return new Box(2,h-44,w-4,42);}
        internal static Box Faction(float w,float h){return new Box(4,4,28,24);}
        internal static Box Stars(float w,float h){return new Box(78,2,w-86,22);}
        internal static Box Cost(float w,float h){return new Box(39,2,36,22);}
        internal static Box Troop(float w,float h){return new Box(4,h-72,28,24);}
        internal static Box Skill(float w,float h){return new Box(10,h-43,w-20,22);}
        internal static Box Scope(float w,float h){return new Box(10,h-21,w-20,19);}
    }
}
