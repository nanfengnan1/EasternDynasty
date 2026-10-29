using System;

namespace EasternDynasty
{
    internal struct LandCommandTextMetrics
    {
        internal float Width, Height, LineHeight;
    }

    internal struct LandCommandTextFit
    {
        internal int FontSize;
        internal float Width, Height, LineHeight;
        internal bool Fits;
    }

    // Font size is not an advance width or line height. The adapter supplies
    // measurements from the same GUIStyle and complete caption used for drawing.
    internal static class LandCommandTextLayout
    {
        internal static LandCommandTextFit Fit(float width, float height, int preferredSize,
            int minimumSize, Func<int, LandCommandTextMetrics> measure)
        {
            if (measure == null) throw new ArgumentNullException("measure");
            if (!Finite(width) || !Finite(height) || width <= 0 || height <= 0)
                throw new ArgumentOutOfRangeException("width");
            if (minimumSize < 1 || preferredSize < minimumSize)
                throw new ArgumentOutOfRangeException("minimumSize");
            var fit = new LandCommandTextFit();
            for (int size = preferredSize; size >= minimumSize; size--)
            {
                var actual = measure(size);
                fit = new LandCommandTextFit { FontSize = size, Width = actual.Width,
                    Height = Math.Max(actual.Height, actual.LineHeight), LineHeight = actual.LineHeight };
                fit.Fits = Finite(actual.Width) && Finite(actual.Height) && Finite(actual.LineHeight)
                    && actual.Width > 0 && actual.Height > 0 && actual.LineHeight > 0
                    && fit.Width <= width && fit.Height <= height;
                if (fit.Fits) return fit;
            }
            // Never make an action unreadable to satisfy geometry. Keep the minimum
            // visible caption and report failure for the unchanged publisher gate.
            return fit;
        }

        private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }
    }
}
