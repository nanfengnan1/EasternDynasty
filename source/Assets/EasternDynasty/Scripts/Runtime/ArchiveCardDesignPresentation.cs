using System;
using System.Collections.Generic;
using UnityEngine;
namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D archiveFrameSkin,archiveSelectedSkin,archiveRibbonSkin,archiveFooterSkin;
        private bool archiveDesignTextFits=true;
        private int archiveDesignColumns;
        private float archiveDesignCardHeight;
        private sealed class ArchiveDesignNode
        {internal string Id,Name;internal Rect Card,Art,NameArea,Footer;}
        private readonly List<ArchiveDesignNode> archiveDesignNodes=new List<ArchiveDesignNode>();
        private void InitArchiveDesign()
        {
            if(archiveFrameSkin!=null)return;
            archiveFrameSkin=GameAssets.LoadTexture("ui.youzhou.archive_frame");
            archiveSelectedSkin=GameAssets.LoadTexture("ui.youzhou.archive_selected");
            archiveRibbonSkin=GameAssets.LoadTexture("ui.youzhou.archive_ribbon");
            archiveFooterSkin=GameAssets.LoadTexture("ui.youzhou.archive_footer");
        }
        private bool ArchiveDesignAssetsReady()
        {
            InitArchiveDesign();
            return archiveFrameSkin.width==384&&archiveFrameSkin.height==504
                &&archiveSelectedSkin.width==384&&archiveSelectedSkin.height==504
                &&archiveRibbonSkin.width==56&&archiveRibbonSkin.height==140
                &&archiveFooterSkin.width==384&&archiveFooterSkin.height==96;
        }
        private static Rect ArchiveDesignRect(Rect parent,ArchiveCardLayout.Box box)
        {return new Rect(parent.x+box.X,parent.y+box.Y,box.W,box.H);}
        private void ArchiveDesignText(Rect r,string value,int size,Color color,TextAnchor alignment=TextAnchor.MiddleLeft)
        {
            var style=new GUIStyle(body){fontSize=size,wordWrap=false,alignment=alignment,clipping=TextClipping.Clip,normal={textColor=color}};
            var content=new GUIContent(value);
            while(style.fontSize>12&&!HeaderTextFits(r,content,style,false))style.fontSize--;
            archiveDesignTextFits&=HeaderTextFits(r,content,style,false);
            GUI.Label(r,content,style);
        }
        private bool DrawArchiveDesignCard(Rect r,GeneralDefinition g,bool selected)
        {
            InitArchiveDesign();
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            GUI.DrawTexture(r,selected||hover?archiveSelectedSkin:archiveFrameSkin,ScaleMode.StretchToFill,true);
            Rect art=ArchiveDesignRect(r,ArchiveCardLayout.Art(r.width,r.height));
            PortraitComplete(art,g.id);
            Rect name=ArchiveDesignRect(r,ArchiveCardLayout.Name(r.width,r.height,g.name.Length));
            GUI.DrawTexture(name,archiveRibbonSkin,ScaleMode.StretchToFill,true);
            Rect faction=ArchiveDesignRect(r,ArchiveCardLayout.Faction(r.width,r.height));
            Color previous=GUI.color;GUI.color=FactionColor(g.faction);
            GUI.DrawTexture(faction,archiveFooterSkin,ScaleMode.StretchToFill,true);GUI.color=previous;
            ArchiveDesignText(faction,g.faction,18,Paper,TextAnchor.MiddleCenter);
            float step=ArchiveCardLayout.NameStep(r.height,g.name.Length);
            for(int i=0;i<g.name.Length;i++)
                ArchiveDesignText(new Rect(name.x,name.y+i*step,name.width,step),g.name[i].ToString(),Math.Min(20,(int)step-2),Paper,TextAnchor.MiddleCenter);
            ArchiveDesignText(ArchiveDesignRect(r,ArchiveCardLayout.Troop(r.width,r.height)),g.troop,16,Gold,TextAnchor.MiddleCenter);
            ArchiveDesignText(ArchiveDesignRect(r,ArchiveCardLayout.Cost(r.width,r.height)),"C "+g.cost.ToString("0.0"),12,Paper);
            ArchiveDesignText(ArchiveDesignRect(r,ArchiveCardLayout.Stars(r.width,r.height)),new string('★',g.stars),13,Gold,TextAnchor.MiddleRight);
            Rect footer=ArchiveDesignRect(r,ArchiveCardLayout.Footer(r.width,r.height));
            GUI.DrawTexture(footer,archiveFooterSkin,ScaleMode.StretchToFill,true);
            string skill=string.IsNullOrWhiteSpace(g.nativeSkill)?g.skill:g.nativeSkill;
            ArchiveDesignText(ArchiveDesignRect(r,ArchiveCardLayout.Skill(r.width,r.height)),string.IsNullOrWhiteSpace(skill)?"战法资料待补":skill,14,Paper);
            ArchiveDesignText(ArchiveDesignRect(r,ArchiveCardLayout.Scope(r.width,r.height)),g.recruitable?"试玩可招募":"资料收藏",12,g.recruitable?Gold:Muted);
            return ContentTileClicked(r,g.id,0);
        }
        private void RecordArchiveDesignNode(Rect visible,GeneralDefinition g)
        {
            if(!GUI.enabled)return;
            archiveDesignNodes.Add(new ArchiveDesignNode{Id=g.id,Name=g.name,Card=visible,
                Art=ArchiveDesignRect(visible,ArchiveCardLayout.Art(visible.width,visible.height)),
                NameArea=ArchiveDesignRect(visible,ArchiveCardLayout.Name(visible.width,visible.height,g.name.Length)),
                Footer=ArchiveDesignRect(visible,ArchiveCardLayout.Footer(visible.width,visible.height))});
        }
    }
}
