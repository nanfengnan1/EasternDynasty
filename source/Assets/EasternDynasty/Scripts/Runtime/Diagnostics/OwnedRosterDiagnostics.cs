using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public bool ownedRosterOpen;
            public float ownedRosterScrollY, ownedProgressScrollY;
            public int ownedRosterCount, ownedRosterSelectedUid, ownedRosterSelectedSlot;
            public int[] ownedRosterVisibleUids, ownedFormationUids;
        }

        private void FillOwnedRosterReview(ReviewState review)
        {
            review.ownedRosterOpen = world.IsYouzhou && tab == "武将" && cardDetailId == null
                && drawResult == null && confirmation == null && cityQuestReceipt == null;
            review.ownedRosterScrollY = rosterScroll.y; review.ownedProgressScrollY = generalScroll.y;
            review.ownedRosterCount = world.cards.Count;
            review.ownedRosterSelectedUid = selectedCard; review.ownedRosterSelectedSlot = selectedSlot;
            review.ownedFormationUids = Enumerable.Range(0, 3).Select(i => i < world.formation.Count ? world.formation[i] : 0).ToArray();
            // Only actually drawn, fully visible instance cards count. The first-card
            // alias and detail-modal actions are not invented viewport entries.
            review.ownedRosterVisibleUids = review.ownedRosterOpen ? reviewTargets.Keys
                .Where(IsOwnedRosterCardTarget).Select(id => int.Parse(id.Substring(11))).OrderBy(id => id).ToArray() : new int[0];
        }

        private static bool IsOwnedRosterCardTarget(string id)
        { int uid; return id.StartsWith("owned-card-", StringComparison.Ordinal) && int.TryParse(id.Substring(11), out uid); }

        private static bool IsOwnedRosterBackgroundTarget(string id)
        {
            return IsOwnedRosterCardTarget(id) || id == "owned-card-first" || id == "owned-roster-scroll"
                || id == "owned-general-detail" || id == "owned-progress-scroll" || id == "owned-assign"
                || id == "general-training" || id == "owned-card-training" || id.StartsWith("owned-slot-", StringComparison.Ordinal)
                || id.StartsWith("owned-formation-", StringComparison.Ordinal);
        }

        private void PopulateOwnedRosterFixture()
        {
            // Synthetic ownership exists only in a transient diagnostic session;
            // every identity comes from the already configured S1 recruitable pool.
            var definitions = Catalog.Generals.Where(g => g.s1 && g.recruitable).OrderBy(g => g.id).ToArray();
            if (definitions.Length == 0) throw new Exception("Owned roster fixture has no configured S1 identities.");
            while (world.cards.Count < 36)
            {
                int index = world.cards.Count;
                var card = world.AddCard(definitions[index % definitions.Length].id);
                card.level = 1 + index % 12; card.experience = index * 11 % (card.level * 80);
            }
            string problem;
            if (!world.IsValid(out problem)) throw new Exception("Owned roster fixture is invalid: " + problem);
        }

        private void ConfigureOwnedRosterReview()
        {
            if (!reviewMode || !transientRun || world == null || !world.IsYouzhou)
                throw new InvalidOperationException("Owned roster review requires an isolated read-only S1 session.");
            PopulateOwnedRosterFixture(); speed = 0; tab = "武将";
            selectedCard = OwnedRosterCards()[0].uid; selectedSlot = 0;
            rosterScroll = generalScroll = Vector2.zero;
            status = "只读界面验收 · 36 张测试持有卡，不写玩家存档";
            Debug.Log("OWNED_ROSTER_REVIEW_READY: isolated read-only S1 campaign; 36 synthetic owned instances from configured recruitable identities; awaiting actual visible-card input checks.");
        }

        private void AssertOwnedRosterTargets()
        {
            if (!world.IsYouzhou || tab != "武将" || cardDetailId != null || !BlocksMapInput)
                throw new Exception("Owned roster review is not the visible, map-blocking S1 roster.");
            string[] fixedControls = { "owned-roster-scroll", "owned-general-detail", "owned-progress-scroll",
                "owned-formation-0", "owned-formation-1", "owned-formation-2", "owned-slot-0", "owned-slot-1", "owned-slot-2",
                "owned-assign", "general-training", "owned-card-training" };
            foreach (string id in fixedControls)
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("Owned roster control was not drawn: " + id);
                Rect r = ArchiveReviewRect(target);
                if (r.width <= 0 || r.height <= 0 || r.xMin < uiPixels.xMin - .1f || r.yMin < uiPixels.yMin - .1f
                    || r.xMax > uiPixels.xMax + .1f || r.yMax > uiPixels.yMax + .1f)
                    throw new Exception("Owned roster control is clipped by the safe area: " + id);
                if (map.inputEnabled && !map.IsBlocked(r.center)) throw new Exception("Owned roster leaks map input: " + id);
            }
            // The second training ID is an intentional compatibility alias.
            // Every other fixed control, including the progression viewport,
            // must remain separate so scrolling cannot cover assignment/actions.
            var separated = fixedControls.Where(id => id != "owned-card-training").ToArray();
            for (int i = 0; i < separated.Length; i++)
                for (int j = 0; j < i; j++)
                    if (ArchiveReviewRect(reviewTargets[separated[i]]).Overlaps(ArchiveReviewRect(reviewTargets[separated[j]])))
                        throw new Exception("Owned roster fixed controls overlap: " + separated[i] + " / " + separated[j]);
            Rect viewport = ArchiveReviewRect(reviewTargets["owned-roster-scroll"]);
            var cards = reviewTargets.Values.Where(t => IsOwnedRosterCardTarget(t.id)).ToArray();
            for (int i = 0; i < cards.Length; i++)
            {
                int uid = int.Parse(cards[i].id.Substring(11));
                if (!world.cards.Any(c => c.uid == uid)) throw new Exception("Owned roster exposes a card without that owned UID.");
                Rect r = ArchiveReviewRect(cards[i]);
                if (r.width <= 0 || r.height <= 0 || r.xMin < viewport.xMin - .1f || r.yMin < viewport.yMin - .1f
                    || r.xMax > viewport.xMax + .1f || r.yMax > viewport.yMax + .1f)
                    throw new Exception("Owned roster exposes a clipped instance card: " + uid);
                for (int j = 0; j < i; j++)
                    if (r.Overlaps(ArchiveReviewRect(cards[j]))) throw new Exception("Owned instance card targets overlap.");
            }
            ReviewTarget first, actual;
            if (reviewTargets.TryGetValue("owned-card-first", out first)
                && (!reviewTargets.TryGetValue("owned-card-" + OwnedRosterCards()[0].uid, out actual)
                    || ArchiveReviewRect(first) != ArchiveReviewRect(actual)))
                throw new Exception("Owned first-card alias does not refer to the fully visible first owned instance.");
            if (ArchiveReviewRect(reviewTargets["general-training"]) != ArchiveReviewRect(reviewTargets["owned-card-training"]))
                throw new Exception("Owned training aliases disagree about their fixed visible control.");
        }

        private IEnumerator OwnedRosterVisualSmoke()
        {
            var previousSession = session; string previousTab = tab, previousStatus = status, previousDetail = cardDetailId;
            int previousSelected = selectedCard, previousSlot = selectedSlot, previousUid = cardDetailUid, previousSection = cardDetailSection;
            int previousMapSelected = map.selected, previousTrainingTarget = trainingSelectedTarget;
            string previousTrainingReturn = trainingReturnTab;
            bool previousTrainingEditing = trainingEditingFormation, previousCityEditing = cityEditingFormation;
            bool previousReinforcementEditing = reinforcementEditingFormation, previousHistoryOpen = trainingHistoryOpen;
            bool previousPickerOpen = trainingTargetPickerOpen;
            var previousHistorySelection = trainingHistorySelection;
            var previousTargetScroll = trainingTargetScroll; var previousExecutionScroll = trainingExecutionScroll;
            bool previousReview = reviewMode; float previousSpeed = speed, previousZoom = map.zoom;
            var previousFocus = map.focus; var previousRoster = rosterScroll; var previousProgress = generalScroll; var previousCardScroll = cardDetailScroll;
            string original = JsonUtility.ToJson(world);
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,
                "smoke-owned-roster-unused")), () => DateTime.UtcNow, true);
            try
            {
                session.StartYouzhou(0, "武将名册只读验收"); map.Setup(world); reviewMode = true; speed = 0;
                trainingEditingFormation = cityEditingFormation = reinforcementEditingFormation = false;
                trainingHistoryOpen = trainingTargetPickerOpen = false; trainingHistorySelection = null;
                PopulateOwnedRosterFixture(); tab = "武将"; cardDetailId = null; cardDetailUid = 0;
                selectedCard = OwnedRosterCards()[0].uid; selectedSlot = 0; rosterScroll = generalScroll = Vector2.zero;
                string unchanged = JsonUtility.ToJson(world);
                for (int i = 0; i < 5; i++) yield return null;
                AssertOwnedRosterTargets();
                rosterScroll = new Vector2(0, 10000);
                yield return CaptureState("smoke-owned-roster-scrolled.png");
                AssertOwnedRosterTargets();
                if (rosterScroll.y <= 0 || reviewTargets.ContainsKey("owned-card-first"))
                    throw new Exception("Long owned roster failed to cull the first card at a real nonzero scroll.");
                int visibleUid = reviewTargets.Keys.Where(IsOwnedRosterCardTarget).Select(id => int.Parse(id.Substring(11))).First();
                var chosen = world.cards.First(c => c.uid == visibleUid); float actualScroll = rosterScroll.y;
                ShowCardDetails(chosen.definition, chosen.uid);
                for (int i = 0; i < 5; i++) yield return null;
                if (cardDetailUid != visibleUid || cardDetailId != chosen.definition || reviewTargets.Keys.Any(IsOwnedRosterBackgroundTarget))
                    throw new Exception("Owned modal lost its UID or exposed a hidden roster target.");
                HandleMobileBack();
                for (int i = 0; i < 5; i++) yield return null;
                AssertOwnedRosterTargets();
                if (cardDetailId != null || selectedCard != visibleUid || Math.Abs(rosterScroll.y - actualScroll) > .01f)
                    throw new Exception("Owned card return lost its selected UID or real nonzero roster scroll.");
                selectedSlot = 2;
                OpenTraining();
                for (int i = 0; i < 5; i++) yield return null;
                if (tab != "练兵" || trainingReturnTab != "武将" || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Owned roster training navigation charged or lost its roster origin.");
                ReturnFromTraining();
                for (int i = 0; i < 5; i++) yield return null;
                AssertOwnedRosterTargets();
                if (selectedSlot != 2 || selectedCard != visibleUid || Math.Abs(rosterScroll.y - actualScroll) > .01f
                    || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Owned roster browsing, slot selection or training return changed the campaign.");
                world.formation.Clear(); world.troops = 0; rosterScroll = Vector2.zero;
                unchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-owned-roster-empty-formation.png");
                AssertOwnedRosterTargets();
                if (world.ActiveGenerals.Count != 0 || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Empty formation review created or deployed a general.");
                Debug.Log("OWNED_ROSTER_VISUAL_RUNTIME_OK: full owned-instance cards and fixed formation/training controls fit safe area; clipped cards have no targets; scrolled detail preserves UID and scroll; modal hides roster targets; empty formation and training navigation leave complete World unchanged; actual clicks checked separately.");
            }
            finally
            {
                session = previousSession; map.Setup(world); map.selected = previousMapSelected; map.focus = previousFocus; map.zoom = previousZoom;
                tab = previousTab; status = previousStatus; speed = previousSpeed; reviewMode = previousReview;
                selectedCard = previousSelected; selectedSlot = previousSlot; rosterScroll = previousRoster; generalScroll = previousProgress;
                cardDetailId = previousDetail; cardDetailUid = previousUid; cardDetailSection = previousSection; cardDetailScroll = previousCardScroll;
                trainingSelectedTarget = previousTrainingTarget; trainingReturnTab = previousTrainingReturn;
                trainingEditingFormation = previousTrainingEditing; cityEditingFormation = previousCityEditing;
                reinforcementEditingFormation = previousReinforcementEditing; trainingHistoryOpen = previousHistoryOpen;
                trainingTargetPickerOpen = previousPickerOpen; trainingHistorySelection = previousHistorySelection;
                trainingTargetScroll = previousTargetScroll; trainingExecutionScroll = previousExecutionScroll;
                if (!reviewMode) reviewTargets.Clear();
                if (JsonUtility.ToJson(world) != original) throw new Exception("Owned roster fixture modified the original campaign.");
            }
        }
    }
}
