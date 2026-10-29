using System;

namespace EasternDynasty
{
    // Unity-independent layout math for landscape phones, tablets and desktop reviews.
    internal struct MobileScreenLayout
    {
        internal float X, Y, Width, Height, Scale, LogicalWidth, LogicalHeight;
        internal static MobileScreenLayout Calculate(float screenWidth, float screenHeight,
            float safeX, float safeYBottom, float safeWidth, float safeHeight, bool mobile)
        {
            if (screenWidth <= 0 || screenHeight <= 0) throw new ArgumentOutOfRangeException("screenWidth");
            float x = mobile ? Math.Max(0, safeX) : 0;
            float yBottom = mobile ? Math.Max(0, safeYBottom) : 0;
            float w = mobile ? Math.Min(screenWidth - x, safeWidth) : screenWidth;
            float h = mobile ? Math.Min(screenHeight - yBottom, safeHeight) : screenHeight;
            if (w < 1 || h < 1) { x = yBottom = 0; w = screenWidth; h = screenHeight; }
            float scale = Math.Max(mobile ? .1f : .35f, Math.Min(w / 1440f, h / (mobile ? 820f : 900f)));
            return new MobileScreenLayout { X = x, Y = screenHeight - yBottom - h, Width = w, Height = h,
                Scale = scale, LogicalWidth = w / scale, LogicalHeight = h / scale };
        }
    }
}
