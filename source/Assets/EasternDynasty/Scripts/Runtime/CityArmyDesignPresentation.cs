using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityArmyCardSkin, cityArmyCardHoverSkin, cityArmyStripSkin, cityArmyCommandSkin, cityArmyCommandHoverSkin;
        private bool cityArmyTextFits;

        private void InitCityArmyDesign()
        {
            if (cityArmyCardSkin != null) return;
            cityArmyCardSkin = GameAssets.LoadTexture("ui.youzhou.city_army_card");
            cityArmyCardHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_army_card_hover");
            cityArmyStripSkin = GameAssets.LoadTexture("ui.youzhou.city_army_strip");
            cityArmyCommandSkin = GameAssets.LoadTexture("ui.youzhou.city_army_command");
            cityArmyCommandHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_army_command_hover");
        }

        private static Rect CityArmyRect(Rect parent, CityPanoramaBounds relative)
        { return new Rect(parent.x + relative.X, parent.y + relative.Y, relative.Width, relative.Height); }

        private void CityArmyText(Rect bounds, string text, GUIStyle style, int minimumSize)
        {
            var fitted = new GUIStyle(style) { wordWrap = false, clipping = TextClipping.Clip };
            var content = new GUIContent(text);
            while (fitted.fontSize > minimumSize && !HeaderTextFits(bounds, content, fitted, false)) fitted.fontSize--;
            cityArmyTextFits &= HeaderTextFits(bounds, content, fitted, false);
            GUI.Label(bounds, content, fitted);
        }

        private void AssertCityArmyDesign()
        {
            if (!cityArmyTextFits || cityArmyCardSkin == null || cityArmyCardHoverSkin == null
                || cityArmyStripSkin == null || cityArmyCommandSkin == null || cityArmyCommandHoverSkin == null)
                throw new System.Exception("City army design assets or actual-font fit are missing.");
            var parent = cityOverviewArmyArea;
            if (System.Math.Abs(parent.height - CityArmyLayout.Height(parent.width, cityContentArea.height - 230)) > .1f)
                throw new System.Exception("City army kept an empty stretched mat instead of the inspected compact footprint.");
            for (int i = 0; i < 3; i++)
            {
                var card = CityArmyLayout.Card(parent.width, parent.height, i);
                Rect expected = CityArmyRect(parent, card);
                Rect actual = cityOverviewGeneralRects[i];
                if ((expected.position - actual.position).sqrMagnitude > .01f || (expected.size - actual.size).sqrMagnitude > .01f)
                    throw new System.Exception("City army card differs from the inspected design geometry.");
                if (i < world.ActiveGenerals.Count)
                {
                    AssertCityCompositionTarget("city-general-art-" + i, cityOverviewGeneralArtRects[i], parent);
                    AssertCityCompositionTarget("city-general-name-" + i, cityOverviewGeneralNameRects[i], parent);
                }
                else if (reviewTargets.ContainsKey("city-general-art-" + i) || reviewTargets.ContainsKey("city-general-name-" + i))
                    throw new System.Exception("An empty city army slot advertises a nonexistent owned portrait.");
            }
        }
    }
}
