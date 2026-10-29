using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private BattleRecord replayRecord;
        private BattleReplayPlayer replayPlayer;
        private bool replayPlaying;
        private bool replayShowEvents;
        private bool replayShowOutcome;
        private BattleOutcomeSummary replayOutcome;
        private float replayClock, replaySpeed = 1;
        private Vector2 battleTextScroll, replayEventScroll;

        private void OpenBattleReplay(BattleRecord record)
        {
            if(record==null||!record.HasReplay)return;
            replayRecord=record;replayPlayer=new BattleReplayPlayer(record);
            replayOutcome=BattleOutcomeSummary.Create(record);replayShowOutcome=false;
            battleFormationOpen=false;battleFormationFromReport=false;
            watchingCurrentBattle=false;
            replayPlaying=false;replayShowEvents=false;replayClock=0;replaySpeed=1;replayEventScroll=Vector2.zero;
            tab="战斗回放";map.CancelPointerInput();
        }

        private void CloseBattleReplay()
        {
            if(battleFormationOpen){CloseBattleFormation();return;}
            if(replayShowOutcome){CloseBattleOutcome();return;}
            if(watchingCurrentBattle){CloseCurrentBattle();return;}
            replayPlaying=false;tab="战报";
            RestoreArchivedReport(replayRecord);
            map.CancelPointerInput();
        }

        private void UpdateBattleReplay()
        {
            if(tab!="战斗回放"||battleFormationOpen||replayShowOutcome||!replayPlaying||replayPlayer==null||mobileClock.IsPaused)return;
            replayClock+=Time.unscaledDeltaTime*replaySpeed;
            if(replayClock<.85f)return;
            replayClock=0;SeekBattleEvent(replayPlayer.Position+1,false);
            if(replayPlayer.Position>=replayRecord.events.Count-1)replayPlaying=false;
        }

        private void SeekBattleEvent(int position,bool pause=true)
        {
            if(replayPlayer==null)return;
            replayPlayer.Seek(position);replayClock=0;
            if(pause)replayPlaying=false;
            replayEventScroll.y=Mathf.Max(0,(replayPlayer.Position-1)*ReplayEventRowHeight);
        }

        private void SeekBattleRound(int direction)
        {
            int round=replayPlayer.Current.round;
            int index=replayPlayer.Position+direction;
            while(index>0&&index<replayRecord.events.Count-1&&replayRecord.events[index].round==round)index+=direction;
            SeekBattleEvent(index);
        }

        private void DrawBattleArmy(Rect area,BattleArmySnapshot army,int troops,Color accent,bool large)
        {
            Frame(area,Panel);RectFill(new Rect(area.x,area.y,4,area.height),accent);
            Label(area.x+14,area.y+10,area.width-28,25,army.name,body);
            Label(area.x+14,area.y+40,area.width-28,28,troops+" / "+army.troops+" 兵",heading);
            var bar=new Rect(area.x+14,area.y+76,area.width-28,7);RectFill(bar,Ink);
            RectFill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(army.troops>0?(float)troops/army.troops:0),bar.height),accent);
            var generals=army.generals;
            if(generals!=null&&generals.Count>0)
            {
                float cell=(area.width-28)/generals.Count;
                for(int i=0;i<generals.Count;i++)
                {
                    var g=generals[i];float x=area.x+14+i*cell;
                    float artHeight=Mathf.Max(42,area.height-124), artWidth=Mathf.Min(cell-10,artHeight*.74f);
                    var art=new Rect(x,area.y+94,artWidth,artHeight);Portrait(art,g.definitionId);Outline(art,accent);
                    if(large)
                    {
                        Label(x+artWidth+8,art.y+8,cell-artWidth-14,55,g.name,body);
                        Label(x+artWidth+8,art.y+68,cell-artWidth-14,50,"Lv."+g.level+"\n"+g.rank+"阶",small);
                    }
                    else Label(x+artWidth+8,art.y,cell-artWidth-14,55,g.name+"\nLv."+g.level,tiny);
                    if(large)Label(x,area.yMax-26,cell-8,24,Slots[Mathf.Min(i,2)]+" · 战前阵容",tiny);
                }
            }
            else
            {
                var icon=new Rect(area.x+20,area.y+102,44,44);Frame(icon,Ink);GUI.Label(icon,"守",centered);
                Label(icon.xMax+14,icon.y,area.width-92,50,"地块整队守军\n无独立武将配置",small);
            }
        }

        private void DrawBattleReplay()
        {
            if(replayRecord==null||replayPlayer==null){CloseBattleReplay();return;}
            var page=new Rect(24,94,width-48,height-198);Block(page);
            if(battleFormationOpen){DrawBattleFormation(page);return;}
            Label(page.x,page.y,page.width-760,34,(watchingCurrentBattle?"本场战斗过程  ·  ":"战斗过程回放  ·  ")+replayRecord.targetName,heading);
            var back=new Rect(page.xMax-150,page.y,150,38);RecordReviewTarget("replay-back",back);
            if(Button(back,replayShowOutcome?"返回过程":watchingCurrentBattle?"返回地图":"返回战报"))CloseBattleReplay();
            if(ReplayButton("replay-formation",new Rect(back.x-588,page.y,140,38),"参战阵容"))OpenBattleFormation();
            if(ReplayButton("replay-field",new Rect(back.x-438,page.y,128,38),"战场",!replayShowEvents&&!replayShowOutcome)){replayShowOutcome=false;replayShowEvents=false;}
            if(ReplayButton("replay-events",new Rect(back.x-300,page.y,140,38),"事件详情",replayShowEvents&&!replayShowOutcome)){replayShowOutcome=false;replayShowEvents=true;}
            if(ReplayButton("replay-outcome",new Rect(back.x-150,page.y,140,38),"战果总览",replayShowOutcome))OpenBattleOutcome();
            Label(page.x,page.y+39,page.width,25,"已结算记录 · 项目整队战斗示意 · 回放期间暂停本地推演",small);
            if(replayShowOutcome){DrawBattleOutcome(new Rect(page.x,page.y+74,page.width,page.height-74));return;}
            var current=replayPlayer.Current;
            var frame=BattleReplayFrame.Create(replayRecord,replayPlayer.Position);
            if(frame==null)
            {
                replayPlaying=false;
                Label(page.x,page.y+90,page.width,80,"这份记录缺少有效的顺序事件，请返回战报查看原文。",body);
                return;
            }
            float half=(page.width-18)/2;
            DrawReplayArmyHeader(new Rect(page.x,page.y+74,half,92),replayRecord.attacker,current.attackerTroops,frame.attackerBefore,Green);
            DrawReplayArmyHeader(new Rect(page.x+half+18,page.y+74,half,92),replayRecord.defender,current.defenderTroops,frame.defenderBefore,Red);
            var controlsY=page.yMax-46;
            float roundsY=controlsY-82;
            var scene=new Rect(page.x,page.y+178,page.width-340,roundsY-(page.y+178)-12);
            if(replayShowEvents)DrawReplayEventList(scene);else DrawReplayBattlefield(scene,current);
            DrawReplayEventSummary(new Rect(scene.xMax+14,scene.y,326,scene.height),frame);
            DrawReplayRoundStrip(new Rect(page.x,roundsY,page.width,42));
            var slider=new Rect(page.x,controlsY-23,page.width,18);
            RecordReviewTarget("replay-progress",slider);
            int seek=Mathf.RoundToInt(GUI.HorizontalSlider(slider,replayPlayer.Position,0,replayRecord.events.Count-1));
            if(seek!=replayPlayer.Position)SeekBattleEvent(seek);
            float w=Mathf.Min(155,(page.width-60)/7);
            if(ReplayButton("replay-play",new Rect(page.x,controlsY,w,40),replayPlaying?"暂停":"播放",true))
            {if(replayPlayer.Position>=replayRecord.events.Count-1)SeekBattleEvent(0);replayPlaying=!replayPlaying;}
            if(ReplayButton("replay-speed",new Rect(page.x+(w+10),controlsY,w,40),"速度 ×"+replaySpeed))replaySpeed=replaySpeed==1?2:1;
            if(ReplayButton("replay-start",new Rect(page.x+2*(w+10),controlsY,w,40),"从头"))SeekBattleEvent(0);
            if(ReplayButton("replay-prev",new Rect(page.x+3*(w+10),controlsY,w,40),"上一回合"))SeekBattleRound(-1);
            if(ReplayButton("replay-next",new Rect(page.x+4*(w+10),controlsY,w,40),"下一回合"))SeekBattleRound(1);
            if(ReplayButton("replay-step",new Rect(page.x+5*(w+10),controlsY,w,40),"下一事件"))SeekBattleEvent(replayPlayer.Position+1);
            if(ReplayButton("replay-end",new Rect(page.x+6*(w+10),controlsY,w,40),"查看结果"))SeekBattleEvent(replayRecord.events.Count-1);
        }

        private bool ReplayButton(string id,Rect rect,string text,bool highlighted=false)
        {RecordReviewTarget(id,rect);return Button(rect,text,highlighted);}
    }
}
