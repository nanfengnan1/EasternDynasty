using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillTrainingTargetReview(ReviewState review)
        {
            review.trainingPickerOpen = tab == "练兵" && trainingTargetPickerOpen;
            review.trainingCandidate = review.trainingPickerOpen ? trainingCandidate : -1;
            review.trainingTargetSort = trainingTargetSort;
            review.trainingPickerScrollY = trainingPickerScroll.y;
            review.trainingCandidateOrder = review.trainingPickerOpen ? trainingCandidates.Select(p => p.Target).ToArray() : new int[0];
            var preview = tab == "练兵" && !TrainingActive ? TrainingPlanPreview.Create(world, review.trainingPickerOpen ? trainingCandidate : trainingSelectedTarget) : null;
            review.trainingPreviewReachable = preview != null && preview.Reachable;
            review.trainingPreviewTotal = preview == null ? 0 : preview.TotalExperience;
            review.trainingPreviewSeconds = review.trainingPreviewReachable ? preview.TotalSeconds : -1;
            review.trainingPreviewMarch = review.trainingPreviewReachable ? preview.MarchSeconds : -1;
            review.trainingPreviewAwards = preview == null ? new int[0] : preview.Participants.Select(p => p.AwardedExperience).ToArray();
            review.trainingPreviewLevels = preview == null ? new int[0] : preview.Participants.Select(p => p.LevelAfter).ToArray();
            review.trainingPreviewExperience = preview == null ? new int[0] : preview.Participants.Select(p => p.ExperienceAfter).ToArray();
        }

        private sealed partial class ReviewState
        {
            public bool trainingPickerOpen, trainingPreviewReachable;
            public int trainingCandidate, trainingTargetSort, trainingPreviewTotal;
            public float trainingPickerScrollY;
            public double trainingPreviewSeconds, trainingPreviewMarch;
            public int[] trainingPreviewAwards, trainingPreviewLevels, trainingPreviewExperience, trainingCandidateOrder;
        }

        private IEnumerator TrainingTargetSmoke(int target)
        {
            // This is still TrainingSmoke's separate read-only campaign. Obtain a
            // second comparison target through the actual recruitment/march rules.
            world.Recruit(Math.Max(0, world.TroopCap - world.troops));
            int next = world.lands.Where(t => world.AttackProblem(t.id) == "").OrderBy(t => t.level).ThenBy(t => t.id).First().id;
            world.March(next); world.Tick(1000); map.RefreshOwnership();
            if (!world.lands[next].owned || world.order != null) throw new Exception("Training comparison fixture could not occupy its second target.");
            OpenTraining(target);
            string unchanged = JsonUtility.ToJson(world); string origin = trainingReturnTab; int selected = map.selected;
            OpenTrainingTargetPicker();
            if (!trainingTargetPickerOpen || trainingCandidates.Count < 2 || !BlocksMapInput) throw new Exception("Training comparison is missing real candidates or map blocking.");
            yield return CaptureState("smoke-training-targets.png");
            trainingCandidate = next; SortTrainingTargets(1);
            if (trainingCandidate != next || trainingCandidates.Where(p => p.Reachable).Select(p => p.TotalExperience).SequenceEqual(trainingCandidates.Where(p => p.Reachable).Select(p => p.TotalExperience).OrderByDescending(n => n)) == false)
                throw new Exception("Training comparison sorting lost selection or placed rewards out of order.");
            yield return CaptureState("smoke-training-targets-gain.png");
            HandleMobileBack();
            if (trainingTargetPickerOpen || trainingSelectedTarget != target || tab != "练兵" || trainingReturnTab != origin || map.selected != selected || JsonUtility.ToJson(world) != unchanged)
                throw new Exception("Training comparison back changed the plan, map, source or world.");
            OpenTrainingTargetPicker(); trainingCandidate = next; CloseTrainingTargetPicker(true);
            if (trainingSelectedTarget != next || trainingTargetPickerOpen || JsonUtility.ToJson(world) != unchanged) throw new Exception("Choosing a training target charged or failed to return to preparation.");
            OpenTrainingTargetPicker(); trainingCandidate = target; CloseTrainingTargetPicker(true);

            // Existing serialized card fields exercise capped/near-cap rendering;
            // this disposable clone never replaces or writes the player's world.
            var savedSession = session;
            session = new CampaignSession(new JsonCampaignRepository(System.IO.Path.Combine(Application.persistentDataPath, "smoke-training-preview-unused")), () => DateTime.UtcNow, true);
            session.StartYouzhou(0, "练兵上限预览"); map.Setup(world);
            try
            {
                world.troops = world.TroopCap;
                int cappedTarget = world.lands.Where(t => world.AttackProblem(t.id) == "").OrderBy(t => t.level).First().id;
                world.March(cappedTarget); world.Tick(1000);
                if (!world.lands[cappedTarget].owned) throw new Exception("Capped training fixture did not capture its real target.");
                var cards = world.ActiveGenerals;
                cards[0].level = cards[0].LevelCap; cards[0].experience = 0;
                cards[1].level = cards[1].LevelCap - 1; cards[1].experience = cards[1].level * 80 - 7;
                OpenTraining(cappedTarget); OpenTrainingTargetPicker();
                string cappedBefore = JsonUtility.ToJson(world);
                var estimate = TrainingPlanPreview.Create(world, cappedTarget);
                if (estimate.Participants[0].AwardedExperience != 0 || estimate.Participants[1].AwardedExperience != 7) throw new Exception("Training preview did not display exact capped awards.");
                yield return CaptureState("smoke-training-targets-capped.png");
                if (JsonUtility.ToJson(world) != cappedBefore) throw new Exception("Rendering the training preview mutated the campaign.");
            }
            finally { trainingTargetPickerOpen = false; session = savedSession; map.Setup(world); map.selected = selected; OpenTraining(target); }
            Debug.Log("TRAINING_TARGETS_RUNTIME_OK: real occupied candidates; sort/choose/back preserve world and source; capped participant forecasts; no new save fields.");
        }
    }
}
