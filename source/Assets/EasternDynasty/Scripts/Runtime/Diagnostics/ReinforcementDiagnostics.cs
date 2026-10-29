using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillReinforcementReview(ReviewState review)
        {
            var plan = ReinforcementPlan.Create(world, reinforcementAmount);
            review.reinforcementAmount = plan.amount; review.reinforcementAffordable = plan.affordable;
            review.reinforcementCap = plan.capacity; review.reinforcementProblem = plan.problem;
            review.reinforcementCost = new[] { plan.cost.grain, plan.cost.grass, plan.cost.iron };
            review.reinforcementBalance = new[] { world.resources.grain, world.resources.grass, world.resources.iron };
            review.reinforcementScrollY = reinforcementCostScroll.y;
            review.reinforcementReturnTab = reinforcementReturnTab; review.reinforcementReturnPage = reinforcementReturnPage;
            review.reinforcementReadyQuestId = ReinforcementReadyQuest == null ? 0 : ReinforcementReadyQuest.id;
            var receipt = reinforcementReceipt;
            review.reinforcementCompleted = receipt != null;
            if (receipt != null)
            {
                review.reinforcementAdded = receipt.amount; review.reinforcementBefore = receipt.beforeTroops;
                review.reinforcementAfter = receipt.afterTroops;
                review.reinforcementPaid = new[] { receipt.cost.grain, receipt.cost.grass, receipt.cost.iron };
            }
        }

        private sealed partial class ReviewState
        {
            public int reinforcementAmount, reinforcementAffordable, reinforcementCap, reinforcementAdded, reinforcementBefore, reinforcementAfter, reinforcementReadyQuestId;
            public string reinforcementProblem, reinforcementReturnTab, reinforcementReturnPage;
            public bool reinforcementCompleted;
            public float reinforcementScrollY;
            public double[] reinforcementCost, reinforcementBalance, reinforcementPaid;
        }

        private IEnumerator ReinforcementSmoke()
        {
            var oldSession = session; string oldTab = tab, oldPage = cityPage, oldStatus = status;
            var oldFocus = map.focus; float oldZoom = map.zoom, oldSpeed = speed; int oldSelected = map.selected;
            int oldAmount = reinforcementAmount; var oldReceipt = reinforcementReceipt; var oldScroll = reinforcementCostScroll;
            string oldReturn = reinforcementReturnTab, oldReturnPage = reinforcementReturnPage;
            bool oldFormation = reinforcementEditingFormation; var oldConfirmation = confirmation; string oldConfirmText = confirmText;
            var oldQuestReceipt = cityQuestReceipt; int oldChapter = cityQuestChapter;
            bool oldCompletedFilter = cityQuestCompleted, oldQuestFocus = cityQuestFocusPending; var oldQuestScroll = cityQuestScroll;
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath, "smoke-reinforcement-unused")), () => DateTime.UtcNow, true);
            try
            {
                session.StartYouzhou(0, "征兵流程验收"); map.Setup(world); speed = 0;
                // Prepare the actual fifth objective through the existing construction and reward commands.
                world.resources = new Supplies { grain = 50000, grass = 50000, stone = 50000, iron = 50000, gold = 50000 };
                OpenCityFacilities(); world.ClaimCityQuest(1);
                CompleteCitySmokeConstruction(world, 0, CityBuilding.Mill); world.ClaimCityQuest(2);
                CompleteCitySmokeConstruction(world, 4, CityBuilding.TownHall); world.ClaimCityQuest(3); world.ClaimCityQuest(4);
                if (world.CurrentCityQuest == null || world.CurrentCityQuest.id != 5) throw new Exception("Reinforcement fixture did not reach the actual recruit objective.");
                OpenCityQuests(); string unchanged = JsonUtility.ToJson(world);
                GuideCityQuest(world.CurrentCityQuest);
                if (tab != "征兵" || !BlocksMapInput || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Recruit objective must open actual independent reinforcement without charging.");
                yield return CaptureState("smoke-reinforcement-ready.png");
                ConfirmReinforcement();
                if (confirmation == null || JsonUtility.ToJson(world) != unchanged) throw new Exception("Reinforcement confirmation charged before approval.");
                yield return CaptureState("smoke-reinforcement-confirm.png");
                confirmation = null; ReturnFromReinforcement();
                if (tab != "主城" || cityPage != "quests" || JsonUtility.ToJson(world) != unchanged) throw new Exception("Cancelled reinforcement did not preserve quest origin and world.");
                OpenReinforcement(); reinforcementAmount = Math.Min(137, world.TroopCap - world.troops);
                int expectedAmount = reinforcementAmount;
                ConfirmReinforcement(); var approved = confirmation; confirmation = null; approved();
                if (reinforcementReceipt == null || reinforcementReceipt.amount != expectedAmount || world.troops != reinforcementReceipt.afterTroops || !world.CityQuestReady(5))
                    throw new Exception("Actual reinforcement confirmation failed to create its exact completion receipt.");
                string completed = JsonUtility.ToJson(world); approved();
                if (JsonUtility.ToJson(world) != completed) throw new Exception("Repeated confirmation charged twice.");
                // This has no new save fields. Actual Unity JSON must retain completed troops and resources.
                var loaded = JsonUtility.FromJson<World>(completed);
                string loadProblem = ReinforcementRoundTripProblem(world, loaded);
                if (loadProblem.Length != 0) throw new Exception("Reinforcement JSON roundtrip: " + loadProblem);
                // Negative controls keep the rounding allowance from concealing a real debit or lost progress.
                double loadedIron = loaded.resources.iron; loaded.resources.iron += .01;
                if (ReinforcementRoundTripProblem(world, loaded).Length == 0) throw new Exception("Reinforcement JSON accepted a changed resource balance.");
                loaded.resources.iron = loadedIron; loaded.troops++;
                if (ReinforcementRoundTripProblem(world, loaded).Length == 0) throw new Exception("Reinforcement JSON accepted a changed troop count.");
                loaded.troops--; loaded.cityRecruitActions++;
                if (ReinforcementRoundTripProblem(world, loaded).Length == 0) throw new Exception("Reinforcement JSON accepted a changed task counter.");
                Debug.Log("REINFORCEMENT_JSON_RUNTIME_OK: 137-person fractional debit, valid world, exact troops/task counter, five resource balances within existing repository JSON tolerance; changed resources/troops/task rejected.");
                if (ReinforcementReadyQuest == null || ReinforcementReadyQuest.id != 5)
                    throw new Exception("Actual recruitment did not expose the unclaimed recruit objective.");
                yield return CaptureState("smoke-reinforcement-completed.png");
                GoToReinforcementQuest();
                if (tab != "主城" || cityPage != "quests" || reinforcementReceipt != null || cityQuestCompleted
                    || cityQuestChapter != CityQuestChapter(5) || JsonUtility.ToJson(world) != completed)
                    throw new Exception("Reinforcement follow-up must locate the actual objective without paying or claiming.");
                yield return CaptureState("smoke-reinforcement-quest.png");
                ClaimCityQuestReward(5);
                if (cityQuestReceipt == null || cityQuestReceipt.questId != 5 || world.CurrentCityQuest.id != 6)
                    throw new Exception("Recruit objective did not pay through the real reward command.");
                string claimed = JsonUtility.ToJson(world); ClaimCityQuestReward(5);
                if (JsonUtility.ToJson(world) != claimed) throw new Exception("Recruit objective reward was duplicated.");
                CloseCityQuestReward();
                if (ReinforcementReadyQuest != null) throw new Exception("Claimed recruit objective remained actionable in reinforcement.");
                completed = claimed; OpenReinforcement();
                if (reinforcementReceipt != null || JsonUtility.ToJson(world) != completed) throw new Exception("Reopening reinforcement repeated completion.");
                var resources = world.resources;
                // Explicit shortage fixture in this isolated, read-only campaign; no real save is changed.
                world.resources = new Supplies { grain = 34.5, grass = 8.1, iron = 4.7 };
                reinforcementAmount = 200; unchanged = JsonUtility.ToJson(world); ConfirmReinforcement();
                var shortage = ReinforcementPlan.Create(world, reinforcementAmount);
                if (shortage.affordable != 20 || shortage.amount <= shortage.affordable || !shortage.problem.Contains("资源不足") || confirmation != null || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Shortage fixture must expose a real resource deficit and reject payment.");
                yield return CaptureState("smoke-reinforcement-shortage.png");
                world.resources = resources; world.Recruit(world.TroopCap - world.troops);
                OpenReinforcement(); unchanged = JsonUtility.ToJson(world); ConfirmReinforcement();
                if (confirmation != null || JsonUtility.ToJson(world) != unchanged) throw new Exception("Full army could recruit again.");
                yield return CaptureState("smoke-reinforcement-full.png");
                var target = world.lands.First(t => world.AttackProblem(t.id).Length == 0);
                world.March(target.id); OpenReinforcement(); unchanged = JsonUtility.ToJson(world); ConfirmReinforcement();
                if (confirmation != null || JsonUtility.ToJson(world) != unchanged || world.order == null) throw new Exception("Busy reinforcement changed actual order.");
                yield return CaptureState("smoke-reinforcement-busy.png");
                Debug.Log("REINFORCEMENT_RUNTIME_OK: isolated read-only campaign; task entry, cancel/return, approved exact cost and receipt, duplicate confirmation rejection, Unity JSON roundtrip, task reward handoff without navigation credits, shortage/full/busy reasons without charges.");
            }
            finally
            {
                session = oldSession; map.Setup(world); map.focus = oldFocus; map.zoom = oldZoom; map.selected = oldSelected;
                tab = oldTab; cityPage = oldPage; status = oldStatus; speed = oldSpeed;
                reinforcementAmount = oldAmount; reinforcementReceipt = oldReceipt; reinforcementCostScroll = oldScroll;
                reinforcementReturnTab = oldReturn; reinforcementReturnPage = oldReturnPage; reinforcementEditingFormation = oldFormation;
                confirmation = oldConfirmation; confirmText = oldConfirmText;
                cityQuestReceipt = oldQuestReceipt; cityQuestChapter = oldChapter; cityQuestCompleted = oldCompletedFilter;
                cityQuestFocusPending = oldQuestFocus; cityQuestScroll = oldQuestScroll;
            }
        }

        private static string ReinforcementRoundTripProblem(World expected, World loaded)
        {
            string problem;
            if (loaded == null) return "loaded world is null";
            if (!loaded.IsValid(out problem)) return "invalid loaded world: " + problem;
            if (loaded.troops != expected.troops) return "troops changed: " + expected.troops + " -> " + loaded.troops;
            if (loaded.cityRecruitActions != expected.cityRecruitActions) return "recruit task counter changed";
            if (!loaded.cityClaimedQuests.SequenceEqual(expected.cityClaimedQuests)) return "claimed objectives changed";
            if (loaded.resources.jade != expected.resources.jade) return "jade changed";
            double[] before = { expected.resources.grain, expected.resources.grass, expected.resources.stone, expected.resources.iron, expected.resources.gold };
            double[] after = { loaded.resources.grain, loaded.resources.grass, loaded.resources.stone, loaded.resources.iron, loaded.resources.gold };
            string[] names = { "grain", "grass", "stone", "iron", "gold" };
            for (int i = 0; i < before.Length; i++)
            {
                // Same tolerance as RepositoryChecks.Difference: JSON decimal text may round
                // the last binary digit. Troops and objective progress remain exact integers.
                if (Math.Abs(before[i] - after[i]) > Math.Max(1, Math.Abs(before[i])) * 1e-12)
                    return names[i] + " changed: " + before[i].ToString("R") + " -> " + after[i].ToString("R");
            }
            return "";
        }
    }
}
