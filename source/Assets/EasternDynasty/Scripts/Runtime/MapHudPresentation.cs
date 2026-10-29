using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private GUIStyle hudNumber, hudCaption, hudName, hudSmall, hudResourceName, hudCardIdentity, hudIdentityBase, hudRecruitSeal;
        private Texture2D hudCity;
        private Texture2D hudTicket, hudTicketActive, hudRecruitTicket, hudLetter, hudBook, hudShelf, hudMountain;
        private GUIStyle hudTicketTitle, hudTicketCaption;
        private bool hudNavigationTextFits;
        private Texture2D hudMapRecruitSeal,hudMapRecruitSealHover;
        private bool hudMapRecruitTextFits;

        private void InitMapHudStyles()
        {
            if(hudNumber!=null)return;
            hudNumber=new GUIStyle(cardName){fontSize=22,wordWrap=false,clipping=TextClipping.Clip};
            hudCaption=new GUIStyle(body){fontSize=17,alignment=TextAnchor.MiddleCenter,wordWrap=false};
            hudName=new GUIStyle(body){fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            hudSmall=new GUIStyle(tiny){fontSize=14,wordWrap=false};
            hudResourceName=new GUIStyle(tiny){fontSize=13,wordWrap=false};
            hudCardIdentity=new GUIStyle(body){fontSize=17,alignment=TextAnchor.MiddleCenter,wordWrap=false};
            hudIdentityBase=new GUIStyle(hudSmall){alignment=TextAnchor.MiddleCenter};
            hudRecruitSeal=new GUIStyle(hudCardIdentity){fontSize=25};hudRecruitSeal.normal.textColor=C("4F3E2A");
            hudCity=GameAssets.LoadTexture("map.youzhou.city",false);
            hudTicket=GameAssets.LoadTexture("ui.youzhou.nav_panel");
            hudTicketActive=GameAssets.LoadTexture("ui.youzhou.nav_panel_active");
            hudRecruitTicket=GameAssets.LoadTexture("ui.youzhou.nav_recruit");
            hudLetter=GameAssets.LoadTexture("ui.youzhou.nav_letter");
            hudBook=GameAssets.LoadTexture("ui.youzhou.nav_book");
            hudShelf=GameAssets.LoadTexture("ui.youzhou.nav_shelf");
            hudMountain=GameAssets.LoadTexture("map.youzhou.mountain");
            hudTicketTitle=new GUIStyle(hudCaption){fontSize=15};hudTicketTitle.normal.textColor=C("E0D2AA");
            hudTicketCaption=new GUIStyle(hudCaption){fontSize=16};hudTicketCaption.normal.textColor=C("E0D2AA");
        }

        private void HudSurface(Rect area, bool accent=false)
        {
            RectFill(area,new Color(.065f,.075f,.078f,.91f));
            RectFill(new Rect(area.x,area.y,area.width,1),accent?Gold:Border);
            RectFill(new Rect(area.x,area.yMax-1,area.width,1),new Color(.68f,.59f,.4f,.32f));
            // Short corner inlays leave the map-facing edges visually open.
            RectFill(new Rect(area.x,area.y,18,2),Gold*.7f);
            RectFill(new Rect(area.x,area.y,2,12),Gold*.7f);
            RectFill(new Rect(area.xMax-18,area.yMax-2,18,2),Gold*.7f);
        }

        private void DrawYouzhouMapHeader()
        {
            DrawDesignedMapHeader();
        }

        // Shared live stocks and income: city and map must never maintain separate values.
        private void DrawYouzhouResourceLedger()
        {
            string[] names={"木材","粮食","石料","铁矿","金币","灵玉"};
            double[] amounts={world.resources.grass,world.resources.grain,world.resources.stone,world.resources.iron,world.resources.gold,world.resources.jade};
            var rates=world.HourlyIncome();double[] income={rates.grass,rates.grain,rates.stone,rates.iron,rates.gold,0};
            float cell=(width-286)/6;
            for(int i=0;i<6;i++)
            {
                float x=280+i*cell;
                RectFill(new Rect(x,14,1,42),Border*.65f);
                ResourceIcon(new Rect(x+10,20,30,30),i);
                Label(x+49,5,cell-56,19,names[i],hudResourceName);
                Label(x+48,24,cell-53,28,HudQuantity(amounts[i]),hudNumber);
                Label(x+49,51,cell-56,17,i<5?"+"+HudQuantity(income[i])+" / 时":"试玩灵玉",hudSmall);
                RecordMapHudTarget("hud-resource-"+i,new Rect(x+3,5,cell-6,61));
            }
        }

        private static string HudQuantity(double value)
        {
            // Abbreviate only extreme inventories; normal play keeps exact integers.
            if(Math.Abs(value)>=100000000)return (value/100000000).ToString("0.##")+"亿";
            if(Math.Abs(value)>=1000000)return (value/10000).ToString("0.#")+"万";
            return Math.Floor(value).ToString("N0");
        }

        private void HudFittedLabel(Rect bounds,string text,GUIStyle source,int minimumSize)
        {
            var fitted=new GUIStyle(source);
            while(fitted.fontSize>minimumSize&&fitted.CalcSize(new GUIContent(text)).x>bounds.width)fitted.fontSize--;
            GUI.Label(bounds,text,fitted);
        }

        private void DrawYouzhouMapFooter()
        {
            InitMapHudStyles();
            // The city and map now share the inspected component system. The
            // horizontal image/caption draft is in DesignUI/s1-20261028-172751912.
            hudNavigationTextFits=true;
            hudMapRecruitTextFits=false;
            var footer=new Rect(0,height-78,width,78);Block(footer);
            // Exported layered design, no runtime illustration algorithms.
            GUI.DrawTexture(footer,hudShelf,ScaleMode.StretchToFill,true);
            var save=HudDesignRect(MapHudNavigationLayout.Utility(width,height,false));
            if(HudDesignedUtility(save,"保存"))Save(true);
            RecordMapHudTarget("hud-save",save);
            string[] tabs={"主城","领地","武将","战报","规则","招募"};
            string[] ids={"city-open","map-tab","roster-tab","reports-tab","hud-help","recruit-tab"};
            for(int i=0;i<tabs.Length;i++)
            {
                bool raisedRecruit=i==5&&tab=="领地"&&map.selected<0;
                var box=raisedRecruit?MapHudNavigationLayout.RecruitEntry(width,height):MapHudNavigationLayout.Entry(width,height,i);
                var r=HudDesignRect(box);
                RecordMapHudTarget(ids[i],r);
                if(raisedRecruit?DrawDesignedMapRecruit(box):HudDesignedNavigation(box,i==4?"帮助":tabs[i],i,tab==tabs[i],ids[i]))
                {
                    if(i==0){OpenCity();continue;}
                    if(tab=="战斗等待"||watchingCurrentBattle)CloseCurrentBattle();
                    ClearCityQuestLandOrigin();tab=tabs[i];drawResult=null;
                    trainingEditingFormation=false;cityEditingFormation=false;reinforcementEditingFormation=false;replayPlaying=false;
                }
            }
            var clock=HudDesignRect(MapHudNavigationLayout.Utility(width,height,true));RecordMapHudTarget("speed-cycle",clock);
            if(HudDesignedUtility(clock,speed==0?"时间暂停 · 继续":"时间  ×"+speed))speed=speed==1?20:speed==20?60:speed==60?0:1;
        }

        private static Rect HudDesignRect(MapHudNavigationLayout.Box b){return new Rect(b.X,b.Y,b.W,b.H);}

        private bool DrawDesignedMapRecruit(MapHudNavigationLayout.Box box)
        {
            if(hudMapRecruitSeal==null)
            {
                hudMapRecruitSeal=GameAssets.LoadTexture("ui.youzhou.map_recruit_seal");
                hudMapRecruitSealHover=GameAssets.LoadTexture("ui.youzhou.map_recruit_seal_hover");
            }
            Rect bounds=HudDesignRect(box),art=HudDesignRect(MapHudNavigationLayout.RecruitArt(box));
            Rect caption=HudDesignRect(MapHudNavigationLayout.RecruitCaption(box));
            // Footer draws before labels. Block the complete raised footprint so
            // terrain gestures and city labels cannot pass through its silhouette.
            Block(bounds);
            bool hover=GUI.enabled&&bounds.Contains(Event.current.mousePosition);
            GUI.DrawTexture(bounds,hover?hudMapRecruitSealHover:hudMapRecruitSeal,ScaleMode.StretchToFill,true);
            PortraitComplete(art,"s1_100039"); // Existing illustration, never a promised recruitment outcome.
            var fitted=new GUIStyle(hudTicketCaption){fontSize=19,wordWrap=false,clipping=TextClipping.Clip};
            var title=new GUIContent("招募");
            while(fitted.fontSize>12&&!HeaderTextFits(caption,title,fitted,false))fitted.fontSize--;
            hudMapRecruitTextFits=HeaderTextFits(caption,title,fitted,false);
            hudNavigationTextFits&=hudMapRecruitTextFits;
            GUI.Label(caption,title,fitted);
            RecordMapHudTarget("recruit-tab-art",art);
            RecordMapHudTarget("recruit-tab-caption",caption);
            return ContentTileClicked(bounds,"map-recruit-entry",-1);
        }

        private void HudNavigationText(Rect bounds,string value,GUIStyle source,int minimumSize)
        {
            var fitted=new GUIStyle(source){wordWrap=false,clipping=TextClipping.Clip};
            var content=new GUIContent(value);
            while(fitted.fontSize>minimumSize&&!HeaderTextFits(bounds,content,fitted,false))fitted.fontSize--;
            hudNavigationTextFits&=HeaderTextFits(bounds,content,fitted,false);
            GUI.Label(bounds,content,fitted);
        }

        private bool HudDesignedUtility(Rect r,string text)
        {
            RectFill(r,new Color(.086f,.129f,.165f,.85f));Outline(r,C("797765"));
            HudNavigationText(r,text,hudTicketCaption,12);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }

        private bool HudDesignedNavigation(MapHudNavigationLayout.Box box,string text,int index,bool active,string target)
        {
            Rect r=HudDesignRect(box),art=HudDesignRect(MapHudNavigationLayout.Art(box,index==5));
            Rect caption=HudDesignRect(MapHudNavigationLayout.Caption(box,index==5));
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            // Recruitment object's exported visible bounds are x=12.5..119.5.
            var skin=index==5?new Rect(r.x+12.5f,r.y,107,r.height):r;
            GUI.DrawTexture(skin,index==5?hudRecruitTicket:active||hover?hudTicketActive:hudTicket,ScaleMode.StretchToFill,true);
            if(index==0)GUI.DrawTexture(art,hudCity,ScaleMode.ScaleToFit,true);
            else if(index==1)GUI.DrawTexture(art,hudMountain,ScaleMode.ScaleToFit,true);
            else if(index==2)
            {
                var generals=world.ActiveGenerals;
                for(int slot=0;slot<Math.Min(3,generals.Count);slot++)
                {
                    int source=slot==0&&generals.Count>1?1:slot==1?0:slot;
                    PortraitComplete(new Rect(art.x+5+slot*30,art.y,30,42),generals[source].definition);
                }
                if(generals.Count==0)HudNavigationText(art,"未编队",new GUIStyle(hudTicketCaption){fontSize=14},12);
            }
            else if(index==3)GUI.DrawTexture(art,hudLetter,ScaleMode.ScaleToFit,true);
            else if(index==4)GUI.DrawTexture(art,hudBook,ScaleMode.ScaleToFit,true);
            else PortraitComplete(art,"s1_100039"); // Existing starter identity, never a pull result.
            HudNavigationText(caption,text,index==5?hudTicketCaption:hudTicketTitle,12);
            if(GUI.enabled){RecordMapHudTarget(target+"-art",art);RecordMapHudTarget(target+"-caption",caption);}
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }

        private bool HudTextAction(Rect r,string text,bool active=false,bool enabled=true)
        {
            bool hover=r.Contains(Event.current.mousePosition)&&enabled;
            RectFill(r,active?new Color(.42f,.24f,.19f,.95f):hover?new Color(.23f,.25f,.23f,.97f):new Color(.10f,.13f,.14f,.92f));
            RectFill(new Rect(r.x,r.yMax-1,r.width,1),active?Gold:Border);
            var oldColor=GUI.color;if(!enabled)GUI.color=new Color(.55f,.55f,.55f,1);
            GUI.Label(r,text,hudCaption);GUI.color=oldColor;
            bool before=GUI.enabled;GUI.enabled=before&&enabled;
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);GUI.enabled=before;return clicked;
        }

        private bool HudNavigationButton(Rect r,string text,int icon,bool active,string target)
        {
            bool hover=GUI.enabled&&r.Contains(Event.current.mousePosition);
            if(active||hover)
            {
                RectFill(new Rect(r.x+7,r.y+1,r.width-14,r.height-2),new Color(.34f,.28f,.20f,active?.58f:.3f));
                RectFill(new Rect(r.x+7,r.yMax-2,r.width-14,2),active?Gold:Border);
            }
            var art=new Rect(r.center.x-42,r.y+2,84,43);
            DrawHudNavigationArt(art,icon);
            var caption=new Rect(r.x+8,r.y+46,r.width-16,22);
            GUI.Label(caption,text,hudCaption);
            if(GUI.enabled)
            {
                // These are visible art/text regions, not extra buttons.
                RecordMapHudTarget(target+"-art",art);
                RecordMapHudTarget(target+"-caption",caption);
            }
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }

        private void DrawHudNavigationArt(Rect r,int kind)
        {
            Color paper=C("C5BB96"),ink=C("464A3C"),bronze=C("9A8050");
            if(kind==5)
            {
                if(hudCity!=null)GUI.DrawTexture(r,hudCity,ScaleMode.ScaleToFit,true);
                else DrawHudNavigationIcon(r,5,Gold);
            }
            else if(kind==1)
            {
                // An emblem made from the current army, never an advertised pull.
                var generals=world.ActiveGenerals;
                for(int layer=0;layer<3;layer++)
                {
                    int slot=layer==2?0:layer+1;
                    var card=new Rect(r.x+6+(slot==0?21:slot==1?0:42),r.y+(slot==0?0:5),29,slot==0?42:36);
                    RectFill(card,ink);Outline(card,bronze);
                    if(slot<generals.Count)PortraitComplete(new Rect(card.x+2,card.y+2,card.width-4,card.height-4),generals[slot].definition);
                    else GUI.Label(card,"将",hudCardIdentity);
                }
            }
            else if(kind==2)
            {
                // Original recruitment seal; no invented hero, price or rarity.
                RectFill(new Rect(r.x+10,r.y+7,17,27),C("6C5144"));
                RectFill(new Rect(r.xMax-27,r.y+7,17,27),C("6C5144"));
                HudLine(r,.12f,.08f,.12f,.95f,bronze,2);
                HudLine(r,.88f,.08f,.88f,.95f,bronze,2);
                var disc=new Rect(r.center.x-21,r.y,42,42);
                DrawHudSealDisc(disc,bronze,paper);
                GUI.Label(disc,"募",hudRecruitSeal);
            }
            else if(kind==0)
            {
                var sheet=new Rect(r.x+10,r.y+5,r.width-20,r.height-10);
                RectFill(sheet,paper);Outline(sheet,bronze);
                RectFill(new Rect(sheet.x-3,sheet.y-2,5,sheet.height+4),bronze);
                RectFill(new Rect(sheet.xMax-2,sheet.y-2,5,sheet.height+4),bronze);
                HudLine(sheet,.12f,.7f,.37f,.23f,ink,4);
                HudLine(sheet,.37f,.23f,.57f,.62f,ink,4);
                HudLine(sheet,.44f,.67f,.70f,.18f,ink,3);
                HudLine(sheet,.70f,.18f,.87f,.51f,ink,3);
                HudLine(sheet,.12f,.84f,.88f,.74f,C("6B8980"),2);
            }
            else if(kind==3)
            {
                Rect scroll=new Rect(r.x+17,r.y+2,r.width-34,r.height-4);
                RectFill(new Rect(scroll.x+4,scroll.y+2,scroll.width,scroll.height),new Color(0,0,0,.35f));
                RectFill(scroll,paper);
                RectFill(new Rect(scroll.x-4,scroll.y-1,scroll.width+8,5),bronze);
                RectFill(new Rect(scroll.x-4,scroll.yMax-4,scroll.width+8,5),bronze);
                for(int i=0;i<3;i++)RectFill(new Rect(scroll.x+8,scroll.y+10+i*6,scroll.width-21-i*3,1),ink);
                RectFill(new Rect(scroll.xMax-15,scroll.yMax-16,10,10),C("87534A"));
                RectFill(new Rect(scroll.xMax-12,scroll.yMax-13,4,4),paper);
            }
            else
            {
                Rect book=new Rect(r.x+13,r.y+5,r.width-26,r.height-9);
                RectFill(new Rect(book.x-3,book.y+3,book.width+6,book.height),bronze);
                RectFill(book,paper);
                RectFill(new Rect(book.center.x-1,book.y,3,book.height),ink);
                for(int i=0;i<4;i++)
                {
                    RectFill(new Rect(book.x+5,book.y+6+i*6,book.width/2-10,1),ink);
                    RectFill(new Rect(book.center.x+6,book.y+6+i*6,book.width/2-11,1),ink);
                }
            }
        }

        private void DrawHudSealDisc(Rect r,Color edge,Color center)
        {
            float radius=r.width*.5f;
            for(int y=0;y<Mathf.CeilToInt(r.height);y++)
            {
                float dy=y+.5f-radius,span=Mathf.Sqrt(Mathf.Max(0,radius*radius-dy*dy));
                RectFill(new Rect(r.center.x-span,r.y+y,span*2,1),edge);
                float inner=radius-3;
                if(Mathf.Abs(dy)<inner)
                {
                    float innerSpan=Mathf.Sqrt(inner*inner-dy*dy);
                    RectFill(new Rect(r.center.x-innerSpan,r.y+y,innerSpan*2,1),center);
                }
            }
        }

        private void DrawHudNavigationIcon(Rect r,int kind,Color color)
        {
            // Original line emblems, not screenshots or official skill/feature icons.
            if(kind==5&&hudCity!=null){GUI.DrawTexture(r,hudCity,ScaleMode.ScaleToFit,true);return;}
            if(kind==0)
            {
                HudLine(r,.12f,.3f,.5f,.1f,color);HudLine(r,.5f,.1f,.88f,.3f,color);
                HudLine(r,.12f,.3f,.5f,.5f,color);HudLine(r,.5f,.5f,.88f,.3f,color);
                HudLine(r,.12f,.52f,.5f,.72f,color);HudLine(r,.5f,.72f,.88f,.52f,color);
                HudLine(r,.12f,.74f,.5f,.94f,color);HudLine(r,.5f,.94f,.88f,.74f,color);
            }
            else if(kind==1||kind==2)
            {
                for(int i=0;i<(kind==1?3:1);i++)
                {
                    float x=kind==1?.13f+i*.28f:.28f,y=kind==1?.2f+(i==1?-.1f:.08f):.1f;
                    HudLine(r,x,y,x,.92f,color);
                    HudLine(r,x,y,x+(kind==1?.18f:.48f),y+.06f,color);
                    HudLine(r,x+(kind==1?.18f:.48f),y+.06f,x+(kind==1?.18f:.48f),y+.43f,color);
                    HudLine(r,x,y+.36f,x+(kind==1?.18f:.48f),y+.43f,color);
                }
                if(kind==2){HudLine(r,.41f,.31f,.64f,.31f,color);HudLine(r,.525f,.21f,.525f,.41f,color);}
            }
            else
            {
                Outline(new Rect(r.x+r.width*.17f,r.y+r.height*.13f,r.width*.66f,r.height*.74f),color,1.5f);
                if(kind==4)HudLine(r,.5f,.14f,.5f,.86f,color);
                for(int i=0;i<3;i++)HudLine(r,.29f,.32f+i*.17f,kind==4?.43f:.7f,.32f+i*.17f,color);
                if(kind==4)for(int i=0;i<3;i++)HudLine(r,.57f,.32f+i*.17f,.73f,.32f+i*.17f,color);
            }
        }

        private void HudLine(Rect r,float x1,float y1,float x2,float y2,Color color,float thickness=1.5f)
        {
            if(Event.current.type!=EventType.Repaint)return;
            // Keep both the existing group/scroll clip and mobile safe-area matrix.
            // Rotating GUI.matrix inside a scroll view changes its clipping origin.
            var raster=new UiLineRaster(r.x+x1*r.width,r.y+y1*r.height,r.x+x2*r.width,r.y+y2*r.height,thickness);
            UiLineStrip strip;
            while(raster.MoveNext(out strip))
                RectFill(new Rect(strip.X,strip.Y,strip.Width,strip.Height),color);
        }

        private void RecordMapHudTarget(string id,Rect rect)
        { if(GUI.enabled)RecordReviewTarget(id,rect); }

    }
}
