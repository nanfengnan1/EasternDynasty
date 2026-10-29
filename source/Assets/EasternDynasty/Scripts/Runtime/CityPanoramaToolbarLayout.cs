using System;

namespace EasternDynasty
{
    // Geometry from the inspected Inkscape toolbar, independent of Unity or game state.
    internal static class CityPanoramaToolbarLayout
    {
        internal static CityPanoramaBounds Title(CityPanoramaBounds area)
        { return Box(area.X, area.Y + 2, Math.Min(320, area.Width - 374), 52); }
        internal static CityPanoramaBounds TitleArt(CityPanoramaBounds area)
        { return Box(area.X + 6, area.Y + 10, 44, 36); }
        internal static CityPanoramaBounds TitleText(CityPanoramaBounds area)
        { return Box(area.X + 54, area.Y + 6, Title(area).Width - 62, 40); }
        internal static CityPanoramaBounds Action(CityPanoramaBounds area, int index)
        {
            if (index < 0 || index > 4) throw new ArgumentOutOfRangeException("index");
            float offset = index == 0 ? 358 : index == 1 ? 270 : index == 2 ? 220 : index == 3 ? 168 : 80;
            bool zoom = index == 1 || index == 2;
            return Box(area.X + area.Width - offset, area.Y + (zoom ? 6 : 0), zoom ? 44 : 80, zoom ? 44 : 56);
        }
        internal static CityPanoramaBounds Art(CityPanoramaBounds action)
        { return Box(action.X + 26, action.Y + 4, 28, 28); }
        internal static CityPanoramaBounds Caption(CityPanoramaBounds action)
        { return Box(action.X + 4, action.Y + 36, 72, 20); }
        private static CityPanoramaBounds Box(float x, float y, float width, float height)
        { return new CityPanoramaBounds { X=x, Y=y, Width=width, Height=height }; }
    }
}
