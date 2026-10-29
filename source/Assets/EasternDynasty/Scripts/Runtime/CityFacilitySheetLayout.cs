using System;

namespace EasternDynasty
{
    // Geometry of the inspected 2024-reference Inkscape sheet, s1-20261028-002751316.
    internal static class CityFacilitySheetLayout
    {
        internal const string Style = "inkscape-facility-sheet-v1";
        internal const float ContentHeight = 374;
        // The bundled CJK 12px status line needs 17.376 units; the existing
        // 108-unit row spacing accommodates a 100-high tile and 8-unit gap.
        internal const float OptionHeight = 100;
        internal struct Box
        {
            internal float X, Y, W, H;
            internal Box(float x,float y,float w,float h) { X=x;Y=y;W=w;H=h; }
        }
        internal static float InnerWidth(float panelWidth) { return panelWidth-64; }
        internal static Box View(float width,float height,bool options)
        { float top=92+(options?108:0);return new Box(20,top,width-40,height-112-top); }
        internal static Box Description(float w) { return new Box(8,0,w-16,34); }
        internal static Box Effect(float w) { return new Box(0,44,w,76); }
        internal static Box Unit(float w) { return new Box(16,60,w*.32f-16,44); }
        internal static Box Current(float w) { return new Box(w*.38f,48,w*.18f,64); }
        internal static Box Arrow(float w) { return new Box(w*.64f,73,36,24); }
        internal static Box Next(float w) { return new Box(w*.76f,48,w*.22f,64); }
        internal static Box Costs(float w) { return new Box(0,154,w,88); }
        internal static Box Cost(float w,int index)
        { if(index<0||index>4)throw new ArgumentOutOfRangeException("index");return new Box(index*w/5,154,w/5,88); }
        internal static Box Requirement(float w) { return new Box(4,258,w-8,44); }
        internal static Box Duration(float w) { return new Box(4,312,w-8,28); }
        internal static Box Note(float w) { return new Box(4,350,w-8,24); }
        internal static Box Action(float width,float height) { return new Box((width-360)/2,height-62,360,44); }
        internal static Box Options(float width,int index)
        { if(index<0||index>7)throw new ArgumentOutOfRangeException("index");float cell=(width-40)/8;return new Box(20+index*cell,92,cell-6,OptionHeight); }
    }
}
