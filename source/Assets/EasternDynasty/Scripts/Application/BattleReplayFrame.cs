using System;

namespace EasternDynasty
{
    // Transient display state calculated from the frozen event ledger. No live battle is advanced.
    public sealed class BattleReplayFrame
    {
        public readonly int position, round, amount;
        public readonly BattleReplayEventKind kind;
        public readonly int attackerBefore, attackerAfter, defenderBefore, defenderAfter;
        public readonly int damageTotal, lossTotal, recoveryTotal;
        public readonly bool isResult, won, captured;
        public readonly int goldReward, experienceReward;

        private BattleReplayFrame(BattleRecord record, int cursor)
        {
            position = cursor;
            var current = record.events[cursor];
            round = current.round; kind = current.kind; amount = current.amount;
            attackerBefore = cursor == 0 ? record.attacker.troops : record.events[cursor - 1].attackerTroops;
            defenderBefore = cursor == 0 ? record.defender.troops : record.events[cursor - 1].defenderTroops;
            attackerAfter = current.attackerTroops; defenderAfter = current.defenderTroops;
            for (int i = 0; i <= cursor; i++)
            {
                var item = record.events[i];
                if (item.kind == BattleReplayEventKind.Attack) damageTotal += item.amount;
                else if (item.kind == BattleReplayEventKind.Loss) lossTotal += item.amount;
                else if (item.kind == BattleReplayEventKind.Recovery) recoveryTotal += item.amount;
            }
            isResult = current.kind == BattleReplayEventKind.Result;
            if (isResult)
            {
                won = record.won; captured = record.captured; goldReward = record.goldReward;
                foreach (var general in record.attacker.generals) experienceReward += general.awardedExperience;
            }
        }

        public static BattleReplayFrame Create(BattleRecord record, int position)
        {
            // Historical text and malformed/unknown records cannot supply recorded display facts.
            if (record == null || !record.HasReplay || !BattleReplay.IsValid(record)) return null;
            return new BattleReplayFrame(record, Math.Max(0, Math.Min(record.events.Count - 1, position)));
        }
    }
}
