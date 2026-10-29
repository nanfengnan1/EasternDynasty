using System;

namespace EasternDynasty
{
    // Unity-independent gesture state: a drag may never also commit a land click.
    internal sealed class MapPointerGesture
    {
        internal const float DragThreshold = 6;
        internal int Button { get; private set; } = -1;
        internal bool Dragging { get; private set; }
        internal float PreviousX { get; private set; }
        internal float PreviousY { get; private set; }
        private float startX, startY;

        internal void Begin(int button, float x, float y)
        {
            if (button < 0 || button > 2) return;
            Button = button; startX = PreviousX = x; startY = PreviousY = y;
            Dragging = button != 0;
        }
        internal bool Move(float x, float y, bool allowed)
        {
            if (!allowed) { Cancel(); return false; }
            if (Button < 0) return false;
            float dx = x - startX, dy = y - startY;
            if (!Dragging && dx * dx + dy * dy >= DragThreshold * DragThreshold) Dragging = true;
            return Dragging;
        }
        internal void Remember(float x, float y) { PreviousX = x; PreviousY = y; }
        internal bool Release(int button, bool allowed)
        {
            if (button != Button) return false;
            bool click = allowed && Button == 0 && !Dragging; Cancel(); return click;
        }
        internal void Cancel() { Button = -1; Dragging = false; }
        internal static float EdgeAxis(float position, float minimum, float maximum, float margin)
        {
            if (position < minimum || position > maximum || maximum <= minimum || margin <= 0) return 0;
            margin = Math.Min(margin, (maximum - minimum) * .25f);
            if (position < minimum + margin) return -(1 - (position - minimum) / margin);
            if (position > maximum - margin) return 1 - (maximum - position) / margin;
            return 0;
        }
    }
}
