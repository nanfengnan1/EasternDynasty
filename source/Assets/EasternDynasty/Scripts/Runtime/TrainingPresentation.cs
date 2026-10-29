using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private int trainingSelectedTarget = -1;
        private string trainingReturnTab = "主城";
        private bool trainingEditingFormation;
        private Vector2 trainingTargetScroll;
        private bool TrainingActive { get { return world.order != null && world.order.action == (int)ArmyAction.Training; } }

        private bool IsTrainingLand(Land land)
        { return land != null && land.owned && land.Walkable && !land.home && !land.bridge && !land.capital && !world.IsCityDistrict(land.id); }

        private void OpenTraining(int target = -1)
        {
            if (world == null || !world.IsYouzhou) return;
            trainingHistoryOpen = false;
            trainingTargetPickerOpen = false;
            trainingExecutionScroll = Vector2.zero;
            trainingHistorySelection = null;
            reinforcementEditingFormation = false;
            if (trainingEditingFormation && tab == "武将") { ResumeTraining(); return; }
            if (tab != "练兵") trainingReturnTab = tab;
            if (TrainingActive) trainingSelectedTarget = world.order.target;
            else if (target >= 0 && target < world.lands.Count && IsTrainingLand(world.lands[target])) trainingSelectedTarget = target;
            else
            {
                var first = world.lands.Where(IsTrainingLand).OrderBy(t => world.TrainingProblem(t.id).Length == 0 ? 0 : 1)
                    .ThenBy(t => t.level).ThenBy(t => t.id).FirstOrDefault();
                trainingSelectedTarget = first == null ? -1 : first.id;
            }
            var ordered = world.lands.Where(IsTrainingLand).OrderBy(t => t.level).ThenBy(t => t.id).ToList();
            trainingTargetScroll = new Vector2(0, Math.Max(0, ordered.FindIndex(t => t.id == trainingSelectedTarget)) * 52);
            tab = "练兵"; map.CancelPointerInput();
        }

        private void ReturnFromTraining()
        { if (trainingTargetPickerOpen) { CloseTrainingTargetPicker(false); return; } if (trainingHistoryOpen) { CloseTrainingHistory(); return; } trainingEditingFormation = false; tab = string.IsNullOrEmpty(trainingReturnTab) || trainingReturnTab == "练兵" ? "主城" : trainingReturnTab; map.CancelPointerInput(); }

        private void EditTrainingFormation() { trainingEditingFormation = true; tab = "武将"; }
        private void ResumeTraining() { trainingEditingFormation = false; tab = "练兵"; map.CancelPointerInput(); }

        private void DrawTrainingPanel()
        {
            if (trainingTargetPickerOpen) { DrawTrainingTargetPicker(); return; }
            if (trainingHistoryOpen) { DrawTrainingHistory(); return; }
            var page = new Rect(24, 94, width - 48, height - 198); Block(page);
            Label(page.x + 4, page.y, page.width - 300, 37, "部队练兵", heading);
            Label(page.x + 155, page.y + 9, page.width - 460, 25, "选择土地  /  确认出发  /  完成返城", small);
            var back = new Rect(page.xMax - 176, page.y, 176, 38); RecordReviewTarget("training-back", back);
            if (Button(back, "返回" + trainingReturnTab)) ReturnFromTraining();
            var history = new Rect(back.x - 158, page.y, 146, 38); RecordReviewTarget("training-history", history);
            if (Button(history, "练兵记录")) OpenTrainingHistory();
            float gap = 18, leftWidth = (page.width - gap) * .54f;
            var squad = new Rect(page.x, page.y + 52, leftWidth, page.height - 52);
            var plan = new Rect(squad.xMax + gap, squad.y, page.width - leftWidth - gap, squad.height);
            Frame(squad, Ink); Frame(plan, Panel);
            DrawTrainingSquad(squad);
            DrawTrainingPlan(plan);
        }

        private void DrawTrainingSquad(Rect area)
        {
            Label(area.x + 18, area.y + 13, area.width - 36, 28, "第一部队  ·  " + world.troops + " 兵", body);
            Label(area.x + 18, area.y + 46, area.width - 36, 24, world.ArmyState, small);
            var active = world.ActiveGenerals;
            var preview = !TrainingActive ? TrainingPlanPreview.Create(world, trainingSelectedTarget) : null;
            float cardWidth = (area.width - 60) / 3, cardHeight = Mathf.Min(270, area.height - 255);
            for (int i = 0; i < 3; i++)
            {
                var card = new Rect(area.x + 18 + i * (cardWidth + 12), area.y + 84, cardWidth, cardHeight);
                if (i >= active.Count)
                {
                    Frame(card, Panel); Label(card.x + 12, card.y + 28, card.width - 24, 60, Slots[i] + "\n尚未配置", small);
                    continue;
                }
                var general = active[i];
                RecordReviewTarget("training-general-" + i, card);
                if (DefinitionTile(card, general.Definition, false, general)) ShowCardDetails(general.definition, general.uid);
                bool capped = general.level >= general.LevelCap;
                int reward = TrainingActive ? world.order.trainingExperience : world.TrainingExperience(trainingSelectedTarget);
                Label(card.x, card.yMax + 10, card.width, 22, capped ? "已达等级上限" : "经验 " + general.experience + " / " + general.level * 80, tiny);
                var settled = TrainingActive && world.order.trainingSettled ? world.LastTrainingReceipt : null;
                int settledIndex = settled == null ? -1 : settled.participantIds.IndexOf(general.uid);
                var estimate = preview == null ? null : preview.Participants.FirstOrDefault(p => p.Uid == general.uid);
                string gain = settledIndex >= 0 ? "实际 +" + settled.awardedExperience[settledIndex] + " 经验" : capped ? "本次不获经验" : estimate != null ? "预计 +" + estimate.AwardedExperience + "  ·  Lv." + estimate.LevelAfter : "最多 +" + reward + " 经验";
                Label(card.x, card.yMax + 34, card.width, 22, gain, new GUIStyle(small) { normal = { textColor = settledIndex >= 0 ? Green : capped ? Muted : Gold } });
                if (estimate != null) DrawTrainingExperienceBar(new Rect(card.x, card.yMax + 60, card.width, 5), estimate);
            }
            float progressY = area.y + 84 + cardHeight + 79;
            DrawTrainingProgress(new Rect(area.x + 18, progressY, area.width - 36, 62));
            var formation = new Rect(area.x + 18, area.yMax - 50, 146, 36); RecordReviewTarget("training-formation", formation);
            if (Button(formation, "调整部队", false, world.order == null)) EditTrainingFormation();
            Label(formation.xMax + 14, formation.y + 4, area.width - 198, 32, "出发后锁定参训武将", tiny);
        }

        private void DrawTrainingProgress(Rect area)
        {
            bool active = TrainingActive;
            int stage = !active ? 0 : world.order.returning ? 3 : world.order.phase == (int)ArmyPhase.Working ? 2 : 1;
            string[] labels = { "准备", "行军", "练兵", "返城" };
            float cell = area.width / 4;
            for (int i = 0; i < 4; i++)
            {
                RectFill(new Rect(area.x + i * cell, area.y, cell - 6, 3), i <= stage ? Gold : Border);
                Label(area.x + i * cell, area.y + 9, cell - 6, 25, (i + 1) + "  " + labels[i], i == stage ? small : tiny);
            }
            if (active && !world.order.returning) Label(area.x, area.y + 36, area.width, 24, world.ArmyState + "  ·  本阶段剩余 " + CityTime(world.order.remaining), small);
            else DrawTrainingReceipt(area.x, area.y + 36, area.width);
        }

        private void DrawTrainingReceipt(float x, float y, float w)
        {
            // Receipt is read from the authoritative Core state, including after reload.
            var receipt = world.LastTrainingReceipt;
            string text = receipt == null ? "完成后自动发放经验，无需再次领取。" : receipt.cancelled ? "最近练兵已取消 · 未发放经验" : "最近练兵已完成 · 合计 +" + receipt.awardedExperience.Sum() + " 经验已到账";
            Label(x, y, w, 24, text, new GUIStyle(tiny) { normal = { textColor = receipt == null ? Muted : receipt.cancelled ? Muted : Green } });
        }

        private void DrawTrainingPlan(Rect area)
        {
            if (TrainingActive) { DrawTrainingExecution(area); return; }
            float x = area.x + 18, w = area.width - 36;
            Label(x, area.y + 13, w, 28, "目标土地", body);
            var compare = new Rect(area.xMax - 172, area.y + 8, 154, 34);
            RecordReviewTarget("training-compare", compare);
            if (Button(compare, "比较土地")) OpenTrainingTargetPicker();
            var targets = world.lands.Where(IsTrainingLand).OrderBy(t => t.level).ThenBy(t => t.id).ToList();
            float listHeight = Mathf.Min(158, area.height * .34f);
            var view = new Rect(x, area.y + 48, w, listHeight);
            trainingTargetScroll = BeginContentScroll(view, trainingTargetScroll, new Rect(0, 0, w - 20, Math.Max(listHeight, targets.Count * 52)));
            for (int i = 0; i < targets.Count; i++)
            {
                var land = targets[i]; var row = new Rect(0, i * 52, w - 24, 46);
                bool visible = row.y >= trainingTargetScroll.y && row.yMax <= trainingTargetScroll.y + listHeight;
                if (visible)
                {
                    var screen = new Rect(view.x, view.y + row.y - trainingTargetScroll.y, row.width, row.height);
                    RecordReviewTarget("training-target-" + land.id, screen);
                    if (i == 0) RecordReviewTarget("training-target-first", screen);
                }
                if (Button(row, "Lv." + land.level + "  " + land.name + "  (" + land.x + "," + land.z + ")", trainingSelectedTarget == land.id)) trainingSelectedTarget = land.id;
            }
            GUI.EndScrollView(GUI.enabled);
            if (targets.Count == 0) Label(x + 12, view.y + 18, w - 24, 85, "尚无可练兵的己方资源地。\n先占领城区外土地，再安排练兵。", small);
            float y = view.yMax + 14;
            int reward = world.TrainingExperience(trainingSelectedTarget);
            string problem = world.TrainingProblem(trainingSelectedTarget);
            Label(x, y, w, 24, "政令  " + World.TrainingCommandCost + "   /   持有 " + world.AvailableCommands, body);
            Label(x, y + 30, w, 25, "练兵 " + CityTime(World.TrainingSeconds) + "  ·  每将最多 +" + reward + " 经验", small);
            var preview = TrainingPlanPreview.Create(world, trainingSelectedTarget);
            Label(x, y + 59, w, 25, preview.Reachable ? "含往返共 " + CityTime(preview.TotalSeconds) + "  ·  单程 " + CityTime(preview.MarchSeconds) : "暂无可通行路线", small);
            Label(x, y + 88, w, 35, "项目试玩配置 · 未复刻官方体力及经验分配", tiny);
            var action = new Rect(x, area.yMax - 55, w, 40);
            Label(x, y + 124, w, Math.Max(40, action.y - y - 132), problem.Length == 0 ? "全队参与，达到等级上限的武将不获经验。" : problem, small);
            if (targets.Count == 0)
            {
                RecordReviewTarget("training-map", action);
                if (Button(action, "前往占领土地", true)) GoToTrainingTerritory();
            }
            else
            {
                RecordReviewTarget("training-confirm", action);
                if (Button(action, "确认练兵", true, problem.Length == 0))
                {
                    int target = trainingSelectedTarget;
                    Ask("派第一部队前往 " + world.lands[target].name + " 练兵？\n消耗 " + World.TrainingCommandCost + " 政令 · 含往返 " + CityTime(preview.TotalSeconds) + "\n预计全队实得 " + preview.TotalExperience + " 经验（已计等级上限），完成后返城。", () => { Act(world.StartTraining(target)); Save(false); });
                }
            }
        }

        private void GoToTrainingTerritory()
        {
            tab = "领地";
            var target = world.lands.Where(t => !t.owned && !t.bridge && !t.capital && t.Walkable && world.AttackProblem(t.id).Length == 0).OrderBy(t => t.level).ThenBy(t => t.id).FirstOrDefault();
            if (target == null) { map.selected = world.homeId; map.CenterHome(); }
            else { map.selected = target.id; map.focus = new Vector3(target.x, 0, target.z); }
        }
    }
}
