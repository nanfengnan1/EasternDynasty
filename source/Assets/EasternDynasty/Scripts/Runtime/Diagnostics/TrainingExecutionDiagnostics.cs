using System;
using System.Collections;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillTrainingExecutionReview(ReviewState review)
        {
            var status = TrainingExecutionStatus.Create(world);
            review.trainingExecutionVisible = tab == "练兵" && !trainingHistoryOpen && !trainingTargetPickerOpen && status != null;
            review.trainingExecutionStage = status == null ? -1 : status.Stage;
            review.trainingUntilAward = status == null ? 0 : status.UntilAward;
            review.trainingUntilHome = status == null ? 0 : status.UntilHome;
            review.trainingPhaseRemaining = status == null ? 0 : status.PhaseRemaining;
            review.trainingPhaseProgress = status == null ? 0 : status.PhaseProgress;
            review.trainingExecutionAward = status == null ? 0 : status.AwardedExperience;
        }

        private sealed partial class ReviewState
        {
            public bool trainingExecutionVisible;
            public int trainingExecutionStage, trainingExecutionAward;
            public double trainingUntilAward, trainingUntilHome, trainingPhaseRemaining, trainingPhaseProgress;
        }

        private IEnumerator CaptureTrainingExecution(string file, int stage, bool cancelled)
        {
            var status = TrainingExecutionStatus.Create(world);
            if (status == null || status.Stage != stage || status.Cancelled != cancelled
                || status.UntilHome < status.UntilAward || status.PhaseRemaining != world.order.remaining)
                throw new Exception("Training execution status differs from authoritative order.");
            if (stage < 2 && (status.Settled || status.ReceiptAvailable || status.AwardedExperience != 0))
                throw new Exception("Training execution awarded before completion.");
            if (stage == 2 && (!status.ReceiptAvailable || cancelled && status.AwardedExperience != 0))
                throw new Exception("Training execution lost the actual result.");
            string before = JsonUtility.ToJson(world);
            bool previousReviewMode = reviewMode;
            reviewMode = true; // Collect only controls actually drawn by the normal page.
            try
            {
                yield return CaptureState(file);
                if (JsonUtility.ToJson(world) != before || !reviewTargets.ContainsKey("training-execution-scroll")
                    || !reviewTargets.ContainsKey(stage == 2 ? "training-result" : "training-cancel")
                    || reviewTargets.ContainsKey("training-confirm") || reviewTargets.ContainsKey("training-compare"))
                    throw new Exception("Execution rendering changed World or did not expose its visible controls.");
                foreach (string id in new[] { "training-execution-scroll", stage == 2 ? "training-result" : "training-cancel" })
                {
                    var rect = reviewTargets[id];
                    if (rect.w <= 0 || rect.h <= 0 || rect.x - rect.w / 2 < 0 || rect.x + rect.w / 2 > Screen.width
                        || rect.y - rect.h / 2 < 0 || rect.y + rect.h / 2 > Screen.height)
                        throw new Exception("Training execution control is outside the actual client: " + id);
                }
            }
            finally { reviewMode = previousReviewMode; if (!reviewMode) reviewTargets.Clear(); }
        }
    }
}
