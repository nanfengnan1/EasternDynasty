using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private GUIStyle ownedName, ownedSmall, ownedStars, ownedValue;

        private List<GeneralCard> OwnedRosterCards()
        { return world.cards.OrderByDescending(c => c.Definition.stars).ThenBy(c => c.uid).ToList(); }

        private void RecordOwnedRosterTarget(string id, Rect rect)
        { if (GUI.enabled) RecordReviewTarget(id, rect); }

        private void InitOwnedRosterStyles()
        {
            if (ownedName != null) return;
            ownedName = new GUIStyle(cardName) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            ownedSmall = new GUIStyle(small) { fontSize = 16, wordWrap = false };
            ownedSmall.normal.textColor = C("C6BFAE");
            ownedStars = new GUIStyle(ownedSmall) { alignment = TextAnchor.MiddleRight };
            ownedStars.normal.textColor = Gold;
            ownedValue = new GUIStyle(body) { fontSize = 20, wordWrap = false };
        }

        private void DrawOwnedRoster()
        {
            InitOwnedRosterStyles();
            RectFill(main, C("181D1D"));
            DrawCardSeal(new Rect(main.x + 14, main.y + 10, 38, 38), "将", Gold);
            Label(main.x + 68, main.y + 4, 240, 38, "帐下武将", heading);
            Label(main.x + 70, main.y + 42, main.width - 270, 24,
                world.cards.Count + " 位武将  /  点选卡面查看与管理", ownedSmall);
            var back = new Rect(main.xMax - 182, main.y + 12, 168, 44);
            if (reinforcementEditingFormation)
            { RecordOwnedRosterTarget("reinforcement-formation-back", back); if (HudTextAction(back, "返回征兵")) ResumeReinforcement(); }
            else if (trainingEditingFormation)
            { RecordOwnedRosterTarget("training-formation-back", back); if (HudTextAction(back, "返回练兵")) ResumeTraining(); }
            else if (cityEditingFormation)
            { RecordOwnedRosterTarget("city-formation-back", back); if (HudTextAction(back, cityPage == "quest-detail" ? "返回任务目标" : "返回主城")) ReturnToCityFormationOrigin(); }

            float slotWidth = (main.width - 40) / 3;
            for (int i = 0; i < 3; i++)
            {
                Rect slot = new Rect(main.x + 12 + i * (slotWidth + 8), main.y + 76, slotWidth, 64);
                RectFill(slot, C("272D29"));
                RectFill(new Rect(slot.x, slot.yMax - 2, slot.width, 2), selectedSlot == i ? Gold : C("514C3E"));
                int uid = i < world.formation.Count ? world.formation[i] : 0;
                GeneralCard card = world.cards.Find(c => c.uid == uid);
                if (card != null) PortraitHead(new Rect(slot.x + 7, slot.y + 6, 50, 50), card.definition);
                else DrawCardSeal(new Rect(slot.x + 13, slot.y + 12, 38, 38), "空", Muted);
                HudFittedLabel(new Rect(slot.x + 67, slot.y + 6, slot.width - 79, 29),
                    Slots[i] + "  " + (card == null ? "待任命" : card.Definition.name), ownedValue, 15);
                Label(slot.x + 68, slot.y + 36, slot.width - 80, 24,
                    card == null ? "选择武将后任命" : "Lv." + card.level + "  ·  " + card.Definition.troop + "兵", ownedSmall);
                RecordOwnedRosterTarget("owned-formation-" + i, slot);
                if (ContentTileClicked(slot, "owned-formation-" + i, uid))
                { selectedSlot = i; if (card != null) { selectedCard = card.uid; generalScroll = Vector2.zero; } }
            }

            Rect area = new Rect(main.x + 12, main.y + 156, main.width - 24, main.height - 168);
            RecordOwnedRosterTarget("owned-roster-scroll", area);
            var cards = OwnedRosterCards();
            float gap = 18, innerWidth = area.width - 24;
            int columns = Mathf.Max(1, Mathf.FloorToInt((innerWidth + gap) / 242));
            float cardWidth = Mathf.Min(252, (innerWidth - (columns - 1) * gap) / columns);
            float cardHeight = (cardWidth - 38) * 1.45f + 80;
            float rowHeight = cardHeight + 20;
            int shownColumns = Math.Min(columns, cards.Count);
            float left = Mathf.Max(0, (innerWidth - shownColumns * cardWidth - Math.Max(0, shownColumns - 1) * gap) / 2);
            float contentHeight = Mathf.Max(area.height, Mathf.Ceil(cards.Count / (float)columns) * rowHeight);
            rosterScroll = BeginContentScroll(area, rosterScroll, new Rect(0, 0, innerWidth, contentHeight));
            for (int i = 0; i < cards.Count; i++)
            {
                GeneralCard card = cards[i];
                Rect r = new Rect(left + i % columns * (cardWidth + gap), i / columns * rowHeight, cardWidth, cardHeight);
                // Keep the control sequence stable while culling art. Only fully
                // visible cards are advertised to the real-input review driver.
                if (r.yMax < rosterScroll.y || r.y > rosterScroll.y + area.height)
                { GUI.Button(r, GUIContent.none, GUIStyle.none); continue; }
                Rect visible = new Rect(area.x + r.x, area.y + r.y - rosterScroll.y, r.width, r.height);
                if (visible.y >= area.y && visible.yMax <= area.yMax)
                {
                    RecordOwnedRosterTarget("owned-card-" + card.uid, visible);
                    if (i == 0) RecordOwnedRosterTarget("owned-card-first", visible);
                }
                if (DrawOwnedRosterCard(r, card)) { generalScroll = Vector2.zero; ShowCardDetails(card.definition, card.uid); }
            }
            GUI.EndScrollView(GUI.enabled);
        }

        private bool DrawOwnedRosterCard(Rect r, GeneralCard card)
        {
            GeneralDefinition g = card.Definition;
            bool selected = selectedCard == card.uid;
            Color accent = FactionColor(g.faction);
            RectFill(new Rect(r.x + 3, r.y + 4, r.width, r.height), new Color(0, 0, 0, .4f));
            RectFill(r, C("252824"));
            Rect art = new Rect(r.x + 35, r.y + 30, r.width - 39, r.height - 111);
            PortraitComplete(art, g.id);
            Rect rail = new Rect(r.x + 3, r.y + 3, 29, r.height - 84);
            RectFill(rail, C("30352D"));
            RectFill(new Rect(rail.x, rail.y, rail.width, 31), Color.Lerp(accent, C("252C29"), .4f));
            GUI.Label(new Rect(rail.x, rail.y, rail.width, 31), g.faction, centered);
            float step = Mathf.Min(29, (rail.height - 80) / Mathf.Max(1, g.name.Length));
            ownedName.fontSize = Mathf.Clamp(Mathf.FloorToInt(step - 3), 14, 24);
            for (int i = 0; i < g.name.Length; i++)
                GUI.Label(new Rect(rail.x, rail.y + 40 + i * step, rail.width, step + 2), g.name[i].ToString(), ownedName);
            DrawCardSeal(new Rect(rail.x + 1, rail.yMax - 29, 27, 27), g.troop, accent);
            Label(art.x + 3, r.y + 4, 70, 24, "C " + g.cost.ToString("0.0"), ownedSmall);
            GUI.Label(new Rect(art.x + 67, r.y + 4, art.width - 72, 24), new string('★', g.stars), ownedStars);

            RectFill(new Rect(r.x + 7, r.yMax - 78, r.width - 14, 1), C("756A52"));
            Label(r.x + 10, r.yMax - 75, 120, 27, "Lv." + card.level, ownedValue);
            GUI.Label(new Rect(r.xMax - 94, r.yMax - 72, 84, 25), card.rank + " / " + g.stars + " 阶", ownedStars);
            DrawOwnedExperience(new Rect(r.x + 10, r.yMax - 46, r.width - 20, 3), card);
            string skill = string.IsNullOrEmpty(g.nativeSkill) ? g.skill : g.nativeSkill;
            HudFittedLabel(new Rect(r.x + 10, r.yMax - 40, r.width - 20, 20), skill, ownedSmall, 13);
            int assignedSlot = world.formation.IndexOf(card.uid);
            Label(r.x + 10, r.yMax - 19, r.width - 20, 18,
                assignedSlot >= 0 ? "部队中  ·  " + Slots[assignedSlot] : "待命  ·  可编入部队", tiny);
            Outline(r, selected ? Gold : C("756A52"), selected ? 2 : 1);
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? r.x : r.xMax - 12, y = i < 2 ? r.y : r.yMax - 2;
                RectFill(new Rect(x, y, 12, 2), Gold);
                RectFill(new Rect(i % 2 == 0 ? r.x : r.xMax - 2, i < 2 ? r.y : r.yMax - 12, 2, 12), Gold);
            }
            return ContentTileClicked(r, g.id, card.uid);
        }

        private void DrawOwnedExperience(Rect bar, GeneralCard card)
        {
            RectFill(bar, C("4B4C41"));
            float progress = card.level >= card.LevelCap ? 1 : Mathf.Clamp01(card.experience / (float)Math.Max(1, card.level * 80));
            if (progress > 0) RectFill(new Rect(bar.x, bar.y, bar.width * progress, bar.height), Gold);
        }

        private void DrawOwnedGeneralPanel()
        {
            InitOwnedRosterStyles();
            var card = world.cards.Find(c => c.uid == selectedCard) ?? world.cards.FirstOrDefault();
            if (card == null) return;
            selectedCard = card.uid;
            GeneralDefinition g = card.Definition;
            Rect panel = new Rect(width - 340, 100, 308, height - 218);
            HudSurface(panel); Block(panel);
            float x = panel.x + 14, w = panel.width - 28;
            Label(x, panel.y + 10, w, 28, "编队与培养", ownedSmall);
            PortraitHead(new Rect(x, panel.y + 49, 68, 68), g.id);
            HudFittedLabel(new Rect(x + 80, panel.y + 49, w - 80, 33), g.name, heading, 18);
            Label(x + 80, panel.y + 88, w - 80, 25, g.faction + " · " + g.troop + "兵 · C " + g.cost.ToString("0.0"), ownedSmall);
            Label(x, panel.y + 127, w, 27, "Lv." + card.level + " / " + card.LevelCap + "     " + card.rank + " / " + g.stars + " 阶", ownedValue);
            DrawOwnedExperience(new Rect(x, panel.y + 161, w, 4), card);
            Label(x, panel.y + 173, w, 24, card.level >= card.LevelCap ? "等级已满" : "升级经验  " + card.experience + " / " + card.level * 80, ownedSmall);
            Rect detail = new Rect(x, panel.y + 207, w, 44);
            RecordOwnedRosterTarget("owned-general-detail", detail);
            if (HudTextAction(detail, "属性与战法详情  ›")) ShowCardDetails(g.id, card.uid);

            // Progression scrolls independently. Formation and training remain
            // visible at the foot of the panel at every supported safe area.
            Rect actions = new Rect(x, panel.yMax - 168, w, 154);
            Rect area = new Rect(x, panel.y + 267, w, actions.y - (panel.y + 267) - 12);
            RecordOwnedRosterTarget("owned-progress-scroll", area);
            float inner = w - 22;
            generalScroll = BeginContentScroll(area, generalScroll, new Rect(0, 0, inner, (g.s1 ? 256 : 306) + world.learnedSkills.Count * 50));
            Label(0, 0, inner, 24, "自带战法", ownedSmall);
            HudFittedLabel(new Rect(0, 27, inner, 28), g.skill, ownedValue, 15);
            Label(0, 62, inner, 46, "学习战法  " + (string.IsNullOrEmpty(card.learnedSkill) ? "尚未学习" : card.learnedSkill), small);
            RectFill(new Rect(0, 115, inner, 1), Border);
            bool assigned = world.formation.Contains(card.uid), marching = assigned && world.order != null;
            int materials = world.cards.Count(c => c.uid != card.uid && c.definition == card.definition && c.rank == 0 && !world.formation.Contains(c.uid));
            Label(0, 127, inner, 25, "进阶材料  ·  同名闲置卡 " + materials + " / 1", ownedSmall);
            string promotion = card.rank >= g.stars ? "已达进阶上限" : marching ? "出征中 · 暂不可进阶" : "消耗同名卡 · 进阶 +1";
            if (HudTextAction(new Rect(0, 159, inner, 44), promotion, false, !marching && card.rank < g.stars && materials > 0))
                Ask("消耗一张未上阵、未进阶的同名同星卡，为" + g.name + "提升一阶。", () => Act(world.Promote(card.uid)));
            Label(0, 215, inner, 30, g.s1 ? "拆解战法尚未开放" : "旧将拆解需换下部队", small);
            // Migrated legacy cards remain fully manageable in the S1 slot.
            bool known = world.learnedSkills.Contains(g.skill);
            if (!g.s1 && HudTextAction(new Rect(0, 252, inner, 44), known ? "此战法已解锁" : "拆解为「" + g.skill + "」", false, !assigned && !known))
                Ask("拆解将永久消耗编号 " + card.uid + " 的" + g.name + "，解锁「" + g.skill + "」。", () => Act(world.Dismantle(card.uid)));
            for (int i = 0; i < world.learnedSkills.Count; i++)
            {
                string skill = world.learnedSkills[i];
                if (HudTextAction(new Rect(0, (g.s1 ? 252 : 306) + i * 50, inner, 44), "学习" + skill + " · 100金币")) Act(world.Teach(card.uid, skill));
            }
            GUI.EndScrollView(GUI.enabled);

            RectFill(new Rect(actions.x, actions.y - 5, actions.width, 1), C("756A52"));
            float slotWidth = (w - 12) / 3;
            for (int i = 0; i < 3; i++)
            {
                Rect slot = new Rect(x + i * (slotWidth + 6), actions.y + 4, slotWidth, 44);
                RecordOwnedRosterTarget("owned-slot-" + i, slot);
                if (HudTextAction(slot, Slots[i], selectedSlot == i)) selectedSlot = i;
            }
            bool current = world.formation.Count > selectedSlot && world.formation[selectedSlot] == card.uid;
            string assignText = world.order != null ? "行军中 · 暂不可换将" : current ? "已任" + Slots[selectedSlot] : "任命为" + Slots[selectedSlot];
            Rect assign = new Rect(x, actions.y + 54, w, 44);
            RecordOwnedRosterTarget("owned-assign", assign);
            if (HudTextAction(assign, assignText, true, world.order == null && !current)) Act(world.Assign(card.uid, selectedSlot));
            Rect training = new Rect(x, actions.y + 108, w, 44);
            RecordOwnedRosterTarget("general-training", training);
            RecordOwnedRosterTarget("owned-card-training", training);
            if (HudTextAction(training, "部队练兵  ›")) OpenTraining();
        }
    }
}
