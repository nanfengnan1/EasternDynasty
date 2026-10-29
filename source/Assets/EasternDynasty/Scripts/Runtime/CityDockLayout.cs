namespace EasternDynasty
{
    // Logical bounds from the inspected s1-20261027-190751837 Inkscape source.
    // The dock and city canvas retain their existing ownership of input.
    internal static class CityDockLayout
    {
        internal const string Style = "inkscape-city-dock-v1";

        internal static CityPanoramaBounds Art(CityPanoramaBounds parent)
        { return Bounds(parent.X + 6, parent.Y + 2, 72, 54); }

        internal static CityPanoramaBounds Caption(CityPanoramaBounds parent)
        { return Bounds(parent.X + 2, parent.Y + 60, 80, 24); }

        internal static CityPanoramaBounds BackArt(CityPanoramaBounds parent)
        { return Bounds(parent.X + parent.Width / 2 - 16, parent.Y + 6, 32, 32); }

        internal static CityPanoramaBounds BackCaption(CityPanoramaBounds parent)
        { return Bounds(parent.X + 2, parent.Y + 42, parent.Width - 4, 22); }

        private static CityPanoramaBounds Bounds(float x, float y, float width, float height)
        { return new CityPanoramaBounds { X = x, Y = y, Width = width, Height = height }; }
    }
}
