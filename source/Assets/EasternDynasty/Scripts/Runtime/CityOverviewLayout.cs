using System;

namespace EasternDynasty
{
    // Logical UI geometry only. The edge dock replaces the overview's second
    // heading row; child dossiers keep their wider horizontal navigation.
    internal static class CityOverviewLayout
    {
        internal static CityPanoramaBounds Dock(float width, float height)
        { return Bounds(24, 82, 92, height - 180); }

        internal static CityPanoramaBounds Content(float width, float height)
        { return Bounds(128, 82, width - 152, height - 180); }

        internal static float ArmyRailWidth(float contentHeight)
        { return Math.Max(342, Math.Min(406, 342 + (contentHeight - 640) * .8f)); }

        internal static CityPanoramaBounds Navigation(float width, float height, int index)
        {
            if (index < 0 || index >= 5) throw new ArgumentOutOfRangeException("index");
            var dock = Dock(width, height);
            return Bounds(dock.X + 4, dock.Y + 8 + index * 90, dock.Width - 8, 86);
        }

        internal static CityPanoramaBounds Back(float width, float height)
        {
            var dock = Dock(width, height);
            return Bounds(dock.X + 4, dock.Y + dock.Height - 72, dock.Width - 8, 64);
        }

        private static CityPanoramaBounds Bounds(float x, float y, float width, float height)
        { return new CityPanoramaBounds { X = x, Y = y, Width = width, Height = height }; }
    }
}
