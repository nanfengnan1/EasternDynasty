using System;

namespace EasternDynasty
{
    // Logical geometry from the inspected Inkscape city-affairs design.
    // The full city/army footprint and gameplay commands remain unchanged.
    internal static class CityAffairsLayout
    {
        internal const string Style = "inkscape-city-affairs-v1";
        internal static CityPanoramaBounds Seal(float width) { return Bounds(8, 0, 112, 112); }
        internal static CityPanoramaBounds Building(float width) { return Bounds(12, 4, 104, 78); }
        internal static CityPanoramaBounds SealCaption(float width) { return Bounds(20, 82, 88, 26); }
        internal static CityPanoramaBounds Summary(float width) { return Bounds(132, 2, width - 140, 108); }
        internal static CityPanoramaBounds Action(float width, int index)
        {
            if (index < 0 || index > 1) throw new ArgumentOutOfRangeException("index");
            float half = (width - 24) / 2;
            return Bounds(8 + index * (half + 8), 116, half, 44);
        }
        internal static CityPanoramaBounds Quest(float width) { return Bounds(8, 168, width - 16, 44); }
        internal static CityPanoramaBounds QuestArt(float width) { return Bounds(12, 172, 36, 36); }
        internal static CityPanoramaBounds QuestCaption(float width) { return Bounds(57, 169, width - 95, 42); }
        private static CityPanoramaBounds Bounds(float x, float y, float width, float height)
        { return new CityPanoramaBounds { X = x, Y = y, Width = width, Height = height }; }
    }
}
