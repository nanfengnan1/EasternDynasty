using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D hudArmyCard,hudArmyCardHover,hudArmyStrip,hudArmyCommand,hudArmyCommandHover;
        private GUIStyle hudArmyText,hudArmyStrength,hudArmyName,hudArmyIdentity,hudArmyAction;
        private bool hudArmyTextFits;

        private void DrawYouzhouArmyHud()
        {
            InitMapHudStyles();
            if(hudArmyCard==null)
            {
                hudArmyCard=GameAssets.LoadTexture("ui.youzhou.army_row");
                hudArmyCardHover=GameAssets.LoadTexture("ui.youzhou.army_row_hover");
                hudArmyStrip=GameAssets.LoadTexture("ui.youzhou.army_strip");
                hudArmyCommand=GameAssets.LoadTexture("ui.youzhou.army_command");
                hudArmyCommandHover=GameAssets.LoadTexture("ui.youzhou.army_command_hover");
                hudArmyText=new GUIStyle(body){fontSize=13,alignment=TextAnchor.MiddleLeft,wordWrap=false,clipping=TextClipping.Clip};
                hudArmyText.normal.textColor=C("E8E2CD");
                hudArmyStrength=new GUIStyle(hudArmyText){fontSize=20,alignment=TextAnchor.MiddleRight};
                hudArmyStrength.normal.textColor=C("EEE7D4");
                hudArmyName=new GUIStyle(hudArmyText){fontSize=18,alignment=TextAnchor.MiddleLeft};
                hudArmyName.normal.textColor=C("EEE7D4");
                hudArmyIdentity=new GUIStyle(hudArmyText){alignment=TextAnchor.MiddleRight};
                hudArmyAction=new GUIStyle(hudArmyName){fontSize=16,alignment=TextAnchor.MiddleCenter};
            }
            hudArmyTextFits=true;
            var layout=MapHudArmyLayout.Area(height);
            Rect area=HudDesignRect(layout),header=HudDesignRect(MapHudArmyLayout.Header(layout));
            // Keep the whole existing touch barrier, including the visual gaps.
            Block(area);RecordMapHudTarget("hud-army-panel",area);
            GUI.DrawTexture(header,hudArmyStrip,ScaleMode.StretchToFill,true);
            DrawMapArmyText(new Rect(header.x+8,header.y,100,27),"第一部队",new GUIStyle(hudArmyText){fontSize=16},13);
            DrawMapArmyText(new Rect(header.x+112,header.y,234,27),world.troops+" / "+world.TroopCap+" 兵",hudArmyStrength,13);
            RecordMapHudTarget("hud-army-header",header);
            // Dynamic geometry only: the bar represents the actual troop count.
            RectFill(new Rect(area.x+8,area.y+27,338,3),C("595C50"));
            RectFill(new Rect(area.x+8,area.y+27,338*Mathf.Clamp01((float)world.troops/Math.Max(1,world.TroopCap)),3),C("9CA97D"));
            var generals=world.ActiveGenerals;
            for(int i=0;i<3;i++)
            {
                var cardBox=MapHudArmyLayout.Card(layout,i);
                Rect card=HudDesignRect(cardBox),art=HudDesignRect(MapHudArmyLayout.Art(cardBox));
                Rect name=HudDesignRect(MapHudArmyLayout.Name(cardBox)),identity=HudDesignRect(MapHudArmyLayout.Identity(cardBox));
                bool hover=i<generals.Count&&GUI.enabled&&card.Contains(Event.current.mousePosition);
                GUI.DrawTexture(card,hover?hudArmyCardHover:hudArmyCard,ScaleMode.StretchToFill,true);
                if(i<generals.Count)
                {
                    var general=generals[i];
                    PortraitComplete(art,general.definition);
                    string fullName=general.Definition.name;
                    // The inspected ribbon uses one horizontal name; fit its real
                    // glyph width and line height on both desktop and CJK mobile fonts.
                    DrawMapArmyText(name,fullName,hudArmyName,12);
                    int formationSlot=world.formation.IndexOf(general.uid);
                    DrawMapArmyText(identity,(formationSlot>=0&&formationSlot<Slots.Length?Slots[formationSlot]:"部队")+" Lv."+general.level,hudArmyIdentity,11);
                    RecordMapHudTarget("hud-army-card-"+i,card);
                    RecordMapHudTarget("hud-army-art-"+i,art);
                    RecordMapHudTarget("hud-army-name-"+i,name);
                    RecordMapHudTarget("hud-army-identity-"+i,identity);
                    if(ContentTileClicked(card,"hud-card:"+general.definition,general.uid))ShowCardDetails(general.definition,general.uid);
                }
                else
                {
                    DrawMapArmyText(name,"待任命",hudArmyName,12);
                    DrawMapArmyText(identity,"空位",hudArmyIdentity,12);
                }
            }
            Rect statusRect=HudDesignRect(MapHudArmyLayout.Status(layout));
            GUI.DrawTexture(statusRect,hudArmyStrip,ScaleMode.StretchToFill,true);
            string state=world.ArmyState+(world.order==null?" · 政令 "+world.AvailableCommands+"/"+World.CommandCap:world.order.phase==1?"":" · "+Math.Ceiling(world.order.remaining)+" 秒");
            DrawMapArmyText(new Rect(statusRect.x+8,statusRect.y,statusRect.width-16,statusRect.height),state,hudArmyText,11);
            RecordMapHudTarget("hud-army-status",statusRect);
            Rect reinforce=HudDesignRect(MapHudArmyLayout.Action(layout,false));
            RecordMapHudTarget("army-reinforcement",reinforce);
            if(DrawMapArmyCommand(reinforce,"进入征兵",true))OpenReinforcement();
            Rect action=HudDesignRect(MapHudArmyLayout.Action(layout,true));
            if(world.order!=null)
            {
                RecordMapHudTarget("army-recall",action);
                RecordMapHudTarget("hud-army-recall",action);
                if(DrawMapArmyCommand(action,"撤回部队",!world.order.returning))Act(world.RecallArmy());
            }
            else
            {
                RecordMapHudTarget("hud-army-formation",action);
                if(DrawMapArmyCommand(action,"调整编队",true))tab="武将";
            }
            if(TrainingActive)
            {
                var progress=new Rect(area.x,area.y-51,area.width,44);Block(progress);
                RecordMapHudTarget("army-training-progress",progress);
                if(HudTextAction(progress,"查看练兵进度  ›",true))OpenTraining(world.order.target);
            }
            DrawBattleWatchEntry(new Rect(area.x,area.y-(TrainingActive?102:51),area.width,44));
        }

        private void DrawMapArmyText(Rect bounds,string text,GUIStyle source,int minimumSize)
        {
            var fitted=new GUIStyle(source);var content=new GUIContent(text);
            while(fitted.fontSize>minimumSize&&!HeaderTextFits(bounds,content,fitted,false))fitted.fontSize--;
            hudArmyTextFits&=HeaderTextFits(bounds,content,fitted,false);
            GUI.Label(bounds,content,fitted);
        }

        private bool DrawMapArmyCommand(Rect bounds,string text,bool enabled)
        {
            bool wasEnabled=GUI.enabled;
            Color previousColor=GUI.color;
            if(!enabled)GUI.color=previousColor*new Color(.55f,.55f,.55f,1);
            bool hover=wasEnabled&&enabled&&bounds.Contains(Event.current.mousePosition);
            GUI.DrawTexture(bounds,hover?hudArmyCommandHover:hudArmyCommand,ScaleMode.StretchToFill,true);
            DrawMapArmyText(new Rect(bounds.x+6,bounds.y+8,bounds.width-12,28),text,hudArmyAction,13);
            GUI.color=previousColor;
            GUI.enabled=wasEnabled&&enabled;
            bool clicked=ContentTileClicked(bounds,"map-army-command:"+text,-1);
            GUI.enabled=wasEnabled;
            return clicked;
        }
    }
}
