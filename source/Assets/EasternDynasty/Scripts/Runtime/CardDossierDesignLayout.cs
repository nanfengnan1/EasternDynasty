using System;

namespace EasternDynasty
{
    // Layout of the inspected Inkscape information panel. Artwork is loaded by stable ID.
    internal static class CardDossierDesignLayout
    {
        internal const string Style = "inkscape-dossier-inline-v1";
        internal const float ContentHeight = 440;
        internal struct Box
        {
            internal float X, Y, W, H;
            internal Box(float x, float y, float w, float h) { X=x; Y=y; W=w; H=h; }
        }
        internal static Box Attributes(float width) { return new Box(8,0,width-16,312); }
        internal static Box Status(float width) { return new Box(8,62,width-16,38); }
        internal static Box Stat(float width, int index)
        {
            if(index<0||index>4)throw new ArgumentOutOfRangeException("index");
            float column=(width-28)/2;
            return new Box(8+index%2*(column+12),116+index/2*54,column,42);
        }
        internal static Box Icon(float width,int index) { var r=Stat(width,index);return new Box(r.X,r.Y+5,32,32); }
        internal static Box Label(float width,int index) { var r=Stat(width,index);return new Box(r.X+42,r.Y,44,42); }
        internal static Box Value(float width,int index) { var r=Stat(width,index);return new Box(r.X+90,r.Y,78,42); }
        internal static Box Growth(float width,int index) { var r=Stat(width,index);return new Box(r.X+176,r.Y,r.W-180,42); }
        internal static Box Troops(float width) { return new Box(8,278,width-16,34); }
        internal static Box Skill(float width,int slot)
        {
            if(slot<0||slot>1)throw new ArgumentOutOfRangeException("slot");
            float column=(width-28)/2;return new Box(8+slot*(column+12),332,column,100);
        }
        internal static Box SkillImage(Box r) { return new Box(r.X+8,r.Y+14,72,72); }
        internal static Box SkillTitle(Box r) { return new Box(r.X+92,r.Y+8,r.W-100,30); }
        internal static Box SkillName(Box r) { return new Box(r.X+92,r.Y+38,r.W-100,54); }
    }
}
