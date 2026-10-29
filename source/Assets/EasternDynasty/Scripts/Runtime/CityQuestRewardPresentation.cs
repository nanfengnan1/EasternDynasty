using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private CityQuestClaimReceipt cityQuestReceipt;

        private void ClaimCityQuestReward(int id)
        {
            if(cityQuestReceipt!=null)return;
            string message;
            var receipt=CityQuestClaimReceipt.Claim(world,id,out message);
            status=message;
            if(receipt==null)return;
            cityQuestReceipt=receipt;
            cityQuestFocusPending=true;
            map.CancelPointerInput();Save(false);
        }

        private void CloseCityQuestReward()
        { cityQuestReceipt=null;map.CancelPointerInput(); }

        private void ContinueCityQuestReward()
        {
            if(cityQuestReceipt==null)return;
            CloseCityQuestReward();OpenCityQuests();
            if(world.CurrentCityQuest==null){cityQuestCompleted=true;cityQuestScroll=Vector2.zero;cityQuestFocusPending=false;}
        }

        private void GoToNextCityQuest()
        {
            if(cityQuestReceipt==null)return;
            CloseCityQuestReward();
            var next=world.CurrentCityQuest;
            if(next==null)OpenCityOverview();
            else {OpenCityQuests();GuideCityQuest(next);}
        }

        private void DrawCityQuestReward()
        {
            var receipt=cityQuestReceipt;if(receipt==null)return;
            Block(new Rect(0,0,width,height));
            RectFill(new Rect(0,0,width,height),new Color(.015f,.025f,.04f,.88f));
            // Only the visible overlay is actionable in diagnostic click recordings.
            ClearReviewTargets();
            float panelWidth=Mathf.Min(940,width-64),panelHeight=Mathf.Min(574,height-52);
            var panel=new Rect((width-panelWidth)/2,(height-panelHeight)/2,panelWidth,panelHeight);
            Frame(panel,Ink);Outline(panel,Gold,2);
            RectFill(new Rect(panel.x+2,panel.y+2,panel.width-4,7),Gold);
            RecordReviewTarget("quest-reward-modal",panel);
            var close=new Rect(panel.xMax-60,panel.y+20,38,38);RecordReviewTarget("quest-reward-close",close);
            if(Button(close,"×")){CloseCityQuestReward();return;}

            var quest=CityCatalog.Quests.First(q=>q.id==receipt.questId);
            var emblem=new Rect(panel.x+26,panel.y+26,82,80);
            if(CityCatalog.Get(quest.building)!=null)DrawCityFacilityArt(emblem,quest.building);
            else DrawCityBuildingGlyph(emblem,Gold,"成");
            string headingText=receipt.allComplete?"幽州纪事 · 全部完成":receipt.chapterComplete?"第 "+(receipt.chapterIndex+1)+" 章 · 已完成":"事宜已成";
            Label(panel.x+128,panel.y+27,panel.width-212,37,headingText,heading);
            Label(panel.x+128,panel.y+73,panel.width-212,27,receipt.questId.ToString("00")+"  "+receipt.title,body);
            Label(panel.x+28,panel.y+124,panel.width-56,26,"奖励已到账",new GUIStyle(body){normal={textColor=Green}});

            double[] credited={receipt.credited.grain,receipt.credited.grass,receipt.credited.stone,receipt.credited.iron,receipt.credited.gold};
            double[] balance={receipt.balance.grain,receipt.balance.grass,receipt.balance.stone,receipt.balance.iron,receipt.balance.gold};
            string[] names={"粮食","木材","石料","铁矿","金币"};int[] icons={1,0,2,3,4};
            float cell=(panel.width-56-40)/5;
            for(int i=0;i<5;i++)
            {
                var tile=new Rect(panel.x+28+i*(cell+10),panel.y+160,cell,140);
                Frame(tile,Panel);ResourceIcon(new Rect(tile.center.x-17,tile.y+12,34,32),icons[i]);
                Label(tile.x+6,tile.y+49,tile.width-12,22,names[i],new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
                Label(tile.x+6,tile.y+76,tile.width-12,30,"+"+credited[i].ToString("N0"),new GUIStyle(heading){fontSize=23,alignment=TextAnchor.MiddleCenter});
                Label(tile.x+6,tile.y+112,tile.width-12,22,"到账后 "+Math.Floor(balance[i]).ToString("N0"),new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter});
            }
            var next=world.CurrentCityQuest;
            var nextArea=new Rect(panel.x+28,panel.y+322,panel.width-56,112);Frame(nextArea,Panel);
            Label(nextArea.x+16,nextArea.y+12,nextArea.width-32,23,receipt.chapterComplete?CityChapterNames[receipt.chapterIndex]+" · 本章奖励已领齐":"第 "+(receipt.chapterIndex+1)+" 章 · "+CityChapterNames[receipt.chapterIndex],small);
            Label(nextArea.x+16,nextArea.y+43,nextArea.width-32,27,next==null?"十项主要事宜已完成，可继续建设与开拓。":"下一目标  "+next.id.ToString("00")+" · "+next.title,body);
            Label(nextArea.x+16,nextArea.y+77,nextArea.width-32,23,next==null?"已完成列表保留全部领取记录。":next.objective,tiny);

            float buttonWidth=(panel.width-68)/2;
            var done=new Rect(panel.x+28,panel.yMax-76,buttonWidth,46);
            var go=new Rect(done.xMax+12,done.y,buttonWidth,46);
            RecordReviewTarget("quest-reward-done",done);RecordReviewTarget("quest-reward-next",go);
            if(Button(done,next==null?"查看已完成":"继续查看任务"))ContinueCityQuestReward();
            if(Button(go,next==null?"返回城内全景":"前往下一目标",true))GoToNextCityQuest();
            Label(panel.x+28,panel.yMax-25,panel.width-56,18,"项目任务奖励 · 本次到账收据",new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter});
        }
    }
}
