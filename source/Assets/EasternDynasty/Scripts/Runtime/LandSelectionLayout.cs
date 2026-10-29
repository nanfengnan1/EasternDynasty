using Box = EasternDynasty.MapHudNavigationLayout.Box;

namespace EasternDynasty
{
    // Inspected Inkscape source: Docs/DesignUI/s1-20261028-120252024.
    // Only geometry and data binding live here; exported skins own appearance.
    internal static class LandSelectionLayout
    {
        internal const string Style = "land-compact-facts-v5";
        internal struct Placement { internal Box Facts, Commands; internal bool Flanking; }
        internal static Placement Place(float width, float height, float targetX, float targetY, bool visible, string state = "owned")
        {
            // Left HUD ends at x=374. Keep a 20-unit gutter and leave the
            // projected selected tile between the cards. Off-screen/edge
            // selections use the opposite vertical edge, without moving camera.
            bool flank = visible && targetX >= 718 && targetX <= width - 332
                && targetY >= 144 && targetY <= height - 124;
            float commandHeight = LandCommandMenuLayout.Height(state), blockHeight = System.Math.Max(228, commandHeight);
            float y = flank ? System.Math.Max(100, System.Math.Min(height - 100 - blockHeight, targetY - blockHeight / 2))
                : visible && targetY < height / 2 ? height - 100 - blockHeight : 100;
            return new Placement {
                Facts = new Box(flank ? System.Math.Max(394, targetX - 352) : width - 560, y + (blockHeight - 228) / 2, 280, 228),
                Commands = new Box(flank ? targetX + 72 : width - 260, y + (blockHeight - commandHeight) / 2, 240, commandHeight), Flanking = flank
            };
        }
        internal static Box Header(Box p) { return At(p, 0, 0, 280, 148); }
        internal static Box Art(Box p) { return At(p, 12, 64, 256, 84); }
        internal static Box Image(Box p) { return At(p, 12, 68, 156, 78); }
        internal static Box Identity(Box p) { return At(p, 178, 72, 90, 70); }
        internal static Box Summary(Box p) { return At(p, 16, 156, 248, 60); }
        internal static Box Defense(Box p) { return At(p, 16, 156, 248, 28); }
        internal static Box Yield(Box p) { return At(p, 16, 188, 248, 28); }
        internal static Box FactLabel(Box row) { return At(row, 0, 0, 112, 28); }
        internal static Box FactIcon(Box row) { return At(row, 116, 3, 22, 22); }
        internal static Box FactValue(Box row, bool icon) { return At(row, icon ? 144 : 116, 0, icon ? 104 : 132, 28); }
        internal static Box Actions(Box commandSurface) { return At(commandSurface, 16, 12, 208, commandSurface.H - 24); }
        internal static Box Close(Box p) { return At(p, 224, 12, 44, 44); }
        internal static Box CommandIcon(Box command)
        { return At(command, 5, 5, 34, 34); }
        internal static Box CommandLabel(Box command)
        { return At(command, 48, 4, command.W - 52, 36); }
        private static Box At(Box p, float x, float y, float w, float h)
        { return new Box(p.X + x, p.Y + y, w, h); }
    }
}
