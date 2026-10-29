using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private IEnumerator ExperienceSmoke()
        {
            yield return LandVisualSmoke();
            yield return CardVisualSmoke();
            yield return OwnedRosterVisualSmoke();
            OpenCityOverview();
            yield return CaptureState("smoke-city-overview.png");
            if(cityPage!="overview"||!BlocksMapInput)throw new Exception("City overview did not open as a blocked independent page.");
            OpenCityFacilities();
            yield return CaptureState("smoke-city-district.png");
            if(cityPage!="facilities"||!BlocksMapInput||world.cityPlots.Count!=9||world.CityLevel(CityBuilding.TownHall)!=1)throw new Exception("City facilities lost the independent nine-plot footprint.");
            Act(world.ClaimCityQuest(1));
            int before=world.cityClaimedQuests.Count;world.ClaimCityQuest(1);
            if(world.cityClaimedQuests.Count!=before)throw new Exception("Quest was claimed twice.");
            citySelectedPlot=world.cityPlots.First(p=>p.building==CityBuilding.None&&world.IsCityDistrict(p.landId)).index;
            citySelectedBuilding=CityBuilding.Mill;
            Act(world.StartCityConstruction(citySelectedPlot,CityBuilding.Mill));
            if(world.CityJob==null)throw new Exception("City construction failed.");
            yield return CaptureState("smoke-city-construction.png");
            world.Tick(world.CityJob.remaining+.01);
            if(world.CityLevel(CityBuilding.Mill)!=1||!world.CityQuestReady(2))throw new Exception("City completion did not advance quest.");
            OpenCityQuests();yield return CaptureState("smoke-city-quests.png");
            if(cityPage!="quests"||!BlocksMapInput)throw new Exception("City quests did not open as a blocked independent page.");
            cityQuestCompleted=true;cityQuestScroll=Vector2.zero;
            yield return CaptureState("smoke-city-quests-completed.png");
            if(world.cityClaimedQuests.Count!=before)throw new Exception("Opening completed quests altered claimed rewards.");
            Debug.Log("CITY_PAGES_RUNTIME_OK: overview, independent nine-plot facilities, real construction, chapter quests and completed rewards; read-only campaign.");
            tab="领地";map.CenterHome();yield return CaptureState("smoke-city-map.png");
            yield return CityDevelopedSmoke();
            yield return CityOverviewPlanningSmoke();
            yield return CityPanoramaSmoke();
            yield return CityConstructionSmoke();
            yield return CityFacilitySmoke();
            yield return CityFacilityCatalogSmoke();
            yield return CityQuestNavigationSmoke();
            yield return CityQuestRewardSmoke();
            yield return CityQuestObjectiveSmoke();
            yield return ReinforcementSmoke();
            yield return TrainingSmoke();
        }

        private IEnumerator CityDevelopedSmoke()
        {
            var previousSession=session;string previousTab=tab,previousPage=cityPage,previousStatus=status;
            int previousSelected=map.selected,previousPlot=citySelectedPlot;var previousBuilding=citySelectedBuilding;
            float previousSpeed=speed,previousZoom=map.zoom;var previousFocus=map.focus;
            // This explicit visual fixture supplies resources only in a separate read-only campaign.
            // Every facility still uses the normal construction queue and completion rules.
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-city-developed-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"全设施布局验收");map.Setup(world);speed=0;
                world.EnsureCityState();
                world.resources.grain=world.resources.grass=world.resources.stone=world.resources.iron=world.resources.gold=50000;
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);
                foreach(var definition in CityCatalog.Buildings.Where(d=>d.kind!=CityBuilding.TownHall))
                {
                    var plot=world.cityPlots.FirstOrDefault(p=>p.building==CityBuilding.None&&world.IsCityDistrict(p.landId));
                    if(plot==null)throw new Exception("Developed city fixture has no legal plot for "+definition.kind);
                    CompleteCitySmokeConstruction(world,plot.index,definition.kind);
                }
                if(world.CityLevel(CityBuilding.TownHall)!=3||world.cityPlots.Count(p=>p.building!=CityBuilding.None)!=9||
                    CityCatalog.Buildings.Any(d=>world.CityLevel(d.kind)<1)||world.CityJob!=null)
                    throw new Exception("Developed city fixture did not complete every real facility.");
                OpenCityOverview();status="验收夹具 · 全设施布局 · 独立只读征程";
                yield return CaptureState("smoke-city-developed.png");
                Debug.Log("CITY_DEVELOPED_RUNTIME_OK: isolated read-only visual fixture; supplied resources; town hall level 3 and all eight other facilities completed through normal queues.");
            }
            finally
            {
                session=previousSession;map.Setup(world);map.selected=previousSelected;map.focus=previousFocus;map.zoom=previousZoom;
                tab=previousTab;cityPage=previousPage;citySelectedPlot=previousPlot;citySelectedBuilding=previousBuilding;speed=previousSpeed;status=previousStatus;
            }
        }

        private static void CompleteCitySmokeConstruction(World fixture,int plotIndex,CityBuilding building)
        {
            int expectedLevel=fixture.CityLevel(building)+1;
            string result=fixture.StartCityConstruction(plotIndex,building);
            if(fixture.CityJob==null||fixture.CityJob.building!=building)throw new Exception("Developed city construction rejected: "+result);
            fixture.Tick(fixture.CityJob.remaining+.01);
            if(fixture.CityJob!=null||fixture.CityLevel(building)!=expectedLevel)throw new Exception("Developed city construction did not complete: "+building);
        }

        private IEnumerator CityFacilitySmoke()
        {
            var previousSession=session;string previousTab=tab,previousPage=cityPage,previousStatus=status;
            bool previousReview=reviewMode;
            int previousSelected=map.selected,previousPlot=citySelectedPlot;var previousBuilding=citySelectedBuilding;
            float previousSpeed=speed,previousZoom=map.zoom;var previousFocus=map.focus;var previousScroll=cityDetailScroll;
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-city-facility-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"设施详情验收");map.Setup(world);speed=0;reviewMode=true;
                OpenCityFacilities(0);citySelectedBuilding=CityBuilding.Barracks;
                string unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-locked.png");
                AssertCityFacilityDossierGeometry(true);
                if(JsonUtility.ToJson(world)!=unchanged||world.CityBuildProblem(0,CityBuilding.Barracks).Length==0)
                    throw new Exception("Locked facility preview changed state or allowed construction.");
                OpenCityFacilities(4);
                if(citySelectedPlot!=4||world.CityJob!=null)throw new Exception("Facility prerequisite did not reach the actual town hall.");
                OpenCityFacilities(0);citySelectedBuilding=CityBuilding.Mill;
                unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-dossier.png");
                AssertCityFacilityDossierGeometry(true);
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Facility image sheet changed the campaign while previewing.");
                citySelectedBuilding=CityBuilding.Quarry;
                yield return CaptureState("smoke-facility-gallery-design.png");
                AssertCityFacilityDossierGeometry(true);
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Facility gallery selection changed the isolated campaign.");
                citySelectedBuilding=CityBuilding.Mill;
                var savedStock=world.resources;
                world.resources=new Supplies{grain=999999999999,grass=999999999999,stone=999999999999,iron=999999999999,gold=999999999999};
                citySelectedBuilding=CityBuilding.Workshop;unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-sheet-design.png");
                AssertCityFacilityDossierGeometry(true);
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Facility design stress preview mutated its isolated campaign.");
                world.resources=savedStock;citySelectedBuilding=CityBuilding.Mill;
                world.resources=new Supplies();cityDetailScroll=new Vector2(0,200);
                unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-shortage.png");
                if(JsonUtility.ToJson(world)!=unchanged||world.CityBuildProblem(0,CityBuilding.Mill).Length==0)
                    throw new Exception("Shortage preview changed resources or allowed construction.");
                world.resources=new Supplies{grain=1000000,grass=1000000,stone=1000000,iron=1000000,gold=1000000};
                world.StartCityConstruction(0,CityBuilding.Mill);var oldJob=world.CityJob;
                cityDetailScroll=Vector2.zero;unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-dossier-working.png");
                AssertCityFacilityDossierGeometry(true);
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Facility construction portrait spent or advanced the campaign.");
                unchanged=JsonUtility.ToJson(world);
                ConfirmCityCancellation(oldJob);
                yield return CaptureState("smoke-facility-refund.png");
                if(JsonUtility.ToJson(world)!=unchanged||!ReferenceEquals(world.CityJob,oldJob))
                    throw new Exception("Refund confirmation changed construction or resources before consent.");
                var lateConfirmation=confirmation;confirmation=null;
                world.Tick(oldJob.remaining);
                world.StartCityConstruction(1,CityBuilding.Quarry);var newJob=world.CityJob;
                unchanged=JsonUtility.ToJson(world);lateConfirmation();
                if(world.CityJob!=newJob||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Late cancellation refunded a completed job or cancelled a different construction.");
                world.CancelCityConstruction();
                OpenCityFacilities(1);citySelectedBuilding=CityBuilding.Mill;
                yield return CaptureState("smoke-facility-existing.png");
                OpenCityFacilities(0);
                yield return CaptureState("smoke-facility-upgrade.png");
                AssertCityFacilityDossierGeometry(true);
                if(world.CityLevel(CityBuilding.Mill)!=1||world.CityHourlyIncome().grain!=160||CityCatalog.EffectAtLevel(CityBuilding.Mill,2)!=320)
                    throw new Exception("Facility comparison disagrees with actual income.");
                for(int level=world.CityLevel(CityBuilding.TownHall);level<8;level++)CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);
                for(int level=world.CityLevel(CityBuilding.Mill);level<10;level++)CompleteCitySmokeConstruction(world,0,CityBuilding.Mill);
                OpenCityFacilities(0);unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-facility-maximum.png");
                AssertCityFacilityDossierGeometry(false);
                if(JsonUtility.ToJson(world)!=unchanged||world.CityBuildProblem(0,CityBuilding.Mill).Length==0)
                    throw new Exception("Maximum facility preview changed state or allowed another level.");
                Debug.Log("CITY_FACILITIES_RUNTIME_OK: isolated read-only locked/shortage/existing/upgrade/maximum views; completed-rule effects and stale cancellation preserve the actual queue.");
                Debug.Log("CITY_FACILITY_DOSSIER_RUNTIME_OK: actual facility portrait, nine touch-sized plots, image selectors and five costs visible at the top; independent fixed action and unchanged isolated campaign. Visual quality and real input require publication review.");
                Debug.Log("CITY_FACILITY_SHEET_DESIGN_RUNTIME_OK: five Inkscape components; description/effect/cost order, actual GUIStyle fit, 12-digit inventory and independent 360x44 command; all original isolated rule and cancellation checks retained.");
                Debug.Log("CITY_FACILITY_GALLERY_RUNTIME_OK: three exported rectangular skins; complete project illustration and independent 34-high footer; actual Chinese fonts, eight visible image/name child regions and nine 48-high district controls. Visual quality and actual input require publisher review.");
            }
            finally
            {
                confirmation=null;cityCancellation=null;session=previousSession;map.Setup(world);map.selected=previousSelected;map.focus=previousFocus;map.zoom=previousZoom;
                tab=previousTab;cityPage=previousPage;citySelectedPlot=previousPlot;citySelectedBuilding=previousBuilding;speed=previousSpeed;status=previousStatus;cityDetailScroll=previousScroll;
                reviewMode=previousReview;if(!reviewMode)reviewTargets.Clear();
            }
        }

        private IEnumerator CityQuestNavigationSmoke()
        {
            var previousSession=session;string previousTab=tab,previousPage=cityPage,previousStatus=status;
            int previousSelected=map.selected,previousPlot=citySelectedPlot,previousChapter=cityQuestChapter;
            var previousBuilding=citySelectedBuilding;var previousScroll=cityQuestScroll;
            bool previousCompleted=cityQuestCompleted,previousPending=cityQuestFocusPending;
            float previousSpeed=speed,previousZoom=map.zoom;var previousFocus=map.focus;
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-city-quest-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"任务导航验收");map.Setup(world);speed=0;
                world.resources=new Supplies{grain=50000,grass=50000,stone=50000,iron=50000,gold=50000};
                OpenCityFacilities();world.ClaimCityQuest(1);world.StartCityConstruction(2,CityBuilding.Mill);
                OpenCityQuests();string unchanged=JsonUtility.ToJson(world);GuideCityQuest(world.CurrentCityQuest);
                if(cityPage!="facilities"||citySelectedPlot!=2||citySelectedBuilding!=CityBuilding.Mill||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Quest navigation did not preserve and locate actual pending construction.");
                yield return CaptureState("smoke-quest-construction.png");
                world.Tick(world.CityJob.remaining);world.ClaimCityQuest(2);
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);world.ClaimCityQuest(3);world.ClaimCityQuest(4);
                world.Recruit(200);world.ClaimCityQuest(5);
                if(world.CurrentCityQuest==null||world.CurrentCityQuest.id!=6)throw new Exception("Quest visual fixture did not complete the first five real objectives.");
                OpenCityQuests();unchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-quest-current.png");
                if(cityQuestChapter!=1||cityQuestFocusPending||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Current task focus changed world or failed to select its chapter.");
                GuideCityQuest(world.CurrentCityQuest);
                if(tab!="领地"||map.selected<0||world.AttackProblem(map.selected)!=""||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Territory task did not reach a legal, actionable map target without payment.");
                yield return CaptureState("smoke-quest-land.png");
                world.March(map.selected);unchanged=JsonUtility.ToJson(world);GuideCityQuest(world.CurrentCityQuest);
                if(tab!="领地"||map.selected!=world.order.target||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Busy task route did not locate the actual order without issuing another.");
                Debug.Log("CITY_QUEST_NAVIGATION_RUNTIME_OK: isolated read-only campaign; real first five objectives, current chapter focus, exact construction queue, actionable territory and busy-order destinations without navigation charges.");
            }
            finally
            {
                session=previousSession;map.Setup(world);map.selected=previousSelected;map.focus=previousFocus;map.zoom=previousZoom;
                tab=previousTab;cityPage=previousPage;citySelectedPlot=previousPlot;citySelectedBuilding=previousBuilding;speed=previousSpeed;status=previousStatus;
                cityQuestChapter=previousChapter;cityQuestScroll=previousScroll;cityQuestCompleted=previousCompleted;cityQuestFocusPending=previousPending;
            }
        }

        private IEnumerator CityQuestRewardSmoke()
        {
            var previousSession=session;string previousTab=tab,previousPage=cityPage,previousStatus=status;
            string previousWorld=JsonUtility.ToJson(world);
            int previousSelected=map.selected,previousPlot=citySelectedPlot,previousChapter=cityQuestChapter;
            var previousBuilding=citySelectedBuilding;var previousScroll=cityQuestScroll;var previousDetailScroll=cityDetailScroll;
            var previousReceipt=cityQuestReceipt;
            string previousCurrentBattle=currentBattleId,previousPendingBattle=pendingBattleWatchId,previousDismissedBattle=dismissedBattleWatchId;
            var previousReplayRecord=replayRecord;var previousReplayPlayer=replayPlayer;var previousReplayScroll=replayEventScroll;
            var previousReplayOutcome=replayOutcome;bool previousOutcomeOpen=replayShowOutcome;
            var previousFormation=battleFormation;bool previousFormationOpen=battleFormationOpen,previousFormationFromReport=battleFormationFromReport;
            int previousFormationSelected=battleFormationSelected;var previousFormationScroll=battleFormationScroll;
            var previousOutcomeScroll=battleOutcomeScroll;bool previousReplayEvents=replayShowEvents;
            bool previousReplayPlaying=replayPlaying,previousWatching=watchingCurrentBattle;
            float previousReplayClock=replayClock,previousReplaySpeed=replaySpeed;
            bool previousCompleted=cityQuestCompleted,previousPending=cityQuestFocusPending;
            bool previousCityFormation=cityEditingFormation,previousTrainingFormation=trainingEditingFormation;
            float previousSpeed=speed,previousZoom=map.zoom;var previousFocus=map.focus;
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-city-reward-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"任务到账验收");map.Setup(world);speed=0;cityQuestReceipt=null;
                currentBattleId=pendingBattleWatchId=dismissedBattleWatchId="";replayRecord=null;replayPlayer=null;replayPlaying=watchingCurrentBattle=false;
                // Supplied resources belong only to this explicit isolated fixture. Objectives
                // still complete through real construction, recruitment, capture and farming.
                world.resources=new Supplies{grain=50000,grass=50000,stone=50000,iron=50000,gold=50000};
                OpenCityFacilities();OpenCityQuests();ClaimCityQuestRewardSmoke(1);
                if(cityQuestReceipt.chapterComplete||cityQuestReceipt.allComplete||cityQuestReceipt.nextQuestId!=2)
                    throw new Exception("First task receipt reported the wrong completion state.");
                yield return CaptureState("smoke-quest-reward.png");
                string unchanged=JsonUtility.ToJson(world);GoToNextCityQuest();
                if(cityQuestReceipt!=null||cityPage!="facilities"||citySelectedPlot!=0||citySelectedBuilding!=CityBuilding.Mill||world.CityJob!=null||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Receipt next action did not locate an actionable empty mill plot without another reward.");
                CompleteCitySmokeConstruction(world,0,CityBuilding.Mill);OpenCityQuests();
                yield return CaptureMapQuestCharter("smoke-map-quest-ready.png");
                ClaimCityQuestRewardSmoke(2);
                unchanged=JsonUtility.ToJson(world);CloseCityQuestReward();
                if(cityQuestReceipt!=null||cityPage!="quests"||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Receipt close failed to preserve the originating page and claimed world.");
                ClaimCityQuestReward(2);
                if(cityQuestReceipt!=null||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Reclaiming a dismissed task created another receipt or reward.");
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);OpenCityQuests();ClaimCityQuestRewardSmoke(3);
                if(!cityQuestReceipt.chapterComplete||cityQuestReceipt.allComplete||cityQuestReceipt.chapterIndex!=0||cityQuestReceipt.nextQuestId!=4)
                    throw new Exception("Chapter completion did not follow the actually claimed first three objectives.");
                yield return CaptureState("smoke-quest-chapter-complete.png");
                unchanged=JsonUtility.ToJson(world);ContinueCityQuestReward();
                if(cityQuestReceipt!=null||cityPage!="quests"||cityQuestChapter!=1||cityQuestCompleted||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Chapter receipt continuation did not select the next actual task chapter without rewards.");
                yield return CaptureMapQuestCharter("smoke-map-quest-army.png");
                ClaimCityQuestRewardSmoke(4);CloseCityQuestReward();world.Recruit(200);
                ClaimCityQuestRewardSmoke(5);
                int farmTarget=-1;
                for(int capture=0;capture<3;capture++)
                {
                    world.Recruit(world.TroopCap);
                    var target=world.lands.Where(t=>!t.capital&&!t.bridge&&!world.IsCityDistrict(t.id)&&world.AttackProblem(t.id)=="").OrderBy(t=>t.level).FirstOrDefault();
                    if(target==null)throw new Exception("Quest reward fixture lacks a legal resource-land target.");
                    world.March(target.id,capture==0);ObserveBattleMarch();string arrivalBattle=world.order.battleId;
                    world.Tick(1000);
                    if(!target.owned||world.order!=null)throw new Exception("Quest reward fixture failed its actual resource capture.");
                    if(capture==0)
                    {
                        unchanged=JsonUtility.ToJson(world);UpdateBattleWatch();
                        if(cityQuestReceipt==null||cityQuestReceipt.questId!=5||tab!="主城"||cityPage!="quests"||pendingBattleWatchId!=arrivalBattle||JsonUtility.ToJson(world)!=unchanged)
                            throw new Exception("Battle arrival interrupted an open reward receipt or repeated settlement.");
                        CloseCityQuestReward();UpdateBattleWatch();
                        if(tab!="战斗回放"||!watchingCurrentBattle||replayRecord==null||replayRecord.battleId!=arrivalBattle||JsonUtility.ToJson(world)!=unchanged)
                            throw new Exception("Dismissing receipt failed to resume the matching settled battle without another reward.");
                        CloseCurrentBattle();OpenCityQuests();
                    }
                    if(farmTarget<0)farmTarget=target.id;
                }
                ClaimCityQuestRewardSmoke(6);CloseCityQuestReward();
                CompleteCitySmokeConstruction(world,1,CityBuilding.Barracks);ClaimCityQuestRewardSmoke(7);CloseCityQuestReward();
                world.Farm(farmTarget);world.Tick(1000);
                if(world.cityFarmCompletions!=1||world.order!=null)throw new Exception("Quest reward fixture failed actual farming and return.");
                ClaimCityQuestRewardSmoke(8);CloseCityQuestReward();
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);ClaimCityQuestRewardSmoke(9);CloseCityQuestReward();
                OpenCityQuests();string beforeFinal=JsonUtility.ToJson(world);ClaimCityQuestRewardSmoke(10);
                if(!cityQuestReceipt.allComplete||!cityQuestReceipt.chapterComplete||cityQuestReceipt.chapterIndex!=2||cityQuestReceipt.nextQuestId!=0||world.cityClaimedQuests.Count!=10)
                    throw new Exception("All-complete receipt does not match the ten genuinely completed objectives.");
                yield return CaptureState("smoke-quest-all-complete.png");
                unchanged=JsonUtility.ToJson(world);ContinueCityQuestReward();ClaimCityQuestReward(10);
                if(cityQuestReceipt!=null||cityPage!="quests"||!cityQuestCompleted||world.CurrentCityQuest!=null||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Final receipt did not open completed history or allowed another reward.");
                // Restore only this fixture's pre-claim snapshot to exercise the other
                // final receipt button from a fresh real claim, never a fabricated receipt.
                JsonUtility.FromJsonOverwrite(beforeFinal,world);ClaimCityQuestRewardSmoke(10);
                unchanged=JsonUtility.ToJson(world);GoToNextCityQuest();ClaimCityQuestReward(10);
                if(cityQuestReceipt!=null||cityPage!="overview"||tab!="主城"||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Final receipt return failed to reach the city without another settlement.");
                yield return CaptureMapQuestCharter("smoke-map-quest-complete.png");
                OpenMapQuestChapter();
                if(tab!="主城"||cityPage!="quests"||!cityQuestCompleted||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Completed map charter failed to open claimed chapter history without another reward.");
                Debug.Log("MAP_QUEST_CHARTER_RUNTIME_OK: visible illustrated task, real chapter/target/progress, 44-unit action and distinct open region; genuine construction/formation/all-ten-claimed states; unchanged World while rendering; actual input and visual approval checked separately.");
                Debug.Log("CITY_QUEST_REWARD_RUNTIME_OK: isolated read-only campaign; exact five-resource credits/balances; duplicate claims blocked; map-blocking ordinary/chapter/all-complete receipts; close, continue and next navigation preserve claimed world; all ten real objectives; battle arrival waits for receipt dismissal and resumes its actual record without repeating settlement.");
            }
            finally
            {
                session=previousSession;map.Setup(world);map.selected=previousSelected;map.focus=previousFocus;map.zoom=previousZoom;
                tab=previousTab;cityPage=previousPage;citySelectedPlot=previousPlot;citySelectedBuilding=previousBuilding;speed=previousSpeed;status=previousStatus;
                cityQuestChapter=previousChapter;cityQuestScroll=previousScroll;cityDetailScroll=previousDetailScroll;
                cityQuestCompleted=previousCompleted;cityQuestFocusPending=previousPending;cityQuestReceipt=previousReceipt;
                cityEditingFormation=previousCityFormation;trainingEditingFormation=previousTrainingFormation;
                currentBattleId=previousCurrentBattle;pendingBattleWatchId=previousPendingBattle;dismissedBattleWatchId=previousDismissedBattle;
                replayRecord=previousReplayRecord;replayPlayer=previousReplayPlayer;replayEventScroll=previousReplayScroll;
                replayOutcome=previousReplayOutcome;replayShowOutcome=previousOutcomeOpen;
                battleFormation=previousFormation;battleFormationOpen=previousFormationOpen;battleFormationFromReport=previousFormationFromReport;
                battleFormationSelected=previousFormationSelected;battleFormationScroll=previousFormationScroll;
                battleOutcomeScroll=previousOutcomeScroll;replayShowEvents=previousReplayEvents;
                replayPlaying=previousReplayPlaying;watchingCurrentBattle=previousWatching;replayClock=previousReplayClock;replaySpeed=previousReplaySpeed;
                if(JsonUtility.ToJson(world)!=previousWorld)throw new Exception("Quest reward fixture modified its original session.");
            }
        }

        private void ClaimCityQuestRewardSmoke(int id)
        {
            var quest=world.CurrentCityQuest;
            if(quest==null||quest.id!=id||!world.CityQuestReady(id))throw new Exception("Quest reward fixture is not ready for task "+id);
            var before=CityRewardResourceValues(world.resources);int claims=world.cityClaimedQuests.Count;
            ClaimCityQuestReward(id);
            if(cityQuestReceipt==null||cityQuestReceipt.questId!=id||world.cityClaimedQuests.Count!=claims+1||!BlocksMapInput)
                throw new Exception("Task claim did not produce one map-blocking receipt: "+id);
            var actual=CityRewardResourceValues(world.resources);var reward=CityRewardResourceValues(quest.reward);
            var credited=CityRewardResourceValues(cityQuestReceipt.credited);var balance=CityRewardResourceValues(cityQuestReceipt.balance);
            for(int i=0;i<5;i++)
                if(Math.Abs(actual[i]-before[i]-reward[i])>.00001||Math.Abs(credited[i]-reward[i])>.00001||Math.Abs(balance[i]-actual[i])>.00001)
                    throw new Exception("Task receipt differs from actual resource "+i+" for task "+id);
            var receipt=cityQuestReceipt;string unchanged=JsonUtility.ToJson(world);ClaimCityQuestReward(id);
            if(!ReferenceEquals(receipt,cityQuestReceipt)||JsonUtility.ToJson(world)!=unchanged)
                throw new Exception("Duplicate task claim altered the receipt or world: "+id);
        }

        private static double[] CityRewardResourceValues(Supplies supplies)
        { return new[]{supplies.grain,supplies.grass,supplies.stone,supplies.iron,supplies.gold}; }

        private IEnumerator TrainingSmoke()
        {
            var previousSession=session;string previousTab=tab;int previousSelected=map.selected;float previousSpeed=speed;
            // A separate read-only in-memory campaign makes each training screenshot
            // independent of the earlier recruitment, city and farming smoke stages.
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-training-unused")),()=>DateTime.UtcNow,true);
            session.StartYouzhou(0,"练兵验收");map.Setup(world);speed=0;
            try
            {
                OpenCityOverview();
                if(world.CityQuestReady(1))throw new Exception("Opening city overview completed the district visit before showing its facilities.");
                GuideCityQuest(world.CurrentCityQuest);
                if(!CityPanelOpen||cityPage!="facilities"||!world.CityQuestReady(1))throw new Exception("Fresh campaign city objective did not reach an actionable city visit.");
                OpenCityOverview();OpenTraining();
                if(!BlocksMapInput||tab!="练兵"||world.TrainingProblem(trainingSelectedTarget)=="")throw new Exception("Training empty-state page is not blocked with a clear restriction.");
                yield return CaptureState("smoke-training-no-land.png");
                string emptyHistoryWorld=JsonUtility.ToJson(world);OpenTrainingHistory();
                yield return CaptureState("smoke-training-history-empty.png");
                HandleMobileBack();
                if(trainingHistoryOpen||tab!="练兵"||JsonUtility.ToJson(world)!=emptyHistoryWorld)throw new Exception("Empty training history changed campaign or lost return plan.");
                int target=world.lands.First(t=>world.AttackProblem(t.id)=="").id;
                world.March(target);world.Tick(1000);map.RefreshOwnership();
                if(!world.lands[target].owned||world.order!=null)throw new Exception("Training smoke could not capture a real target.");
                OpenCityOverview();OpenTraining(target);
                yield return TrainingTargetSmoke(target);
                if(trainingSelectedTarget!=target||world.TrainingProblem(target)!="")throw new Exception("Training ready-state target rejected.");
                yield return CaptureState("smoke-training-ready.png");
                var expectedTraining = TrainingPlanPreview.Create(world, target);
                int before=world.AvailableCommands;Act(world.StartTraining(target));
                if(world.order==null||world.AvailableCommands!=before-World.TrainingCommandCost)throw new Exception("Training start did not charge exactly once.");
                yield return CaptureTrainingExecution("smoke-training-march.png", 0, false);
                world.Tick(world.order.remaining);
                if(world.order.phase!=(int)ArmyPhase.Working)throw new Exception("Training did not enter the working phase.");
                yield return CaptureTrainingExecution("smoke-training-working.png", 1, false);
                world.Tick(world.order.remaining+.01);
                if(world.LastTrainingReceipt==null||world.LastTrainingReceipt.cancelled||world.cityTrainingCompletions!=1)throw new Exception("Training did not produce a real completion receipt.");
                if(!world.LastTrainingReceipt.awardedExperience.SequenceEqual(expectedTraining.Participants.Select(p=>p.AwardedExperience)) || !world.ActiveGenerals.Select(c=>c.level).SequenceEqual(expectedTraining.Participants.Select(p=>p.LevelAfter)))throw new Exception("Training preview differs from actual completion.");
                yield return CaptureTrainingExecution("smoke-training-completed.png", 2, false);
                world.Tick(1000);Act(world.StartTraining(target));world.Tick(world.order.remaining);world.RecallArmy();
                if(!world.LastTrainingReceipt.cancelled||world.cityTrainingCompletions!=1)throw new Exception("Training cancellation changed completed count.");
                yield return CaptureTrainingExecution("smoke-training-cancelled.png", 2, true);
                Debug.Log("TRAINING_EXECUTION_RUNTIME_OK: four visible order phases; exact credit/home countdowns; cancelled zero reward; rendering preserves complete World.");
                yield return TrainingHistorySmoke(target);
                Debug.Log("TRAINING_RUNTIME_OK: independent empty/ready/march/working/completed/cancelled views; real capture and training rules; read-only isolated campaign.");
            }
            finally
            {
                trainingTargetPickerOpen=false;CloseTrainingHistory();confirmation=null;session=previousSession;map.Setup(world);map.selected=previousSelected;tab=previousTab;speed=previousSpeed;
            }
        }
    }
}
