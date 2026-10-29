using System;
using System.Linq;
using UnityEngine;
namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private string s1Faction="全部",s1Search="",s1Selected="";
        private bool s1Archives=true;
        private Vector2 s1Scroll,s1DetailsScroll;
        private void FactionButtons(Rect r)
        {
            float w=r.width/Catalog.S1Factions.Length;
            for(int i=0;i<Catalog.S1Factions.Length;i++)
            {
                Rect item=new Rect(r.x+i*w,r.y,w-6,34);
                string faction=Catalog.S1Factions[i];
                RecordReviewTarget("faction-"+faction,item);
                if(faction=="魏")RecordReviewTarget("archive-wei",item);
                if(Button(item,faction,s1Faction==faction)){s1Faction=faction;s1Selected="";s1Scroll=Vector2.zero;s1DetailsScroll=Vector2.zero;poolScroll=Vector2.zero;}
                if(faction!="全部")RectFill(new Rect(item.x+8,item.y+12,4,10),FactionColor(faction));
            }
        }
        private static Color FactionColor(string faction)
        {return faction=="魏"?C("4B8BC2"):faction=="蜀"?C("4CA783"):faction=="吴"?C("C56B55"):faction=="群"?C("B59A5B"):faction=="晋"?C("9A78BE"):C("A95269");}
        private void S1ArmyLabel()
        {
            if(!world.IsYouzhou||world.order==null)return;
            double x,z;world.ArmyPosition(out x,out z);
            var p=map.viewCamera.WorldToScreenPoint(new Vector3((float)x,.65f,(float)z));
            Vector2 logical=ScreenPointToGui(p);var r=new Rect(logical.x-78,logical.y-16,156,43);
            if(p.z<0||r.y<80||r.yMax>height-116||hudBlocks.Any(b=>b.Overlaps(GuiRectToPixels(r))))return;
            Frame(r,Ink);Label(r.x+8,r.y+4,140,21,world.ArmyState,tiny);
            Label(r.x+8,r.y+24,140,17,world.troops+" 兵"+(world.order.phase==1?" · 持续驻守":" · "+Math.Ceiling(world.order.remaining)+"秒"),tiny);
            Block(r);
        }
        private void S1LandActions(Rect r,Land tile)
        {
            float x=r.x+22,y=r.yMax-219;
            var trainingEntry=new Rect(x,y+157,264,38);RecordReviewTarget("land-training",trainingEntry);
            if(Button(trainingEntry,"部队练兵  ›"))OpenTraining(tile.id);
            if(world.order!=null)
            {Label(x,y,264,56,world.ArmyState+"\n"+(world.order.phase==1?"驻守保持至主动撤回":"剩余 "+Math.Ceiling(world.order.remaining)+" 秒"),small);RecordReviewTarget("army-recall",new Rect(x,y+63,264,42));if(Button(new Rect(x,y+63,264,42),"撤回部队",false,!world.order.returning))Act(world.RecallArmy());return;}
            string guard=world.TerritoryOrderProblem(tile.id,false),farm=world.TerritoryOrderProblem(tile.id,true);
            RecordReviewTarget("army-garrison",new Rect(x,y,128,40));
            RecordReviewTarget("army-farm",new Rect(x+136,y,128,40));
            if(Button(new Rect(x,y,128,40),"驻守",false,guard.Length==0))Act(world.Garrison(tile.id));
            if(Button(new Rect(x+136,y,128,40),"屯田 · 2令",true,farm.Length==0))Act(world.Farm(tile.id));
            var gain=world.FarmYield(tile.id);
            Label(x,y+51,264,70,"政令 "+world.AvailableCommands+" / 20\n预计：粮 "+gain.grain+"  木 "+gain.grass+"\n石 "+gain.stone+"  铁 "+gain.iron,small);
            Label(x,y+126,264,29,farm.Length>0?farm:"作业 90 秒 · 完成后返城",tiny);
        }
    }
}
