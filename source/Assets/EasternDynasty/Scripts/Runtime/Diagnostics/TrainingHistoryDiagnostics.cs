using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillTrainingHistoryReview(ReviewState review)
        {
            review.trainingHistoryOpen = tab == "练兵" && trainingHistoryOpen;
            var receipt = review.trainingHistoryOpen ? trainingHistorySelection : null;
            review.trainingHistorySelected = receipt == null ? -1 : world.trainingHistory.IndexOf(receipt);
            review.trainingHistoryStatus = receipt == null ? "none" : receipt.cancelled ? "cancelled" : "completed";
            review.trainingHistoryTarget = receipt == null ? -1 : receipt.target;
            review.trainingHistoryParticipants = receipt == null ? new int[0] : receipt.participantIds.ToArray();
            review.trainingHistoryAwards = receipt == null ? new int[0] : receipt.awardedExperience.ToArray();
            review.trainingHistoryScrollY = trainingHistoryScroll.y;
            review.visibleTrainingHistoryRows = reviewTargets.Keys.Where(id => id.StartsWith("training-history-row-", StringComparison.Ordinal))
                .Select(id => int.Parse(id.Substring("training-history-row-".Length))).OrderBy(id => id).ToArray();
        }

        private sealed partial class ReviewState
        {
            public bool trainingHistoryOpen;
            public int trainingHistorySelected, trainingHistoryTarget;
            public string trainingHistoryStatus;
            public float trainingHistoryScrollY;
            public int[] trainingHistoryParticipants, trainingHistoryAwards, visibleTrainingHistoryRows;
        }

        private IEnumerator TrainingHistorySmoke(int target)
        {
            // Called inside TrainingSmoke's isolated, read-only campaign after real
            // completion and cancellation. No synthetic receipts or player saves.
            world.Tick(1000);
            string unchanged = JsonUtility.ToJson(world);
            int selectedTarget = trainingSelectedTarget; var targetScroll = trainingTargetScroll;
            OpenTrainingHistory();
            if (!trainingHistoryOpen || trainingHistorySelection != world.LastTrainingReceipt || !trainingHistorySelection.cancelled)
                throw new Exception("Training history did not select the real cancelled receipt.");
            yield return CaptureState("smoke-training-history-cancelled.png");
            var completed = world.trainingHistory.First(r => !r.cancelled);
            SelectTrainingHistory(completed);
            if (trainingHistorySelection.awardedExperience.Sum() <= 0 || trainingHistorySelection != completed)
                throw new Exception("Training history replaced the actual completed result.");
            yield return CaptureState("smoke-training-history-completed.png");
            var participant = world.cards.Find(c => c.uid == completed.participantIds[0]);
            ShowCardDetails(participant.definition);HandleMobileBack();
            if(cardDetailId != null || !trainingHistoryOpen || trainingHistorySelection != completed)
                throw new Exception("History card back lost the selected receipt.");
            HandleMobileBack();
            if(trainingHistoryOpen || tab != "练兵" || trainingSelectedTarget != selectedTarget || trainingTargetScroll != targetScroll || JsonUtility.ToJson(world) != unchanged)
                throw new Exception("History navigation mutated the world or training plan.");
            world.StartTraining(target);OpenTrainingHistory();SelectTrainingHistory(completed);
            world.Tick(1000);
            if(trainingHistorySelection != completed || world.LastTrainingReceipt == completed || world.LastTrainingReceipt.cancelled || world.cityTrainingCompletions != 2)
                throw new Exception("A new settlement stole the selected older training result.");
            yield return CaptureState("smoke-training-history-updated.png");
            CloseTrainingHistory();
            // Use real orders to fill the retained ten-record list and exercise the
            // clipped scroll view. All entries still obey Core's settlement path.
            for(int i=0;i<8;i++)
            {
                if(world.TrainingProblem(target).Length != 0)throw new Exception("History archive fixture cannot start training.");
                world.StartTraining(target);world.RecallArmy();world.Tick(1000);
            }
            if(world.trainingHistory.Count != 10 || world.trainingHistory.Contains(completed))
                throw new Exception("Training history retention did not keep the latest ten real receipts.");
            OpenTrainingHistory();
            trainingHistoryScroll = new Vector2(0, 10000);
            SelectTrainingHistory(world.trainingHistory[0]);
            unchanged = JsonUtility.ToJson(world);
            yield return CaptureState("smoke-training-history-archive.png");
            // Re-select both ends of the archive without accumulating any reward.
            var recordedIds = trainingHistorySelection.participantIds.ToArray();
            var recordedAwards = trainingHistorySelection.awardedExperience.ToArray();
            for(int i=0;i<3;i++)
            {
                SelectTrainingHistory(world.LastTrainingReceipt);
                SelectTrainingHistory(world.trainingHistory[0]);
            }
            if(!trainingHistorySelection.participantIds.SequenceEqual(recordedIds) || !trainingHistorySelection.awardedExperience.SequenceEqual(recordedAwards) || JsonUtility.ToJson(world) != unchanged)
                throw new Exception("History re-selection changed the settled data or World.");
            // Loading existing persisted receipts keeps their values. This is a
            // JsonUtility roundtrip, not a new save format or a second settlement.
            var restored = JsonUtility.FromJson<World>(unchanged);restored.EnsureSeasonState();
            if(restored.trainingHistory.Count != 10 || !restored.trainingHistory[0].participantIds.SequenceEqual(recordedIds) || !restored.trainingHistory[0].awardedExperience.SequenceEqual(recordedAwards))
                throw new Exception("Training history JSON roundtrip changed recorded participants or gains.");
            CloseTrainingHistory();
            Debug.Log("TRAINING_HISTORY_RUNTIME_OK: real completed/cancelled results; retained ten orders; card/back/selection read-only; no repeat reward.");
        }
    }
}
