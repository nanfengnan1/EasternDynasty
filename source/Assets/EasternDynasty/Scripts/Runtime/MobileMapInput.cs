using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class MapView
    {
        public bool mobileInputMode;
        private readonly MobileMapGesture mobileGesture = new MobileMapGesture();

        public void CancelPointerInput() { pointer.Cancel(); mobileGesture.Cancel(); }
        private void OnApplicationFocus(bool focused) { if(!focused) CancelPointerInput(); }
        private void OnDisable() { CancelPointerInput(); }

        private MobileTouchPoint ReadMobileTouch(int index)
        {
            Touch touch = Input.GetTouch(index);
            var top = new Vector2(touch.position.x, Screen.height - touch.position.y);
            MobileTouchPhase phase = touch.phase == TouchPhase.Began ? MobileTouchPhase.Began :
                touch.phase == TouchPhase.Moved ? MobileTouchPhase.Moved :
                touch.phase == TouchPhase.Stationary ? MobileTouchPhase.Stationary :
                touch.phase == TouchPhase.Ended ? MobileTouchPhase.Ended : MobileTouchPhase.Cancelled;
            return new MobileTouchPoint { Id = touch.fingerId, X = touch.position.x, Y = touch.position.y, Phase = phase,
                Allowed = inputEnabled && Application.isFocused && screenArea.Contains(top) && !IsBlocked(top) };
        }

        private void UpdateMobileMapInput()
        {
            int count = Input.touchCount;
            var first = count > 0 ? ReadMobileTouch(0) : default(MobileTouchPoint);
            var second = count > 1 ? ReadMobileTouch(1) : default(MobileTouchPoint);
            float threshold = Mathf.Clamp(Screen.dpi > 0 ? Screen.dpi / 160f * 8f : 12f, 10f, 30f);
            MobileMapMotion motion = mobileGesture.Step(count, first, second, threshold);
            if (motion.Pan)
            {
                Vector3 previous, current;
                bool anchor = TryPlanePoint(new Vector2(motion.PreviousX, motion.PreviousY), out previous);
                if (motion.Pinch)
                {
                    zoom = Mathf.Clamp(zoom * motion.ZoomRatio, world.IsYouzhou ? 2.7f : 3.5f, world.IsYouzhou ? 18 : 17);
                    PositionCamera();
                }
                if (anchor && TryPlanePoint(new Vector2(motion.X, motion.Y), out current)) focus += previous - current;
            }
            if (motion.Tap)
            {
                int id;
                if (TryPickLand(new Vector2(motion.X, motion.Y), out id))
                { selected = id; if (LandClicked != null) LandClicked(id); }
            }
        }
    }
}
