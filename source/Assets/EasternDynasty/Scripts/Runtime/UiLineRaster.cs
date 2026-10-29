using System;

namespace EasternDynasty
{
    internal struct UiLineStrip
    {
        internal float X, Y, Width, Height;
    }

    // A butt-capped stroke rasterized in the caller's local GUI coordinates.
    // Value types only: no per-stroke arrays, delegates, textures or GUI matrices.
    internal struct UiLineRaster
    {
        internal const int MaximumStrips = 1024;
        private const double StripSize = .75d;
        private double x0, y0, x1, y1, x2, y2, x3, y3, lower, extent;
        private int count, index;
        private bool transpose;

        internal UiLineRaster(float startX, float startY, float endX, float endY, float thickness)
        {
            this = default(UiLineRaster);
            if (!Finite(startX) || !Finite(startY) || !Finite(endX) || !Finite(endY) ||
                !Finite(thickness) || thickness <= 0f) return;
            double dx = (double)endX - startX, dy = (double)endY - startY;
            double length = Math.Sqrt(dx * dx + dy * dy);
            // A coincident pair has no direction or butt-cap area.
            if (length <= 0d) return;
            double nx = -dy / length * thickness * .5d, ny = dx / length * thickness * .5d;
            double left = Math.Min(startX, endX) - Math.Abs(nx);
            double right = Math.Max(startX, endX) + Math.Abs(nx);
            double top = Math.Min(startY, endY) - Math.Abs(ny);
            double bottom = Math.Max(startY, endY) + Math.Abs(ny);
            if (left < -float.MaxValue || right > float.MaxValue || top < -float.MaxValue || bottom > float.MaxValue)
                return;
            // Scan the shorter axis to keep near-horizontal/vertical strokes cheap.
            // Near-square bounds always use rows: float rounding after a parent
            // translation must not flip the scan axis of a 45-degree emblem.
            transpose = right - left + .001d < bottom - top;
            x0 = transpose ? startY + ny : startX + nx;
            y0 = transpose ? startX + nx : startY + ny;
            x1 = transpose ? endY + ny : endX + nx;
            y1 = transpose ? endX + nx : endY + ny;
            x2 = transpose ? endY - ny : endX - nx;
            y2 = transpose ? endX - nx : endY - ny;
            x3 = transpose ? startY - ny : startX - nx;
            y3 = transpose ? startX - nx : startY - ny;
            lower = transpose ? left : top;
            extent = transpose ? right - left : bottom - top;
            if (extent <= 0d || extent > float.MaxValue) return;
            // Axial strokes are a single exact rectangle. The bound also prevents
            // hostile geometry from creating an unbounded draw loop.
            count = dx == 0d || dy == 0d ? 1 : (int)Math.Min(MaximumStrips, Math.Max(1d, Math.Ceiling(extent / StripSize)));
        }

        internal bool MoveNext(out UiLineStrip strip)
        {
            strip = default(UiLineStrip);
            while (index < count)
            {
                double low = lower + extent * index / count;
                double high = lower + extent * ++index / count;
                double scan = (low + high) * .5d;
                double left = double.PositiveInfinity, right = double.NegativeInfinity;
                Intersect(x0, y0, x1, y1, scan, ref left, ref right);
                Intersect(x1, y1, x2, y2, scan, ref left, ref right);
                Intersect(x2, y2, x3, y3, scan, ref left, ref right);
                Intersect(x3, y3, x0, y0, scan, ref left, ref right);
                if (right <= left || double.IsInfinity(left) || right - left > float.MaxValue) continue;
                strip = new UiLineStrip
                {
                    X = (float)(transpose ? low : left),
                    Y = (float)(transpose ? left : low),
                    Width = (float)(transpose ? high - low : right - left),
                    Height = (float)(transpose ? right - left : high - low)
                };
                if (strip.Width > 0f && strip.Height > 0f) return true;
            }
            return false;
        }

        private static void Intersect(double ax, double ay, double bx, double by, double scan, ref double left, ref double right)
        {
            if (ay == by || scan < Math.Min(ay, by) || scan > Math.Max(ay, by)) return;
            double x = ax + (bx - ax) * ((scan - ay) / (by - ay));
            left = Math.Min(left, x);
            right = Math.Max(right, x);
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
