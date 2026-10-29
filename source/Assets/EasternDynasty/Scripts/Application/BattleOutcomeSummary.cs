using System.Collections.Generic;

namespace EasternDynasty
{
    // Display facts copied from a settled battle, never from the current army or catalog.
    public sealed class BattleOutcomeParticipant
    {
        public readonly int uid, level, rank, awardedExperience;
        public readonly string definitionId, name;

        internal BattleOutcomeParticipant(BattleParticipantSnapshot source)
        {
            uid = source.uid; definitionId = source.definitionId; name = source.name;
            level = source.level; rank = source.rank; awardedExperience = source.awardedExperience;
        }
    }

    // Transient immutable projection; viewing it has no settlement or persistence side effects.
    public sealed class BattleOutcomeSummary
    {
        public readonly string battleId, targetName, attackerName, defenderName;
        public readonly int targetId, targetX, targetZ, rounds;
        public readonly int attackerInitial, attackerFinal, defenderInitial, defenderFinal;
        public readonly int damageTotal, lossTotal, recoveryTotal, goldReward, experienceReward;
        public readonly bool won, captured;
        public readonly IReadOnlyList<BattleOutcomeParticipant> participants;

        private BattleOutcomeSummary(BattleRecord record, BattleReplayFrame result)
        {
            battleId = record.battleId; targetName = record.targetName;
            targetId = record.targetId; targetX = record.targetX; targetZ = record.targetZ;
            attackerName = record.attacker.name; defenderName = record.defender.name;
            attackerInitial = record.attacker.troops; attackerFinal = result.attackerAfter;
            defenderInitial = record.defender.troops; defenderFinal = result.defenderAfter;
            rounds = result.round; won = result.won; captured = result.captured;
            damageTotal = result.damageTotal; lossTotal = result.lossTotal; recoveryTotal = result.recoveryTotal;
            goldReward = result.goldReward; experienceReward = result.experienceReward;
            var copied = new List<BattleOutcomeParticipant>(record.attacker.generals.Count);
            foreach (var participant in record.attacker.generals)
                copied.Add(new BattleOutcomeParticipant(participant));
            participants = copied.AsReadOnly();
        }

        public static BattleOutcomeSummary Create(BattleRecord record)
        {
            // The final frame validates the whole event ledger and sums only recorded credited XP.
            // Legacy Chinese text and unknown/malformed versions cannot supply these display facts.
            var result = BattleReplayFrame.Create(record, int.MaxValue);
            return result == null ? null : new BattleOutcomeSummary(record, result);
        }
    }
}
