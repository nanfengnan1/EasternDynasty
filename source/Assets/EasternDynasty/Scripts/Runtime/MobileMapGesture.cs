using System;

namespace EasternDynasty
{
    internal enum MobileTouchPhase { Began, Moved, Stationary, Ended, Cancelled }
    internal struct MobileTouchPoint
    {
        internal int Id;
        internal float X, Y;
        internal MobileTouchPhase Phase;
        internal bool Allowed;
        internal bool Active { get { return Phase != MobileTouchPhase.Ended && Phase != MobileTouchPhase.Cancelled; } }
    }
    internal struct MobileMapMotion
    {
        internal bool Pan, Tap, Pinch;
        internal float PreviousX, PreviousY, X, Y, ZoomRatio;
    }

    // Pure touch arbitration. A gesture belongs to the map only if it began
    // there; crossing UI, adding fingers or losing a finger can never become a tap.
    internal sealed class MobileMapGesture
    {
        private int finger = -1, secondFinger = -1;
        private float startX, startY, previousX, previousY, previousDistance;
        private bool blockedUntilLift;
        internal bool Dragging { get; private set; }
        internal bool Pinching { get; private set; }
        internal bool SuppressesTap { get { return blockedUntilLift || Dragging || Pinching; } }

        internal void Cancel()
        { finger = secondFinger = -1; Dragging = Pinching = false; blockedUntilLift = true; }

        private void Clear()
        { finger = secondFinger = -1; Dragging = Pinching = blockedUntilLift = false; previousDistance = 0; }

        internal MobileMapMotion Step(int count, MobileTouchPoint first, MobileTouchPoint second, float threshold)
        {
            var result = new MobileMapMotion { ZoomRatio = 1 };
            if (count == 0) { Clear(); return result; }
            if (count > 2) { Cancel(); return result; }
            if (blockedUntilLift) return result;
            if (!first.Allowed || (count == 2 && !second.Allowed)) { Cancel(); return result; }

            if (count == 2)
            {
                if (!first.Active || !second.Active) { Cancel(); return result; }
                float centerX = (first.X + second.X) * .5f, centerY = (first.Y + second.Y) * .5f;
                float dx = first.X - second.X, dy = first.Y - second.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                if (!Pinching)
                {
                    bool continuesPrimary = finger >= 0 && (first.Id == finger || second.Id == finger);
                    bool newPair = first.Phase == MobileTouchPhase.Began && second.Phase == MobileTouchPhase.Began;
                    if (!continuesPrimary && !newPair) { Cancel(); return result; }
                    finger = first.Id; secondFinger = second.Id; Pinching = true; Dragging = false;
                    previousX = centerX; previousY = centerY; previousDistance = distance;
                    return result;
                }
                if (!((first.Id == finger && second.Id == secondFinger) || (second.Id == finger && first.Id == secondFinger)))
                { Cancel(); return result; }
                result.Pinch = true; result.Pan = true;
                result.PreviousX = previousX; result.PreviousY = previousY; result.X = centerX; result.Y = centerY;
                if (distance > 8 && previousDistance > 8) result.ZoomRatio = Math.Max(.5f, Math.Min(2, previousDistance / distance));
                previousX = centerX; previousY = centerY; previousDistance = distance;
                return result;
            }

            if (Pinching) { Cancel(); return result; }
            if (first.Phase == MobileTouchPhase.Cancelled) { Cancel(); return result; }
            if (finger < 0)
            {
                if (first.Phase != MobileTouchPhase.Began) return result;
                finger = first.Id; startX = previousX = first.X; startY = previousY = first.Y;
                return result;
            }
            if (first.Id != finger) { Cancel(); return result; }
            float travelX = first.X - startX, travelY = first.Y - startY;
            if (travelX * travelX + travelY * travelY >= threshold * threshold) Dragging = true;
            result.X = first.X; result.Y = first.Y; result.PreviousX = previousX; result.PreviousY = previousY;
            result.Pan = Dragging;
            if (first.Phase == MobileTouchPhase.Ended)
            {
                result.Tap = !Dragging;
                // Do not accept another finger in this touch batch, even if index zero changes.
                Cancel(); return result;
            }
            previousX = first.X; previousY = first.Y;
            return result;
        }
    }
}
