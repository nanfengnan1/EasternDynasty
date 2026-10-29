using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D replayFieldTexture;
        private const float ReplayEventRowHeight=88;
        private float replayEventScrollMaximum;

        private void DrawReplayArmyHeader(Rect area, BattleArmySnapshot army, int after, int before, Color accent)
        {
            Frame(area, Panel); RectFill(new Rect(area.x, area.y, 4, area.height), accent);
            Label(area.x+16,area.y+10,area.width*.45f,28,army.name,body);
            Label(area.x+16,area.y+40,area.width*.5f,35,after+" / "+army.troops+" 兵",heading);
            Label(area.x+area.width*.52f,area.y+16,area.width*.46f-14,45,
                before==after?"兵力未变化":"本次 "+before+" → "+after,body);
            var bar=new Rect(area.x+16,area.yMax-14,area.width-32,7); RectFill(bar,Ink);
            float oldRatio=(float)before/army.troops,newRatio=(float)after/army.troops;
            RectFill(new Rect(bar.x,bar.y,bar.width*Mathf.Clamp01(newRatio),bar.height),accent);
            float left=Mathf.Min(oldRatio,newRatio),difference=Mathf.Abs(oldRatio-newRatio);
            RectFill(new Rect(bar.x+bar.width*left,bar.y,bar.width*difference,bar.height),after>before?Gold:Red);
        }

        private void DrawReplayBattlefield(Rect area, BattleReplayEvent current)
        {
            if(replayFieldTexture==null)
            {
                // Original procedural terrain, with no historical screenshot baked into the controls.
                var raster=new CitySceneRaster(1000,420); raster.Ground();
                raster.Polygon(C("53634B"),new Vector2(0,0),new Vector2(1000,0),new Vector2(1000,70),new Vector2(810,36),new Vector2(620,64),new Vector2(400,24),new Vector2(180,55),new Vector2(0,28));
                raster.Polygon(C("706C51"),new Vector2(430,0),new Vector2(500,0),new Vector2(630,420),new Vector2(350,420));
                for(int i=0;i<7;i++)raster.Line(new Vector2(30,75+i*48),new Vector2(970,75+i*48),C("66705A"),1);
                replayFieldTexture=raster.Texture(); replayFieldTexture.name="RecordedBattleSchematic";
            }
            Frame(area,Ink);
            var field=new Rect(area.x+1,area.y+1,area.width-2,area.height-112);
            GUI.DrawTexture(field,replayFieldTexture,ScaleMode.StretchToFill);
            Label(field.x+14,field.y+10,field.width-28,26,"整队对阵 · 兵阵标记示意剩余比例",small);
            float centerY=field.y+field.height*.61f;
            DrawReplayFormation(new Vector2(field.x+field.width*.24f,centerY),field.width*.31f,current.attackerTroops,replayRecord.attacker.troops,Green,false);
            DrawReplayFormation(new Vector2(field.x+field.width*.76f,centerY),field.width*.31f,current.defenderTroops,replayRecord.defender.troops,Red,true);
            bool attack=current.kind==BattleReplayEventKind.Attack,loss=current.kind==BattleReplayEventKind.Loss;
            if(attack||loss)
            {
                float x=field.center.x, y=centerY-10;
                float direction=attack?1:-1;
                Color color=attack?Gold:Red;
                ReplayLine(new Vector2(x-direction*44,y),new Vector2(x+direction*44,y),color,4);
                ReplayLine(new Vector2(x+direction*44,y),new Vector2(x+direction*26,y-12),color,4);
                ReplayLine(new Vector2(x+direction*44,y),new Vector2(x+direction*26,y+12),color,4);
                var impact=new Rect((attack?field.x+field.width*.76f:field.x+field.width*.24f)-74,field.y+40,148,32);
                Frame(impact,Red);GUI.Label(impact,"损失 −"+current.amount,centered);
            }
            else if(current.kind==BattleReplayEventKind.Recovery)
            {
                var impact=new Rect(field.x+field.width*.24f-74,field.y+40,148,32);
                Frame(impact,Green);GUI.Label(impact,"恢复 +"+current.amount,centered);
            }
            var strip=new Rect(area.x+10,area.yMax-102,area.width-20,92);
            float cell=strip.width/4;
            for(int i=0;i<replayRecord.attacker.generals.Count;i++)
            {
                var g=replayRecord.attacker.generals[i];float x=strip.x+i*cell;
                var art=new Rect(x,strip.y,56,78);Portrait(art,g.definitionId);Outline(art,Green);
                Label(x+64,strip.y+3,cell-72,26,g.name,body);
                Label(x+64,strip.y+32,cell-72,24,Slots[Mathf.Min(i,2)]+" · Lv."+g.level,small);
                Label(x+64,strip.y+57,cell-72,24,"查看战前阵容",tiny);
                var card=new Rect(x,strip.y,cell-8,86);
                RecordReviewTarget("replay-participant-"+i,card);
                if(ContentTileClicked(card,"replay-participant-"+i,g.uid))OpenBattleFormation(i);
            }
            Label(strip.x+cell*3+8,strip.y+9,cell-16,72,"对方：地块整队守军\n无独立武将记录\n未分配逐将伤害",small);
        }

        private void DrawReplayFormation(Vector2 center,float span,int troops,int initial,Color accent,bool reverse)
        {
            const int columns=5,rows=4;
            int lit=Mathf.CeilToInt(Mathf.Clamp01((float)troops/initial)*columns*rows);
            float spacing=Mathf.Min(30,span/columns);
            for(int row=0;row<rows;row++)for(int col=0;col<columns;col++)
            {
                float x=center.x+(col-2)*spacing+(reverse?-1:1)*row*5,y=center.y+(row-1.5f)*19;
                bool remaining=row*columns+col<lit;Color color=remaining?accent:C("424C43");
                RectFill(new Rect(x-7,y+9,19,4),C("333D34"));
                RectFill(new Rect(x-3,y-10,7,7),remaining?Paper:color);
                RectFill(new Rect(x-5,y-2,11,12),color);
                RectFill(new Rect(x+9,y-19,2,30),remaining?Gold:color);
                RectFill(new Rect(x-4,y+10,3,6),color);RectFill(new Rect(x+3,y+10,3,6),color);
            }
        }

        private void ReplayLine(Vector2 start,Vector2 end,Color color,float thickness)
        {
            var previous=GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg,start);
            RectFill(new Rect(start.x,start.y-thickness/2,(end-start).magnitude,thickness),color);
            GUI.matrix=previous;
        }

        private string ReplayEventName(BattleReplayEventKind kind)
        {
            switch(kind)
            {
                case BattleReplayEventKind.Attack:return "我方整队进攻";
                case BattleReplayEventKind.Loss:return "我方整队受损";
                case BattleReplayEventKind.Recovery:return "我方兵力恢复";
                case BattleReplayEventKind.Result:return "本场结算";
                default:return "战前准备";
            }
        }

        private void DrawReplayEventSummary(Rect area,BattleReplayFrame frame)
        {
            Frame(area,Panel);
            Label(area.x+16,area.y+12,area.width-32,28,frame.isResult?"战斗结束":frame.round==0?"战前准备":"第 "+frame.round+" 回合",heading);
            // Fixed, non-overlapping rows; detailed participant rewards live in the full outcome view.
            if(frame.isResult)
            {
                Label(area.x+16,area.y+48,area.width-32,26,frame.captured?"胜利 · 已占领":"未占领 · 部队撤回",body);
                Label(area.x+16,area.y+84,area.width-32,26,"造成损失  "+frame.damageTotal,body);
                Label(area.x+16,area.y+116,area.width-32,26,"我方受损  "+frame.lossTotal+" · 恢复 "+frame.recoveryTotal,body);
                Label(area.x+16,area.y+148,area.width-32,26,"我方净减员  "+(frame.lossTotal-frame.recoveryTotal),body);
                Label(area.x+16,area.y+184,area.width-32,44,"已到账  金币 +"+frame.goldReward+"\n实得经验 +"+frame.experienceReward,small);
                if(ReplayButton("replay-outcome-detail",new Rect(area.x+16,area.yMax-48,area.width-32,38),"查看战果与逐将经验",true))OpenBattleOutcome();
            }
            else
            {
                Label(area.x+16,area.y+48,area.width-32,26,ReplayEventName(frame.kind),body);
                Label(area.x+16,area.y+82,area.width-32,30,frame.amount>0?"本次 "+(frame.kind==BattleReplayEventKind.Recovery?"恢复 +":"损失 −")+frame.amount:"事件 "+(replayPlayer.Position+1)+" / "+replayRecord.events.Count,heading);
                Label(area.x+16,area.y+124,area.width-32,25,"累计造成损失  "+frame.damageTotal,body);
                Label(area.x+16,area.y+156,area.width-32,25,"累计受损  "+frame.lossTotal,body);
                Label(area.x+16,area.y+188,area.width-32,25,"累计恢复  "+frame.recoveryTotal,body);
                Label(area.x+16,area.yMax-32,area.width-32,24,"从记录播放 · 不再次扣兵或发奖",tiny);
            }
        }

        private void DrawReplayEventList(Rect area)
        {
            Frame(area,Ink);
            Label(area.x+12,area.y+10,area.width-24,28,"顺序事件 · 选择定位可回看当次变化",body);
            var list=new Rect(area.x+10,area.y+46,area.width-20,area.height-56);
            RecordReviewTarget("replay-event-list",list);
            float contentHeight=replayRecord.events.Count*ReplayEventRowHeight;
            replayEventScrollMaximum=Mathf.Max(0,contentHeight-list.height);
            replayEventScroll=BeginContentScroll(list,replayEventScroll,new Rect(0,0,list.width-24,contentHeight));
            int selected=-1;
            for(int i=0;i<replayRecord.events.Count;i++)
            {
                var e=replayRecord.events[i];var row=new Rect(0,i*ReplayEventRowHeight,list.width-24,ReplayEventRowHeight-4);
                RectFill(row,i==replayPlayer.Position?Panel:Ink);
                RectFill(new Rect(row.x,row.y,3,row.height),e.kind==BattleReplayEventKind.Attack?Gold:e.kind==BattleReplayEventKind.Recovery?Green:Red);
                Label(12,row.y+10,80,36,e.round==0?"战前":"回合 "+e.round,small);
                string eventText=e.kind==BattleReplayEventKind.Result&&i>replayPlayer.Position?"结算事件 · 定位后查看结果与实得奖励":e.text;
                Label(94,row.y+6,row.width-190,40,eventText,body);
                int attackerBefore=i==0?replayRecord.attacker.troops:replayRecord.events[i-1].attackerTroops;
                int defenderBefore=i==0?replayRecord.defender.troops:replayRecord.events[i-1].defenderTroops;
                Label(94,row.y+51,row.width-190,26,"我方 "+attackerBefore+" → "+e.attackerTroops+"     守军 "+defenderBefore+" → "+e.defenderTroops,small);
                var go=new Rect(row.xMax-84,row.y+22,78,40);
                bool visible=row.y>=replayEventScroll.y&&row.yMax<=replayEventScroll.y+list.height;
                if(visible)RecordReviewTarget("replay-event-"+i,new Rect(list.x+go.x,list.y+go.y-replayEventScroll.y,go.width,go.height));
                if(Button(go,"定位",i==replayPlayer.Position,visible))selected=i;
            }
            GUI.EndScrollView(GUI.enabled);
            if(selected>=0)SeekBattleEvent(selected);
        }

        private void DrawReplayRoundStrip(Rect area)
        {
            int maxRound=replayRecord.events.Max(e=>e.round),count=maxRound+2;
            float cell=(area.width-8*(count-1))/count;
            for(int round=0;round<=maxRound+1;round++)
            {
                int target=round==maxRound+1?replayRecord.events.Count-1:round==0?0:replayRecord.events.FindIndex(e=>e.round==round);
                bool selected=round==maxRound+1?replayPlayer.Current.kind==BattleReplayEventKind.Result:
                    replayPlayer.Current.kind!=BattleReplayEventKind.Result&&replayPlayer.Current.round==round;
                var rect=new Rect(area.x+round*(cell+8),area.y,cell,area.height);
                if(ReplayButton("replay-round-"+round,rect,round==maxRound+1?"结算":round==0?"战前":"回合 "+round,selected))SeekBattleEvent(target);
            }
        }
    }
}
