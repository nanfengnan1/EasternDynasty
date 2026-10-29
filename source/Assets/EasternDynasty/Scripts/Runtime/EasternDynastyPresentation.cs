using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    // Presentation stays separate from the serializable campaign and its rules.
    public sealed partial class EasternDynastyGame
    {
        private readonly Dictionary<string, Texture2D> portraits = new Dictionary<string, Texture2D>();
        private GUIStyle centered, tiny, display, darkText;
        private Texture2D shade;
        private GeneralCard drawResult;
        private string toastObservedStatus;
        private float toastStartedAt;
        private Vector2 generalScroll, poolScroll;
        private readonly List<Rect> hudBlocks = new List<Rect>();
        private readonly List<Rect> cityLabels = new List<Rect>();
        private static readonly Color Red = C("B85A52"), Paper = C("E8E2D3");
        private static readonly string[] Slots = { "前锋", "中军", "大营" };

        private void InitStyles()
        {
            if (stylesReady) return;
            font = CreateInterfaceFont();
            body = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true, richText = false, padding = new RectOffset() };
            body.normal.textColor = Text;
            small = new GUIStyle(body) { fontSize = 15 }; small.normal.textColor = Muted;
            tiny = new GUIStyle(small) { fontSize = 13 };
            heading = new GUIStyle(body) { font = font, fontSize = 27 }; heading.normal.textColor = Gold;
            title = new GUIStyle(heading) { fontSize = 40 };
            display = new GUIStyle(heading) { fontSize = 76 };
            cardName = new GUIStyle(heading) { fontSize = 24 }; cardName.normal.textColor = Text;
            muted = new GUIStyle(body); muted.normal.textColor = Muted;
            centered = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter };
            darkText = new GUIStyle(small); darkText.normal.textColor = Ink;
            button = new GUIStyle(body) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
            GUI.skin.textField.font = font; GUI.skin.textField.fontSize = 20;
            GUI.skin.textField.padding = new RectOffset(12,12,8,8);
            shade = new Texture2D(1,64,TextureFormat.RGBA32,false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int i=0;i<64;i++) shade.SetPixel(0,i,new Color(.055f,.065f,.06f,Mathf.Pow(1-i/63f,1.5f)*.98f));
            shade.Apply();
            foreach (var g in Catalog.Generals) portraits[g.id] = GameAssets.LoadTexture("portrait."+g.id, false);
            stylesReady = true;
        }
        private void RectFill(Rect rect, Color color)
        { var before=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=before; }
        private void Frame(Rect r, Color color)
        { RectFill(r,Border);RectFill(new Rect(r.x+1,r.y+1,r.width-2,r.height-2),color); }
        private void Outline(Rect r, Color color, float thickness=1)
        {
            RectFill(new Rect(r.x,r.y,r.width,thickness),color);RectFill(new Rect(r.x,r.yMax-thickness,r.width,thickness),color);
            RectFill(new Rect(r.x,r.y,thickness,r.height),color);RectFill(new Rect(r.xMax-thickness,r.y,thickness,r.height),color);
        }
        private void Label(float x,float y,float w,float h,string text,GUIStyle style=null)
        { GUI.Label(new Rect(x,y,w,h),text,style??body); }
        private bool Button(Rect r,string text,bool highlighted=false,bool enabled=true)
        {
            bool hover=r.Contains(Event.current.mousePosition)&&enabled;
            Color fill=highlighted?Red:Panel;
            if(hover)fill=Color.Lerp(fill,Gold,.15f);
            Frame(r,enabled?fill:Color.Lerp(Ink,Panel,.4f));
            if(highlighted)RectFill(new Rect(r.x,r.yMax-2,r.width,2),Gold);
            Color old=GUI.color; if(!enabled)GUI.color=new Color(.55f,.55f,.55f,1);
            GUI.Label(r,text,button);GUI.color=old;
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool trace=reviewMode&&(text=="领地"||text=="收下军令"||text.StartsWith("屯田",StringComparison.Ordinal))&&
                (Event.current.rawType==EventType.MouseDown||Event.current.rawType==EventType.MouseUp)&&r.Contains(Event.current.mousePosition);
            EventType beforeType=Event.current.type;int beforeHot=GUIUtility.hotControl;
            bool clicked=GUI.Button(r,GUIContent.none,GUIStyle.none);
            if(trace)Debug.Log("UI_CLICK label="+text+" before="+beforeType+" after="+Event.current.type+" hot="+beforeHot+">"+GUIUtility.hotControl+" clicked="+clicked+" enabled="+(previous&&enabled)+" tab="+tab+" poolScroll="+poolScroll+" pointer="+Event.current.mousePosition);
            GUI.enabled=previous;return clicked;
        }
        private void Block(Rect r)
        { hudBlocks.Add(GuiRectToPixels(r)); }
        private void OnGUI()
        {
            InitStyles();
            PrepareInterfaceLayout();
            if(Event.current.type==EventType.Repaint)ClearReviewTargets();
            hudBlocks.Clear();
            bool modal=drawResult!=null||confirmation!=null||cardDetailId!=null||cityQuestReceipt!=null;
            GUI.enabled=!modal;
            if(world==null)
            { map.SetViewport(uiPixels);Welcome(); }
            else
            {
                // The reviewed map header overlays live terrain. Input still starts
                // below its 70-unit blocked band; other pages keep their viewport.
                var mapInput=GuiRectToPixels(HudDesignRect(MapHudHeaderLayout.Input(width,height)));
                map.SetViewport(GuiRectToPixels(world.IsYouzhou&&tab=="领地"
                    ?HudDesignRect(MapHudHeaderLayout.Camera(width,height)):new Rect(0,70,width,height-148)),mapInput);
                if(tab=="领地")RecordReviewTarget("map-surface",new Rect(0,70,width,height-148));
                main=new Rect(32,100,width-396,height-218);
                // Allocate persistent navigation controls before variable card lists and map labels.
                Footer();
                if(tab=="领地") { MapHud();if(world.IsYouzhou)DrawMapQuestRibbon();LandPanel();if(world.IsYouzhou&&map.selected<0)MiniMap();MapLabels();ResourceLabels();S1ArmyLabel(); }
                else
                {
                    RectFill(new Rect(0,70,width,height-148),new Color(.05f,.09f,.15f,.97f));
                    if(tab=="武将"){Roster();GeneralPanel();}
                    else if(tab=="招募"){Recruitment();RecruitmentPanel();}
                    else if(tab=="图鉴"){S1Archive();}
                    else if(tab=="主城"){DrawCityPanel();}
                    else if(tab=="练兵"){DrawTrainingPanel();}
                    else if(tab=="征兵"){DrawReinforcementPanel();}
                    else if(tab=="战报"){DrawBattleReports();}
                    else if(tab=="战斗回放"){DrawBattleReplay();}
                    else if(tab=="战斗等待"){DrawBattleWaiting();}
                    else {Help();InformationPanel("天下未定","黄巾之乱开局，历史不会自动推进。\n\n当前开放单人领主路线。\n\n商盟、贸易、同盟建国、玩家战争与太守车仍在开发计划中。");}
                }
                Header();StatusToast();
            }
            GUI.enabled=true;
            if(cityQuestReceipt!=null)DrawCityQuestReward();
            if(drawResult!=null)DrawResult();
            if(cardDetailId!=null)DrawCardDetails();
            if(confirmation!=null)ConfirmDialog();
            map.blockedRects=hudBlocks.ToArray();
            GUI.matrix=Matrix4x4.identity;
        }
        private void Welcome()
        {
            if(useYouzhou){YouzhouWelcome();return;}
            RectFill(new Rect(0,0,width,height),new Color(.07f,.085f,.08f,.82f));
            float x=(width-1180)/2,y=(height-720)/2;
            var portrait=new Rect(x,y,480,720);
            Portrait(portrait,"huangfu_song");GUI.DrawTexture(portrait,shade);
            Label(x+32,y+472,420,90,"山河待定",display);
            Label(x+38,y+574,390,55,"一城为始，逐鹿九州。",heading);
            Label(x+39,y+674,405,24,"甲子 · 黄巾之乱    /    公元 184 年",small);
            Frame(new Rect(x+480,y,700,720),Ink);
            float cx=x+522;
            Label(cx,y+34,610,32,"E A S T E R N   D Y N A S T Y",heading);
            Label(cx,y+79,610,30,"择一方水土，书写你的征程",muted);
            Label(cx,y+140,600,28,"01    选择出生区域",body);
            for(int i=0;i<3;i++)
                if(Button(new Rect(cx+i*207,y+183,195,48),Catalog.OriginName((Homeland)i),origin==(Homeland)i))
                {origin=(Homeland)i;region=i==0?4:i==1?9:11;}
            Label(cx,y+264,600,28,"02    选择立足之地",body);
            int n=0;
            for(int i=0;i<Catalog.Regions.Length;i++)if(World.RegionOrigin(i)==origin)
            {
                if(Button(new Rect(cx+n%3*207,y+306+n/3*46,195,36),Catalog.Regions[i],region==i))region=i;
                n++;
            }
            Label(cx,y+465,135,28,"03    势力名称",body);
            ruler=GUI.TextField(new Rect(cx+160,y+457,450,44),ruler,12);
            if(Button(new Rect(cx,y+526,610,38),"返回幽州 · 40×40 方格地图"))useYouzhou=true;
            if(Button(new Rect(cx,y+588,300,53),"于"+Catalog.Regions[region]+"起兵",true))
            {
                if(HasSave)Ask("建立新势力会替换当前存档，并保留上一份备份。是否继续？",()=>StartWorld(true));
                else StartWorld(true);
            }
            if(Button(new Rect(cx+315,y+588,295,53),"继续征程",false,HasSave))Load();
            Label(cx,y+667,610,35,string.IsNullOrEmpty(status)?"单人试玩  /  原创视觉样板  /  无付费内容":status,small);
        }
        private void Header()
        {
            if(world.IsYouzhou&&tab=="领地"){DrawYouzhouMapHeader();return;}
            if(world.IsYouzhou&&tab=="主城"){DrawCityHudHeader();return;}
            RectFill(new Rect(0,0,width,70),Ink);RectFill(new Rect(0,69,width,1),Gold*.65f);
            RectFill(new Rect(22,16,4,37),Gold);
            Label(38,10,226,31,"EASTERN DYNASTY",new GUIStyle(cardName){fontSize=21});
            Label(39,43,220,20,world.IsYouzhou?"S1  /  幽州":"黄巾骤起",tiny);
            string[] names={"草料","粮食","石料","铁器","金币","灵玉"};
            if(world.IsYouzhou){names[0]="木材";names[3]="铁矿";}
            double[] amounts={world.resources.grass,world.resources.grain,world.resources.stone,world.resources.iron,world.resources.gold,world.resources.jade};
            var rates=world.HourlyIncome();double[] income={rates.grass,rates.grain,rates.stone,rates.iron,rates.gold,0};
            float cell=(width-302)/6;
            for(int i=0;i<6;i++)
            {
                float x=285+i*cell;
                ResourceIcon(new Rect(x,22,28,28),i);
                Label(x+39,9,cell-47,19,names[i],tiny);
                Label(x+38,30,cell-45,31,Math.Floor(amounts[i]).ToString("N0"),new GUIStyle(cardName){fontSize=23});
                var hover=new Rect(x,0,cell-8,69);
                if(hover.Contains(Event.current.mousePosition))GUI.Label(new Rect(x,71,cell+22,28),i<5?"+"+income[i].ToString("0")+" / 小时":"试玩灵玉 · 无充值",small);
            }
            Block(new Rect(0,0,width,70));
        }
        private void Footer()
        {
            if(world.IsYouzhou&&(tab=="领地"||tab=="主城")){DrawYouzhouMapFooter();return;}
            RectFill(new Rect(0,height-78,width,78),Ink);RectFill(new Rect(0,height-78,width,1),Border);
            if(Button(new Rect(24,height-59,102,40),"保存"))Save(true);
            if(Button(new Rect(138,height-59,112,40),"主城",tab=="主城")){if(tab=="战斗等待"||watchingCurrentBattle)CloseCurrentBattle();if(world.IsYouzhou)OpenCity();else{tab="领地";map.CenterHome();}}
            RecordReviewTarget("city-open",new Rect(138,height-59,112,40));
            string[] tabs={"领地","武将","招募","战报","规则"};
            float navWidth=680, navX=(width-navWidth)/2;
            RecordReviewTarget("map-tab",new Rect(navX,height-65,126,51));
            RecordReviewTarget("recruit-tab",new Rect(navX+272,height-65,126,51));
            RecordReviewTarget("roster-tab",new Rect(navX+136,height-65,126,51));
            RecordReviewTarget("reports-tab",new Rect(navX+408,height-65,126,51));
            for(int i=0;i<tabs.Length;i++)
                if(Button(new Rect(navX+i*136,height-65,126,51),tabs[i]=="规则"?"帮助":tabs[i],tab==tabs[i])){if(tab=="战斗等待"||watchingCurrentBattle)CloseCurrentBattle();ClearCityQuestLandOrigin();tab=tabs[i];drawResult=null;trainingEditingFormation=false;cityEditingFormation=false;reinforcementEditingFormation=false;replayPlaying=false;}
            if(tab=="战斗回放")Label(width-222,height-51,198,40,"回放中 · 推演暂停",small);
            else
            {
                if(Button(new Rect(width-222,height-59,198,40),speed==0?"时间暂停 · 继续":"时间  ×"+speed))speed=speed==1?20:speed==20?60:speed==60?0:1;
                RecordReviewTarget("speed-cycle",new Rect(width-222,height-59,198,40));
            }
            Block(new Rect(0,height-78,width,78));
        }
        private void StatusToast()
        {
            if(status!=toastObservedStatus){toastObservedStatus=status;toastStartedAt=Time.unscaledTime;}
            if(!string.IsNullOrEmpty(status)&&Time.unscaledTime-toastStartedAt<6f)
            {
                var r=new Rect((width-600)/2,height-115,600,28);
                RectFill(r,new Color(.05f,.09f,.15f,.91f));
                GUI.Label(new Rect(r.x+12,r.y+4,r.width-24,24),status,small);Block(r);
            }
        }
        private void MapHud()
        {
            if(world.IsYouzhou){DrawYouzhouArmyHud();return;}
            Rect crest=new Rect(20,89,268,70);Block(crest);Frame(crest,Ink);
            Frame(new Rect(30,99,48,50),Red);GUI.Label(new Rect(30,99,48,50),"城",new GUIStyle(heading){alignment=TextAnchor.MiddleCenter});
            Label(90,99,180,29,world.ruler,cardName);
            Label(91,132,180,22,(world.IsYouzhou?"幽州 · 野外":world.RegionName(world.birthRegion)+" · 领地")+" "+world.OwnedCount,small);
            if(GUI.Button(crest,GUIContent.none,GUIStyle.none)){if(world.IsYouzhou)OpenCity();else map.CenterHome();}
            Rect army=new Rect(20,height-275,405,177);Block(army);Frame(army,Ink);
            RectFill(new Rect(army.x,army.y,3,army.height),Green);
            Label(34,army.y+12,205,26,"第一部队",cardName);
            Label(236,army.y+18,175,24,world.troops+" / "+world.TroopCap+" 兵",small);
            var generals=world.ActiveGenerals;
            for(int i=0;i<generals.Count;i++)
            {
                var c=generals[i];float x=34+i*88;
                PortraitHead(new Rect(x,army.y+52,64,64),c.definition);
                Label(x,army.y+127,82,23,c.Definition.name,tiny);
                if(GUI.Button(new Rect(x,army.y+52,78,97),GUIContent.none,GUIStyle.none))ShowCardDetails(c.definition,c.uid);
            }
            var reinforce = new Rect(306,army.y+53,104,38);
            if(world.IsYouzhou)
            { RecordReviewTarget("army-reinforcement",reinforce);if(Button(reinforce,"进入征兵"))OpenReinforcement(); }
            else if(Button(reinforce,"征兵 +200",false,world.order==null))Act(world.Recruit(200));
            if(world.order!=null){RecordReviewTarget("army-recall",new Rect(306,army.y+102,104,36));if(Button(new Rect(306,army.y+102,104,36),"撤回部队",false,!world.order.returning))Act(world.RecallArmy());}
            else if(Button(new Rect(306,army.y+102,104,36),"调整编队"))tab="武将";
            Label(34,army.y+151,367,22,world.ArmyState+(world.order==null?(world.IsYouzhou?"  ·  政令 "+world.AvailableCommands+"/20":""):(world.order.phase==1?"":"  ·  "+Math.Ceiling(world.order.remaining)+" 秒")),tiny);
            if(world.IsYouzhou && TrainingActive)
            {
                var progressEntry=new Rect(34,army.y+149,367,26);RecordReviewTarget("army-training-progress",progressEntry);
                if(GUI.Button(progressEntry,GUIContent.none,GUIStyle.none))OpenTraining(world.order.target);
            }
            DrawBattleWatchEntry(new Rect(20,army.y-46,405,38));
        }
        private void MapLabels()
        {
            if(world.IsYouzhou){DrawDesignedMapCityLabels();return;}
            cityLabels.Clear();
            var occupied=new List<Rect>();
            foreach(var t in world.lands.Where(t=>t.capital||t.home).OrderByDescending(t=>t.home))
            {
                Vector3 p=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(t)+Vector3.up*.5f);
                Vector2 logical=ScreenPointToGui(p);float x=logical.x,y=logical.y-24;
                var r=new Rect(x-62,y-16,124,29);
                if(p.z<0||r.x<0||r.xMax>width||r.y<82||r.yMax>height-119||occupied.Any(q=>q.Overlaps(r)))continue;
                if(hudBlocks.Any(q=>q.Overlaps(GuiRectToPixels(r))))continue;
                occupied.Add(r);cityLabels.Add(r);Block(r);Frame(r,t.owned?C("244B51"):Ink);
                RecordReviewTarget("city-label-"+t.id,r);
                if(t.capital&&!t.home)RecordReviewTarget("capital-label-"+t.id,r);
                GUI.Label(r,t.name+(t.home?" · 主城":""),new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
                if(GUI.Button(r,GUIContent.none,GUIStyle.none))
                {map.selected=t.id;if(t.home&&world.IsYouzhou)OpenCity(t.id);}
            }
        }
        private Rect RightPanel(string name)
        {
            var r=new Rect(width-340,100,308,height-218);Block(r);Frame(r,Ink);
            RectFill(new Rect(r.x+18,r.y+20,3,25),Red);
            Label(r.x+32,r.y+18,246,35,name,heading);
            RectFill(new Rect(r.x+20,r.y+65,268,1),Border);return r;
        }
        private void LandPanel()
        {
            if(map.selected<0)return;
            if(world.IsYouzhou){DrawYouzhouLandPanel(world.lands[map.selected]);return;}
            var t=world.lands[map.selected];string landName=world.IsYouzhou&&t.kind==LandKind.Pasture?"林地":Catalog.LandName(t.kind);var r=RightPanel(t.home||t.capital||t.bridge?t.name:landName);Block(r);float x=r.x+22,y=r.y+90;
            if(Button(new Rect(r.xMax-49,r.y+16,30,30),"×"))map.selected=-1;
            RecordReviewTarget("map-close",new Rect(r.xMax-49,r.y+16,30,30));
            RecordReviewTarget("land-close",new Rect(r.xMax-49,r.y+16,30,30));
            bool district=world.IsYouzhou&&world.IsCityDistrict(t.id);
            Label(x,y,262,30,(district?"主城城区":t.capital?"战略城池":t.bridge?"桥梁通道":landName)+"    Lv."+t.level,cardName);
            Label(x,y+43,262,28,world.RegionName(t.region)+"  /  "+t.x+", "+t.z,small);
            RectFill(new Rect(x,y+91,264,4),t.owned?Green:Red);
            Label(x,y+110,126,23,t.owned?"归属":"守军兵力",small);
            Label(x,y+140,264,39,t.owned?"己方领地":world.Defenders(t).ToString("N0"),heading);
            string resource=t.kind==LandKind.Grain?"粮食":t.kind==LandKind.Pasture?"草料":t.kind==LandKind.Stone?"石料":"铁器";
            if(world.IsYouzhou&&t.kind==LandKind.Pasture)resource="木材";
            if(world.IsYouzhou&&t.kind==LandKind.Iron)resource="铁矿";
            Label(x,y+213,264,86,district?"九格城区 · 可建设设施":t.capital?"占领增益\n全资源产出 +15%":t.bridge?"连接两岸 · 可通行":t.Walkable?"每小时  +"+t.level*180+" "+resource:"不可通行",body);
            if(district)
            {if(Button(new Rect(x,r.yMax-130,264,46),"进入主城",true))OpenCity(t.id);Label(x,r.yMax-67,264,45,"建设 · 征兵 · 城建任务",small);return;}
            if(world.IsYouzhou&&t.owned&&!t.home){S1LandActions(r,t);return;}
            string problem=world.AttackProblem(t.id);
            RecordReviewTarget("army-attack",new Rect(x,r.yMax-130,264,46));
            if(t.home)
            {if(Button(new Rect(x,r.yMax-130,264,46),"征兵 +200",true,world.order==null))Act(world.Recruit(200));}
            else if(Button(new Rect(x,r.yMax-130,264,46),t.capital?"派兵攻城":"派兵占领",true,problem.Length==0))BeginAttack(t.id);
            Label(x,r.yMax-67,264,55,t.home?"培养武将可以提高带兵上限。":problem.Length>0?problem:"出征消耗 30 粮食、20 草料。",small);
        }
        private void Portrait(Rect r,string id)
        {
            Texture2D texture;portraits.TryGetValue(id,out texture);
            if(texture!=null)GUI.DrawTexture(r,texture,ScaleMode.ScaleAndCrop,true);
            else
            {
                RectFill(r,C("333d39"));Outline(new Rect(r.x+8,r.y+8,r.width-16,r.height-16),Border);
                var g=Catalog.Generals.Find(d=>d.id==id);
                GUI.Label(new Rect(r.x,r.y+r.height*.3f,r.width,r.height*.35f),g==null?"将":g.name.Substring(0,1),new GUIStyle(display){alignment=TextAnchor.MiddleCenter,fontSize=Mathf.Clamp(Mathf.RoundToInt(r.width*.28f),20,76)});
                if(r.height>150)GUI.Label(new Rect(r.x,r.y+r.height*.63f,r.width,26),"待绘制",new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter});
            }
        }
        private void PortraitHead(Rect r,string id)
        {
            Texture2D texture;portraits.TryGetValue(id,out texture);
            if(texture==null){Portrait(r,id);return;}
            // A head-and-shoulders crop; UVs are measured from the bottom of the source image.
            float sourceWidth=.64f,sourceHeight=sourceWidth*texture.width/texture.height*r.height/r.width;
            GUI.DrawTextureWithTexCoords(r,texture,new Rect(.18f,1-.065f-sourceHeight,sourceWidth,sourceHeight));
        }
        private void PortraitComplete(Rect r,string id)
        {
            Texture2D texture;portraits.TryGetValue(id,out texture);
            if(texture==null){Portrait(r,id);return;}
            GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit,true);
        }
        private void PortraitBanner(Rect r,string id)
        {
            Texture2D texture;portraits.TryGetValue(id,out texture);
            if(texture==null){Portrait(r,id);return;}
            float aspect=texture.width/(float)texture.height,viewAspect=r.width/r.height;
            if(viewAspect>aspect)
            {
                float croppedHeight=aspect/viewAspect;
                GUI.DrawTextureWithTexCoords(r,texture,new Rect(0,1-croppedHeight,1,croppedHeight));
            }
            else Portrait(r,id);
        }
        private bool GeneralTile(Rect r,GeneralCard c,bool selected,bool interactive=true)
        {
            Portrait(r,c.definition);GUI.DrawTexture(new Rect(r.x,r.yMax-70,r.width,70),shade);
            Outline(r,selected?Gold:C("7c6854"),selected?2:1);
            Outline(new Rect(r.x+4,r.y+4,r.width-8,r.height-8),new Color(.78f,.65f,.4f,.38f));
            RectFill(new Rect(r.x+9,r.y+10,31,30),Red);
            GUI.Label(new Rect(r.x+9,r.y+10,31,30),c.Definition.s1?c.Definition.faction:c.Definition.homeland==Homeland.Han?"汉":c.Definition.homeland==Homeland.Steppe?"原":"辽",centered);
            var stars=new GUIStyle(small){fontSize=17,alignment=TextAnchor.UpperRight};stars.normal.textColor=Gold;
            RectFill(new Rect(r.x+48,r.y+10,r.width-60,25),new Color(.07f,.07f,.06f,.5f));
            GUI.Label(new Rect(r.x+48,r.y+12,r.width-64,24),new string('★',c.Definition.stars),stars);
            RectFill(new Rect(r.x+9,r.y+47,33,c.Definition.name.Length*29+10),new Color(.08f,.085f,.075f,.74f));
            for(int i=0;i<c.Definition.name.Length;i++)GUI.Label(new Rect(r.x+11,r.y+51+i*29,29,31),c.Definition.name[i].ToString(),cardName);
            if(world!=null&&world.formation.Contains(c.uid))
            {RectFill(new Rect(r.xMax-60,r.yMax-61,47,23),Red);GUI.Label(new Rect(r.xMax-60,r.yMax-61,47,23),"上阵",new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter});}
            Label(r.x+13,r.yMax-28,r.width-26,23,"Lv."+c.level+" / "+c.LevelCap+"     "+c.rank+" / "+c.Definition.stars+"阶",small);
            return interactive&&GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        private void Roster()
        {
            if(world.IsYouzhou){DrawOwnedRoster();return;}
            Label(main.x,main.y+2,main.width,40,"帐下武将",title);
            Label(main.x+220,main.y+22,main.width-(trainingEditingFormation||cityEditingFormation||reinforcementEditingFormation?410:220),24,world.cards.Count+" 位武将  ·  点击卡面查看详情",small);
            if(reinforcementEditingFormation)
            {
                var reinforcementBack=new Rect(main.xMax-176,main.y+8,176,38);RecordReviewTarget("reinforcement-formation-back",reinforcementBack);
                if(Button(reinforcementBack,"返回征兵"))ResumeReinforcement();
            }
            else if(trainingEditingFormation)
            {
                var trainingBack=new Rect(main.xMax-176,main.y+8,176,38);RecordReviewTarget("training-formation-back",trainingBack);
                if(Button(trainingBack,"返回练兵"))ResumeTraining();
            }
            else if(cityEditingFormation)
            {
                var cityBack=new Rect(main.xMax-176,main.y+8,176,38);RecordReviewTarget("city-formation-back",cityBack);
                if(Button(cityBack,cityPage=="quest-detail"?"返回任务目标":"返回主城"))ReturnToCityFormationOrigin();
            }
            var formation=new Rect(main.x,main.y+68,main.width,94);Frame(formation,Panel);
            var active=world.ActiveGenerals;
            float slotWidth=formation.width/3;
            for(int i=0;i<3;i++)
            {
                float x=formation.x+i*slotWidth+12;
                if(i<active.Count)
                {
                    var c=active[i];PortraitHead(new Rect(x,formation.y+17,60,60),c.definition);
                    Label(x+64,formation.y+14,slotWidth-90,29,Slots[i]+"  "+c.Definition.name,cardName);
                    Label(x+65,formation.y+53,slotWidth-90,23,"Lv."+c.level+"   "+c.Definition.skill,small);
                    if(GUI.Button(new Rect(x,formation.y,slotWidth-18,94),GUIContent.none,GUIStyle.none)){selectedCard=c.uid;selectedSlot=i;}
                }
            }
            float cardW=204,cardH=306,gap=24;
            int columns=Mathf.Max(1,Mathf.FloorToInt((main.width-24+gap)/(cardW+gap)));
            var area=new Rect(main.x,main.y+188,main.width,main.height-188);
            int rows=Mathf.CeilToInt(world.cards.Count/(float)columns);
            rosterScroll=BeginContentScroll(area,rosterScroll,new Rect(0,0,area.width-24,rows*(cardH+36)));
            int index=0;
            foreach(var c in world.cards.OrderByDescending(g=>g.Definition.stars).ThenBy(g=>g.uid))
            {
                var r=new Rect(index%columns*(cardW+gap),index/columns*(cardH+36),cardW,cardH);
                if(index==0&&r.yMax>=rosterScroll.y)RecordReviewTarget("owned-card-first",new Rect(area.x+r.x,area.y+r.y-rosterScroll.y,r.width,r.height));
                if(DefinitionTile(r,c.Definition,selectedCard==c.uid,c))ShowCardDetails(c.definition,c.uid);
                Label(r.x,r.yMax+7,r.width,22,c.Definition.skill,tiny);index++;
            }
            GUI.EndScrollView(GUI.enabled);
        }
        private void GeneralPanel()
        {
            if(world.IsYouzhou){DrawOwnedGeneralPanel();return;}
            var c=world.cards.Find(g=>g.uid==selectedCard);if(c==null){c=world.cards[0];selectedCard=c.uid;}
            Rect r=RightPanel("编队与培养");
            var area=new Rect(r.x+20,r.y+82,268,r.height-100);
            generalScroll=BeginContentScroll(area,generalScroll,new Rect(0,0,246,570+world.learnedSkills.Count*41));
            Label(0,0,178,35,c.Definition.name,heading);
            if(Button(new Rect(178,0,68,32),"详情"))ShowCardDetails(c.definition,c.uid);
            Label(0,43,246,28,new string('★',c.Definition.stars)+"   "+c.rank+" / "+c.Definition.stars+"阶",body);
            Label(0,81,246,25,"Lv."+c.level+" / "+c.LevelCap+"    经验 "+c.experience,small);
            Label(0,120,246,62,c.Definition.s1?c.Definition.faction+" · "+c.Definition.troop+"兵 · COST "+c.Definition.cost.ToString("0.0")+"\n原始攻击 "+c.Definition.attack+" · 防御 "+c.Definition.defense:c.Definition.description,muted);
            RectFill(new Rect(0,193,246,1),Border);
            Label(0,211,246,64,"自带战法  "+c.Definition.skill+"\n学习战法  "+(string.IsNullOrEmpty(c.learnedSkill)?"尚未学习":c.learnedSkill),body);
            for(int i=0;i<3;i++)if(Button(new Rect(i*84,294,78,34),Slots[i],selectedSlot==i))selectedSlot=i;
            bool assigned=world.formation.Contains(c.uid),marching=assigned&&world.order!=null;
            bool current=world.formation.Count>selectedSlot&&world.formation[selectedSlot]==c.uid;
            string assignText=world.order!=null?"行军中 · 暂不可换将":current?"已任"+Slots[selectedSlot]:"任命为"+Slots[selectedSlot];
            if(Button(new Rect(0,339,246,42),assignText,true,world.order==null&&!current))Act(world.Assign(c.uid,selectedSlot));
            bool canTrain=!marching&&c.level<c.LevelCap&&world.resources.grain>=200&&world.resources.gold>=60;
            if(393>=generalScroll.y&&431<=generalScroll.y+area.height)
            {
                RecordReviewTarget("general-training",new Rect(area.x,area.y+393-generalScroll.y,246,38));
                RecordReviewTarget("owned-card-training",new Rect(area.x,area.y+393-generalScroll.y,246,38));
            }
            if(world.IsYouzhou)
            {
                if(Button(new Rect(0,393,246,38),"部队练兵  ›"))OpenTraining();
                Label(0,437,246,24,"选择己方土地 · 出发前确认收益",tiny);
            }
            else
            {
                if(Button(new Rect(0,393,246,38),marching?"出征中 · 暂不可演武":c.level>=c.LevelCap?"已达等级上限":"演武  +240 经验",false,canTrain))Act(world.Train(c.uid));
                Label(0,437,246,24,"消耗 200 粮食 · 60 金币",tiny);
            }
            int materials=world.cards.Count(g=>g.uid!=c.uid&&g.definition==c.definition&&g.rank==0&&!world.formation.Contains(g.uid));
            string promotion=c.rank>=c.Definition.stars?"已达进阶上限":marching?"出征中 · 暂不可进阶":"同名卡 "+materials+"/1 · 进阶 +1";
            if(Button(new Rect(0,473,246,38),promotion,false,!marching&&c.rank<c.Definition.stars&&materials>0))Ask("消耗一张未上阵、未进阶的同名同星卡，为"+c.Definition.name+"提升一阶。",()=>Act(world.Promote(c.uid)));
            bool known=world.learnedSkills.Contains(c.Definition.skill);
            string dismantle=assigned?"已上阵 · 换下后可拆解":known?"此战法已解锁":"拆解为「"+c.Definition.skill+"」";
            if(c.Definition.s1)dismantle="拆解战法尚未开放";
            if(Button(new Rect(0,523,246,36),dismantle,false,!assigned&&!known&&!c.Definition.s1))Ask("拆解将永久消耗编号 "+c.uid+" 的"+c.Definition.name+"，解锁「"+c.Definition.skill+"」。",()=>Act(world.Dismantle(c.uid)));
            for(int i=0;i<world.learnedSkills.Count;i++)
            {string skill=world.learnedSkills[i];if(Button(new Rect(0,574+i*41,246,33),"学习"+skill+" · 100金币"))Act(world.Teach(c.uid,skill));}
            GUI.EndScrollView(GUI.enabled);
        }
        private void Recruitment()
        {
            Label(main.x,main.y+2,main.width,56,"访贤求将",title);
            if(world.IsYouzhou)
            {
                if(Button(new Rect(main.x+main.width-190,main.y+9,190,40),"S1 武将图鉴")){s1Archives=true;tab="图鉴";}
                Label(main.x,main.y+72,main.width,28,"选择阵营  ·  访求良将",small);
                FactionButtons(new Rect(main.x,main.y+104,main.width,34));
            }
            else Label(main.x,main.y+72,main.width,49,world.allPoolsUnlocked?"山河相通 · 全区域武将已开放":"当前开放"+Catalog.OriginName(world.origin)+"人物 · 攻占跨区默认城池可解锁全区域",small);
            float w=(main.width-26)/2;
            DrawPool(new Rect(main.x,main.y+145,w,main.height-145),false);
            DrawPool(new Rect(main.x+w+26,main.y+145,w,main.height-145),true);
        }
        private void DrawPool(Rect r,bool premium)
        {
            Frame(r,Panel);
            var art=new Rect(r.x+1,r.y+55,r.width-2,r.height-225);
            var banner=world.IsYouzhou?world.S1Pool(s1Faction).FirstOrDefault(g=>premium?g.stars==5:g.stars==4):null;
            RectFill(art,new Color(.04f,.07f,.12f,1));
            PortraitComplete(new Rect(art.x+12,art.y+10,art.width-24,art.height-82),banner==null?(premium?"huangfu_song":"han_officer"):banner.id);
            GUI.DrawTexture(new Rect(art.x,art.yMax-80,art.width,80),shade);
            Label(r.x+24,r.y+12,r.width-48,32,premium?"名 将 · 访 求":"乡 里 · 招 募",heading);
            Label(r.x+24,art.yMax-67,r.width-48,39,premium?"天下英杰，待君而来":"聚乡勇，立根基",cardName);
            Label(r.x+24,art.yMax+16,r.width-48,27,world.IsYouzhou?(premium?"4—5 星  /  100 灵玉":"4 星试玩  /  200 金币"):(premium?"3—5 星  /  100 灵玉":"1—3 星  /  200 金币"),body);
            Label(r.x+24,art.yMax+52,r.width-48,40,world.IsYouzhou?"池内等概率 · 试玩招募":premium?"3星60% · 4星30% · 5星10%":"1星55% · 2星35% · 3星10%",tiny);
            bool poolAvailable=!world.IsYouzhou||world.S1Pool(s1Faction).Any(g=>premium?g.stars>=4:g.stars==4);
            bool affordable=(premium?world.resources.jade>=100:world.resources.gold>=200)&&poolAvailable;
            if(premium)RecordReviewTarget("draw-premium",new Rect(r.x+24,r.yMax-65,r.width-48,44));
            if(Button(new Rect(r.x+24,r.yMax-65,r.width-48,44),!poolAvailable?"该阵营暂无可招募卡":affordable?"招募一次":premium?"灵玉不足":"金币不足",premium,affordable))
            {
                int before=world.cards.Count;Act(world.IsYouzhou?world.DrawS1(premium,s1Faction,rng):world.Draw(premium,rng));
                if(world.cards.Count>before)drawResult=world.cards[world.cards.Count-1];
            }
        }
        private void RecruitmentPanel()
        {
            var r=RightPanel("可招募武将");
            var area=new Rect(r.x+16,r.y+83,276,r.height-178);
            var list=world.IsYouzhou?world.S1Pool(s1Faction):Catalog.Generals.Where(g=>!g.s1&&(world.allPoolsUnlocked||g.homeland==world.origin)).OrderByDescending(g=>g.stars).ToList();
            poolScroll=BeginContentScroll(area,poolScroll,new Rect(0,0,254,Mathf.CeilToInt(list.Count/2f)*199));
            for(int i=0;i<list.Count;i++)
            {
                Rect card=new Rect(i%2*131,i/2*199,122,183);
                // Invisible items still allocate their one interaction control, while artwork stays culled.
                if(card.yMax<poolScroll.y||card.y>poolScroll.y+area.height){GUI.Button(card,GUIContent.none,GUIStyle.none);continue;}
                if(i==0)RecordReviewTarget("pool-card-first",new Rect(area.x+card.x,area.y+card.y-poolScroll.y,card.width,card.height));
                if(DefinitionTile(card,list[i],false))ShowCardDetails(list[i].id);
            }
            GUI.EndScrollView(GUI.enabled);
            if(list.Count==0)Label(area.x+8,area.y+28,area.width-20,80,"该阵营暂无可招募武将。",muted);
            RecordReviewTarget("card-library",new Rect(r.x+20,r.yMax-79,268,40));
            if(world.IsYouzhou&&Button(new Rect(r.x+20,r.yMax-79,268,40),"浏览全部卡面  ·  "+list.Count+"张",true))
            {s1Archives=false;s1Scroll=Vector2.zero;s1Search="";tab="图鉴";}
            Label(r.x+20,r.yMax-30,268,22,"点击卡面查看属性与战法",tiny);
        }
        private void DrawResult()
        {
            RectFill(new Rect(0,0,width,height),new Color(.025f,.035f,.03f,.94f));
            var c=drawResult;
            Label(width/2-220,70,440,55,"贤才入帐",title);
            var r=new Rect(width/2-160,147,320,480);GeneralTile(r,c,false,false);
            if(GUI.Button(r,GUIContent.none,GUIStyle.none)){drawResult=null;ShowCardDetails(c.definition,c.uid);}
            GUI.Label(new Rect(width/2-300,653,600,28),world.cards.Count(g=>g.definition==c.definition)>1?"同名武将 · 可用于进阶":"新获武将 · 已收入名册",centered);
            RecordReviewTarget("draw-close",new Rect(width/2-160,708,320,48));
            if(Button(new Rect(width/2-160,708,320,48),"收下军令",true))drawResult=null;
        }
        private void InformationPanel(string name,string description)
        {var r=RightPanel(name);Label(r.x+22,r.y+94,264,r.height-115,description,muted);}
        private void Help()
        {
            Label(main.x,main.y,main.width,60,"第一份军令",title);
            var area=new Rect(main.x,main.y+90,main.width,main.height-90);
            helpScroll=BeginContentScroll(area,helpScroll,new Rect(0,0,area.width-24,1000));
            string text="01  开疆\n选择主城旁的一级粮田，派兵占领。只能攻打与领地相邻的目标。\n\n02  整备\n资源随时间产出。征兵、演武升级，然后挑战更强的守军。部队战后返城。\n\n03  武将\n等级上限为星级 ×10，初始零阶。消耗同名同星卡进阶，最高阶数等于星级。\n\n04  战法\n拆解未上阵武将解锁技能，再支付金币学习。突击增伤、坚守减损、抚军恢复兵力。\n\n05  访贤\n金币招募1—3星、灵玉招募3—5星。攻占其他出生区域的默认城池后开放所有武将池。\n\n06  天下\n默认城池提供全资源加成。当前地图为九州、草原与东北的示意沙盘。\n\n操作：右键拖动、WASD移动、滚轮缩放、左键选择。每30秒自动保存。\n离线按真实时间结算，最多七日。底栏加速只影响本次在线推演。\n\n当前为单人视觉与玩法样板，数值和招募概率均非正式商业配置。\n完整160人卡池、商人、商盟贸易、同盟国家、玩家战争和太守车尚未实现。";
            if(world.IsYouzhou)text="01  开城建设\n点击主城或底栏主城进入九格城区，空地可建设设施，已有建筑可升级。左上任务追踪提供前往与领奖，城内主要事宜可查看当前章节。主城中心及周围八格属于城区，不能屯田。\n\n02  幽州开疆\n当前开放40×40幽州。上下左右与斜角都算相邻，选地查看青色路线，出征沿金色路径行进。山河需绕行、桥梁正向通过，不可穿过障碍拐角。\n\n03  驻守与屯田\n己方野外资源地可驻守或屯田。屯田花费2政令，抵达后作业90秒，完成结算并返城；取消不退令、不发未完成收益。政令上限20，每小时恢复1令。当前没有其他玩家入侵与驻军接敌模拟，时间和收益均为试玩配置。\n\n04  卡牌与阵营\n武将页点击已拥有卡牌查看详情，编队与培养提供上阵、演武和进阶。招募页的卡面与浏览全部卡面均可查看具体武将。图鉴收录245个资料身份，其中41张进入试玩池，204张仅档案；不代表现行官方完整S1卡池。金币抽四星、灵玉抽四至五星，池内等概率。\n\n05  属性与战法\n卡牌详情包含属性、成长、攻击距离、战法文字与人物列传。战法、组合、内政的逐条效果尚未实现，S1拆解暂不开放。资料来源与取得范围在详情资料页查阅。\n\n操作：左键或右键拖动、WASD移动、滚轮缩放、单击选地；鼠标移到地图视口边缘可滚动。详情按Esc返回。每30秒自动保存，离线最多结算七日。底栏加速仅影响本次在线时间。\n\n当前为单人原型。地图资源使用本地参考图，城池使用透明派生图；部分HUD符号为程序绘制。精确官方地理、完整S1规则与多部队仍需继续补齐。";
            Label(0,0,area.width-34,980,text,body);GUI.EndScrollView(GUI.enabled);
        }
        private void ConfirmDialog()
        {
            RectFill(new Rect(0,0,width,height),new Color(0,0,0,.76f));
            if(cityCancellation!=null){DrawCityCancellationDialog();return;}
            var r=new Rect((width-580)/2,(height-280)/2,580,280);Frame(r,Ink);
            Label(r.x+30,r.y+27,520,38,"确认军令",heading);Label(r.x+30,r.y+91,520,attackConfirmation?70:100,confirmText,body);
            if(attackConfirmation)
            {
                var watch=new Rect(r.x+30,r.y+160,520,36);RecordReviewTarget("attack-watch-toggle",watch);
                if(Button(watch,(watchAttackOnArrival?"✓ ":"○ ")+"抵达后观看本场战斗过程",watchAttackOnArrival))watchAttackOnArrival=!watchAttackOnArrival;
            }
            RecordReviewTarget("confirm-yes",new Rect(r.x+30,r.y+208,250,43));
            RecordReviewTarget("confirm-no",new Rect(r.x+300,r.y+208,250,43));
            if(tab=="练兵")RecordReviewTarget("training-start",new Rect(r.x+30,r.y+208,250,43));
            if(Button(new Rect(r.x+30,r.y+208,250,43),"确认",true)){Action action=confirmation;confirmation=null;action();}
            if(Button(new Rect(r.x+300,r.y+208,250,43),"取消"))confirmation=null;
        }
    }
}
