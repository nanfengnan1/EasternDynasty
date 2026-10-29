using System.Collections.Generic;

namespace EasternDynasty
{
    // These are recorded formation facts, not per-general combat actions or troop counts.
    public sealed class BattleFormationParticipant
    {
        public readonly int uid, level, rank, attack;
        public readonly string definitionId, name, skill, learnedSkill;

        internal BattleFormationParticipant(BattleParticipantSnapshot source)
        {
            uid = source.uid; definitionId = source.definitionId; name = source.name;
            level = source.level; rank = source.rank; attack = source.attack;
            // Preserve missing (null) and explicitly empty skill fields for truthful display.
            skill = source.skill; learnedSkill = source.learnedSkill;
        }
    }

    // Read-only transient projection of the settlement snapshot; no World or catalog lookup.
    public sealed class BattleFormationSummary
    {
        public readonly string battleId, targetName, attackerName, defenderName;
        public readonly int targetX, targetZ, attackerInitial, defenderInitial;
        public readonly IReadOnlyList<BattleFormationParticipant> participants;

        private BattleFormationSummary(BattleRecord record)
        {
            battleId = record.battleId; targetName = record.targetName;
            targetX = record.targetX; targetZ = record.targetZ;
            attackerName = record.attacker.name; defenderName = record.defender.name;
            attackerInitial = record.attacker.troops; defenderInitial = record.defender.troops;
            var copied = new List<BattleFormationParticipant>(record.attacker.generals.Count);
            foreach (var participant in record.attacker.generals)
                copied.Add(new BattleFormationParticipant(participant));
            participants = copied.AsReadOnly();
        }

        public static BattleFormationSummary Create(BattleRecord record)
        {
            // Legacy text, unknown versions and broken event ledgers cannot supply historical facts.
            return record != null && record.HasReplay && BattleReplay.IsValid(record)
                ? new BattleFormationSummary(record) : null;
        }
    }
}
