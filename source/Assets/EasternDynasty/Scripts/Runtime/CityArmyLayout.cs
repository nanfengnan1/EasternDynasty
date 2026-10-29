using System;

namespace EasternDynasty
{
    // Geometry of the inspected city-army design, in parent-relative logical pixels.
    // Runtime supplies text, portraits and commands; no artwork is generated here.
    internal static class CityArmyLayout
    {
        internal const string Style = "youzhou-city-army-tickets-v1";

        internal static float CardWidth(float width)
        { return (width - 32) / 3; }

        internal static float CardHeight(float width, float availableHeight)
        { return Math.Min(availableHeight - 190, (CardWidth(width) - 8) * 1.45f + 48); }

        internal static float Height(float width, float availableHeight)
        { return CardHeight(width, availableHeight) + 190; }

        internal static CityPanoramaBounds Card(float width, float availableHeight, int index)
        {
            CheckIndex(index);
            float cardWidth = CardWidth(width);
            return Bounds(8 + index * (cardWidth + 8), 52, cardWidth, CardHeight(width, availableHeight));
        }

        internal static CityPanoramaBounds Portrait(CityPanoramaBounds card)
        { return Bounds(card.X + 4, card.Y + 4, card.Width - 8, card.Height - 48); }

        internal static CityPanoramaBounds Identity(CityPanoramaBounds card)
        { return Bounds(card.X + 2, card.Y + card.Height - 43, card.Width - 4, 42); }

        internal static CityPanoramaBounds Strength(float width, float availableHeight)
        { return Bounds(8, CardHeight(width, availableHeight) + 58, width - 16, 28); }

        internal static CityPanoramaBounds Supply(float width, float availableHeight)
        { return Bounds(8, CardHeight(width, availableHeight) + 92, width - 16, 40); }

        internal static CityPanoramaBounds Action(float width, float availableHeight, int index)
        {
            CheckIndex(index);
            float actionWidth = (width - 28) / 3;
            return Bounds(8 + index * (actionWidth + 6), CardHeight(width, availableHeight) + 140, actionWidth, 44);
        }

        private static void CheckIndex(int index)
        { if (index < 0 || index > 2) throw new ArgumentOutOfRangeException("index"); }

        private static CityPanoramaBounds Bounds(float x, float y, float width, float height)
        { return new CityPanoramaBounds { X = x, Y = y, Width = width, Height = height }; }
    }
}
