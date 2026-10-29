using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private bool trainingTargetPickerOpen;
        private int trainingCandidate = -1, trainingTargetSort;
        private Vector2 trainingPickerScroll;
        private string trainingCandidateSignature;
        private World trainingCandidateWorld;
        private List<TrainingPlanPreview> trainingCandidates = new List<TrainingPlanPreview>();

        private void OpenTrainingTargetPicker()
        {
            if (TrainingActive) return;
            RefreshTrainingCandidates();
            trainingCandidate = trainingSelectedTarget;
            trainingTargetPickerOpen = true;
            SortTrainingTargets(trainingTargetSort);
            map.CancelPointerInput();
        }

        private void RefreshTrainingCandidates()
        {
            string signature = string.Join(";", world.ActiveGenerals.Select(c => c.uid + ":" + c.definition + ":" + c.level + ":" + c.experience))
                + "|" + string.Join(";", world.lands.Where(IsTrainingLand).Select(t => t.id + ":" + t.level));
            if (signature == trainingCandidateSignature && trainingCandidateWorld == world) return;
            trainingCandidateWorld = world;
            trainingCandidateSignature = signature;
            trainingCandidates = world.lands.Where(IsTrainingLand).Select(t => TrainingPlanPreview.Create(world, t.id)).ToList();
            SortTrainingTargets(trainingTargetSort);
        }

        private void SortTrainingTargets(int sort)
        {
            trainingTargetSort = sort;
            trainingCandidates = (sort == 0
                ? trainingCandidates.OrderBy(p => p.Reachable ? 0 : 1).ThenBy(p => p.TotalSeconds).ThenByDescending(p => p.TotalExperience).ThenBy(p => p.Target)
                : trainingCandidates.OrderBy(p => p.Reachable ? 0 : 1).ThenByDescending(p => p.TotalExperience).ThenBy(p => p.TotalSeconds).ThenBy(p => p.Target)).ToList();
            trainingPickerScroll = Vector2.zero;
        }

        private void CloseTrainingTargetPicker(bool apply)
        {
            if (apply && trainingCandidates.Any(p => p.Target == trainingCandidate && p.Reachable) && !TrainingActive)
            {
                trainingSelectedTarget = trainingCandidate;
                var ordered = world.lands.Where(IsTrainingLand).OrderBy(t => t.level).ThenBy(t => t.id).ToList();
                trainingTargetScroll = new Vector2(0, Math.Max(0, ordered.FindIndex(t => t.id == trainingSelectedTarget)) * 52);
            }
            trainingTargetPickerOpen = false;
            map.CancelPointerInput();
        }

        private void DrawTrainingExperienceBar(Rect area, TrainingParticipantPreview participant)
        {
            RectFill(area, Border);
            float after = participant.LevelAfter >= participant.LevelCap ? 1 : participant.ExperienceAfter / (participant.LevelAfter * 80f);
            RectFill(new Rect(area.x, area.y, area.width * Mathf.Clamp01(after), area.height), Gold);
        }

        private void DrawTrainingTargetPicker()
        {
            RefreshTrainingCandidates();
            var page = new Rect(24, 94, width - 48, height - 198); Block(page);
            Label(page.x + 4, page.y, page.width - 200, 36, "选择练兵土地", heading);
            var back = new Rect(page.xMax - 176, page.y, 176, 38); RecordReviewTarget("training-picker-back", back);
            if (Button(back, "返回练兵方案")) { CloseTrainingTargetPicker(false); return; }
            float gap = 18, leftWidth = (page.width - gap) * .52f;
            var list = new Rect(page.x, page.y + 52, leftWidth, page.height - 52);
            var detail = new Rect(list.xMax + gap, list.y, page.width - leftWidth - gap, list.height);
            Frame(list, Ink); Frame(detail, Panel);
            for (int i = 0; i < 2; i++)
            {
                var sort = new Rect(list.x + 18 + i * 152, list.y + 14, 140, 36);
                RecordReviewTarget(i == 0 ? "training-sort-time" : "training-sort-gain", sort);
                if (Button(sort, i == 0 ? "用时最短" : "收益最多", trainingTargetSort == i)) SortTrainingTargets(i);
            }
            Label(list.x + 18, list.y + 60, list.width - 36, 24, trainingCandidates.Count + " 处己方土地  ·  点击比较，选定后再确认出发", tiny);
            var view = new Rect(list.x + 18, list.y + 92, list.width - 36, list.height - 110);
            RecordReviewTarget("training-picker-list", view);
            trainingPickerScroll = BeginContentScroll(view, trainingPickerScroll, new Rect(0, 0, view.width - 20, Math.Max(view.height, trainingCandidates.Count * 90)));
            for (int i = 0; i < trainingCandidates.Count; i++)
            {
                var candidate = trainingCandidates[i]; var land = world.lands[candidate.Target];
                var row = new Rect(0, i * 90, view.width - 24, 82);
                if (row.y >= trainingPickerScroll.y && row.yMax <= trainingPickerScroll.y + view.height)
                    RecordReviewTarget("training-picker-target-" + candidate.Target, new Rect(view.x, view.y + row.y - trainingPickerScroll.y, row.width, row.height));
                string text = "Lv." + land.level + "  " + land.name + "  (" + land.x + "," + land.z + ")\n"
                    + (candidate.Reachable ? "全队 +" + candidate.TotalExperience + " 经验  ·  往返共 " + CityTime(candidate.TotalSeconds) : "无可通行路线");
                if (Button(row, text, trainingCandidate == candidate.Target)) trainingCandidate = candidate.Target;
            }
            GUI.EndScrollView(GUI.enabled);
            if (trainingCandidates.Count == 0) Label(view.x + 8, view.y + 18, view.width - 16, 70, "尚无可练兵土地。\n返回方案，前往占领城区外资源地。", small);
            var chosen = trainingCandidates.FirstOrDefault(p => p.Target == trainingCandidate);
            if (chosen == null) { Label(detail.x + 18, detail.y + 20, detail.width - 36, 40, "选择左侧土地查看收益", body); return; }
            DrawTrainingCandidate(detail, chosen);
        }

        private void DrawTrainingCandidate(Rect area, TrainingPlanPreview preview)
        {
            float x = area.x + 18, w = area.width - 36;
            var land = world.lands[preview.Target];
            Label(x, area.y + 14, w, 30, "Lv." + land.level + "  " + land.name + "  (" + land.x + "," + land.z + ")", body);
            Label(x, area.y + 50, w, 28, "预计全队实得  +" + preview.TotalExperience + " 经验", new GUIStyle(body) { normal = { textColor = Gold } });
            float rowHeight = 84;
            for (int i = 0; i < preview.Participants.Count; i++)
            {
                var p = preview.Participants[i]; var definition = Catalog.Generals.Find(g => g.id == p.DefinitionId);
                float y = area.y + 96 + i * rowHeight;
                var portrait = new Rect(x, y, 66, 66); PortraitHead(portrait, p.DefinitionId); Outline(portrait, Border);
                float infoX = x + 82, infoWidth = w - 82;
                Label(infoX, y, infoWidth * .63f, 25, (definition == null ? p.DefinitionId : definition.name) + "  Lv." + p.LevelBefore + " → " + p.LevelAfter, small);
                Label(infoX + infoWidth * .64f, y, infoWidth * .36f, 25, p.AwardedExperience == 0 ? "已达上限" : "+" + p.AwardedExperience + " 经验", small);
                DrawTrainingExperienceBar(new Rect(infoX, y + 33, infoWidth, 7), p);
                Label(infoX, y + 47, infoWidth, 22, p.LevelAfter >= p.LevelCap ? "完成后达到等级上限" : "完成后经验 " + p.ExperienceAfter + " / " + p.LevelAfter * 80, tiny);
            }
            float timeY = area.y + 102 + 3 * rowHeight;
            Label(x, timeY, w, 26, preview.Reachable ? "出发 " + CityTime(preview.MarchSeconds) + "  /  练兵 " + CityTime(preview.WorkSeconds) + "  /  返城 " + CityTime(preview.MarchSeconds) : "此地无可通行路线", small);
            Label(x, timeY + 32, w, 26, "政令 " + World.TrainingCommandCost + "  ·  无额外资源费用 / 无战损", small);
            Label(x, timeY + 64, w, 42, "项目试玩配置；预计收益已计等级上限。\n作业完成才到账，取消未完成练兵无收益。", tiny);
            var apply = new Rect(x, area.yMax - 55, w, 40); RecordReviewTarget("training-picker-apply", apply);
            if (Button(apply, "选用此地 · 返回确认", true, preview.Reachable && !TrainingActive)) CloseTrainingTargetPicker(true);
        }
    }
}
