using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityAffairsSealSkin, cityAffairsSealHoverSkin, cityAffairsRibbonSkin, cityAffairsQuestSkin, cityAffairsQuestHoverSkin;
        private Rect cityAffairsBuildingArea, cityAffairsCaptionArea, cityAffairsQuestCaptionArea;
        private bool cityAffairsTextFits;

        private void InitCityAffairsDesign()
        {
            if (cityAffairsSealSkin != null) return;
            cityAffairsSealSkin = GameAssets.LoadTexture("ui.youzhou.city_affairs_seal");
            cityAffairsSealHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_affairs_seal_hover");
            cityAffairsRibbonSkin = GameAssets.LoadTexture("ui.youzhou.city_affairs_ribbon");
            cityAffairsQuestSkin = GameAssets.LoadTexture("ui.youzhou.city_affairs_quest");
            cityAffairsQuestHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_affairs_quest_hover");
            InitCityArmyDesign();
        }

        private void CityAffairsText(Rect bounds, string text, GUIStyle style, int minimumSize)
        {
            var fitted = new GUIStyle(style) { wordWrap = false, clipping = TextClipping.Clip };
            var content = new GUIContent(text);
            while (fitted.fontSize > minimumSize && !HeaderTextFits(bounds, content, fitted, false)) fitted.fontSize--;
            cityAffairsTextFits &= HeaderTextFits(bounds, content, fitted, false);
            GUI.Label(bounds, content, fitted);
        }

        private bool CityAffairsCommand(Rect bounds, string id, string caption, bool attention)
        {
            bool hover = GUI.enabled && bounds.Contains(Event.current.mousePosition);
            GUI.DrawTexture(bounds, hover ? cityArmyCommandHoverSkin : cityArmyCommandSkin, ScaleMode.StretchToFill, true);
            CityAffairsText(new Rect(bounds.x + 5, bounds.y + 1, bounds.width - 10, 42), caption,
                new GUIStyle(hudCaption) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = C(attention ? "E1CC8E" : "EEE6CB") } }, 13);
            RecordCityOverviewTarget(id, bounds);
            return GUI.enabled && ContentTileClicked(bounds, id, -1);
        }

        private void DrawCityOverviewAffairs(Rect area)
        {
            InitCityAffairsDesign(); cityAffairsTextFits = true;
            int built = world.cityPlots.Count(p => p.building != CityBuilding.None);
            int available = world.cityPlots.Count(CityOverviewPlotAvailable);
            var job = world.CityJob;
            cityAffairsWorking = job != null;
            cityAffairsDisplayedBuilding = job == null ? CityBuilding.TownHall : job.building;
            cityAffairsDisplayedLevel = job == null ? world.CityLevel(cityAffairsDisplayedBuilding) : job.targetLevel;
            cityAffairsArtArea = CityArmyRect(area, CityAffairsLayout.Seal(area.width));
            cityAffairsBuildingArea = CityArmyRect(area, CityAffairsLayout.Building(area.width));
            cityAffairsCaptionArea = CityArmyRect(area, CityAffairsLayout.SealCaption(area.width));
            cityAffairsSummaryArea = CityArmyRect(area, CityAffairsLayout.Summary(area.width));
            bool hover = GUI.enabled && cityAffairsArtArea.Contains(Event.current.mousePosition);
            GUI.DrawTexture(cityAffairsArtArea, hover ? cityAffairsSealHoverSkin : cityAffairsSealSkin, ScaleMode.StretchToFill, true);
            // Reuse the existing facility artwork; the formal frame comes from Inkscape.
            DrawCityFacilityArt(cityAffairsBuildingArea, cityAffairsDisplayedBuilding);
            CityAffairsText(cityAffairsCaptionArea, job == null ? "府治" : "营建",
                new GUIStyle(body) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = C("E1CC8E") } }, 13);
            RecordCityOverviewTarget("city-affairs-art", cityAffairsArtArea);
            RecordCityOverviewTarget("city-affairs-building", cityAffairsBuildingArea);
            RecordCityOverviewTarget("city-affairs-caption", cityAffairsCaptionArea);
            if (ContentTileClicked(cityAffairsArtArea, "city-affairs-art", -1))
            {
                int plot = job == null ? world.cityPlots.First(p => p.building == CityBuilding.TownHall).index : job.plotIndex;
                OpenCityFacilities(plot);
            }
            GUI.DrawTexture(cityAffairsSummaryArea, cityAffairsRibbonSkin, ScaleMode.StretchToFill, true);
            float sx = cityAffairsSummaryArea.x + 10, sw = cityAffairsSummaryArea.width - 20;
            CityAffairsText(new Rect(sx, area.y + 4, sw, 23), job == null ? "城 务  /  府 治" : "城 务  /  营 建",
                new GUIStyle(hudSmall) { fontSize = 13, normal = { textColor = C("B5BBAA") } }, 11);
            CityAffairsText(new Rect(sx, area.y + 29, sw, 27), CityCatalog.Get(cityAffairsDisplayedBuilding).name
                + "  " + (job == null ? "Lv." + cityAffairsDisplayedLevel : "→ " + cityAffairsDisplayedLevel + "级"), new GUIStyle(body) { fontSize = 18, normal = { textColor = C("EEE6CB") } }, 14);
            CityAffairsText(new Rect(sx, area.y + 57, sw, 24), job == null ? "工匠待命 · 可兴建升级" : "剩余 " + CityTime(job.remaining),
                new GUIStyle(hudSmall) { fontSize = 13, normal = { textColor = C(job == null ? "B5BBAA" : "E1CC8E") } }, 11);
            var track = new Rect(sx, area.y + 83, sw, 3);
            RectFill(track, C("46534A"));
            if (job != null) RectFill(new Rect(track.x, track.y, track.width * Mathf.Clamp01((float)(1 - job.remaining / Math.Max(1, job.duration))), 3), C("D4BA78"));
            CityAffairsText(new Rect(sx, area.y + 88, sw, 22), "已建 " + built + "  /  空地 " + available,
                new GUIStyle(hudSmall) { fontSize = 13, normal = { textColor = C("B5BBAA") } }, 11);
            var facilities = CityArmyRect(area, CityAffairsLayout.Action(area.width, 0));
            if (CityAffairsCommand(facilities, "city-overview-facilities", job == null ? "设施一览" : "管理施工", job != null))
            { if (job == null) OpenCityFacilityCatalog(); else OpenCityFacilities(job.plotIndex); }
            var construction = CityArmyRect(area, CityAffairsLayout.Action(area.width, 1));
            if (CityAffairsCommand(construction, "city-construction-page", session.LastCityCompletion == null ? "建设进度" : "查看新完工", session.LastCityCompletion != null)) OpenCityConstruction();
            var entry = CityArmyRect(area, CityAffairsLayout.Quest(area.width));
            cityAffairsQuestArtArea = CityArmyRect(area, CityAffairsLayout.QuestArt(area.width));
            cityAffairsQuestCaptionArea = CityArmyRect(area, CityAffairsLayout.QuestCaption(area.width));
            bool questHover = GUI.enabled && entry.Contains(Event.current.mousePosition);
            GUI.DrawTexture(entry, questHover ? cityAffairsQuestHoverSkin : cityAffairsQuestSkin, ScaleMode.StretchToFill, true);
            DrawCityDocumentArt(cityAffairsQuestArtArea);
            var quest = world.CurrentCityQuest;
            CityAffairsText(new Rect(cityAffairsQuestCaptionArea.x, entry.y + 1, cityAffairsQuestCaptionArea.width, 23),
                quest == null ? "初定幽州 · 本章完成" : quest.title, new GUIStyle(small) { fontSize = 14, normal = { textColor = C("EEE6CB") } }, 12);
            string questState = "主要事宜 · 查看章节";
            bool ready = false;
            if (quest != null)
            {
                int target = world.CityQuestTarget(quest.id), progress = Math.Min(target, world.CityQuestProgress(quest.id));
                ready = world.CityQuestReady(quest.id);
                questState = ready ? "主要事宜 · 可领取奖励" : "主要事宜  " + progress + " / " + target;
            }
            CityAffairsText(new Rect(cityAffairsQuestCaptionArea.x, entry.y + 24, cityAffairsQuestCaptionArea.width, 19), questState,
                new GUIStyle(hudSmall) { fontSize = 12, normal = { textColor = C(ready ? "E1CC8E" : "B5BBAA") } }, 11);
            GUI.Label(new Rect(entry.xMax - 26, entry.y, 24, entry.height), "›", new GUIStyle(heading) { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = C("E1CC8E") } });
            RecordCityOverviewTarget("city-overview-quests", entry);
            RecordCityOverviewTarget("city-overview-quests-art", cityAffairsQuestArtArea);
            RecordCityOverviewTarget("city-overview-quests-caption", cityAffairsQuestCaptionArea);
            if (ContentTileClicked(entry, "city-overview-quests", -1)) OpenCityQuests();
        }

        private void AssertCityAffairsDesign()
        {
            if (!cityAffairsTextFits || cityAffairsSealSkin == null || cityAffairsSealHoverSkin == null
                || cityAffairsRibbonSkin == null || cityAffairsQuestSkin == null || cityAffairsQuestHoverSkin == null)
                throw new Exception("City affairs formal assets or actual-font fit are missing.");
            var area = cityOverviewAffairsArea;
            var actual = new[] { cityAffairsArtArea, cityAffairsBuildingArea, cityAffairsCaptionArea, cityAffairsSummaryArea,
                cityAffairsQuestArtArea, cityAffairsQuestCaptionArea };
            var expected = new[] { CityAffairsLayout.Seal(area.width), CityAffairsLayout.Building(area.width), CityAffairsLayout.SealCaption(area.width),
                CityAffairsLayout.Summary(area.width), CityAffairsLayout.QuestArt(area.width), CityAffairsLayout.QuestCaption(area.width) };
            for (int i = 0; i < actual.Length; i++)
            {
                var rect = CityArmyRect(area, expected[i]);
                if (!CityOverviewContains(area, actual[i]) || (actual[i].position - rect.position).sqrMagnitude > .01f
                    || (actual[i].size - rect.size).sqrMagnitude > .01f)
                    throw new Exception("City affairs visible geometry differs from the inspected design.");
            }
            AssertCityCompositionTarget("city-affairs-building", cityAffairsBuildingArea, cityAffairsArtArea);
            AssertCityCompositionTarget("city-affairs-caption", cityAffairsCaptionArea, cityAffairsArtArea);
            AssertCityCompositionTarget("city-overview-quests-caption", cityAffairsQuestCaptionArea, area);
        }
    }
}
