using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void OpenCityConstruction()
        {
            cityOverviewPlanning = false; ResetCityPanorama();
            tab = "主城"; cityPage = "construction"; map.CancelPointerInput();
        }

        private void DrawCityConstructionPage(Rect area)
        {
            float gap = 18, half = (area.width - gap) / 2;
            DrawCityActiveConstruction(new Rect(area.x, area.y, half, area.height));
            DrawCityCompletedConstruction(new Rect(area.x + half + gap, area.y, half, area.height));
        }

        private void DrawCityActiveConstruction(Rect area)
        {
            Frame(area, Panel);
            float x = area.x + 22, w = area.width - 44;
            Label(x, area.y + 16, w, 32, "建设进度 · 当前工地", heading);
            var job = world.CityJob;
            if (job == null)
            {
                DrawCityFacilityArt(new Rect(area.center.x - 110, area.y + 78, 220, 150), CityBuilding.TownHall);
                Label(x, area.y + 245, w, 34, "工匠待命", heading);
                Label(x, area.y + 290, w, 50, "选择设施查看升级，或在城内空地兴建。", small);
                Label(x, area.yMax - 116, w, 24, "项目配置 · 单一施工队列", tiny);
                var browse = new Rect(x, area.yMax - 72, (w - 12) / 2, 44);
                var plan = new Rect(browse.xMax + 12, browse.y, browse.width, 44);
                RecordReviewTarget("city-construction-catalog", browse);
                RecordReviewTarget("city-construction-plan", plan);
                if (Button(browse, "选择设施", true)) OpenCityFacilityCatalog();
                if (Button(plan, "城内选址")) { cityPage = "overview"; SetCityOverviewPlanning(true); }
                return;
            }

            var definition = CityCatalog.Get(job.building);
            int previous = world.cityPlots[job.plotIndex].level;
            DrawCityFacilityArt(new Rect(x, area.y + 68, 160, 118), job.building);
            Label(x + 174, area.y + 76, w - 174, 32, definition.name, heading);
            Label(x + 174, area.y + 117, w - 174, 27, "Lv. " + previous + " → " + job.targetLevel, body);
            Label(x + 174, area.y + 155, w - 174, 24, "城区 " + (job.plotIndex + 1) + " · 施工中", small);
            float progress = Mathf.Clamp01((float)(1 - job.remaining / Math.Max(1, job.duration)));
            Label(x, area.y + 204, w, 26, "剩余 " + CityTime(job.remaining) + "  /  总工期 " + CityTime(job.duration), small);
            RectFill(new Rect(x, area.y + 242, w, 8), Border);
            RectFill(new Rect(x, area.y + 242, w * progress, 8), Gold);
            DrawConstructionEffect(new Rect(x, area.y + 272, w, 88), job.building,
                CityCatalog.EffectAtLevel(job.building, previous), CityCatalog.EffectAtLevel(job.building, job.targetLevel), false);
            Label(x, area.y + 362, w, 25, "已投入资源", small);
            DrawCityResources(new Rect(x, area.y + 394, w, 58), job.paid);
            Label(x, area.yMax - 116, w, 30, "项目配置 · 完工自动生效，离开页面仍继续施工", tiny);
            var manage = new Rect(x, area.yMax - 72, (w - 12) / 2, 44);
            var cancel = new Rect(manage.xMax + 12, manage.y, manage.width, 44);
            RecordReviewTarget("city-construction-manage", manage);
            RecordReviewTarget("city-construction-cancel", cancel);
            if (Button(manage, "查看设施", true)) OpenCityFacilities(job.plotIndex);
            if (Button(cancel, "中断与返还")) ConfirmCityCancellation(job);
        }

        private void DrawCityCompletedConstruction(Rect area)
        {
            Frame(area, Ink);
            float x = area.x + 22, w = area.width - 44;
            Label(x, area.y + 16, w, 32, "最近完工", heading);
            var receipt = session.LastCityCompletion;
            if (receipt == null)
            {
                Label(x, area.y + 100, w, 40, "暂无本次会话完工记录", body);
                Label(x, area.y + 160, w, 62, "工程完成后，在这里查看设施等级与已生效的变化。", small);
                Label(x, area.yMax - 116, w, 52, "取消施工不会产生完工记录。\n已建成的设施可在设施一览中查看。", tiny);
                return;
            }

            DrawCityFacilityArt(new Rect(x, area.y + 68, 160, 118), receipt.building);
            Label(x + 174, area.y + 76, w - 174, 32, CityCatalog.Get(receipt.building).name, heading);
            Label(x + 174, area.y + 117, w - 174, 27, "Lv. " + receipt.previousLevel + " → " + receipt.targetLevel, body);
            Label(x + 174, area.y + 155, w - 174, 24, "城区 " + (receipt.plotIndex + 1) + " · 已完工", new GUIStyle(small) { normal = { textColor = Green } });
            Label(x, area.y + 204, w, 32, receipt.completedWhileAway ? "离线期间完成 · 效果已生效" : "建设完成 · 效果已生效", body);
            DrawConstructionEffect(new Rect(x, area.y + 272, w, 88), receipt.building, receipt.previousEffect, receipt.completedEffect, true);
            Label(x, area.y + 382, w, 48, "项目设施配置 · 无需领取，效果已经加入城池。", small);
            Label(x, area.yMax - 116, w, 38, "仅保留本次会话最近一次完工；关闭不会撤销效果。", tiny);
            var view = new Rect(x, area.yMax - 72, (w - 12) / 2, 44);
            var dismiss = new Rect(view.xMax + 12, view.y, view.width, 44);
            RecordReviewTarget("city-completion-view", view);
            RecordReviewTarget("city-completion-dismiss", dismiss);
            if (Button(view, "查看已建设施", true)) OpenCityFacilities(receipt.plotIndex);
            if (Button(dismiss, "关闭记录")) session.ClearCityCompletion(receipt);
        }

        private void DrawConstructionEffect(Rect area, CityBuilding building, int before, int after, bool complete)
        {
            Frame(area, C("25374A"));
            Label(area.x + 14, area.y + 8, area.width - 28, 22, complete ? "已生效的设施变化" : "完工后生效", tiny);
            Label(area.x + 14, area.y + 38, area.width - 28, 34,
                before + " → " + after + CityFacilityEffectUnit(building),
                new GUIStyle(small) { normal = { textColor = complete ? Green : Gold } });
        }
    }
}
