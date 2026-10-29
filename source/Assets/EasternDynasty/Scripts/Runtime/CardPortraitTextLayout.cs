using System;

namespace EasternDynasty
{
    internal struct CardPortraitGlyphMetrics
    {
        internal float Width, Height;
    }

    // A font size is not its rendered line height. The Unity adapter supplies
    // actual font metrics; keeping the fit independent makes fallback fonts testable.
    internal static class CardPortraitTextLayout
    {
        internal static int FitName(string name, float cellWidth, float cellHeight,
            int preferredSize, Func<char, int, CardPortraitGlyphMetrics> measure)
        {
            if (measure == null) throw new ArgumentNullException("measure");
            if (cellWidth <= 0 || cellHeight <= 0 || float.IsNaN(cellWidth) || float.IsNaN(cellHeight))
                throw new ArgumentOutOfRangeException("cellWidth");
            if (preferredSize < 1) throw new ArgumentOutOfRangeException("preferredSize");
            for (int size = preferredSize; size >= 1; size--)
            {
                bool fits = true;
                foreach (char glyph in name ?? "")
                {
                    var actual = measure(glyph, size);
                    if (float.IsNaN(actual.Width) || float.IsNaN(actual.Height)
                        || actual.Width < 0 || actual.Height <= 0
                        || actual.Width > cellWidth || actual.Height > cellHeight)
                    { fits = false; break; }
                }
                if (fits) return size;
            }
            // Preserve visible text on an unsupported font; runtime diagnostics
            // still reject clipping instead of accepting a false successful fit.
            return 1;
        }
    }
}
