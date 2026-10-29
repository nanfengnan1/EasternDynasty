using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    // These values are persisted. The current project resolver models whole armies, not individual attacks.
    public enum BattleReplayEventKind { Start = 0, Attack = 1, Loss = 2, Recovery = 3, Result = 4 }

    [Serializable]
    public class BattleParticipantSnapshot
    {
        public int uid, level, rank, attack, awardedExperience;
        public string definitionId, name, skill, learnedSkill;
    }

    [Serializable]
    public class BattleArmySnapshot
    {
        public string name;
        public int troops;
        public List<BattleParticipantSnapshot> generals = new List<BattleParticipantSnapshot>();
    }

    [Serializable]
    public class BattleReplayEvent
    {
        public int sequence, round, amount, attackerTroops, defenderTroops;
        public BattleReplayEventKind kind;
        public string text;
    }

    // Presentation advances a cursor over recorded facts. It has no World reference or settlement API.
    public sealed class BattleReplayPlayer
    {
        private readonly BattleRecord record;
        public BattleReplayPlayer(BattleRecord source) { record = source; Position = 0; }
        public int Position { get; private set; }
        public int Count { get { return record != null && record.HasReplay ? record.events.Count : 0; } }
        public BattleReplayEvent Current { get { return Count == 0 ? null : record.events[Position]; } }
        public void Seek(int position) { Position = Math.Max(0, Math.Min(Math.Max(0, Count - 1), position)); }
        public void Step(int offset) { Seek(Position + offset); }
    }

    public static class BattleReplay
    {
        public const string RuleVersion = "project-army-combat-v1";

        // Checks only the frozen record: no catalog lookup and no world mutation or re-resolution.
        public static bool IsValid(BattleRecord record)
        {
            if (record == null || double.IsNaN(record.time) || double.IsInfinity(record.time) || record.time < 0) return false;
            if (record.replayVersion == 0) return record.events == null || record.events.Count == 0;
            if (!record.HasReplay || record.ruleVersion != RuleVersion || string.IsNullOrEmpty(record.battleId)
                || string.IsNullOrEmpty(record.targetName) || record.targetId < 0 || record.targetX < 0 || record.targetZ < 0
                || !ValidArmy(record.attacker, true) || !ValidArmy(record.defender, false)
                || record.attackerFinal < 0 || record.defenderFinal < 0 || record.goldReward < 0 || record.experienceReward < 0
                || record.events.Count > 26 || record.captured != record.won) return false;
            int army = record.attacker.troops, enemy = record.defender.troops, round = 0;
            var previous = BattleReplayEventKind.Start;
            for (int i = 0; i < record.events.Count; i++)
            {
                var item = record.events[i];
                if (item == null || item.sequence != i || item.amount < 0 || string.IsNullOrEmpty(item.text)) return false;
                switch (item.kind)
                {
                    case BattleReplayEventKind.Start:
                        if (i != 0 || item.round != 0 || item.amount != 0) return false;
                        break;
                    case BattleReplayEventKind.Attack:
                        if (i == 0 || (previous != BattleReplayEventKind.Start && previous != BattleReplayEventKind.Loss && previous != BattleReplayEventKind.Recovery)
                            || army <= 0 || enemy <= 0 || item.round != round + 1 || item.round > 8 || item.amount < 1 || item.amount > enemy) return false;
                        round = item.round; enemy -= item.amount;
                        break;
                    case BattleReplayEventKind.Loss:
                        if (previous != BattleReplayEventKind.Attack || item.round != round || item.amount < 1 || item.amount > army) return false;
                        army -= item.amount;
                        break;
                    case BattleReplayEventKind.Recovery:
                        if (previous != BattleReplayEventKind.Loss || item.round != round || army <= 0 || item.amount < 1 || item.amount > record.attacker.troops - army) return false;
                        army += item.amount;
                        break;
                    case BattleReplayEventKind.Result:
                        if (i != record.events.Count - 1 || (previous != BattleReplayEventKind.Loss && previous != BattleReplayEventKind.Recovery)
                            || item.round != round || item.amount != 0 || (army > 0 && enemy > 0 && round < 8)) return false;
                        break;
                    default: return false;
                }
                if (item.attackerTroops != army || item.defenderTroops != enemy) return false;
                previous = item.kind;
            }
            return previous == BattleReplayEventKind.Result && army == record.attackerFinal && enemy == record.defenderFinal
                && record.won == (enemy == 0 && army > 0) && (record.won || record.goldReward == 0);
        }

        private static bool ValidArmy(BattleArmySnapshot army, bool player)
        {
            if (army == null || string.IsNullOrEmpty(army.name) || army.troops < 1 || army.generals == null || army.generals.Count > 3
                || player && army.generals.Count < 1) return false;
            return army.generals.All(g => g != null && g.uid > 0 && g.level > 0 && g.rank >= 0 && g.attack >= 0 && g.awardedExperience >= 0
                && !string.IsNullOrEmpty(g.definitionId) && !string.IsNullOrEmpty(g.name))
                && army.generals.Select(g => g.uid).Distinct().Count() == army.generals.Count;
        }
    }
}
