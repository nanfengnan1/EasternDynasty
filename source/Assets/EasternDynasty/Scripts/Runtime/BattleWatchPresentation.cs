using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private bool attackConfirmation, watchAttackOnArrival = true, watchingCurrentBattle;
        private string currentBattleId = "", pendingBattleWatchId = "", dismissedBattleWatchId = "";

        private bool AttackMarchActive
        {
            get { return world != null && world.order != null && !world.order.returning && world.order.action == (int)ArmyAction.Attack; }
        }

        private void BeginAttack(int target)
        {
            if (!world.IsYouzhou) { Act(world.March(target)); return; }
            var tile = world.lands[target];
            var path = world.FindMarchPath(world.homeId, target);
            if (path == null) { Act(world.AttackProblem(target)); return; }
            watchAttackOnArrival = true;
            Ask(tile.name + " Lv." + tile.level + "  (" + tile.x + ", " + tile.z + ")\n粮食 30 · 木材 20 · 行军约 " + Math.Ceiling(8 + world.MarchPathLength(path) * 4) + " 秒", () =>
            {
                attackConfirmation = false;
                string problem = world.AttackProblem(target);
                if (problem.Length > 0) { Act(problem); return; }
                Act(world.March(target, watchAttackOnArrival));
                if (!AttackMarchActive || world.order.target != target) return;
                currentBattleId = world.order.battleId;
                dismissedBattleWatchId = "";
                pendingBattleWatchId = "";
                ObserveBattleMarch();
                Save(false);
                if (watchAttackOnArrival) OpenCurrentBattle();
            });
            attackConfirmation = true;
        }

        // Observe before Tick too: one large online/offline step may finish the return journey.
        private void ObserveBattleMarch()
        {
            if (!AttackMarchActive) return;
            currentBattleId = world.order.battleId ?? "";
            if (world.order.watchOnArrival && currentBattleId != dismissedBattleWatchId)
                pendingBattleWatchId = currentBattleId;
        }

        private BattleRecord CurrentBattleRecord()
        {
            if (world == null || string.IsNullOrEmpty(currentBattleId)) return null;
            return world.reports.FirstOrDefault(r => r.HasReplay && r.battleId == currentBattleId);
        }

        private void UpdateBattleWatch()
        {
            if (world == null || mobileClock.IsPaused) return;
            ObserveBattleMarch();
            if (string.IsNullOrEmpty(pendingBattleWatchId)) return;
            var record = world.reports.FirstOrDefault(r => r.HasReplay && r.battleId == pendingBattleWatchId);
            if (record == null) return;
            // Keep an open receipt/overlay in its source page until it is dismissed.
            if (confirmation != null || cardDetailId != null || drawResult != null || cityQuestReceipt != null || tab == "战斗回放") return;
            pendingBattleWatchId = "";
            OpenCurrentBattleRecord(record);
        }

        private void OpenCurrentBattle()
        {
            if (AttackMarchActive)
            {
                currentBattleId = world.order.battleId ?? "";
                dismissedBattleWatchId = "";
                pendingBattleWatchId = currentBattleId;
                replayPlaying = false;
                tab = "战斗等待";
                map.CancelPointerInput();
                return;
            }
            var record = CurrentBattleRecord();
            // On a loaded campaign, use the latest actual replayable report; never infer events.
            if (record == null) record = world.reports.FirstOrDefault(r => r.HasReplay);
            if (record != null) OpenCurrentBattleRecord(record);
        }

        private void OpenCurrentBattleRecord(BattleRecord record)
        {
            currentBattleId = record.battleId;
            dismissedBattleWatchId = currentBattleId;
            pendingBattleWatchId = "";
            if (replayRecord == null || replayPlayer == null || replayRecord.battleId != record.battleId)
                OpenBattleReplay(record);
            else { tab = "战斗回放"; map.CancelPointerInput(); }
            watchingCurrentBattle = true;
            battleFormationOpen = false; battleFormationFromReport = false;
            replayShowOutcome = false;
            replayPlaying = replayPlayer.Position < record.events.Count - 1;
        }

        private void CloseCurrentBattle()
        {
            battleFormationOpen = false; battleFormationFromReport = false;
            dismissedBattleWatchId = currentBattleId;
            pendingBattleWatchId = "";
            replayPlaying = false;
            watchingCurrentBattle = false;
            tab = "领地";
            map.CancelPointerInput();
        }

        private void DrawBattleWaiting()
        {
            var page = new Rect(32, 98, width - 64, height - 206); Block(page);
            Label(page.x, page.y, page.width - 180, 40, "本场战斗 · 等待抵达", heading);
            var back = new Rect(page.xMax - 160, page.y, 160, 40); RecordReviewTarget("waiting-back", back);
            if (Button(back, "返回地图")) CloseCurrentBattle();
            bool marching = AttackMarchActive && world.order.battleId == currentBattleId;
            if (!marching)
            {
                var settled = CurrentBattleRecord();
                Label(page.x, page.y + 84, page.width, 60, settled == null
                    ? "部队已撤回，本次行军没有产生战斗记录。"
                    : "部队已经抵达，本场战斗已结算。可查看本次记录。", body);
                if (settled != null)
                {
                    var result = new Rect(page.x, page.y + 164, 240, 44); RecordReviewTarget("waiting-result", result);
                    if (Button(result, "观看本场过程", true)) OpenCurrentBattleRecord(settled);
                }
                return;
            }
            var order = world.order; var tile = world.lands[order.target];
            float side = page.width * .36f, cardWidth = Math.Min(178, (page.width - side - 80) / 3);
            Frame(new Rect(page.x, page.y + 70, page.width - side - 24, page.height - 146), Ink);
            Label(page.x + 22, page.y + 88, page.width - side - 64, 30, "第一部队  ·  " + world.troops + " 兵", heading);
            var generals = world.ActiveGenerals;
            for (int i = 0; i < generals.Count; i++)
            {
                var g = generals[i];
                var art = new Rect(page.x + 22 + i * (cardWidth + 12), page.y + 136, cardWidth, Math.Min(236, page.height - 245));
                Portrait(art, g.definition); Outline(art, Gold);
                Label(art.x, art.yMax + 12, art.width, 30, g.Definition.name + "  Lv." + g.level, small);
            }
            var info = new Rect(page.xMax - side, page.y + 70, side, page.height - 146); Frame(info, Panel);
            Label(info.x + 22, info.y + 20, info.width - 44, 38, tile.name + " · Lv." + tile.level, heading);
            Label(info.x + 22, info.y + 65, info.width - 44, 30, "目标坐标  (" + tile.x + ", " + tile.z + ")", small);
            Label(info.x + 22, info.y + 112, info.width - 44, 46, Math.Ceiling(order.remaining) + " 秒后抵达", title);
            var progress = new Rect(info.x + 22, info.y + 176, info.width - 44, 10); RectFill(progress, Ink);
            RectFill(new Rect(progress.x, progress.y, progress.width * Mathf.Clamp01((float)(1 - order.remaining / order.duration)), progress.height), Green);
            Label(info.x + 22, info.y + 208, info.width - 44, 75, "尚未交战\n抵达后自动观看本次结算记录", body);
            Label(page.x, page.yMax - 56, page.width - 240, 52, (speed == 0 ? "在线时间暂停 · 点击底栏时间继续" : "行军继续推进 · 返回地图不撤军") + "\n抵达时结算一次，播放与倍速只影响观看", small);
            var recall = new Rect(page.xMax - 190, page.yMax - 50, 190, 42); RecordReviewTarget("waiting-recall", recall);
            if (Button(recall, "撤回部队")) AskBattleRecall();
        }

        private void AskBattleRecall()
        {
            Ask("撤回后本次行军不会交战，已付出征资源不退回。", () =>
            {
                // Arrival may occur while this confirmation is open. Do not call a
                // settled battle a cancelled march or silently discard its viewing intent.
                var settled = CurrentBattleRecord();
                if (settled != null)
                {
                    status = "部队已抵达并结算，撤回操作未改变本场结果。";
                    OpenCurrentBattleRecord(settled);
                    return;
                }
                if (AttackMarchActive && world.order.battleId == currentBattleId) Act(world.RecallArmy());
                CloseCurrentBattle(); Save(false);
            });
        }

        private void DrawBattleWatchEntry(Rect rect)
        {
            if (!world.IsYouzhou) return;
            bool active = AttackMarchActive;
            if (!active && !world.reports.Any(r => r.HasReplay)) return;
            RecordReviewTarget("army-battle-watch", rect); Block(rect);
            if (Button(rect, active ? "进入战斗 · 等待抵达" : "观看最近战斗", true)) OpenCurrentBattle();
        }
    }
}
