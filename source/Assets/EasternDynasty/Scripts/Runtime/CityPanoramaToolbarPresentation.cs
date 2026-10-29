using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D panoramaTitleSkin, panoramaCommandSkin, panoramaCommandActiveSkin, panoramaZoomSkin, panoramaResetIcon, panoramaGridIcon;
        private bool panoramaToolbarTextFits;
        private Rect panoramaToolbarArea;

        private void InitPanoramaToolbarDesign()
        {
            if (panoramaTitleSkin != null) return;
            panoramaTitleSkin = GameAssets.LoadTexture("ui.youzhou.panorama_title");
            panoramaCommandSkin = GameAssets.LoadTexture("ui.youzhou.panorama_command");
            panoramaCommandActiveSkin = GameAssets.LoadTexture("ui.youzhou.panorama_command_active");
            panoramaZoomSkin = GameAssets.LoadTexture("ui.youzhou.panorama_zoom");
            panoramaResetIcon = GameAssets.LoadTexture("ui.youzhou.panorama_reset");
            panoramaGridIcon = GameAssets.LoadTexture("ui.youzhou.panorama_grid");
        }

        private void DrawCityPanoramaToolbar(Rect area)
        {
            InitPanoramaToolbarDesign(); panoramaToolbarTextFits = true; panoramaToolbarArea = area;
            var bounds = CityDockBounds(area);
            var title = CityLayoutRect(CityPanoramaToolbarLayout.Title(bounds));
            var titleArt = CityLayoutRect(CityPanoramaToolbarLayout.TitleArt(bounds));
            var titleText = CityLayoutRect(CityPanoramaToolbarLayout.TitleText(bounds));
            GUI.DrawTexture(title, panoramaTitleSkin, ScaleMode.StretchToFill, true);
            DrawHudNavigationArt(titleArt, 5);
            string name = world.lands[world.homeId].name, subtitle = cityOverviewPlanning ? "建设选址" : "城内全景";
            var nameArea = new Rect(titleText.x, titleText.y, titleText.width, 22);
            var subtitleArea = new Rect(titleText.x, titleText.y + 22, titleText.width, 18);
            float nameHeight = PanoramaToolbarText(nameArea, name, 16, 12, TextAnchor.MiddleLeft, Text);
            float subtitleHeight = PanoramaToolbarText(subtitleArea, subtitle, 13, 12, TextAnchor.MiddleLeft, C("D2C79F"));
            cityPanoramaTitleText = name + "\n" + subtitle; cityPanoramaTitleArea = titleText;
            cityPanoramaTitleMeasuredHeight = nameHeight + subtitleHeight;
            RecordCityOverviewTarget("city-panorama-title-art", titleArt);
            RecordCityOverviewTarget("city-panorama-title-text", titleText);
            var expand = CityLayoutRect(CityPanoramaToolbarLayout.Action(bounds, 0));
            var minus = CityLayoutRect(CityPanoramaToolbarLayout.Action(bounds, 1));
            var plus = CityLayoutRect(CityPanoramaToolbarLayout.Action(bounds, 2));
            var reset = CityLayoutRect(CityPanoramaToolbarLayout.Action(bounds, 3));
            var planning = CityLayoutRect(CityPanoramaToolbarLayout.Action(bounds, 4));
            if (PanoramaToolbarAction(expand, "city-panorama-expand", cityOverviewExpanded ? "收起城景" : "展开城景", null, cityOverviewExpanded)) SetCityPanoramaExpanded(!cityOverviewExpanded);
            if (PanoramaToolbarZoom(minus, "city-panorama-minus", "－", cityPanoramaCamera.Zoom > 1)) cityPanoramaCamera.SetZoom(cityPanoramaCamera.Zoom - .25f);
            if (PanoramaToolbarZoom(plus, "city-panorama-plus", "＋", cityPanoramaCamera.Zoom < 2.25f)) cityPanoramaCamera.SetZoom(cityPanoramaCamera.Zoom + .25f);
            if (PanoramaToolbarAction(reset, "city-panorama-reset", "总览复位", panoramaResetIcon, false)) { cityPanoramaCamera.Reset(); CancelCityPanoramaPointer(); }
            if (PanoramaToolbarAction(planning, "city-planning", cityOverviewPlanning ? "结束选址" : "选址建设", panoramaGridIcon, cityOverviewPlanning)) SetCityOverviewPlanning(!cityOverviewPlanning);
        }

        private float PanoramaToolbarText(Rect area, string value, int size, int minimum, TextAnchor alignment, Color color)
        {
            var style = new GUIStyle(hudSmall) { fontSize=size, alignment=alignment, wordWrap=false, clipping=TextClipping.Clip, normal={textColor=color} };
            var content = new GUIContent(value);
            while (style.fontSize > minimum && !HeaderTextFits(area, content, style, false)) style.fontSize--;
            panoramaToolbarTextFits &= HeaderTextFits(area, content, style, false);
            GUI.Label(area, content, style);
            return style.CalcSize(content).y;
        }

        private bool PanoramaToolbarAction(Rect area, string id, string caption, Texture2D icon, bool selected)
        {
            bool hover = GUI.enabled && !mobileMode && area.Contains(Event.current.mousePosition);
            GUI.DrawTexture(area, selected || hover ? panoramaCommandActiveSkin : panoramaCommandSkin, ScaleMode.StretchToFill, true);
            var bounds = CityDockBounds(area);
            var art = CityLayoutRect(CityPanoramaToolbarLayout.Art(bounds));
            var text = CityLayoutRect(CityPanoramaToolbarLayout.Caption(bounds));
            if (icon == null) DrawHudNavigationArt(art, 5); else GUI.DrawTexture(art, icon, ScaleMode.ScaleToFit, true);
            PanoramaToolbarText(text, caption, 14, 13, TextAnchor.MiddleCenter, C("EEE6CB"));
            RecordCityOverviewTarget(id, area);
            RecordCityOverviewTarget(id + "-art", art);
            RecordCityOverviewTarget(id + "-caption", text);
            return GUI.enabled && ContentTileClicked(area, id, -1);
        }

        private bool PanoramaToolbarZoom(Rect area, string id, string caption, bool enabled)
        {
            RecordCityOverviewTarget(id, area);
            bool previous = GUI.enabled; GUI.enabled = previous && enabled;
            Color old = GUI.color;
            try {
                if (!enabled) GUI.color = new Color(old.r, old.g, old.b, old.a * .45f);
                GUI.DrawTexture(area, panoramaZoomSkin, ScaleMode.StretchToFill, true);
                PanoramaToolbarText(area, caption, 24, 20, TextAnchor.MiddleCenter, C("E0CF91"));
                return GUI.enabled && ContentTileClicked(area, id, -1);
            } finally { GUI.color = old; GUI.enabled = previous; }
        }

        private void AssertCityPanoramaToolbarDesign()
        {
            if (!panoramaToolbarTextFits || panoramaTitleSkin == null || panoramaTitleSkin.width != 640 || panoramaTitleSkin.height != 104
                || panoramaCommandSkin.width != 160 || panoramaCommandSkin.height != 112 || panoramaCommandActiveSkin.width != 160
                || panoramaZoomSkin.width != 88 || panoramaResetIcon.width != 64 || panoramaGridIcon.width != 64)
                throw new Exception("City panorama toolbar lost formal Inkscape textures or clips actual text.");
            var b = CityDockBounds(panoramaToolbarArea);
            string[] ids = { "city-panorama-expand", "city-panorama-minus", "city-panorama-plus", "city-panorama-reset", "city-planning" };
            for (int i = 0; i < ids.Length; i++) {
                var action = CityLayoutRect(CityPanoramaToolbarLayout.Action(b, i));
                var actual = CityOverviewTargetBounds(ids[i]);
                var expected = GuiRectToPixels(action);
                if ((actual.position-expected.position).sqrMagnitude > .01f || (actual.size-expected.size).sqrMagnitude > .01f
                    || !CityOverviewContains(cityPanoramaCanvas, action) || action.Overlaps(cityPanoramaViewport))
                    throw new Exception("City panorama toolbar action escapes reserved band: " + ids[i]);
                if (i == 1 || i == 2) continue;
                var art = CityOverviewTargetBounds(ids[i]+"-art"); var caption = CityOverviewTargetBounds(ids[i]+"-caption");
                if (!CityOverviewContains(actual, art) || !CityOverviewContains(actual, caption) || art.Overlaps(caption))
                    throw new Exception("City panorama toolbar image/caption not independently visible: " + ids[i]);
            }
        }
    }
}
