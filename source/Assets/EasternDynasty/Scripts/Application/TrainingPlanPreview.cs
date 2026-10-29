using System;
using System.Collections.Generic;

namespace EasternDynasty
{
    // A detached preparation preview, never a receipt or a persisted training order.
    // StartTraining remains authoritative for eligibility, cost and frozen participants.
    public sealed class TrainingPlanPreview
    {
        public readonly int Target, ConfiguredExperience, TotalExperience;
        public readonly bool Reachable;
        public readonly double MarchSeconds, WorkSeconds, TotalSeconds;
        public readonly IReadOnlyList<TrainingParticipantPreview> Participants;

        private TrainingPlanPreview(World world, int target)
        {
            Target = target;
            ConfiguredExperience = world.IsYouzhou ? world.TrainingExperience(target) : 0;
            var path = ConfiguredExperience > 0 ? world.FindMarchPath(world.homeId, target) : null;
            Reachable = path != null;
            MarchSeconds = Reachable ? world.MarchDurationSeconds(path) : double.PositiveInfinity;
            WorkSeconds = World.TrainingSeconds;
            TotalSeconds = MarchSeconds * 2 + WorkSeconds;

            var participants = new List<TrainingParticipantPreview>();
            foreach (var card in world.ActiveGenerals)
            {
                var participant = new TrainingParticipantPreview(card, ConfiguredExperience);
                participants.Add(participant);
                TotalExperience += participant.AwardedExperience;
            }
            Participants = participants.AsReadOnly();
        }

        public static TrainingPlanPreview Create(World world, int target)
        { return world == null ? null : new TrainingPlanPreview(world, target); }
    }

    public sealed class TrainingParticipantPreview
    {
        public readonly int Uid, LevelBefore, ExperienceBefore, LevelAfter, ExperienceAfter, AwardedExperience, LevelCap;
        public readonly string DefinitionId;

        internal TrainingParticipantPreview(GeneralCard card, int configuredExperience)
        {
            Uid = card.uid; DefinitionId = card.definition;
            LevelBefore = card.level; ExperienceBefore = card.experience; LevelCap = card.LevelCap;
            // Match SettleTraining's cap before calling the actual growth rule on a copy.
            int capacity = -card.experience;
            for (int level = card.level; level < LevelCap; level++) capacity += level * 80;
            AwardedExperience = card.level >= LevelCap ? 0 : Math.Max(0, Math.Min(configuredExperience, capacity));
            var projected = new GeneralCard { uid = card.uid, definition = card.definition,
                level = card.level, experience = card.experience, rank = card.rank, learnedSkill = card.learnedSkill };
            if (card.level < LevelCap) projected.GainExperience(AwardedExperience);
            LevelAfter = projected.level; ExperienceAfter = projected.experience;
        }
    }
}
