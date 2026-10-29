namespace EasternDynasty
{
    // Shared geometry of the inspected Inkscape gallery, s1-20261028-012751756.
    internal static class CityFacilityGalleryLayout
    {
        internal const string Style = "inkscape-facility-gallery-v1";
        internal struct Box
        {
            internal float X,Y,W,H;
            internal Box(float x,float y,float w,float h){X=x;Y=y;W=w;H=h;}
        }
        internal static Box Stage(float w,float h){return new Box(12,8,w-24,h-314);}
        internal static Box Image(float w,float h){return new Box(12,22,w-24,h-58);}
        internal static Box Footer(float w,float h){return new Box(0,h-34,w,34);}
        internal static Box LocatorHeading(float w,float h){return new Box(18,h-298,w-36,24);}
        internal static Box Grid(float w,float h){return new Box(16,h-268,w-32,156);}
        internal static Box OptionImage(float w){return new Box(4,1,w-8,58);}
        internal static Box OptionFooter(float w){return new Box(0,60,w,CityFacilitySheetLayout.OptionHeight-60);}
        internal static Box OptionName(float w){return new Box(0,60,w,20);}
        internal static Box OptionState(float w){return new Box(0,80,w,CityFacilitySheetLayout.OptionHeight-80);}
    }
}
