using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityDockSealSkin, cityDockSealActiveSkin, cityDockSealHoverSkin, cityDockBackSkin, cityDockBackHoverSkin;
        private bool cityDockTextFits;

        private void InitCityDockDesign()
        {
            if (cityDockSealSkin != null) return;
            cityDockSealSkin = GameAssets.LoadTexture("ui.youzhou.city_dock_seal");
            cityDockSealActiveSkin = GameAssets.LoadTexture("ui.youzhou.city_dock_seal_active");
            cityDockSealHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_dock_seal_hover");
            cityDockBackSkin = GameAssets.LoadTexture("ui.youzhou.city_dock_back");
            cityDockBackHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_dock_back_hover");
        }

        private static CityPanoramaBounds CityDockBounds(Rect area)
        { return new CityPanoramaBounds { X = area.x, Y = area.y, Width = area.width, Height = area.height }; }

        private void CityDockText(Rect bounds, string value, GUIStyle style, int minimum)
        {
            var fitted = new GUIStyle(style) { wordWrap = false, clipping = TextClipping.Clip };
            var content = new GUIContent(value);
            while (fitted.fontSize > minimum && !HeaderTextFits(bounds, content, fitted, false)) fitted.fontSize--;
            cityDockTextFits &= HeaderTextFits(bounds, content, fitted, false);
            GUI.Label(bounds, content, fitted);
        }

        private bool CityDockNavigationAction(Rect area, string title, string page, bool selected)
        {
            bool hover = GUI.enabled && area.Contains(Event.current.mousePosition);
            GUI.DrawTexture(area, selected ? cityDockSealActiveSkin : hover ? cityDockSealHoverSkin : cityDockSealSkin, ScaleMode.StretchToFill, true);
            var bounds = CityDockBounds(area);
            var art = CityLayoutRect(CityDockLayout.Art(bounds));
            var caption = CityLayoutRect(CityDockLayout.Caption(bounds));
            // Reuse the current production images and existing district/document
            // symbols. The inspected design changes their composition, not their art.
            DrawCityNavigationArt(art, page, true);
            CityDockText(caption, title, new GUIStyle(hudCaption) { fontSize = 16, alignment = TextAnchor.MiddleCenter,
                normal = { textColor = C(selected ? "1C2923" : "EEE6CB") } }, 13);
            RecordCityOverviewTarget("city-nav-" + page + "-art", art);
            RecordCityOverviewTarget("city-nav-" + page + "-caption", caption);
            return GUI.enabled && ContentTileClicked(area, "city-nav-" + page, -1);
        }

        private bool CityDockBackAction(Rect area, string title)
        {
            bool hover = GUI.enabled && area.Contains(Event.current.mousePosition);
            GUI.DrawTexture(area, hover ? cityDockBackHoverSkin : cityDockBackSkin, ScaleMode.StretchToFill, true);
            var bounds = CityDockBounds(area);
            var art = CityLayoutRect(CityDockLayout.BackArt(bounds));
            var caption = CityLayoutRect(CityDockLayout.BackCaption(bounds));
            CityDockText(art, "‹", new GUIStyle(heading) { fontSize = 28, alignment = TextAnchor.MiddleCenter, normal = { textColor = C("E1CC8E") } }, 20);
            CityDockText(caption, title, new GUIStyle(hudCaption) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = C("EEE6CB") } }, 13);
            RecordCityOverviewTarget("city-dock-back-art", art);
            RecordCityOverviewTarget("city-dock-back-caption", caption);
            return GUI.enabled && ContentTileClicked(area, "city-dock-back", -1);
        }

        private void AssertCityDockDesign()
        {
            if (!cityDockTextFits || cityDockSealSkin == null || cityDockSealActiveSkin == null || cityDockSealHoverSkin == null
                || cityDockBackSkin == null || cityDockBackHoverSkin == null)
                throw new Exception("City dock formal assets or actual-font fit are missing.");
            var pages = new[] { "overview", "catalog", "facilities", "quests", "training" };
            for (int i = 0; i < pages.Length; i++)
            {
                var parent = CityOverviewLayout.Navigation(width, height, i);
                AssertCityDockRegion("city-nav-" + pages[i] + "-art", CityDockLayout.Art(parent));
                AssertCityDockRegion("city-nav-" + pages[i] + "-caption", CityDockLayout.Caption(parent));
            }
            var back = CityOverviewLayout.Back(width, height);
            AssertCityDockRegion("city-dock-back-art", CityDockLayout.BackArt(back));
            AssertCityDockRegion("city-dock-back-caption", CityDockLayout.BackCaption(back));
        }

        private void AssertCityDockRegion(string id, CityPanoramaBounds expected)
        {
            var actual = CityOverviewTargetBounds(id);
            var pixels = GuiRectToPixels(CityLayoutRect(expected));
            if (!CityOverviewContains(GuiRectToPixels(cityNavigationDockArea), actual)
                || (actual.position - pixels.position).sqrMagnitude > .01f || (actual.size - pixels.size).sqrMagnitude > .01f)
                throw new Exception("City dock visible target differs from inspected design: " + id);
        }
    }
}
