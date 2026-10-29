using System;

namespace EasternDynasty
{
    // Geometry of the inspected Inkscape portrait composition, not artwork generation.
    internal static class CardPortraitDesignLayout
    {
        internal const string Style = "inkscape-portrait-column-v2";
        internal struct Box
        {
            internal float X, Y, W, H;
            internal Box(float x, float y, float w, float h) { X=x; Y=y; W=w; H=h; }
        }
        internal static Box ImageArea(Box art) { return new Box(art.X+68,art.Y+12,art.W-82,art.H-104); }
        internal static float NameStep(float artHeight, int nameLength)
        { return Math.Min(38,(artHeight-200)/Math.Max(1,nameLength)); }
        internal static Box Banner(Box art, int nameLength)
        { return new Box(art.X+10,art.Y+12,48,68+nameLength*NameStep(art.H,nameLength)); }
        internal static Box Stars(Box art, int nameLength, int stars)
        { var b=Banner(art,nameLength); return new Box(b.X,b.Y+b.H+8,48,stars*30); }
        internal static Box Base(Box art) { return new Box(art.X+12,art.Y+art.H-80,art.W-24,68); }
        internal static Box FitImage(Box area, float sourceWidth, float sourceHeight)
        {
            if(sourceWidth<=0||sourceHeight<=0) throw new ArgumentOutOfRangeException("sourceWidth");
            float fit=Math.Min(area.W/sourceWidth,area.H/sourceHeight);
            return new Box(area.X+(area.W-sourceWidth*fit)/2,area.Y,sourceWidth*fit,sourceHeight*fit);
        }
    }
}
