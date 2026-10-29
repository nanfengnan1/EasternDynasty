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
            public bool battleFormationOpen, battleFormationFromReport;
            public string battleFormationId, battleFormationName, battleFormationSkill, battleFormationLearnedSkill;
            public int battleFormationSelected, battleFormationUid, battleFormationLevel, battleFormationRank, battleFormationAttack;
            public int[] battleFormationParticipants;
            public float battleFormationScrollY, battleFormationScrollMaximum;
        }

        private void CaptureBattleFormationReview(ReviewState review)
        {
            review.battleFormationOpen = tab == "战斗回放" && battleFormationOpen && battleFormation != null;
            if (!review.battleFormationOpen) return;
            var selected = battleFormation.participants[battleFormationSelected];
            review.battleFormationFromReport = battleFormationFromReport;
            review.battleFormationId = battleFormation.battleId;
            review.battleFormationSelected = battleFormationSelected;
            review.battleFormationParticipants = battleFormation.participants.Select(p => p.uid).ToArray();
            review.battleFormationUid = selected.uid; review.battleFormationName = selected.name;
            review.battleFormationLevel = selected.level; review.battleFormationRank = selected.rank;
            review.battleFormationAttack = selected.attack; review.battleFormationSkill = selected.skill;
            review.battleFormationLearnedSkill = selected.learnedSkill;
            review.battleFormationScrollY = battleFormationScroll.y;
            review.battleFormationScrollMaximum = Mathf.Max(0, 412 - (height - 198 - 244));
        }

        private IEnumerator BattleFormationSmoke(string capture, int participant = 0)
        {
            int cursor = replayPlayer.Position;
            bool events = replayShowEvents, outcome = replayShowOutcome, watching = watchingCurrentBattle;
            var eventScroll = replayEventScroll; var outcomeScroll = battleOutcomeScroll;
            string before = ReplayWorldHash();
            OpenBattleFormation(participant);
            if (!battleFormationOpen || battleFormation == null || replayPlaying || !BlocksMapInput
                || battleFormation.battleId != replayRecord.battleId)
                throw new Exception("Frozen formation did not open from the actual battle record.");
            for (int i = 0; i < battleFormation.participants.Count; i++)
            {
                SelectBattleParticipant(i);
                var shown = battleFormation.participants[battleFormationSelected];
                var recorded = replayRecord.attacker.generals[i];
                if (shown.uid != recorded.uid || shown.name != recorded.name || shown.definitionId != recorded.definitionId
                    || shown.level != recorded.level || shown.rank != recorded.rank || shown.attack != recorded.attack
                    || shown.skill != recorded.skill || shown.learnedSkill != recorded.learnedSkill)
                    throw new Exception("Formation detail differs from the frozen participant or equipment.");
            }
            SelectBattleParticipant(participant);
            yield return CaptureState(capture);
            HandleMobileBack();
            if (battleFormationOpen || tab != "战斗回放" || replayPlayer.Position != cursor
                || replayShowEvents != events || replayShowOutcome != outcome || watchingCurrentBattle != watching
                || replayEventScroll != eventScroll || battleOutcomeScroll != outcomeScroll || ReplayWorldHash() != before)
                throw new Exception("Formation back lost its exact source or changed the settled campaign.");
        }
    }
}
