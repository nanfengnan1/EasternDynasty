using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillCityQuestObjectiveReview(ReviewState review)
        {
            var plan=tab=="主城"&&cityPage=="quest-detail"?CityQuestObjectivePlan.Create(world,cityQuestDetailId):null;
            review.questDetailId=plan==null?0:plan.QuestId;
            review.questDetailCurrent=plan!=null&&plan.Current;review.questDetailReady=plan!=null&&plan.Ready;
            review.questDetailClaimed=plan!=null&&plan.Claimed;
            review.questDetailAction=plan==null?"":plan.ActionLabel;
            review.questDetailRequirement=plan==null?"":plan.RequirementTitle;
            review.questDetailScrollY=cityQuestObjectiveScroll.y;
            review.questDetailCost=plan==null||plan.Cost==null?new double[0]:CityRewardResourceValues(plan.Cost);
            review.questDetailRoutePlot=plan==null||plan.Route==null?-1:plan.Route.plotIndex;
            review.questLandReturn=CityQuestLandReturnAvailable;
        }

        private sealed partial class ReviewState
        {
            public int questDetailId,questDetailRoutePlot;
            public bool questDetailCurrent,questDetailReady,questDetailClaimed;
            public bool questLandReturn;
            public string questDetailAction,questDetailRequirement;
            public float questDetailScrollY;
            public double[] questDetailCost;
        }

        private IEnumerator CaptureCityQuestObjective(string file)
        {
            string before=JsonUtility.ToJson(world);bool oldReview=reviewMode;reviewMode=true;
            try
            {
                yield return CaptureState(file);
                if(cityPage!="quest-detail"||!BlocksMapInput||JsonUtility.ToJson(world)!=before)
                    throw new Exception("Quest objective rendering changed the campaign or lost its input barrier.");
                foreach(string id in new[]{"quest-detail-action","quest-detail-content","city-back"})
                {
                    if(!reviewTargets.ContainsKey(id))throw new Exception("Missing drawn objective control: "+id);
                    var rect=reviewTargets[id];
                    if(rect.w<=0||rect.h<=0||rect.x-rect.w/2<0||rect.x+rect.w/2>Screen.width
                        ||rect.y-rect.h/2<0||rect.y+rect.h/2>Screen.height)
                        throw new Exception("Objective control clipped outside client: "+id);
                }
            }
            finally{reviewMode=oldReview;if(!reviewMode)reviewTargets.Clear();}
        }

        private IEnumerator CityQuestObjectiveSmoke()
        {
            var oldSession=session;string oldTab=tab,oldPage=cityPage,oldStatus=status,oldOrigin=cityFacilitiesOrigin;
            int oldSelected=map.selected,oldPlot=citySelectedPlot,oldChapter=cityQuestChapter,oldId=cityQuestDetailId;
            var oldBuilding=citySelectedBuilding;var oldScroll=cityQuestScroll;var oldDetailScroll=cityQuestObjectiveScroll;
            var oldFacilityScroll=cityDetailScroll;var oldReceipt=cityQuestReceipt;
            var oldLandWorld=cityQuestLandWorld;int oldLandId=cityQuestLandId;
            bool oldCompleted=cityQuestCompleted,oldPending=cityQuestFocusPending,oldFormation=cityEditingFormation;
            var oldFocus=map.focus;float oldZoom=map.zoom,oldSpeed=speed;string original=JsonUtility.ToJson(world);
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-quest-objective-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"任务目标验收");map.Setup(world);speed=0;cityQuestReceipt=null;
                OpenCityFacilities();world.ClaimCityQuest(1);OpenCityQuests();
                cityQuestChapter=2;cityQuestScroll=new Vector2(0,32);cityQuestFocusPending=false;
                string unchanged=JsonUtility.ToJson(world);OpenCityQuestObjective(8);
                yield return CaptureCityQuestObjective("smoke-quest-objective-locked.png");
                ExecuteCityQuestObjective();
                if(cityQuestDetailId!=2||cityPage!="quest-detail"||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Future objective bypassed sequence or failed to locate actual current objective.");
                HandleMobileBack();
                if(cityPage!="quests"||cityQuestChapter!=2||cityQuestScroll.y!=32||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Objective system back lost its original chapter/scroll.");
                OpenCityQuests();OpenCityQuestObjective(2);
                yield return CaptureCityQuestObjective("smoke-quest-objective-active.png");
                ExecuteCityQuestObjective();int plot=citySelectedPlot;
                if(cityPage!="facilities"||cityFacilitiesOrigin!="quest-detail"||citySelectedBuilding!=CityBuilding.Mill
                    ||world.CityJob!=null||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Objective action failed to locate the real mill without paying.");
                HandleMobileBack();
                if(cityPage!="quest-detail"||cityQuestDetailId!=2)throw new Exception("Facility back lost objective origin.");
                world.StartCityConstruction(plot,CityBuilding.Mill);
                if(world.CityJob==null)throw new Exception("Objective fixture did not start real construction.");
                yield return CaptureCityQuestObjective("smoke-quest-objective-working.png");
                unchanged=JsonUtility.ToJson(world);ExecuteCityQuestObjective();
                if(cityPage!="facilities"||citySelectedPlot!=plot||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Working objective did not locate its real job.");
                HandleMobileBack();world.Tick(world.CityJob.remaining+.01);
                yield return CaptureCityQuestObjective("smoke-quest-objective-ready.png");
                if(!CityQuestObjectivePlan.Create(world,2).Ready)throw new Exception("Live objective did not observe construction completion.");
                ExecuteCityQuestObjective();
                if(cityQuestReceipt==null||cityQuestReceipt.questId!=2||!world.cityClaimedQuests.Contains(2))
                    throw new Exception("Objective did not use the actual reward receipt.");
                unchanged=JsonUtility.ToJson(world);ExecuteCityQuestObjective();
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Objective duplicate action issued another reward.");
                CloseCityQuestReward();OpenCityQuestObjective(2);
                yield return CaptureCityQuestObjective("smoke-quest-objective-claimed.png");
                ExecuteCityQuestObjective();
                if(cityPage!="quests"||JsonUtility.ToJson(world)!=unchanged)throw new Exception("Claimed objective performed an action.");
                // A direct map entry must discard the prior objective return path.
                OpenCityQuestObjective(3);ExecuteCityQuestObjective();tab="领地";OpenCityFacilities(4);HandleMobileBack();
                if(cityPage!="overview"||cityFacilitiesOrigin!="overview"||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Map facility entry inherited a stale objective origin.");
                CompleteCitySmokeConstruction(world,4,CityBuilding.TownHall);world.ClaimCityQuest(3);world.ClaimCityQuest(4);
                world.Recruit(137);world.ClaimCityQuest(5);
                if(world.CurrentCityQuest==null||world.CurrentCityQuest.id!=6)throw new Exception("Task land fixture did not complete five real objectives.");
                OpenCityQuests();OpenCityQuestObjective(6);unchanged=JsonUtility.ToJson(world);ExecuteCityQuestObjective();
                if(!CityQuestLandReturnAvailable||world.order!=null||world.AttackProblem(map.selected).Length!=0||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Objective land route paid, marched or lost its return source.");
                bool oldReview=reviewMode;reviewMode=true;
                try
                {
                    yield return CaptureState("smoke-quest-objective-land.png");
                    if(!reviewTargets.ContainsKey("quest-land-return")||JsonUtility.ToJson(world)!=unchanged)
                        throw new Exception("Task land did not draw its actual return control.");
                }
                finally{reviewMode=oldReview;if(!reviewMode)reviewTargets.Clear();}
                HandleMobileBack();
                if(tab!="主城"||cityPage!="quest-detail"||cityQuestDetailId!=6||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Task land system back failed to restore the original objective.");
                ExecuteCityQuestObjective();ReturnFromCityQuestLand();
                if(cityPage!="quest-detail"||JsonUtility.ToJson(world)!=unchanged)throw new Exception("Task land button return changed campaign.");
                ExecuteCityQuestObjective();map.selected=world.homeId;DrawCityQuestLandReturn();
                if(cityQuestLandId!=-1||CityQuestLandReturnAvailable||JsonUtility.ToJson(world)!=unchanged)
                    throw new Exception("Selecting other land retained stale task origin.");
                Debug.Log("CITY_QUEST_OBJECTIVE_RUNTIME_OK: five objective states plus actionable land, real mill action/queue/completion/reward, future sequence, claimed read-only, mobile layered return and complete campaign invariance.");
            }
            finally
            {
                session=oldSession;map.Setup(world);map.selected=oldSelected;map.focus=oldFocus;map.zoom=oldZoom;
                tab=oldTab;cityPage=oldPage;status=oldStatus;cityFacilitiesOrigin=oldOrigin;speed=oldSpeed;
                citySelectedPlot=oldPlot;citySelectedBuilding=oldBuilding;cityQuestChapter=oldChapter;cityQuestDetailId=oldId;
                cityQuestScroll=oldScroll;cityQuestObjectiveScroll=oldDetailScroll;cityDetailScroll=oldFacilityScroll;
                cityQuestReceipt=oldReceipt;cityQuestCompleted=oldCompleted;cityQuestFocusPending=oldPending;cityEditingFormation=oldFormation;
                cityQuestLandWorld=oldLandWorld;cityQuestLandId=oldLandId;
                if(JsonUtility.ToJson(world)!=original)throw new Exception("Objective fixture changed the original campaign.");
            }
        }
    }
}
