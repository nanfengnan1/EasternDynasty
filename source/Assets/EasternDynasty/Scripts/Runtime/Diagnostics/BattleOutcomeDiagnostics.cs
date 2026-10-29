using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public bool outcomeOpen,outcomeCaptured;
            public string outcomeBattleId;
            public int outcomeGold,outcomeExperience,outcomeInitial,outcomeFinal,outcomeLoss,outcomeRecovery,outcomeDefenderInitial,outcomeDefenderFinal;
            public int[] outcomeParticipantIds,outcomeParticipantExperience;
            public float outcomeScrollY;
        }

        private void CaptureBattleOutcomeReview(ReviewState review)
        {
            review.outcomeOpen=tab=="战斗回放"&&replayShowOutcome&&replayOutcome!=null&&!battleFormationOpen;
            if(!review.outcomeOpen)return;
            var result=replayOutcome;
            review.outcomeBattleId=result.battleId;review.outcomeCaptured=result.captured;
            review.outcomeGold=result.goldReward;review.outcomeExperience=result.experienceReward;
            review.outcomeInitial=result.attackerInitial;review.outcomeFinal=result.attackerFinal;
            review.outcomeLoss=result.lossTotal;review.outcomeRecovery=result.recoveryTotal;
            review.outcomeDefenderInitial=result.defenderInitial;review.outcomeDefenderFinal=result.defenderFinal;
            review.outcomeParticipantIds=result.participants.Select(p=>p.uid).ToArray();
            review.outcomeParticipantExperience=result.participants.Select(p=>p.awardedExperience).ToArray();
            review.outcomeScrollY=battleOutcomeScroll.y;
        }

        private IEnumerator BattleOutcomeSmoke(string capture)
        {
            int cursor=replayPlayer.Position;bool events=replayShowEvents,watching=watchingCurrentBattle;
            string before=ReplayWorldHash();
            OpenBattleOutcome();
            if(!replayShowOutcome||replayOutcome==null||replayPlaying||!BlocksMapInput
                ||replayOutcome.battleId!=replayRecord.battleId
                ||replayOutcome.attackerInitial-replayOutcome.lossTotal+replayOutcome.recoveryTotal!=replayOutcome.attackerFinal
                ||replayOutcome.experienceReward!=replayRecord.attacker.generals.Sum(g=>g.awardedExperience))
                throw new Exception("Outcome view did not use the settled event ledger and participant awards.");
            yield return CaptureState(capture);
            if(capture=="smoke-battle-outcome.png")yield return BattleFormationSmoke("smoke-battle-formation-outcome.png",replayRecord.attacker.generals.Count-1);
            for(int repeat=0;repeat<5;repeat++)
            {
                HandleMobileBack();
                if(tab!="战斗回放"||replayShowOutcome||replayPlayer.Position!=cursor||replayShowEvents!=events||watchingCurrentBattle!=watching)
                    throw new Exception("Outcome back lost the current event or battle viewing origin.");
                OpenBattleOutcome();
            }
            CloseBattleOutcome();
            if(ReplayWorldHash()!=before||replayPlayer.Position!=cursor)
                throw new Exception("Outcome viewing repeated settlement or moved the event cursor.");
        }
    }
}
