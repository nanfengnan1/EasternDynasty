using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Vector2 battleOutcomeScroll;

        private void OpenBattleOutcome()
        {
            if(replayOutcome==null)return;
            replayPlaying=false;replayClock=0;replayShowOutcome=true;
            battleOutcomeScroll=Vector2.zero;map.CancelPointerInput();
        }

        private void CloseBattleOutcome()
        {
            replayShowOutcome=false;replayPlaying=false;map.CancelPointerInput();
        }

        private void DrawBattleOutcome(Rect area)
        {
            var result=replayOutcome;
            if(result==null){CloseBattleOutcome();return;}
            // Keep actions outside scrolling so short safe areas retain a visible return path.
            var viewport=new Rect(area.x,area.y,area.width,area.height-62);
            RecordReviewTarget("outcome-content",viewport);
            float contentWidth=viewport.width-24, contentHeight=Mathf.Max(378,viewport.height);
            battleOutcomeScroll=BeginContentScroll(viewport,battleOutcomeScroll,new Rect(0,0,contentWidth,contentHeight));
            Frame(new Rect(0,0,contentWidth,84),Panel);
            RectFill(new Rect(0,0,5,84),result.captured?Green:Red);
            Label(18,12,contentWidth*.42f,32,result.captured?"胜利 · 已占领":"未占领 · 部队撤回",heading);
            Label(18,51,contentWidth*.42f,26,result.targetName+"  ("+result.targetX+", "+result.targetZ+") · "+result.rounds+" 回合",small);
            Label(contentWidth*.46f,12,contentWidth*.23f,30,"金币  +"+result.goldReward,heading);
            Label(contentWidth*.72f,12,contentWidth*.27f-16,30,"实得经验  +"+result.experienceReward,heading);
            Label(contentWidth*.46f,51,contentWidth*.53f-16,26,"战斗时已到账 · 查看、播放和返回均不重复发放",small);

            float leftWidth=contentWidth*.43f;
            var ledger=new Rect(0,98,leftWidth,280);Frame(ledger,Ink);
            Label(16,ledger.y+12,leftWidth-32,30,"整队兵力结算",heading);
            DrawOutcomeTroopBar(new Rect(16,ledger.y+56,leftWidth-32,52),"我方",result.attackerInitial,result.attackerFinal,Green);
            Label(16,ledger.y+121,leftWidth-32,27,"受损  −"+result.lossTotal+"    恢复  +"+result.recoveryTotal+"    净减员  "+(result.lossTotal-result.recoveryTotal),body);
            Label(16,ledger.y+153,leftWidth-32,25,result.attackerInitial+" − "+result.lossTotal+" + "+result.recoveryTotal+" = "+result.attackerFinal+" 兵",small);
            DrawOutcomeTroopBar(new Rect(16,ledger.y+192,leftWidth-32,52),"守军",result.defenderInitial,result.defenderFinal,Red);
            Label(16,ledger.y+251,leftWidth-32,23,"造成守军损失  "+result.damageTotal+" · 未记录逐将伤害",tiny);

            var roster=new Rect(leftWidth+16,98,contentWidth-leftWidth-16,280);Frame(roster,Ink);
            Label(roster.x+16,roster.y+12,roster.width-32,30,"参战武将 · 点卡查看阵容",heading);
            float cardWidth=(roster.width-32-12*(result.participants.Count-1))/result.participants.Count;
            for(int i=0;i<result.participants.Count;i++)
            {
                var participant=result.participants[i];
                var card=new Rect(roster.x+16+i*(cardWidth+12),roster.y+56,cardWidth,208);Frame(card,Panel);
                var art=new Rect(card.x+12,card.y+12,Mathf.Min(82,card.width*.42f),108);
                Portrait(art,participant.definitionId);Outline(art,Gold);
                float textX=art.xMax+10,textWidth=card.xMax-textX-8;
                Label(textX,card.y+12,textWidth,52,participant.name,body);
                Label(textX,card.y+69,textWidth,52,"Lv."+participant.level+"\n"+participant.rank+"阶",small);
                Label(card.x+12,card.y+130,card.width-24,27,Slots[Mathf.Min(i,2)]+" · 战前等级",small);
                Label(card.x+12,card.y+165,card.width-24,30,"经验 +"+participant.awardedExperience,heading);
                bool visible=card.y>=battleOutcomeScroll.y&&card.yMax<=battleOutcomeScroll.y+viewport.height;
                if(visible)
                {
                    RecordReviewTarget("outcome-participant-"+i,new Rect(viewport.x+card.x,viewport.y+card.y-battleOutcomeScroll.y,card.width,card.height));
                    if(ContentTileClicked(card,"outcome-participant-"+i,participant.uid))OpenBattleFormation(i);
                }
            }
            GUI.EndScrollView(GUI.enabled);

            float y=area.yMax-44;
            if(ReplayButton("outcome-return",new Rect(area.x,y,180,42),"回到当前事件"))CloseBattleOutcome();
            if(ReplayButton("outcome-play",new Rect(area.x+192,y,180,42),"从头播放",true))
            {CloseBattleOutcome();replayShowEvents=false;SeekBattleEvent(0);replayPlaying=true;}
            if(ReplayButton("outcome-location",new Rect(area.x+384,y,180,42),"查看战斗地点"))
            {
                replayShowOutcome=false;replayPlaying=false;
                if(watchingCurrentBattle)CloseCurrentBattle();else tab="领地";
                map.selected=result.targetId;map.focus=new Vector3(result.targetX,0,result.targetZ);map.CancelPointerInput();
            }
            Label(area.x+584,y+5,area.width-584,34,"记录中的阵容与收益，不随当前编队变化",small);
        }

        private void DrawOutcomeTroopBar(Rect area,string name,int initial,int final,Color accent)
        {
            Label(area.x,area.y,area.width,28,name+"  "+initial+" → "+final+" 兵",body);
            var bar=new Rect(area.x,area.y+38,area.width,8);RectFill(bar,Panel);
            RectFill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01((float)final/initial),bar.height),accent);
        }
    }
}
