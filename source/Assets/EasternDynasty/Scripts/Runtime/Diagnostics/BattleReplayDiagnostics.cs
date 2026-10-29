using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public bool replayEventsVisible,replayResultVisible;
            public int replayRound,replayKind,replayAttackerBefore,replayAttackerAfter,replayDefenderBefore,replayDefenderAfter;
            public int replayDamageTotal,replayLossTotal,replayRecoveryTotal,replayGold,replayExperience;
            public float replayEventScrollY,replayEventScrollMaximum;
        }

        private void CaptureReplayPresentationReview(ReviewState review)
        {
            CaptureBattleOutcomeReview(review);
            CaptureBattleFormationReview(review);
            review.replayEventsVisible=review.replayOpen&&replayShowEvents&&!replayShowOutcome&&!battleFormationOpen;
            review.replayEventScrollY=replayEventScroll.y;
            review.replayEventScrollMaximum=replayEventScrollMaximum;
            var frame=BattleReplayFrame.Create(replayRecord,replayPlayer==null?0:replayPlayer.Position);
            if(!review.replayOpen||frame==null)return;
            review.replayRound=frame.round;review.replayKind=(int)frame.kind;
            review.replayAttackerBefore=frame.attackerBefore;review.replayAttackerAfter=frame.attackerAfter;
            review.replayDefenderBefore=frame.defenderBefore;review.replayDefenderAfter=frame.defenderAfter;
            review.replayDamageTotal=frame.damageTotal;review.replayLossTotal=frame.lossTotal;review.replayRecoveryTotal=frame.recoveryTotal;
            review.replayResultVisible=frame.isResult;review.replayGold=frame.goldReward;review.replayExperience=frame.experienceReward;
        }

        private string ReplayWorldHash()
        {
            using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(JsonUtility.ToJson(world)))).Replace("-","");
        }

        private IEnumerator BattleReplaySmoke()
        {
            float previousSpeed=speed;string previousTab=tab;int previousReport=selectedReport;
            var previousRecord=replayRecord;var previousPlayer=replayPlayer;var previousOutcome=replayOutcome;
            bool previousPlaying=replayPlaying,previousEvents=replayShowEvents,previousOutcomeOpen=replayShowOutcome,previousWatching=watchingCurrentBattle;
            float previousClock=replayClock,previousReplaySpeed=replaySpeed;
            var previousEventScroll=replayEventScroll;var previousOutcomeScroll=battleOutcomeScroll;
            var previousFormation=battleFormation;bool previousFormationOpen=battleFormationOpen,previousFormationFromReport=battleFormationFromReport;
            int previousFormationSelected=battleFormationSelected;var previousFormationScroll=battleFormationScroll;
            var record=world.reports.FirstOrDefault(r=>r.HasReplay);
            if(record==null)throw new Exception("Battle replay smoke requires a real settled battle record.");
            speed=0;string before=ReplayWorldHash();
            try
            {
                OpenBattleReplay(record);
                if(!BlocksMapInput)throw new Exception("Replay did not block underlying map input.");
                yield return CaptureState("smoke-battle-replay-start.png");
                SeekBattleEvent(1);
                yield return CaptureState("smoke-battle-replay-event.png");
                yield return BattleFormationSmoke("smoke-battle-formation.png");
                int loss=record.events.FindIndex(e=>e.kind==BattleReplayEventKind.Loss);
                if(loss<0)throw new Exception("Replay field requires a real loss event.");
                SeekBattleEvent(loss);
                yield return CaptureState("smoke-battle-replay-loss.png");
                replayShowEvents=true;
                yield return CaptureState("smoke-battle-replay-details.png");
                replayShowEvents=false;
                SeekBattleEvent(record.events.Count-1);
                yield return CaptureState("smoke-battle-replay-result.png");
                yield return BattleOutcomeSmoke("smoke-battle-outcome.png");
                for(int repeat=0;repeat<10;repeat++)
                {
                    CloseBattleReplay();OpenBattleReplay(record);replaySpeed=2;
                    SeekBattleEvent(0);SeekBattleRound(1);SeekBattleRound(-1);
                    SeekBattleEvent(record.events.Count-1);SeekBattleEvent(1);
                    if(ReplayWorldHash()!=before)throw new Exception("Replay changed campaign state on repeat "+repeat);
                }
                CloseBattleReplay();
                OpenBattleReplay(record);OpenBattleFormation(0,true);HandleMobileBack();
                if(tab!="战报"||battleFormationOpen||ReplayWorldHash()!=before)
                    throw new Exception("Direct report formation did not return to its unchanged report.");
                var healed=CreateReplayPresentationFixture(2000,1,true);
                OpenBattleReplay(healed);
                int recovery=healed.events.FindIndex(e=>e.kind==BattleReplayEventKind.Recovery);
                if(recovery<0)throw new Exception("Replay field recovery fixture did not record actual recovery.");
                SeekBattleEvent(recovery);
                var healingFrame=BattleReplayFrame.Create(healed,recovery);
                if(healingFrame.attackerAfter-healingFrame.attackerBefore!=healingFrame.amount||healingFrame.goldReward!=0)
                    throw new Exception("Recovery display differs from the recorded transition.");
                yield return CaptureState("smoke-battle-replay-recovery.png");
                yield return BattleFormationSmoke("smoke-battle-formation-learned.png",1);
                yield return BattleOutcomeSmoke("smoke-battle-outcome-recovery.png");
                var defeated=CreateReplayPresentationFixture(50,5,false);
                OpenBattleReplay(defeated);SeekBattleEvent(defeated.events.Count-1);
                var defeatFrame=BattleReplayFrame.Create(defeated,replayPlayer.Position);
                if(defeatFrame.captured||defeatFrame.goldReward!=0||!defeatFrame.isResult||defeatFrame.attackerAfter!=0)
                    throw new Exception("Defeat display fabricated occupation or rewards.");
                yield return CaptureState("smoke-battle-replay-defeat.png");
                yield return BattleOutcomeSmoke("smoke-battle-outcome-defeat.png");
                var capped=CreateReplayPresentationFixture(2000,1,false,true);
                OpenBattleReplay(capped);SeekBattleEvent(1);
                if(replayOutcome.experienceReward!=0||replayOutcome.participants.Any(p=>p.awardedExperience!=0))
                    throw new Exception("Outcome manufactured experience for capped participants.");
                yield return BattleOutcomeSmoke("smoke-battle-outcome-capped.png");
                Debug.Log("BATTLE_OUTCOME_RUNTIME_OK: actual settlement projections for victory, defeat, recovery and capped awards; repeated layered mobile back preserves event and complete World JSON.");
                CloseBattleReplay();
                if(ReplayWorldHash()!=before)throw new Exception("Battlefield fixture playback changed the active campaign.");
                Debug.Log("BATTLE_PRESENTATION_RUNTIME_OK: whole-army battlefield, loss/recovery/defeat from actual isolated settlements, event details and cumulative transitions; active World JSON unchanged.");
                var historical=new BattleRecord{title="历史文字战报",text="旧存档仅保存文字结果，未记录可播放事件。",won=true,time=world.elapsed};
                world.reports.Add(historical);
                try
                {
                    selectedReport=world.reports.Count-1;tab="战报";
                    if(historical.HasReplay)throw new Exception("Legacy report was treated as replayable.");
                    yield return CaptureState("smoke-battle-legacy-report.png");
                }
                finally {world.reports.Remove(historical);}
                if(ReplayWorldHash()!=before)throw new Exception("Legacy report display changed campaign state.");
                Debug.Log("BATTLE_REPLAY_RUNTIME_OK: real settlement, frozen event playback, 10 reopen/seek cycles preserve complete World JSON SHA256; legacy text fallback.");
            }
            finally
            {
                speed=previousSpeed;tab=previousTab;selectedReport=previousReport;
                replayRecord=previousRecord;replayPlayer=previousPlayer;replayOutcome=previousOutcome;
                replayPlaying=previousPlaying;replayShowEvents=previousEvents;replayShowOutcome=previousOutcomeOpen;watchingCurrentBattle=previousWatching;
                replayClock=previousClock;replaySpeed=previousReplaySpeed;replayEventScroll=previousEventScroll;battleOutcomeScroll=previousOutcomeScroll;
                battleFormation=previousFormation;battleFormationOpen=previousFormationOpen;battleFormationFromReport=previousFormationFromReport;
                battleFormationSelected=previousFormationSelected;battleFormationScroll=previousFormationScroll;
            }
        }

        private BattleRecord CreateReplayPresentationFixture(int troops,int level,bool healing,bool capped=false)
        {
            var fixture=World.CreateYouzhou();fixture.troops=troops;
            var target=fixture.lands.First(t=>fixture.AttackProblem(t.id)=="");target.level=level;
            foreach(var general in fixture.ActiveGenerals)
            {
                general.learnedSkill=healing?"抚军":"";
                if(capped){general.level=general.LevelCap;general.experience=0;}
            }
            fixture.March(target.id);fixture.Tick(fixture.order.remaining);
            var record=fixture.reports[0];
            if(!BattleReplay.IsValid(record))throw new Exception("Battlefield fixture did not produce a valid real record.");
            return record;
        }

        private IEnumerator BattleWatchSmoke()
        {
            var previousSession=session;string previousTab=tab,previousStatus=status;
            string previousCurrent=currentBattleId,previousPending=pendingBattleWatchId,previousDismissed=dismissedBattleWatchId;
            int previousSelected=map.selected,previousReport=selectedReport;float previousSpeed=speed,previousZoom=map.zoom;
            var previousFocus=map.focus;var previousRecord=replayRecord;var previousPlayer=replayPlayer;
            var previousOutcome=replayOutcome;bool previousOutcomeOpen=replayShowOutcome;
            var previousFormation=battleFormation;bool previousFormationOpen=battleFormationOpen,previousFormationFromReport=battleFormationFromReport;
            int previousFormationSelected=battleFormationSelected;var previousFormationScroll=battleFormationScroll;
            var previousOutcomeScroll=battleOutcomeScroll;bool previousEvents=replayShowEvents;
            bool previousWatching=watchingCurrentBattle,previousPlaying=replayPlaying,previousWatchIntent=watchAttackOnArrival;
            session=new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,"smoke-battle-watch-unused")),()=>DateTime.UtcNow,true);
            try
            {
                session.StartYouzhou(0,"抵达观看验收");map.Setup(world);speed=0;tab="领地";
                currentBattleId=pendingBattleWatchId=dismissedBattleWatchId="";
                replayRecord=null;replayPlayer=null;replayPlaying=watchingCurrentBattle=false;
                int target=world.lands.First(t=>world.AttackProblem(t.id)=="").id;map.selected=target;
                BeginAttack(target);
                if(confirmation==null||world.order!=null||!watchAttackOnArrival)throw new Exception("Attack confirmation failed to precede departure.");
                Action confirm=confirmation;confirmation=null;confirm();
                string battleId=world.order==null?"":world.order.battleId;
                if(tab!="战斗等待"||!BlocksMapInput||string.IsNullOrEmpty(battleId)||world.reports.Count!=0||CurrentBattleRecord()!=null)
                    throw new Exception("Waiting view revealed a battle before arrival.");
                yield return CaptureState("smoke-battle-waiting.png");
                CloseCurrentBattle();world.Tick(world.order.remaining*.5);UpdateBattleWatch();
                if(tab!="领地"||world.reports.Count!=0||CurrentBattleRecord()!=null)throw new Exception("Leaving waiting produced or opened an early result.");
                OpenCurrentBattle();
                if(tab!="战斗等待"||currentBattleId!=battleId||world.reports.Count!=0)throw new Exception("Waiting reentry lost the active march identity.");
                world.Tick(world.order.remaining+.01);UpdateBattleWatch();
                if(tab!="战斗回放"||!watchingCurrentBattle||replayRecord==null||replayRecord.battleId!=battleId||world.reports.Count!=1)
                    throw new Exception("Arrival did not open the matching real settlement record once.");
                SeekBattleEvent(1);
                yield return CaptureState("smoke-battle-current.png");
                yield return BattleFormationSmoke("smoke-battle-formation-current.png");
                yield return BattleOutcomeSmoke("smoke-battle-outcome-current.png");
                string settled=ReplayWorldHash();
                for(int repeat=0;repeat<10;repeat++)
                {
                    SeekBattleEvent(replayRecord.events.Count-1);CloseCurrentBattle();
                    if(tab!="领地")throw new Exception("Current battle exit did not return to the map.");
                    OpenCurrentBattle();SeekBattleEvent(1);replaySpeed=2;SeekBattleRound(1);
                    if(currentBattleId!=battleId||replayRecord.battleId!=battleId||ReplayWorldHash()!=settled)
                        throw new Exception("Current battle reopening repeated settlement or changed identity on repeat "+repeat);
                }
                CloseCurrentBattle();world.Tick(1000);
                int next=world.lands.First(t=>world.AttackProblem(t.id)=="").id;
                BeginAttack(next);confirm=confirmation;confirmation=null;confirm();
                string recalledId=world.order.battleId;Act(world.RecallArmy());CloseCurrentBattle();world.Tick(1000);UpdateBattleWatch();
                if(tab!="领地"||world.reports.Count!=1||world.reports.Any(r=>r.battleId==recalledId))throw new Exception("Recalled march created a battle result.");
                world.Recruit(200);
                next=world.lands.First(t=>world.AttackProblem(t.id)=="").id;
                BeginAttack(next);confirm=confirmation;confirmation=null;confirm();
                string lateId=world.order.battleId;AskBattleRecall();
                world.Tick(world.order.remaining+.01);UpdateBattleWatch();
                if(confirmation==null||tab!="战斗等待"||CurrentBattleRecord()==null||world.reports.Count!=2)
                    throw new Exception("Arrival while recalling did not retain its actual settlement behind the confirmation.");
                string lateSettled=ReplayWorldHash();confirm=confirmation;confirmation=null;confirm();
                if(tab!="战斗回放"||!watchingCurrentBattle||replayRecord.battleId!=lateId||ReplayWorldHash()!=lateSettled)
                    throw new Exception("Late recall changed an already settled battle or lost its current viewing route.");
                Debug.Log("BATTLE_WATCH_RUNTIME_OK: isolated read-only campaign; confirmation, no result before arrival, waiting return/reentry, matching battle identity, 10 reopen/seek cycles preserve complete World JSON SHA256; recall creates no result; arrival during recall confirmation preserves settlement.");
                Debug.Log("BATTLE_FORMATION_RUNTIME_OK: frozen identity, base attack and carried skills; participant switching and layered mobile back preserve history/current battle origins, event cursor and complete World JSON.");
            }
            finally
            {
                confirmation=null;attackConfirmation=false;session=previousSession;map.Setup(world);map.selected=previousSelected;map.focus=previousFocus;map.zoom=previousZoom;
                tab=previousTab;status=previousStatus;selectedReport=previousReport;speed=previousSpeed;
                currentBattleId=previousCurrent;pendingBattleWatchId=previousPending;dismissedBattleWatchId=previousDismissed;
                replayRecord=previousRecord;replayPlayer=previousPlayer;replayPlaying=previousPlaying;watchingCurrentBattle=previousWatching;watchAttackOnArrival=previousWatchIntent;
                replayOutcome=previousOutcome;replayShowOutcome=previousOutcomeOpen;
                battleFormation=previousFormation;battleFormationOpen=previousFormationOpen;battleFormationFromReport=previousFormationFromReport;
                battleFormationSelected=previousFormationSelected;battleFormationScroll=previousFormationScroll;
                battleOutcomeScroll=previousOutcomeScroll;replayShowEvents=previousEvents;
            }
        }
    }
}
