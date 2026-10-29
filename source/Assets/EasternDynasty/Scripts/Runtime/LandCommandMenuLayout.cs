using Box = EasternDynasty.MapHudNavigationLayout.Box;

namespace EasternDynasty
{
    // Inkscape contract: Docs/DesignUI/s1-20261028-110251322/design-spec.json.
    internal static class LandCommandMenuLayout
    {
        internal static float Height(string state)
        { return state == "owned" ? 292 : state == "busy" ? 216 : state == "city" ? 144 : 268; }
        internal static float InformationHeight(string state)
        { return state == "owned" ? 116 : state == "busy" ? 96 : state == "city" ? 76 : 200; }
        internal static float CommandTop(string state, string id)
        {
            if (state == "owned") return id == "army-garrison" ? 128 : id == "army-farm" ? 180 : 232;
            if (state == "busy") return id == "army-recall" ? 108 : 160;
            return state == "city" ? 88 : 212;
        }
        internal static Box Information(Box menu, string state)
        { return new Box(menu.X, menu.Y, menu.W, InformationHeight(state)); }
        internal static Box Command(Box content, string state, string id)
        { return new Box(content.X, content.Y + CommandTop(state, id) - 12, 208, 44); }
    }
}
