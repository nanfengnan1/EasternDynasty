using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private int cityQuestChapter;
        private bool cityQuestCompleted;
        private Vector2 cityQuestScroll;
        private bool cityQuestFocusPending;
        private static readonly string[] CityChapterNames={"立足幽州","整军开疆","安定郡县"};
        private static readonly string[] CityChapterGoals={"查看城池，发展农事与府署","配置部队，补兵并开拓资源地","屯田补给，扩建并巩固领地"};
        private static int CityQuestChapter(int id) { return CityQuestClaimReceipt.ChapterIndex(id); }

        private void DrawCityQuestPage(Rect area)
        {
            float chapterWidth=Mathf.Clamp(area.width*.25f,260,340);
            var chapters=new Rect(area.x,area.y,chapterWidth,area.height);
            var list=new Rect(chapters.xMax+18,area.y,area.width-chapterWidth-18,area.height);
            Frame(chapters,Ink);Frame(list,Panel);
            Label(chapters.x+20,chapters.y+15,chapters.width-40,34,"幽州纪事",heading);
            Label(chapters.x+20,chapters.y+55,chapters.width-40,26,"已领取 "+world.cityClaimedQuests.Count+" / "+CityCatalog.Quests.Length,small);
            float chapterHeight=Mathf.Min(102,(area.height-198)/3);
            for(int i=0;i<3;i++)
            {
                int index=i;
                var row=new Rect(chapters.x+16,chapters.y+93+i*(chapterHeight+10),chapters.width-32,chapterHeight);
                var quests=CityCatalog.Quests.Where(q=>CityQuestChapter(q.id)==index).ToArray();
                int completed=quests.Count(q=>world.cityClaimedQuests.Contains(q.id));
                bool selected=!cityQuestCompleted&&cityQuestChapter==i;
                Frame(row,selected?C("35483E"):Panel);
                RectFill(new Rect(row.x,row.y,4,row.height),completed==quests.Length?Green:Gold);
                Label(row.x+14,row.y+9,row.width-28,25,"第 "+(i+1)+" 章  ·  "+CityChapterNames[i],body);
                Label(row.x+14,row.y+39,row.width-28,22,completed+" / "+quests.Length+" 项已领取",small);
                RectFill(new Rect(row.x+14,row.yMax-13,row.width-28,4),Border);
                RectFill(new Rect(row.x+14,row.yMax-13,(row.width-28)*completed/quests.Length,4),Green);
                RecordReviewTarget("quest-chapter-"+i,row);
                if(GUI.Button(row,GUIContent.none,GUIStyle.none)){cityQuestChapter=i;cityQuestCompleted=false;cityQuestScroll=Vector2.zero;cityQuestFocusPending=false;}
            }
            var training=new Rect(chapters.x+16,chapters.yMax-65,chapters.width-32,40);
            RecordReviewTarget("quest-training-go",training);
            if(Button(training,"前往练兵 · 部队成长"))OpenTraining();
            Label(chapters.x+16,chapters.yMax-23,chapters.width-32,18,"项目任务配置 · 保留原领取进度",tiny);

            var activeTab=new Rect(list.x+18,list.y+14,132,36);RecordReviewTarget("quest-active",activeTab);
            if(Button(activeTab,"主要事宜",!cityQuestCompleted)){cityQuestCompleted=false;cityQuestChapter=CityQuestChapter(world.CurrentCityQuest==null?10:world.CurrentCityQuest.id);cityQuestScroll=Vector2.zero;cityQuestFocusPending=true;}
            var completedTab=new Rect(list.x+160,list.y+14,132,36);RecordReviewTarget("quest-completed",completedTab);
            if(Button(completedTab,"已完成",cityQuestCompleted)){cityQuestCompleted=true;cityQuestScroll=Vector2.zero;cityQuestFocusPending=false;}
            var current=world.CurrentCityQuest;
            var focusCurrent=new Rect(list.xMax-152,list.y+14,134,36);
            if(current!=null)
            {
                RecordReviewTarget("quest-focus-current",focusCurrent);
                if(Button(focusCurrent,"定位当前目标",true))
                {cityQuestCompleted=false;cityQuestChapter=CityQuestChapter(current.id);cityQuestFocusPending=true;}
            }
            Label(list.x+18,list.y+61,list.width-36,25,current==null?"幽州纪事已完成 · 奖励记录可随时查看":
                "当前目标  "+current.id.ToString("00")+" · "+current.title+"   "+Math.Min(world.CityQuestTarget(current.id),world.CityQuestProgress(current.id))+" / "+world.CityQuestTarget(current.id),small);
            var visible=CityCatalog.Quests.Where(q=>cityQuestCompleted?world.cityClaimedQuests.Contains(q.id):CityQuestChapter(q.id)==cityQuestChapter).ToArray();
            var view=new Rect(list.x+14,list.y+98,list.width-28,list.height-112);
            const float rowHeight=144, rowGap=12;
            if(cityQuestFocusPending&&current!=null&&!cityQuestCompleted)
            {
                int index=Array.FindIndex(visible,q=>q.id==current.id);
                if(index>=0)cityQuestScroll.y=Mathf.Clamp(index*(rowHeight+rowGap),0,Mathf.Max(0,visible.Length*(rowHeight+rowGap)-view.height));
                cityQuestFocusPending=false;
            }
            cityQuestScroll=BeginContentScroll(view,cityQuestScroll,new Rect(0,0,view.width-20,Mathf.Max(view.height,visible.Length*(rowHeight+rowGap))));
            if(visible.Length==0)Label(18,24,view.width-56,65,"还没有已领取的任务。\n完成当前目标后，在主要事宜领取奖励。",body);
            for(int i=0;i<visible.Length;i++)
            {
                var quest=visible[i];
                var row=new Rect(0,i*(rowHeight+rowGap),view.width-24,rowHeight);
                var absolute=new Rect(view.x,row.y+view.y-cityQuestScroll.y,row.width,row.height);
                bool fullyVisible=absolute.y>=view.y&&absolute.yMax<=view.yMax;
                bool claimed=world.cityClaimedQuests.Contains(quest.id),isCurrent=current!=null&&current.id==quest.id;
                bool ready=isCurrent&&world.CityQuestReady(quest.id);
                Frame(row,claimed?C("20372F"):isCurrent?C("293D50"):Ink);
                RectFill(new Rect(row.x,row.y,4,row.height),claimed?Green:isCurrent?Gold:Border);
                var glyph=new Rect(row.x+12,row.y+12,88,70);
                var definition=CityCatalog.Get(quest.building);
                if(definition!=null)DrawCityFacilityArt(glyph,definition.kind);
                else DrawCityBuildingGlyph(new Rect(glyph.x+18,glyph.y+8,52,49),claimed?Green:Gold,quest.action=="formation"?"将":quest.action=="farm"?"田":"地");
                float textX=row.x+112,actionX=row.xMax-151;
                Label(textX,row.y+12,actionX-textX-12,27,quest.id.ToString("00")+"  "+quest.title,body);
                Label(textX,row.y+43,actionX-textX-12,43,quest.objective,small);
                int goal=world.CityQuestTarget(quest.id),progress=Math.Min(goal,world.CityQuestProgress(quest.id));
                Label(actionX,row.y+12,134,24,claimed?"已领取":isCurrent?(ready?"可领取":"进行中 "+progress+"/"+goal) :"待解锁",new GUIStyle(small){alignment=TextAnchor.MiddleCenter,normal={textColor=claimed?Green:ready?Gold:Muted}});
                var action=new Rect(actionX,row.y+43,134,38);
                if(fullyVisible)RecordReviewTarget("quest-row-"+quest.id,absolute);
                if(fullyVisible&&isCurrent)RecordReviewTarget(ready?"quest-claim":"quest-go",new Rect(view.x+action.x,view.y+action.y-cityQuestScroll.y,action.width,action.height));
                if(Button(action,claimed?"奖励已入库":isCurrent?(ready?"领取奖励":"前往完成"):"先完成前项",ready,isCurrent))
                {
                    if(ready)
                    {
                        ClaimCityQuestReward(quest.id);
                    }
                    else GuideCityQuest(quest);
                }
                var detailButton=new Rect(actionX,row.y+94,134,36);
                if(fullyVisible)RecordReviewTarget("quest-detail-"+quest.id,new Rect(view.x+detailButton.x,view.y+detailButton.y-cityQuestScroll.y,detailButton.width,detailButton.height));
                if(Button(detailButton,"查看目标"))OpenCityQuestObjective(quest.id);
                var rewardArea=new Rect(row.x+16,row.y+94,row.width-184,36);
                DrawQuestRewards(rewardArea,quest.reward);
            }
            GUI.EndScrollView(GUI.enabled);
        }

        private void DrawQuestRewards(Rect area,Supplies reward)
        {
            double[] amounts={reward.grain,reward.grass,reward.stone,reward.iron,reward.gold};int[] icons={1,0,2,3,4};
            float cell=area.width/5;
            for(int i=0;i<5;i++)
            {
                float x=area.x+i*cell;
                ResourceIcon(new Rect(x,area.y+6,20,20),icons[i]);
                Label(x+23,area.y+7,cell-25,23,amounts[i].ToString("0"),tiny);
            }
        }
    }
}
