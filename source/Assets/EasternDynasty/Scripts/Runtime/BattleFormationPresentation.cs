using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private BattleFormationSummary battleFormation;
        private bool battleFormationOpen, battleFormationFromReport;
        private int battleFormationSelected;
        private Vector2 battleFormationScroll;

        private void OpenBattleFormation(int participant = 0, bool fromReport = false)
        {
            var frozen = BattleFormationSummary.Create(replayRecord);
            if (frozen == null) return;
            battleFormation = frozen;
            battleFormationSelected = Mathf.Clamp(participant, 0, frozen.participants.Count - 1);
            battleFormationFromReport = fromReport;
            battleFormationOpen = true;
            battleFormationScroll = Vector2.zero;
            replayPlaying = false; replayClock = 0;
            map.CancelPointerInput();
        }

        private void CloseBattleFormation()
        {
            battleFormationOpen = false; replayPlaying = false;
            if (battleFormationFromReport) { tab = "战报"; RestoreArchivedReport(replayRecord); }
            battleFormationFromReport = false;
            map.CancelPointerInput();
        }

        private void SelectBattleParticipant(int participant)
        {
            if (battleFormation == null || participant < 0 || participant >= battleFormation.participants.Count) return;
            battleFormationSelected = participant;
            battleFormationScroll = Vector2.zero;
        }

        private void DrawBattleFormation(Rect page)
        {
            var frozen = battleFormation;
            if (frozen == null) { CloseBattleFormation(); return; }
            Label(page.x, page.y, page.width - 210, 36, "参战阵容  ·  " + frozen.targetName, heading);
            var back = new Rect(page.xMax - 180, page.y, 180, 42);
            RecordReviewTarget("formation-back", back);
            if (Button(back, battleFormationFromReport ? "返回战报" : replayShowOutcome ? "返回战果" : "返回战斗过程"))
            { CloseBattleFormation(); return; }
            Label(page.x, page.y + 42, page.width, 26, "战前记录 · 后续升级、学习战法和编队变动不改变本场阵容", small);

            float cell = (page.width - 24) / 3;
            for (int i = 0; i < 3; i++)
            {
                var tile = new Rect(page.x + i * (cell + 12), page.y + 80, cell, 88);
                Frame(tile, Panel);
                if (i >= frozen.participants.Count)
                { Label(tile.x + 18, tile.y + 26, tile.width - 36, 36, Slots[i] + " · 本场未配置", muted); continue; }
                var g = frozen.participants[i];
                var art = new Rect(tile.x + 8, tile.y + 7, 52, 74); Portrait(art, g.definitionId);
                Outline(tile, i == battleFormationSelected ? Gold : Border, i == battleFormationSelected ? 2 : 1);
                Label(art.xMax + 16, tile.y + 12, tile.width - 92, 30, Slots[i] + " · " + g.name, body);
                Label(art.xMax + 16, tile.y + 48, tile.width - 92, 26, "Lv." + g.level + "  /  " + g.rank + "阶   ·   查看战前卡牌", small);
                RecordReviewTarget("formation-participant-" + i, tile);
                if (ContentTileClicked(tile, "battle-formation-" + i, g.uid)) SelectBattleParticipant(i);
            }

            var viewport = new Rect(page.x, page.y + 182, page.width, page.height - 244);
            RecordReviewTarget("formation-content", viewport);
            float contentWidth = viewport.width - 24, contentHeight = Mathf.Max(412, viewport.height);
            battleFormationScroll = BeginContentScroll(viewport, battleFormationScroll, new Rect(0, 0, contentWidth, contentHeight));
            var participant = frozen.participants[battleFormationSelected];
            float artWidth = Mathf.Min(306, contentWidth * .27f);
            var portrait = new Rect(0, 0, artWidth, 406); Frame(portrait, Ink);
            PortraitComplete(new Rect(10, 10, artWidth - 20, 322), participant.definitionId);
            Outline(portrait, Gold);
            Label(16, 338, artWidth - 32, 32, participant.name, heading);
            Label(16, 376, artWidth - 32, 24, Slots[battleFormationSelected] + " · 战前身份 #" + participant.uid, small);
            float x = artWidth + 22, w = contentWidth - x;
            Label(x, 0, w, 30, "战前属性", heading);
            float statWidth = (w - 24) / 3;
            DrawFrozenBattleStat(new Rect(x, 44, statWidth, 80), "等级", "Lv." + participant.level);
            DrawFrozenBattleStat(new Rect(x + statWidth + 12, 44, statWidth, 80), "进阶", participant.rank + "阶");
            DrawFrozenBattleStat(new Rect(x + 2 * (statWidth + 12), 44, statWidth, 80), "基础攻击", participant.attack.ToString());
            Label(x, 140, w, 28, "携带战法 · 项目战斗配置", body);
            float skillWidth = (w - 12) / 2;
            DrawFrozenBattleSkill(new Rect(x, 182, skillWidth, 94), "自带", participant.skill, false);
            DrawFrozenBattleSkill(new Rect(x + skillWidth + 12, 182, skillWidth, 94), "学习", participant.learnedSkill, true);
            Label(x, 290, w, 28, "记录携带名称；本场未记录逐将发动次数或伤害。", small);
            Frame(new Rect(x, 332, w, 74), Panel);
            Label(x + 16, 342, w - 32, 27, "战前整队兵力   我方 " + frozen.attackerInitial + "    /    守军 " + frozen.defenderInitial, body);
            Label(x + 16, 375, w - 32, 24, "守军未配置独立武将 · 兵力不拆分给单将", small);
            GUI.EndScrollView(GUI.enabled);

            float y = page.yMax - 44;
            var previous = new Rect(page.x, y, 160, 42); RecordReviewTarget("formation-previous", previous);
            if (Button(previous, "上一位", false, battleFormationSelected > 0)) SelectBattleParticipant(battleFormationSelected - 1);
            var next = new Rect(page.x + 172, y, 160, 42); RecordReviewTarget("formation-next", next);
            if (Button(next, "下一位", false, battleFormationSelected + 1 < frozen.participants.Count)) SelectBattleParticipant(battleFormationSelected + 1);
            Label(page.x + 354, y + 6, page.width - 354, 30, "第 " + (battleFormationSelected + 1) + " / " + frozen.participants.Count + " 位  ·  查看不改变部队、战果或奖励", small);
        }

        private void DrawFrozenBattleStat(Rect area, string name, string value)
        {
            Frame(area, Panel); Label(area.x + 14, area.y + 10, area.width - 28, 24, name, small);
            Label(area.x + 14, area.y + 40, area.width - 28, 32, value, heading);
        }

        private void DrawFrozenBattleSkill(Rect area, string category, string value, bool learned)
        {
            Frame(area, Panel); RectFill(new Rect(area.x, area.y, 3, area.height), learned ? Green : Gold);
            Label(area.x + 16, area.y + 10, area.width - 32, 24, category, small);
            Label(area.x + 16, area.y + 42, area.width - 32, 42,
                value == null ? "未记录" : value.Length == 0 ? "未携带" : value, body);
        }
    }
}
