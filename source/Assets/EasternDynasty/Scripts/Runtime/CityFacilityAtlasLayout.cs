namespace EasternDynasty
{
    // Geometry transcribed from the inspected Inkscape catalog design.
    internal static class CityFacilityAtlasLayout
    {
        internal struct Box
        {
            internal float X,Y,W,H;
            internal Box(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;}
        }
        internal const float Height=206,Gap=16;
        internal static int Columns(float viewWidth){return viewWidth>=1460?3:2;}
        internal static float Width(float viewWidth){int columns=Columns(viewWidth);return (viewWidth-24-Gap*(columns-1))/columns;}
        internal static Box Art(float width){return new Box(10,14,216,162);}
        internal static Box Name(float width){return new Box(238,8,width-250,32);}
        internal static Box State(float width){return new Box(238,42,width-250,22);}
        internal static Box Effect(float width){return new Box(238,72,width-250,24);}
        internal static Box Condition(float width){return new Box(238,102,width-250,28);}
        internal static Box Action(float width){return new Box(238,150,width-250,44);}
    }
}
