using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillCityConstructionReview(ReviewState review)
        {
            var receipt = session.LastCityCompletion;
            review.cityCompletionAvailable = receipt != null;
            review.cityCompletionPlot = receipt == null ? -1 : receipt.plotIndex;
            review.cityCompletionBuilding = receipt == null ? 0 : (int)receipt.building;
            review.cityCompletionLevel = receipt == null ? 0 : receipt.targetLevel;
            review.cityCompletionBefore = receipt == null ? 0 : receipt.previousEffect;
            review.cityCompletionAfter = receipt == null ? 0 : receipt.completedEffect;
            review.cityCompletionAway = receipt != null && receipt.completedWhileAway;
        }

        private sealed partial class ReviewState
        {
            public bool cityCompletionAvailable, cityCompletionAway;
            public int cityCompletionPlot, cityCompletionBuilding, cityCompletionLevel, cityCompletionBefore, cityCompletionAfter;
        }

        // Real Unity JSON in memory. No player or test-directory saves are written by smoke.
        private sealed class ConstructionSmokeRepository : ICampaignRepository
        {
            public string json;
            public bool Exists(bool youzhou) { return youzhou && json != null; }
            public World Load(bool youzhou, out bool backup) { backup = false; return JsonUtility.FromJson<World>(json); }
            public void Save(World value) { throw new Exception("Construction smoke must remain read-only."); }
        }

        private IEnumerator CityConstructionSmoke()
        {
            var oldSession = session; string oldTab = tab, oldPage = cityPage, oldStatus = status, oldOrigin = cityFacilitiesOrigin;
            int oldSelected = map.selected, oldPlot = citySelectedPlot;
            bool oldPlanning = cityOverviewPlanning; var oldBuilding = citySelectedBuilding; var oldScroll = cityDetailScroll;
            var oldFocus = map.focus; float oldZoom = map.zoom, oldSpeed = speed;
            string original = JsonUtility.ToJson(world);
            var repository = new ConstructionSmokeRepository();
            var now = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);
            session = new CampaignSession(repository, () => now, true);
            try
            {
                session.StartYouzhou(0, "建设进度验收"); map.Setup(world); speed = 0;
                world.resources = new Supplies { grain = 50000, grass = 50000, stone = 50000, iron = 50000, gold = 50000 };
                OpenCityConstruction(); string before = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-construction-idle.png");
                if (!BlocksMapInput || world.CityQuestReady(1) || JsonUtility.ToJson(world) != before || session.LastCityCompletion != null)
                    throw new Exception("Construction page idle browsing changed campaign state or invented completion.");
                int plot = world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index;
                OpenCityFacilities(plot); HandleMobileBack();
                if (cityPage != "construction" || cityFacilitiesOrigin != "construction")
                    throw new Exception("Construction facility return lost its parent page.");
                world.StartCityConstruction(plot, CityBuilding.Mill);
                if (world.CityJob == null) throw new Exception("Construction fixture failed to start real mill.");
                session.Tick(world.CityJob.duration * .4);
                yield return CaptureState("smoke-construction-working.png");
                if (session.LastCityCompletion != null || world.CityLevel(CityBuilding.Mill) != 0)
                    throw new Exception("Pending construction showed a premature completion.");
                before = JsonUtility.ToJson(world); OpenCityFacilities(plot); HandleMobileBack();
                if (cityPage != "construction" || JsonUtility.ToJson(world) != before)
                    throw new Exception("Working construction navigation spent resources or lost its return.");
                session.Tick(world.CityJob.remaining + .01);
                var completed = session.LastCityCompletion;
                yield return CaptureState("smoke-construction-completed.png");
                if (completed == null || completed.plotIndex != plot || completed.previousLevel != 0 || completed.targetLevel != 1
                    || completed.previousEffect != 0 || completed.completedEffect != 160 || world.CityHourlyIncome().grain != 160)
                    throw new Exception("Completion did not reflect actual mill construction and live income.");
                before = JsonUtility.ToJson(world); OpenCityFacilities(completed.plotIndex); HandleMobileBack();
                session.Tick(0);
                if (JsonUtility.ToJson(world) != before || session.LastCityCompletion != completed)
                    throw new Exception("Reopening completion repeated settlement or replaced receipt.");
                world.StartCityConstruction(plot, CityBuilding.Mill);
                session.Tick(world.CityJob.duration * .25);
                yield return CaptureState("smoke-construction-upgrading.png");
                if (session.LastCityCompletion != completed || completed.completedEffect != 160)
                    throw new Exception("New construction mutated the frozen last completion.");
                world.CancelCityConstruction(); session.Tick(.01);
                if (session.LastCityCompletion != completed || world.CityLevel(CityBuilding.Mill) != 1)
                    throw new Exception("Cancelled upgrade became a completion.");
                before = JsonUtility.ToJson(world); session.ClearCityCompletion(completed);
                if (session.LastCityCompletion != null || JsonUtility.ToJson(world) != before)
                    throw new Exception("Dismissing completion changed the campaign.");
                world.StartCityConstruction(plot, CityBuilding.Mill);
                world.savedUtcTicks = now.AddSeconds(-60).Ticks;
                repository.json = JsonUtility.ToJson(world);
                bool backup; session.Load(true, out backup); map.Setup(world); OpenCityConstruction();
                yield return CaptureState("smoke-construction-offline.png");
                var offline = session.LastCityCompletion;
                if (offline == null || !offline.completedWhileAway || offline.previousEffect != 160 || offline.completedEffect != 320
                    || world.CityLevel(CityBuilding.Mill) != 2 || backup)
                    throw new Exception("Unity JSON offline construction did not produce the actual upgrade receipt.");
                repository.json = JsonUtility.ToJson(world); session.Load(true, out backup); map.Setup(world);
                if (session.LastCityCompletion != null || world.CityLevel(CityBuilding.Mill) != 2)
                    throw new Exception("Loading already completed construction invented a new receipt.");
                session.Save(); HandleMobileBack();
                if (cityPage != "overview") throw new Exception("Construction page system back did not return to city.");
                Debug.Log("CITY_CONSTRUCTION_RUNTIME_OK: isolated read-only queue, actual new build/upgrade/cancel, frozen effects, navigation and dismiss preserve state, Unity JSON offline completion without invented history.");
            }
            finally
            {
                session = oldSession; map.Setup(world); map.selected = oldSelected; map.focus = oldFocus; map.zoom = oldZoom;
                tab = oldTab; cityPage = oldPage; status = oldStatus; cityFacilitiesOrigin = oldOrigin;
                citySelectedPlot = oldPlot; citySelectedBuilding = oldBuilding; cityOverviewPlanning = oldPlanning;
                cityDetailScroll = oldScroll; speed = oldSpeed;
                if (JsonUtility.ToJson(world) != original) throw new Exception("Construction fixture modified original campaign.");
            }
        }
    }
}
