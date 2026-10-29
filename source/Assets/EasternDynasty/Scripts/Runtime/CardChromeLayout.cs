using System;
using Box = EasternDynasty.CardPortraitDesignLayout.Box;

namespace EasternDynasty
{
    // Fixed regions from the inspected Inkscape card detail composition.
    // No state, rendering, artwork generation or persistence is owned here.
    internal static class CardChromeLayout
    {
        internal const string Style = "inkscape-card-chrome-v1";
        internal static Box Nav(Box right) { return new Box(right.X, right.Y + 24, right.W - 66, 54); }
        internal static Box Tab(Box right, int index)
        {
            if (index < 0 || index >= 3) throw new ArgumentOutOfRangeException("index");
            Box nav = Nav(right);
            return new Box(nav.X + index * nav.W / 3, nav.Y, nav.W / 3, nav.H);
        }
        internal static Box Close(Box right) { return new Box(right.X + right.W - 54, right.Y + 24, 54, 54); }
        internal static Box Identity(Box right) { return new Box(right.X, right.Y + 90, right.W, 40); }
        internal static Box Viewport(Box right) { return new Box(right.X, right.Y + 142, right.W, right.H - 226); }
        internal static Box Return(Box right) { return new Box(right.X + right.W - 180, right.Y + right.H - 62, 180, 54); }
        internal static Box Manage(Box right) { return new Box(right.X, right.Y + right.H - 62, Math.Min(240, right.W - 196), 54); }
        internal static Box FooterNote(Box right) { return new Box(right.X, right.Y + right.H - 58, right.W - 196, 44); }
    }
}
