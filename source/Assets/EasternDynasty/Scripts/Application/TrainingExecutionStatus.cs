using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    // Detached, read-only view of the current order. No simulation or settlement.
    public sealed class TrainingExecutionStatus
    {
        public readonly int Target, Stage, ConfiguredExperience, AwardedExperience;
        public readonly bool Settled, Cancelled, ReceiptAvailable;
        public readonly double PhaseRemaining, PhaseProgress, UntilAward, UntilHome;
        public readonly IReadOnlyList<int> Participants;

        private TrainingExecutionStatus(World world)
        {
            var order = world.order;
            Target = order.target;
            Stage = order.returning ? 2 : order.phase == (int)ArmyPhase.Working ? 1 : 0;
            Settled = order.trainingSettled; Cancelled = order.trainingCancelled;
            ConfiguredExperience = order.trainingExperience;
            Participants = new List<int>(order.trainingParticipants).AsReadOnly();
            PhaseRemaining = Math.Max(0, order.remaining);
            PhaseProgress = order.duration <= 0 ? 1 : Math.Max(0, Math.Min(1, 1 - PhaseRemaining / order.duration));
            double returnSeconds = world.MarchDurationSeconds(order.path, order.routeVersion);
            UntilAward = Settled ? 0 : PhaseRemaining + (Stage == 0 ? World.TrainingSeconds : 0);
            UntilHome = Stage == 2 ? PhaseRemaining : UntilAward + returnSeconds;
            var receipt = world.LastTrainingReceipt;
            ReceiptAvailable = Settled && receipt != null && receipt.target == Target
                && receipt.cancelled == Cancelled && receipt.participantIds.SequenceEqual(Participants)
                && receipt.experiencePerGeneral == ConfiguredExperience;
            AwardedExperience = ReceiptAvailable ? receipt.awardedExperience.Sum() : 0;
        }

        public static TrainingExecutionStatus Create(World world)
        {
            return world == null || world.order == null || world.order.action != (int)ArmyAction.Training
                ? null : new TrainingExecutionStatus(world);
        }
    }
}
