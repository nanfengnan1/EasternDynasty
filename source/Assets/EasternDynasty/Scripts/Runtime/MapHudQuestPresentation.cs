using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D hudQuestBook,hudQuestSleeve,hudQuestSleeveHover,hudQuestStrip,hudQuestAction,hudQuestActionReady;
        private GUIStyle hudQuestTitle,hudQuestText,hudQuestSecondary,hudQuestCaption;
        private bool hudQuestTextFits,hudQuestHovered;

        private void DrawMapQuestRibbon()
        {
            InitMapHudStyles();DrawCityQuestLandReturn();
            if(hudQuestBook==null)
            {
                hudQuestBook=GameAssets.LoadTexture("ui.youzhou.nav_book");
                hudQuestSleeve=GameAssets.LoadTexture("ui.youzhou.quest_sleeve");
                hudQuestSleeveHover=GameAssets.LoadTexture("ui.youzhou.quest_sleeve_hover");
                hudQuestStrip=GameAssets.LoadTexture("ui.youzhou.quest_strip");
                hudQuestAction=GameAssets.LoadTexture("ui.youzhou.quest_action");
                hudQuestActionReady=GameAssets.LoadTexture("ui.youzhou.quest_action_ready");
                hudQuestText=new GUIStyle(body){fontSize=14,alignment=TextAnchor.MiddleLeft,wordWrap=false,clipping=TextClipping.Clip};
                hudQuestText.normal.textColor=C("E8E2CD");
                hudQuestTitle=new GUIStyle(hudQuestText){fontSize=19};
                hudQuestSecondary=new GUIStyle(hudQuestText){fontSize=13};
                hudQuestSecondary.normal.textColor=C("C1BDA9");
                hudQuestCaption=new GUIStyle(hudQuestText){fontSize=16,alignment=TextAnchor.MiddleCenter};
                hudQuestCaption.normal.textColor=C("EEE7D4");
            }
            hudQuestTextFits=true;
            Rect area=HudDesignRect(MapHudQuestLayout.Panel),art=HudDesignRect(MapHudQuestLayout.Banner);
            Rect open=HudDesignRect(MapHudQuestLayout.Open),titleRect=HudDesignRect(MapHudQuestLayout.Title);
            Rect objective=HudDesignRect(MapHudQuestLayout.Objective),ledger=HudDesignRect(MapHudQuestLayout.Progress);
            Rect action=HudDesignRect(MapHudQuestLayout.Action);
            Block(area);
            var quest=world.CurrentCityQuest;
            bool ready=quest!=null&&world.CityQuestReady(quest.id);
            int chapter=quest==null?2:CityQuestChapter(quest.id);
            int goal=quest==null?CityCatalog.Quests.Length:world.CityQuestTarget(quest.id);
            int progress=quest==null?world.cityClaimedQuests.Count:Math.Min(goal,world.CityQuestProgress(quest.id));
            Rect sleeve=HudDesignRect(MapHudQuestLayout.Body);
            bool chapterHover=GUI.enabled&&open.Contains(Event.current.mousePosition);
            hudQuestHovered=chapterHover;
            GUI.DrawTexture(sleeve,chapterHover?hudQuestSleeveHover:hudQuestSleeve,ScaleMode.StretchToFill,true);
            GUI.DrawTexture(art,hudQuestBook,ScaleMode.ScaleToFit,true);
            GUI.DrawTexture(ledger,hudQuestStrip,ScaleMode.StretchToFill,true);
            GUI.DrawTexture(action,ready?hudQuestActionReady:hudQuestAction,ScaleMode.StretchToFill,true);
            DrawMapQuestText(HudDesignRect(MapHudQuestLayout.Chapter),"第 "+(chapter+1)+" 章 · "+CityChapterNames[chapter],hudQuestSecondary,12);
            DrawMapQuestText(titleRect,quest==null?"幽州初定":quest.title,hudQuestTitle,17);
            DrawMapQuestText(objective,quest==null?"全部事宜已完成，查看章节记录。":quest.objective,hudQuestText,13);
            DrawMapQuestText(new Rect(ledger.x+10,ledger.y+8,ledger.width-20,23),
                (quest==null?"已领取 ":ready?"目标达成 ":"目标进度 ")+progress+" / "+goal,
                new GUIStyle(hudQuestText){fontSize=13},12);
            // Dynamic progress geometry represents the actual objective, not decorative artwork.
            RectFill(new Rect(ledger.x+10,ledger.y+37,ledger.width-20,3),C("595C50"));
            RectFill(new Rect(ledger.x+10,ledger.y+37,(ledger.width-20)*Mathf.Clamp01((float)progress/Math.Max(1,goal)),3),C("D1BD80"));
            DrawMapQuestText(new Rect(action.x+6,action.y+8,action.width-12,28),
                quest==null?"章节记录 ›":ready?"领取奖励":"前往 ›",hudQuestCaption,14);
            RecordMapHudTarget("hud-quest-panel",area);
            RecordMapHudTarget("hud-quest-art",art);
            RecordMapHudTarget("hud-quest-body",sleeve);
            RecordMapHudTarget("hud-quest-chapter",HudDesignRect(MapHudQuestLayout.Chapter));
            RecordMapHudTarget("quest-open",open);
            RecordMapHudTarget("hud-quest-title",titleRect);
            RecordMapHudTarget("hud-quest-objective",objective);
            RecordMapHudTarget("hud-quest-progress",ledger);
            RecordMapHudTarget(quest==null?"hud-quest-history":ready?"quest-claim":"quest-go",action);
            // One parent command owns book, title and objective; avoid overlapping
            // gesture owners and retain the shared drag/touch cancellation.
            bool chapterClicked=ContentTileClicked(open,"map-quest:"+(quest==null?0:quest.id),-1);
            if(chapterClicked){OpenMapQuestChapter();return;}
            if(ContentTileClicked(action,"map-quest-action:"+(quest==null?0:quest.id),-1))
            {
                if(quest==null)OpenMapQuestChapter();
                else if(ready)ClaimCityQuestReward(quest.id);
                else GuideCityQuest(quest);
            }
        }

        private void DrawMapQuestText(Rect bounds,string text,GUIStyle source,int minimumSize)
        {
            var fitted=new GUIStyle(source);var content=new GUIContent(text);
            while(fitted.fontSize>minimumSize&&!HeaderTextFits(bounds,content,fitted,false))fitted.fontSize--;
            hudQuestTextFits&=HeaderTextFits(bounds,content,fitted,false);
            GUI.Label(bounds,content,fitted);
        }

        private void OpenMapQuestChapter()
        {
            OpenCityQuests();
            if(world.CurrentCityQuest==null){cityQuestCompleted=true;cityQuestFocusPending=false;}
        }
    }
}
