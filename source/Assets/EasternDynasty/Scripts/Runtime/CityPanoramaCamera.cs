using System;

namespace EasternDynasty
{
    internal struct CityPanoramaBounds
    {
        internal float X, Y, Width, Height;
    }

    // Presentation-only camera math. Coordinates are logical UI pixels, independent of Unity.
    internal sealed class CityPanoramaCamera
    {
        internal const float SourceWidth = 1000f;
        internal const float SourceHeight = 440f;
        internal const float MinimumZoom = 1f;
        internal const float MaximumZoom = 2.25f;

        internal float Zoom { get; private set; } = MinimumZoom;
        internal float PanX { get; private set; }
        internal float PanY { get; private set; }

        internal void Reset()
        {
            Zoom = MinimumZoom;
            PanX = PanY = 0f;
        }

        internal void SetZoom(float zoom)
        {
            if (!Finite(zoom)) return;
            float next = Math.Max(MinimumZoom, Math.Min(MaximumZoom, zoom));
            double ratio = (double)next / Zoom;
            PanX = FiniteFloat(PanX * ratio);
            PanY = FiniteFloat(PanY * ratio);
            Zoom = next;
        }

        internal void Pan(float dx, float dy)
        {
            if (Finite(dx)) PanX = FiniteFloat((double)PanX + dx);
            if (Finite(dy)) PanY = FiniteFloat((double)PanY + dy);
        }

        internal CityPanoramaBounds Fit(float width, float height)
        { return Fit(width, height, 0); }

        // On narrow/tall screens the city may touch the horizontal crop, but its
        // surrounding scenery must still cover the whole canvas. Keep one uniform
        // transform for artwork and input; the nine facility plaques remain visible.
        internal CityPanoramaBounds FitCanvas(float width, float height, float topInset, float bottomInset)
        {
            double minimumScale = Math.Max((width + 2d) / 1800d,
                (height + 2d * Math.Max(topInset, bottomInset)) / 880d);
            return Fit(width, height, minimumScale);
        }

        private CityPanoramaBounds Fit(float width, float height, double minimumScale)
        {
            if (!Finite(width) || !Finite(height) || width <= 0f || height <= 0f)
            {
                PanX = PanY = 0f;
                return default(CityPanoramaBounds);
            }

            double scale = Math.Max(minimumScale, Math.Min(width / (double)SourceWidth, height / (double)SourceHeight)) * Zoom;
            // Retain a finite, proportional rectangle even for hostile layout input near float.MaxValue.
            scale = Math.Min(scale, float.MaxValue / (double)SourceWidth);
            float contentWidth = (float)(SourceWidth * scale);
            float contentHeight = (float)(SourceHeight * scale);
            double limitX = Math.Max(0d, ((double)contentWidth - width) * .5d);
            double limitY = Math.Max(0d, ((double)contentHeight - height) * .5d);
            PanX = (float)Math.Max(-limitX, Math.Min(limitX, PanX));
            PanY = (float)Math.Max(-limitY, Math.Min(limitY, PanY));
            return new CityPanoramaBounds
            {
                X = (float)(((double)width - contentWidth) * .5d + PanX),
                Y = (float)(((double)height - contentHeight) * .5d + PanY),
                Width = contentWidth,
                Height = contentHeight
            };
        }

        private static bool Finite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static float FiniteFloat(double value)
        {
            return (float)Math.Max(-float.MaxValue, Math.Min(float.MaxValue, value));
        }
    }
}
