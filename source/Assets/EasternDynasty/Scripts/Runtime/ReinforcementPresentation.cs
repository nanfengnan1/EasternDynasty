using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private int reinforcementAmount = 200;
        private string reinforcementReturnTab = "主城", reinforcementReturnPage = "overview";
        private bool reinforcementEditingFormation;
        private Vector2 reinforcementCostScroll;
        private ReinforcementReceipt reinforcementReceipt;

        private void OpenReinforcement()
        {
            if (world == null || !world.IsYouzhou) return;
            if (tab != "征兵") { reinforcementReturnTab = tab; reinforcementReturnPage = cityPage; }
            reinforcementAmount = Math.Min(200, Math.Max(0, world.TroopCap - world.troops));
            reinforcementReceipt = null; reinforcementCostScroll = Vector2.zero;
            reinforcementEditingFormation = false; tab = "征兵"; map.CancelPointerInput();
        }

        private void ReturnFromReinforcement()
        {
            tab = reinforcementReturnTab; cityPage = reinforcementReturnPage;
            reinforcementReceipt = null; reinforcementEditingFormation = false; map.CancelPointerInput();
        }

        private void ResumeReinforcement()
        { reinforcementEditingFormation = false; tab = "征兵"; map.CancelPointerInput(); }

        private CityQuestDefinition ReinforcementReadyQuest
        {
            get
            {
                var quest = world == null ? null : world.CurrentCityQuest;
                return reinforcementReceipt != null && quest != null && quest.action == "recruit"
                    && world.CityQuestReady(quest.id) ? quest : null;
            }
        }

        private void GoToReinforcementQuest()
        {
            // Navigation never claims the reward or repeats the completed recruitment.
            if (ReinforcementReadyQuest == null) return;
            reinforcementReceipt = null; reinforcementEditingFormation = false;
            OpenCityQuests();
        }

        private void ConfirmReinforcement()
        {
            var plan = ReinforcementPlan.Create(world, reinforcementAmount);
            if (!plan.CanExecute) { status = plan.problem; return; }
            Ask("确认补充 " + plan.amount + " 名士卒？\n粮食 " + plan.cost.grain.ToString("0.##") + " · 木材 " + plan.cost.grass.ToString("0.##")
                + " · 铁矿 " + plan.cost.iron.ToString("0.##") + "\n项目即时征兵，确认后立即扣费并补充整队兵力。", () =>
            {
                string message; ReinforcementReceipt receipt;
                if (plan.TryExecute(world, out receipt, out message)) { reinforcementReceipt = receipt; Save(false); }
                status = message;
            });
        }

        private void DrawReinforcementPanel()
        {
            var area = new Rect(24, 90, width - 48, height - 188); Block(area);
            Label(area.x + 4, area.y, area.width - 170, 40, "部队征兵  ·  " + (reinforcementReceipt == null ? "准备补充" : "补充完成"), heading);
            var back = new Rect(area.xMax - 154, area.y, 154, 36); RecordReviewTarget("reinforcement-back", back);
            if (Button(back, reinforcementReturnTab == "主城" ? "返回" + (reinforcementReturnPage == "quest-detail" ? "任务目标" : reinforcementReturnPage == "quests" ? "主要事宜" : "主城") : "返回" + reinforcementReturnTab)) ReturnFromReinforcement();
            var content = new Rect(area.x, area.y + 52, area.width, area.height - 52);
            var army = new Rect(content.x, content.y, content.width * .48f, content.height);
            var planArea = new Rect(army.xMax + 16, content.y, content.width - army.width - 16, content.height);
            Frame(army, Ink); Frame(planArea, Ink);
            DrawReinforcementArmy(army);
            if (reinforcementReceipt != null) DrawReinforcementReceipt(planArea);
            else DrawReinforcementPlan(planArea);
        }

        private void DrawReinforcementArmy(Rect area)
        {
            float x = area.x + 18, w = area.width - 36;
            Label(x, area.y + 12, w, 28, "第一部队  ·  " + world.ArmyState, body);
            Label(x, area.y + 48, w, 26, "整队兵力  " + world.troops + " / " + world.TroopCap, small);
            float cardHeight = Mathf.Clamp(area.height - 200, 145, 265), cardWidth = (w - 20) / 3;
            var active = world.ActiveGenerals;
            for (int i = 0; i < 3; i++)
            {
                var card = new Rect(x + i * (cardWidth + 10), area.y + 97, cardWidth, cardHeight);
                Label(card.x, card.y - 23, card.width, 22, Slots[i], tiny);
                if (i < active.Count)
                {
                    RecordReviewTarget("reinforcement-general-" + i, card);
                    if (DefinitionTile(card, active[i].Definition, false, active[i])) ShowCardDetails(active[i].definition, active[i].uid);
                }
                else { Frame(card, Panel); GUI.Label(card, "尚未配置", centered); }
            }
            float barY = area.y + 97 + cardHeight + 14;
            RectFill(new Rect(x, barY, w, 7), Border);
            RectFill(new Rect(x, barY, w * Mathf.Clamp01((float)world.troops / Math.Max(1, world.TroopCap)), 7), Green);
            Label(x, barY + 15, w, 35, "征兵补充当前部队总兵力。", tiny);
            var formation = new Rect(x, area.yMax - 50, 150, 36); RecordReviewTarget("reinforcement-formation", formation);
            if (Button(formation, "调整编队", false, world.order == null && reinforcementReceipt == null))
            {
                trainingEditingFormation = cityEditingFormation = false;
                reinforcementEditingFormation = true; tab = "武将"; map.CancelPointerInput();
            }
            Label(formation.xMax + 12, formation.y + 2, w - 166, 36, "项目配置 · 即时完成", tiny);
        }

        private void DrawReinforcementPlan(Rect area)
        {
            float x = area.x + 18, w = area.width - 36;
            int missing = Math.Max(0, world.TroopCap - world.troops);
            reinforcementAmount = Math.Max(0, Math.Min(missing, reinforcementAmount));
            var plan = ReinforcementPlan.Create(world, reinforcementAmount);
            Label(x, area.y + 12, w, 28, "本次补充  +" + plan.amount + "  /  缺额 " + missing, body);
            float cell = (w - 24) / 4;
            string[] labels = { "−50", "+50", "资源可征", "补至上限" };
            for (int i = 0; i < 4; i++)
            {
                var option = new Rect(x + i * (cell + 8), area.y + 50, cell, 36);
                RecordReviewTarget("reinforcement-amount-" + i, option);
                if (Button(option, labels[i], false, missing > 0 && world.order == null))
                {
                    reinforcementAmount = i == 0 ? Math.Max(0, reinforcementAmount - 50) : i == 1 ? Math.Min(missing, reinforcementAmount + 50) : i == 2 ? plan.affordable : missing;
                }
            }
            var slider = new Rect(x + 4, area.y + 100, w - 8, 26); RecordReviewTarget("reinforcement-slider", slider);
            bool wasEnabled = GUI.enabled; GUI.enabled = wasEnabled && missing > 0 && world.order == null;
            reinforcementAmount = Mathf.RoundToInt(GUI.HorizontalSlider(slider, reinforcementAmount, 0, Math.Max(1, missing)));
            GUI.enabled = wasEnabled;
            plan = ReinforcementPlan.Create(world, reinforcementAmount);
            Label(x, area.y + 130, w, 25, "完成后 " + (world.troops + plan.amount) + " / " + world.TroopCap + " 兵  ·  资源最多可征 " + plan.affordable, small);
            var view = new Rect(x, area.y + 169, w, Math.Max(72, area.height - 300));
            RecordReviewTarget("reinforcement-cost-scroll", view);
            reinforcementCostScroll = BeginContentScroll(view, reinforcementCostScroll, new Rect(0, 0, w - 20, 209));
            DrawReinforcementCostRow(0, w - 25, 1, "粮食", plan.cost.grain, world.resources.grain);
            DrawReinforcementCostRow(53, w - 25, 0, "木材", plan.cost.grass, world.resources.grass);
            DrawReinforcementCostRow(106, w - 25, 3, "铁矿", plan.cost.iron, world.resources.iron);
            Label(4, 166, w - 30, 38, "每兵：粮 1 · 木 0.4 · 铁 0.1；无需政令。\n项目即时补兵，无征兵倒计时或取消退款。", tiny);
            GUI.EndScrollView(GUI.enabled);
            var currentQuest = world.CurrentCityQuest;
            string nextStep = currentQuest != null && currentQuest.action == "recruit" && !world.CityQuestReady(currentQuest.id)
                ? "确认后将完成「" + currentQuest.title + "」。任务奖励需前往主要事宜另行领取。"
                : "核对人数与费用后确认；打开页面和返回均不扣费。";
            Label(x, area.yMax - 116, w, 48, plan.CanExecute ? nextStep : plan.problem, new GUIStyle(small) { normal = { textColor = plan.CanExecute ? Muted : Gold } });
            var confirm = new Rect(x, area.yMax - 55, w, 40);
            if (plan.CanExecute) RecordReviewTarget("reinforcement-confirm", confirm);
            if (Button(confirm, "确认征兵  +" + plan.amount, true, plan.CanExecute)) ConfirmReinforcement();
        }

        private void DrawReinforcementCostRow(float y, float w, int icon, string name, double cost, double owned)
        {
            Frame(new Rect(0, y, w, 47), Panel); ResourceIcon(new Rect(8, y + 10, 30, 25), icon);
            Label(46, y + 3, w - 56, 22, name + "  " + cost.ToString("0.##") + "  /  持有 " + owned.ToString("0.##"), tiny);
            double after = Math.Abs(owned - cost) < .00001 ? 0 : owned - cost;
            Label(46, y + 25, w - 56, 20, after < 0 ? "尚缺 " + (-after).ToString("0.##") : "剩余 " + after.ToString("0.##"), new GUIStyle(tiny) { normal = { textColor = after < 0 ? Red : Green } });
        }

        private void DrawReinforcementReceipt(Rect area)
        {
            var receipt = reinforcementReceipt; float x = area.x + 22, w = area.width - 44;
            Label(x, area.y + 20, w, 36, "士卒已编入部队", heading);
            Label(x, area.y + 67, w, 63, "+" + receipt.amount, new GUIStyle(display) { fontSize = 52, normal = { textColor = Green } });
            Label(x, area.y + 141, w, 32, receipt.beforeTroops + " → " + receipt.afterTroops + " / " + receipt.capacity + " 兵", body);
            Label(x, area.y + 188, w, 27, "实际支出", small);
            double[] paid = { receipt.cost.grain, receipt.cost.grass, receipt.cost.iron };
            int[] icons = { 1, 0, 3 }; string[] names = { "粮食", "木材", "铁矿" };
            float cell = (w - 20) / 3;
            for (int i = 0; i < 3; i++)
            {
                var cost = new Rect(x + i * (cell + 10), area.y + 224, cell, 90); Frame(cost, Panel);
                ResourceIcon(new Rect(cost.x + 10, cost.y + 10, 30, 25), icons[i]);
                Label(cost.x + 48, cost.y + 12, cell - 58, 25, names[i], tiny);
                GUI.Label(new Rect(cost.x + 8, cost.y + 48, cell - 16, 29), paid[i].ToString("0.##"), centered);
            }
            Label(x, area.y + 335, w, 45, "士卒已直接编入，无需领取。返回或再次打开页面不会重复补兵。", small);
            var quest = ReinforcementReadyQuest;
            if (quest != null)
            {
                var next = new Rect(x, area.y + 389, w, 100); Frame(next, Panel);
                Label(next.x + 12, next.y + 8, next.width - 24, 26, "事宜已成  ·  " + quest.title, new GUIStyle(body) { normal = { textColor = Gold } });
                Label(next.x + 12, next.y + 36, next.width - 24, 22, "以下任务奖励尚未领取", tiny);
                DrawQuestRewards(new Rect(next.x + 12, next.y + 59, next.width - 24, 34), quest.reward);
            }
            float doneWidth = quest == null ? w : (w - 12) / 2;
            var done = new Rect(x, area.yMax - 55, doneWidth, 40); RecordReviewTarget("reinforcement-done", done);
            if (Button(done, "完成 · 返回来源", true)) ReturnFromReinforcement();
            if (quest != null)
            {
                var go = new Rect(done.xMax + 12, done.y, doneWidth, done.height);
                RecordReviewTarget("reinforcement-quest", go);
                if (Button(go, "前往任务 · 领取奖励", true)) GoToReinforcementQuest();
            }
        }
    }
}
