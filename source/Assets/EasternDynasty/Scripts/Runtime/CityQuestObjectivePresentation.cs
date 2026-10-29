using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private int cityQuestDetailId;
        private Vector2 cityQuestObjectiveScroll;
        private World cityQuestLandWorld;
        private int cityQuestLandId=-1;
        private bool CityQuestLandReturnAvailable { get { return tab=="领地"&&cityQuestLandWorld==world&&cityQuestLandId>=0&&cityQuestLandId==map.selected; } }
        private void ClearCityQuestLandOrigin() { cityQuestLandWorld=null;cityQuestLandId=-1; }
        private void ReturnFromCityQuestLand()
        {
            if(!CityQuestLandReturnAvailable)return;
            ClearCityQuestLandOrigin();tab="主城";cityPage="quest-detail";map.CancelPointerInput();
        }
        private void DrawCityQuestLandReturn()
        {
            if(!CityQuestLandReturnAvailable){ClearCityQuestLandOrigin();return;}
            var back=new Rect(20,278,268,40);Block(back);RecordReviewTarget("quest-land-return",back);
            if(Button(back,"返回任务目标"))ReturnFromCityQuestLand();
        }

        private void OpenCityQuestObjective(int id)
        {
            if(CityQuestObjectivePlan.Create(world,id)==null)return;
            ClearCityQuestLandOrigin();
            cityQuestDetailId=id;cityQuestObjectiveScroll=Vector2.zero;
            tab="主城";cityPage="quest-detail";map.CancelPointerInput();
        }

        private void ReturnFromCityQuestObjective()
        { cityPage="quests";map.CancelPointerInput(); }

        private void ExecuteCityQuestObjective()
        {
            if(cityQuestReceipt!=null)return;
            // Re-read live state on click: a construction may have completed since the last draw.
            var plan=CityQuestObjectivePlan.Create(world,cityQuestDetailId);
            if(plan==null)return;
            if(plan.Claimed){ReturnFromCityQuestObjective();return;}
            if(!plan.Current){if(plan.CurrentQuestId>0)OpenCityQuestObjective(plan.CurrentQuestId);return;}
            if(plan.Ready){ClaimCityQuestReward(plan.QuestId);return;}
            if(plan.Route==null){ReturnFromCityQuestObjective();return;}
            GuideCityQuest(CityCatalog.Quests.First(q=>q.id==plan.QuestId));
        }

        private void DrawCityQuestObjective(Rect area)
        {
            var plan=CityQuestObjectivePlan.Create(world,cityQuestDetailId);
            if(plan==null){ReturnFromCityQuestObjective();return;}
            float leftWidth=Mathf.Clamp(area.width*.37f,340,480);
            var summary=new Rect(area.x,area.y,leftWidth,area.height);
            var action=new Rect(summary.xMax+18,area.y,area.width-leftWidth-18,area.height);
            Frame(summary,Ink);Frame(action,Panel);
            Label(summary.x+22,summary.y+16,summary.width-44,26,"第 "+(plan.ChapterIndex+1)+" 章 · "+CityChapterNames[plan.ChapterIndex],small);
            var emblem=new Rect(summary.center.x-65,summary.y+64,130,105);
            var quest=CityCatalog.Quests.First(q=>q.id==plan.QuestId);
            if(CityCatalog.Get(quest.building)!=null)DrawCityFacilityArt(emblem,quest.building);
            else DrawCityBuildingGlyph(emblem,Gold,quest.action=="formation"?"将":quest.action=="farm"?"田":"地");
            Label(summary.x+22,summary.y+184,summary.width-44,36,plan.QuestId.ToString("00")+" · "+plan.Title,heading);
            Label(summary.x+22,summary.y+231,summary.width-44,48,plan.Objective,body);
            Color stateColor=plan.Claimed?Green:plan.Ready?Gold:Muted;
            Label(summary.x+22,summary.y+292,summary.width-44,26,plan.StatusLabel+"   "+plan.Progress+" / "+plan.Target,new GUIStyle(body){normal={textColor=stateColor}});
            var progress=new Rect(summary.x+22,summary.y+333,summary.width-44,7);
            RectFill(progress,Border);RectFill(new Rect(progress.x,progress.y,progress.width*Mathf.Clamp01((float)plan.Progress/Math.Max(1,plan.Target)),progress.height),plan.Claimed?Green:Gold);
            Label(summary.x+22,summary.yMax-105,summary.width-44,25,plan.Claimed?"任务奖励 · 已领取":"任务奖励 · 完成后领取",small);
            DrawCityResources(new Rect(summary.x+18,summary.yMax-71,summary.width-36,58),plan.Reward);

            Label(action.x+22,action.y+18,action.width-44,34,plan.Claimed?"事宜已成":plan.Current?"下一步行动":"后续目标预览",heading);
            var view=new Rect(action.x+18,action.y+65,action.width-36,action.height-153);
            RecordReviewTarget("quest-detail-content",view);
            float contentWidth=view.width-20;
            float contentHeight=plan.Cost==null?310:402;
            cityQuestObjectiveScroll=BeginContentScroll(view,cityQuestObjectiveScroll,new Rect(0,0,contentWidth,Mathf.Max(view.height,contentHeight)));
            var requirement=new Rect(0,0,contentWidth,146);Frame(requirement,Ink);
            Label(18,12,contentWidth-36,28,plan.RequirementTitle,body);
            Label(18,51,contentWidth-36,78,plan.RequirementDetail,small);
            var direction=new Rect(0,158,contentWidth,112);Frame(direction,C("293D50"));
            Label(18,direction.y+12,contentWidth-36,25,plan.Current&&!plan.Ready?"实际操作位置":"领取顺序",small);
            Label(18,direction.y+45,contentWidth-36,55,plan.ActionHint,body);
            if(plan.Cost!=null)
            {
                Label(4,284,contentWidth-8,24,plan.CostLabel+" · "+plan.TimeLabel+(plan.EstimatedSeconds>0?" "+CityTime(plan.EstimatedSeconds):""),small);
                DrawCityQuestCosts(new Rect(0,317,contentWidth,58),plan.Cost);
            }
            GUI.EndScrollView(GUI.enabled);
            var execute=new Rect(action.x+18,action.yMax-70,action.width-36,44);
            RecordReviewTarget("quest-detail-action",execute);
            if(Button(execute,plan.Claimed?"返回任务列表":plan.ActionLabel,plan.Ready))ExecuteCityQuestObjective();
            Label(action.x+18,action.yMax-22,action.width-36,18,"项目任务配置 · 查看目标不消耗资源",tiny);
        }

        private void DrawCityQuestCosts(Rect area,Supplies supplies)
        {
            double[] amounts={supplies.grain,supplies.grass,supplies.stone,supplies.iron,supplies.gold};
            int[] icons={1,0,2,3,4};float cell=area.width/5;
            for(int i=0;i<5;i++)
            {
                var tile=new Rect(area.x+i*cell,area.y,cell-4,area.height);Frame(tile,Ink);
                ResourceIcon(new Rect(tile.center.x-12,tile.y+4,24,20),icons[i]);
                GUI.Label(new Rect(tile.x,tile.y+28,tile.width,24),amounts[i].ToString("0.##"),new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter,normal={textColor=Text}});
            }
        }
    }
}
