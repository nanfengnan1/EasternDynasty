using System;

namespace EasternDynasty
{
    // Text is measured by the selected GUI font before entering this layout.
    // Drawing, scroll bounds and validation share the same content geometry.
    internal struct CardDossierSkillLayout
    {
        internal const string Style = "inkscape-skill-sheet-v1";
        internal const float NativeX = 160;
        internal float SheetHeight;
        internal float NativeY, NativeHeight, LearnedY, LearnedHeight;
        internal float FooterY, FooterHeight, ContentHeight;

        internal static float TextWidth(float width)
        {
            if (width <= 184 || float.IsNaN(width) || float.IsInfinity(width))
                throw new ArgumentOutOfRangeException("width");
            return width - 184;
        }

        internal static CardDossierSkillLayout Calculate(float nativeTextHeight,
            float learnedTextHeight, float footerTextHeight, bool hasFooter)
        {
            if (nativeTextHeight < 0 || learnedTextHeight < 0 || footerTextHeight < 0
                || float.IsNaN(nativeTextHeight) || float.IsNaN(learnedTextHeight) || float.IsNaN(footerTextHeight)
                || float.IsInfinity(nativeTextHeight) || float.IsInfinity(learnedTextHeight) || float.IsInfinity(footerTextHeight))
                throw new ArgumentOutOfRangeException("Text heights must be finite and nonnegative.");
            var layout = new CardDossierSkillLayout
            {
                SheetHeight = Math.Max(240, nativeTextHeight + 104),
                NativeY = 88,
                NativeHeight = nativeTextHeight + 4,
                LearnedY = Math.Max(240, nativeTextHeight + 104) + 18,
                LearnedHeight = Math.Max(84, learnedTextHeight + 44)
            };
            layout.FooterY = layout.LearnedY + layout.LearnedHeight + 16;
            layout.FooterHeight = hasFooter ? Math.Max(58, footerTextHeight) : 0;
            layout.ContentHeight = hasFooter ? layout.FooterY + layout.FooterHeight + 2
                : layout.LearnedY + layout.LearnedHeight + 18;
            return layout;
        }

        internal float ScrollLimit(float viewportHeight)
        {
            if (viewportHeight <= 0 || float.IsNaN(viewportHeight) || float.IsInfinity(viewportHeight))
                throw new ArgumentOutOfRangeException("viewportHeight");
            return Math.Max(0, ContentHeight - viewportHeight);
        }
    }
}
