using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    // Developer-only entry points. They never save the player's campaign.
    public sealed partial class EasternDynastyGame
    {
        private readonly System.Collections.Generic.Dictionary<string,ReviewTarget> reviewTargets = new System.Collections.Generic.Dictionary<string,ReviewTarget>();
        private bool reviewWindowConfigured, reviewReadyLogged;
        private int reviewProcessId, reviewCaptureSequence, reviewRepaintFrame = -1, reviewWindowWidth, reviewWindowHeight;
        private void ClearReviewTargets() { if(reviewMode){reviewTargets.Clear();reviewRepaintFrame=Time.frameCount;} }
        private void RecordReviewTarget(string id, Rect absoluteLogicalRect)
        {
            if(!reviewMode)return;
            Rect pixels=GuiRectToPixels(absoluteLogicalRect);
            reviewTargets[id]=new ReviewTarget{id=id,x=pixels.center.x,y=pixels.center.y,w=pixels.width,h=pixels.height};
        }
        [Serializable] private sealed class ReviewTarget { public string id;public float x,y,w,h; }
        private IEnumerator ConfigureValidationWindow(string[] args)
        {
            int requestedWidth=1440,requestedHeight=900;
            for(int i=0;i+1<args.Length;i++)
            {if(args[i]=="-screen-width")int.TryParse(args[i+1],out requestedWidth);if(args[i]=="-screen-height")int.TryParse(args[i+1],out requestedHeight);}
            if(requestedWidth<640||requestedHeight<480)yield break;
            reviewWindowWidth=requestedWidth;reviewWindowHeight=requestedHeight;
            while(!UnityEngine.Rendering.SplashScreen.isFinished)yield return null;
            // Windows can restore a maximized Player window despite the startup arguments.
            Screen.SetResolution(requestedWidth,requestedHeight,FullScreenMode.Windowed);
            for(int i=0;i<10;i++)yield return null;
            reviewWindowConfigured = true;
            Debug.Log("VALIDATION_WINDOW: "+Screen.width+"x"+Screen.height+" requested="+requestedWidth+"x"+requestedHeight);
        }
        private void WatchSmokeErrors(string message, string stack, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            smokeFailed = true;
            // Only -edSmoke subscribes. A failed coroutine cannot reach Smoke's final
            // exit, so terminate this read-only validation player with the real error
            // still in player.log instead of waiting for the host's timeout.
            Application.Quit(1);
        }
        private IEnumerator Smoke()
        {
            if (Environment.GetCommandLineArgs().Contains("-edRoutes"))
            { yield return RouteRuntimeCheck(); yield break; }
            // A frame captured while the Unity splash is active can be entirely black.
            while (!UnityEngine.Rendering.SplashScreen.isFinished) yield return null;
            yield return new WaitForSecondsRealtime(1);
            yield return CaptureState("smoke-welcome.png");
            StartWorld(false);
            for (int i=0;i<5;i++) yield return null;
            if(portraits["huangfu_song"]==null||portraits["han_officer"]==null||portraits["han_scout"]==null)
                throw new Exception("Runtime smoke: original portraits did not import.");
            foreach(string id in new[]{"huangfu_song","han_officer","han_scout"})
                if(Math.Abs(portraits[id].width/(float)portraits[id].height-2f/3f)>.01f)
                    throw new Exception("Runtime smoke: portrait aspect ratio changed: "+id);
            yield return CaptureMapHudState("smoke-overview.png");
            if(world.IsYouzhou)yield return MapHudVisualSmoke();
            if(!map.IsBlocked(GuiPointToPixels(new Vector2(40,110))))
                throw new Exception("Runtime smoke: HUD must block map interaction.");
            int target = world.lands.First(t => world.AttackProblem(t.id).Length == 0).id;
            var targetLand = world.lands[target];
            Vector3 screenPoint = map.viewCamera.WorldToScreenPoint(map.SurfacePoint(targetLand));
            int picked;
            if (!map.TryPickLand(screenPoint, out picked) || picked != target)
                throw new Exception("Runtime smoke: map picking failed.");
            if(world.IsYouzhou)
            {
                if(world.lands.Count!=1600||world.mapId!=YouzhouMapDefinition.Id||!map.YouzhouAssetsReady)
                    throw new Exception("Runtime smoke: Youzhou map or assets missing: "+map.MissingYouzhouAssets);
                foreach(var p in new[]{new Vector2(0,0),new Vector2(39,0),new Vector2(0,39),new Vector2(39,39),new Vector2(7,8),new Vector2(8,7),new Vector2(23,24)})
                {
                    var cell=world.TryGetLand((int)p.x,(int)p.y);map.focus=new Vector3(cell.x,0,cell.z);
                    for(int i=0;i<3;i++)yield return null;
                    var pixel=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(cell));
                    if(!map.TryPickLand(pixel,out picked)||picked!=cell.id)throw new Exception("Grid edge/chunk picking mismatch "+cell.id);
                }
                map.focus=new Vector3(19.5f,0,19.5f);map.zoom=17;
                yield return CaptureState("smoke-map-far.png");
                map.focus=new Vector3(25,0,19);map.zoom=5.8f;
                yield return CaptureState("smoke-river.png");
                map.focus=new Vector3(10,0,12);map.zoom=2.5f;
                yield return CaptureState("smoke-resources.png");
                map.zoom=4.2f;map.CenterHome();
                for(int i=0;i<3;i++)yield return null;
            }
            map.selected=target;
            if(world.IsYouzhou)yield return CaptureLandVisualState("smoke-land.png");
            else yield return CaptureState("smoke-land.png");
            map.selected=world.lands.First(t=>t.capital&&t.region==world.birthRegion).id;
            yield return CaptureState("smoke-city.png");
            map.selected=target;
            world.March(target);
            yield return CaptureMapHudState("smoke-march.png");
            world.Tick(120);
            if (!world.lands[target].owned || world.reports.Count == 0) throw new Exception("Runtime smoke: capture failed.");
            map.RefreshOwnership(); map.selected = target;
            status = world.messages[0];
            string[] tabs = { "领地", "武将", "招募", "战报" };
            string[] captures = { "smoke.png", "smoke-roster.png", "smoke-recruitment.png", "smoke-report.png" };
            for (int i=0;i<tabs.Length;i++)
            {
                tab = tabs[i];
                yield return CaptureState(captures[i]);
            }
            tab="武将";selectedCard=world.cards[1].uid;
            yield return CaptureState("smoke-detail.png");
            tab="招募";int previousCards=world.cards.Count;double previousJade=world.resources.jade;
            Act(world.Draw(true,new System.Random(1)));drawResult=world.cards.Last();
            if(world.cards.Count!=previousCards+1||world.resources.jade!=previousJade-100)
                throw new Exception("Runtime smoke: recruitment failed.");
            yield return CaptureState("smoke-result.png");drawResult=null;
            Act(world.Draw(true,new System.Random(184)));drawResult=world.cards.Last();
            yield return CaptureState("smoke-result-placeholder.png");drawResult=null;
            if(world.IsYouzhou){yield return S1Smoke(target);yield return ExperienceSmoke();yield return BattleReplaySmoke();yield return BattleReportArchiveSmoke();yield return BattleWatchSmoke();}
            tab="领地";map.selected=-1;
            for(int i=0;i<30;i++)yield return null;
            var samples=new System.Collections.Generic.List<float>();
            for(int i=0;i<120;i++){yield return null;samples.Add(Time.unscaledDeltaTime*1000);}
            samples.Sort();
            string metrics="Resolution="+Screen.width+"x"+Screen.height+"; GPU="+SystemInfo.graphicsDeviceName+
                "; frameMedianMs="+samples[60].ToString("F2")+"; frameP95Ms="+samples[114].ToString("F2")+
                "; activeRenderers="+FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length+
                "; note=120 Player frame intervals, includes 60 fps cap; not isolated CPU/GPU timings";
            File.WriteAllText(Path.Combine(Application.persistentDataPath,"visual-metrics.txt"),metrics);
            Debug.Log(metrics);
            yield return new WaitForSecondsRealtime(2);
            if (!smokeFailed) Debug.Log("EASTERN_DYNASTY_RUNTIME_SMOKE_OK: portraits, HUD blocking, grid picking, capture, return, recruitment; "+(world.IsYouzhou?"Youzhou S1 including card detail, city and quests":"12 legacy")+" visual states");
            Application.Quit(smokeFailed ? 1 : 0);
        }
        private IEnumerator CaptureState(string filename)
        {
            for(int i=0;i<5;i++)yield return null;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,filename));
            yield return null;
        }
        private void UpdateReviewCapture()
        {
            if (!reviewMode) return;
            if (reviewProcessId == 0) reviewProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            // Update follows the previous frame's actual GUI pass. The automation
            // must not send its first key while startup or window resizing still runs.
            if (!reviewReadyLogged && reviewWindowConfigured && world != null && stylesReady &&
                Screen.width == reviewWindowWidth && Screen.height == reviewWindowHeight &&
                reviewRepaintFrame >= 0 && Time.frameCount > reviewRepaintFrame && reviewTargets.ContainsKey("roster-tab"))
            {
                reviewReadyLogged = true;
                Debug.Log("ED_REVIEW_READY pid=" + reviewProcessId + " frame=" + Time.frameCount +
                    " targets=" + reviewTargets.Count + " size=" + Screen.width + "x" + Screen.height);
            }
            if(reviewMode&&Input.GetKeyDown(KeyCode.F8))
            {
                int captureSequence = ++reviewCaptureSequence;
                Debug.Log("ED_REVIEW_CAPTURE_REQUEST pid=" + reviewProcessId + " sequence=" + captureSequence +
                    " frame=" + Time.frameCount + " focused=" + Application.isFocused);
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath,"review-capture.png"));
                var review=new ReviewState{tab=tab,selected=map.selected,selectedName=world!=null&&map.selected>=0?world.lands[map.selected].name:"",focusX=map.focus.x,focusZ=map.focus.z,zoom=map.zoom,jade=world==null?0:world.resources.jade,modal=drawResult!=null,blockedCount=map.blockedRects.Length,mouseBlocked=map.IsBlocked(new Vector2(Input.mousePosition.x,Screen.height-Input.mousePosition.y))};
                review.reviewProcessId = reviewProcessId;
                review.reviewCaptureSequence = captureSequence;
                review.reviewCaptureFrame = Time.frameCount;
                if(world!=null)
                {
                    review.orderActive=world.order!=null;review.orderPhase=world.order==null?-1:world.order.phase;
                    review.orderAction=world.order==null?-1:world.order.action;review.confirmationOpen=confirmation!=null;
                    review.orderReturning=world.order!=null&&world.order.returning;review.commands=world.AvailableCommands;review.speed=speed;
                    review.selectedOwned=map.selected>=0&&world.lands[map.selected].owned;
                    review.selectedGuarded=map.selected>=0&&world.IsGuarded(map.selected);
                    var target=world.lands.FirstOrDefault(t=>world.AttackProblem(t.id)=="");
                    if(target!=null){var pixel=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(target));review.nextTarget=target.id;review.targetX=pixel.x;review.targetY=Screen.height-pixel.y;}
                    review.catalogFaction=s1Faction;review.cardCount=world.cards.Count;
                    var chosen=world.cards.Find(c=>c.uid==selectedCard);review.selectedCardLevel=chosen==null?0:chosen.level;review.selectedCardExperience=chosen==null?0:chosen.experience;
                    review.cardDetail=cardDetailId??"";review.cardSection=cardDetailSection;
                    FillCardVisualReview(review);
                    FillArchiveVisualReview(review);
                    FillOwnedRosterReview(review);
                    FillMapHudReview(review);
                    FillLandVisualReview(review);
                    review.cityOpen=CityPanelOpen;review.cityPlot=citySelectedPlot;review.cityBuilding=world.CityJob!=null;
                    review.cityPage=cityPage;review.questChapter=cityQuestChapter;review.questCompleted=cityQuestCompleted;
                    FillReinforcementReview(review);
                    FillCityCatalogReview(review);
                    FillCityFacilityDossierReview(review);
                    FillCityOverviewReview(review);
                    FillCityConstructionReview(review);
                    FillCityQuestObjectiveReview(review);
                    review.cityFacility=(int)citySelectedBuilding;
                    review.cityConstructionPlot=world.CityJob==null?-1:world.CityJob.plotIndex;
                    var facilityDefinition=CityCatalog.Get(citySelectedBuilding);
                    if(facilityDefinition!=null)
                    {
                        int facilityLevel=world.CityLevel(citySelectedBuilding),nextFacilityLevel=Math.Min(facilityDefinition.maximumLevel,facilityLevel+1);
                        review.cityCurrentEffect=CityCatalog.EffectAtLevel(citySelectedBuilding,facilityLevel);
                        review.cityNextEffect=CityCatalog.EffectAtLevel(citySelectedBuilding,nextFacilityLevel);
                        review.cityRequiredHall=CityCatalog.RequiredHallLevel(citySelectedBuilding,nextFacilityLevel);
                    }
                    review.cityDetailScrollY=cityDetailScroll.y;
                    review.questCurrentId=world.CurrentCityQuest==null?0:world.CurrentCityQuest.id;
                    review.questScrollY=cityQuestScroll.y;
                    review.receiptOpen=cityQuestReceipt!=null;
                    if(cityQuestReceipt!=null)
                    {
                        review.receiptId=cityQuestReceipt.questId;review.receiptNext=cityQuestReceipt.nextQuestId;
                        review.receiptChapter=cityQuestReceipt.chapterIndex;review.receiptTitle=cityQuestReceipt.title;
                        review.receiptChapterComplete=cityQuestReceipt.chapterComplete;review.receiptAllComplete=cityQuestReceipt.allComplete;
                        review.receiptCredited=CityRewardResourceValues(cityQuestReceipt.credited);
                        review.receiptBalance=CityRewardResourceValues(cityQuestReceipt.balance);
                    }
                    review.resources=new[]{world.resources.grain,world.resources.grass,world.resources.stone,world.resources.iron,world.resources.gold};
                    var refund=world.CityConstructionRefund();
                    review.cityRefund=new[]{refund.grain,refund.grass,refund.stone,refund.iron,refund.gold};
                    review.cityRefundConfirmation=confirmation!=null&&cityCancellation!=null;
                    review.visibleQuestIds=reviewTargets.Keys.Where(id=>id.StartsWith("quest-row-",StringComparison.Ordinal)).Select(id=>int.Parse(id.Substring(10))).OrderBy(id=>id).ToArray();
                    review.cityLevel=world.CityLevel(CityBuilding.TownHall);review.cityMillLevel=world.CityLevel(CityBuilding.Mill);review.claimedQuests=world.cityClaimedQuests==null?0:world.cityClaimedQuests.Count;
                    review.questReady=world.CurrentCityQuest!=null&&world.CityQuestReady(world.CurrentCityQuest.id);
                    review.trainingReturnTab=trainingReturnTab;review.trainingTarget=trainingSelectedTarget;
                    review.trainingProblem=world.TrainingProblem(trainingSelectedTarget);review.trainingReward=world.TrainingExperience(trainingSelectedTarget);
                    review.trainingActive=world.order!=null&&world.order.action==(int)ArmyAction.Training;
                    review.trainingOrderSettled=review.trainingActive&&world.order.trainingSettled;
                    review.trainingCompletionCount=world.cityTrainingCompletions;review.trainingHistoryCount=world.trainingHistory==null?0:world.trainingHistory.Count;
                    review.generalExperienceTotal=world.cards.Sum(c=>c.level*(c.level-1)*40+c.experience);
                    var trainingReceipt=world.LastTrainingReceipt;
                    review.trainingReceiptStatus=trainingReceipt==null?"none":trainingReceipt.cancelled?"cancelled":"completed";
                    review.trainingReceiptTarget=trainingReceipt==null?-1:trainingReceipt.target;
                    review.trainingReceiptAward=trainingReceipt==null?0:trainingReceipt.awardedExperience.Sum();
                    FillTrainingHistoryReview(review);
                    FillTrainingTargetReview(review);
                    FillTrainingExecutionReview(review);
                    review.replayOpen=tab=="战斗回放";review.replayPlaying=replayPlaying;
                    review.replayPosition=replayPlayer==null?-1:replayPlayer.Position;
                    review.replayCount=replayRecord==null||replayRecord.events==null?0:replayRecord.events.Count;
                    review.replayBattleId=replayRecord==null?"":replayRecord.battleId;
                    review.replaySpeed=replaySpeed;review.replayWorldHash=review.replayOpen?ReplayWorldHash():"";
                    CaptureReplayPresentationReview(review);
                    CaptureReportArchiveReview(review);
                    review.reportCount=world.reports.Count;review.troops=world.troops;
                    review.battleWaiting=tab=="战斗等待";review.watchingCurrentBattle=watchingCurrentBattle;
                    review.watchAttackOnArrival=watchAttackOnArrival;review.currentBattleId=currentBattleId;
                    review.currentBattlePending=!string.IsNullOrEmpty(pendingBattleWatchId);
                    review.orderBattleId=world.order==null?"":world.order.battleId;
                    review.orderWatchOnArrival=world.order!=null&&world.order.watchOnArrival;
                    var lastBattle=world.reports.FirstOrDefault(r=>r.HasReplay);
                    review.lastReportBattleId=lastBattle==null?"":lastBattle.battleId;
                    review.ownedLandCount=world.lands.Count(t=>t.owned);review.battleWorldHash=ReplayWorldHash();
                    var home=world.lands[world.homeId];var homePixel=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(home));review.homeX=homePixel.x;review.homeY=Screen.height-homePixel.y;
                }
                review.targets=reviewTargets.Values.ToArray();
                File.WriteAllText(Path.Combine(Application.persistentDataPath,"review-state.json"),JsonUtility.ToJson(review,true));
                Debug.Log("ED_REVIEW_CAPTURE_STATE_WRITTEN pid=" + reviewProcessId + " sequence=" + captureSequence +
                    " frame=" + Time.frameCount + " targets=" + review.targets.Length + " screenshot=requested");
            }
        }
        private sealed partial class ReviewState
        {
            public int reviewProcessId, reviewCaptureSequence, reviewCaptureFrame;
            public int questCurrentId;
            public float questScrollY;
            public double[] resources,cityRefund;
            public bool cityRefundConfirmation;
            public bool receiptOpen,receiptChapterComplete,receiptAllComplete;
            public int receiptId,receiptNext,receiptChapter;
            public string receiptTitle;
            public double[] receiptCredited,receiptBalance;
        }
        [Serializable] private sealed partial class ReviewState
        {public string tab,selectedName,catalogFaction,cardDetail,trainingReturnTab,trainingProblem,trainingReceiptStatus,cityPage,replayBattleId,replayWorldHash,currentBattleId,orderBattleId,lastReportBattleId,battleWorldHash;public int selected,blockedCount,orderPhase,orderAction,commands,nextTarget,cardCount,cardSection,cityPlot,cityLevel,cityMillLevel,claimedQuests,selectedCardLevel,selectedCardExperience,trainingTarget,trainingReward,trainingCompletionCount,trainingHistoryCount,generalExperienceTotal,trainingReceiptTarget,trainingReceiptAward,questChapter,replayPosition,replayCount,reportCount,troops,ownedLandCount,cityFacility,cityConstructionPlot,cityCurrentEffect,cityNextEffect,cityRequiredHall;public float focusX,focusZ,zoom,speed,targetX,targetY,homeX,homeY,replaySpeed,cityDetailScrollY;public double jade;public bool modal,mouseBlocked,orderActive,orderReturning,selectedOwned,selectedGuarded,cityOpen,cityBuilding,questReady,confirmationOpen,trainingActive,trainingOrderSettled,questCompleted,replayOpen,replayPlaying,battleWaiting,watchingCurrentBattle,watchAttackOnArrival,currentBattlePending,orderWatchOnArrival;public int[] visibleQuestIds;public ReviewTarget[] targets;}
    }
}
