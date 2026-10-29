using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private int citySelectedPlot=4;
        private CityBuilding citySelectedBuilding=CityBuilding.Mill;
        private string cityPage="overview";
        private string cityFacilitiesOrigin="overview";
        private bool cityEditingFormation;
        private Vector2 cityDetailScroll;
        private Rect cityHudIdentityArea,cityHudArtArea,cityHudNameArea;
        private string cityHudDisplayedName,cityHudShortName;
        private int cityHudDisplayedLevel;
        private Rect cityNavigationDockArea,cityContentArea;
        private bool CityPanelOpen { get { return tab=="主城"; } }

        private void OpenCity(int landId=-1)
        {
            if(world==null||!world.IsYouzhou)return;
            ClearCityQuestLandOrigin();
            trainingEditingFormation=false;cityEditingFormation=false;reinforcementEditingFormation=false;
            world.EnsureCityState();tab="主城";cityPage="overview";cityDetailScroll=Vector2.zero;
            cityFacilitiesOrigin="overview";cityOverviewPlanning=false;ResetCityPanorama();
            var plot=world.CityPlotAt(landId<0?world.homeId:landId);citySelectedPlot=plot==null?4:plot.index;
            citySelectedBuilding=plot!=null&&plot.building!=CityBuilding.None?plot.building:DefaultCityBuilding();
        }
        private void OpenCityOverview() { OpenCity(); }
        private void OpenCityFacilities(int plotIndex=-1)
        {
            if(tab!="主城"||(cityPage!="overview"&&cityPage!="facilities")){cityOverviewPlanning=false;ResetCityPanorama();}
            CancelCityPanoramaPointer();
            if(tab!="主城"||cityPage!="facilities")cityFacilitiesOrigin=tab=="主城"&&(cityPage=="catalog"||cityPage=="construction"||cityPage=="quest-detail")?cityPage:"overview";
            world.EnsureCityState();world.RecordCityAction("visit");
            tab="主城";cityPage="facilities";cityDetailScroll=Vector2.zero;map.CancelPointerInput();
            if(plotIndex>=0&&plotIndex<9)citySelectedPlot=plotIndex;
            var plot=world.cityPlots[citySelectedPlot];
            citySelectedBuilding=world.CityJob!=null&&world.CityJob.plotIndex==citySelectedPlot?world.CityJob.building:plot.building==CityBuilding.None?DefaultCityBuilding():plot.building;
        }
        private void OpenCityQuests()
        {
            ClearCityQuestLandOrigin();
            cityOverviewPlanning=false;ResetCityPanorama();
            world.EnsureCityState();tab="主城";cityPage="quests";cityQuestCompleted=false;
            cityQuestChapter=CityQuestChapter(world.CurrentCityQuest==null?10:world.CurrentCityQuest.id);
            cityQuestScroll=Vector2.zero;cityQuestFocusPending=true;map.CancelPointerInput();
        }
        private void ReturnFromCityPage()
        { if(cityPage=="quest-detail"){ReturnFromCityQuestObjective();return;}if(cityPage=="overview"){if(cityOverviewPlanning)cityOverviewPlanning=false;else if(cityOverviewExpanded)SetCityPanoramaExpanded(false);else tab="领地";}else cityPage=cityPage=="facilities"?cityFacilitiesOrigin:"overview";CancelCityPanoramaPointer();map.CancelPointerInput(); }
        private void OpenCityFormation()
        { trainingEditingFormation=false;reinforcementEditingFormation=false;cityEditingFormation=true;tab="武将";map.CancelPointerInput(); }
        private void ReturnToCityFormationOrigin()
        { cityEditingFormation=false;tab="主城";map.CancelPointerInput(); }

        private void DrawCityHudHeader()
        {
            DrawDesignedCityHeader();
        }

        private void DrawCityPanel()
        {
            world.EnsureCityState();
            InitMapHudStyles();
            if(cityPage=="overview")
            {
                cityNavigationDockArea=CityLayoutRect(CityOverviewLayout.Dock(width,height));
                cityContentArea=CityLayoutRect(CityOverviewLayout.Content(width,height));
                Block(cityNavigationDockArea);Block(cityContentArea);
                DrawCityNavigationDock();
                if(tab=="主城"&&cityPage=="overview")DrawCityOverview(cityContentArea);
                return;
            }
            cityNavigationDockArea=default(Rect);
            var area=new Rect(24,90,width-48,height-188);Block(area);
            string pageTitle=cityPage=="overview"?"城务总览":cityPage=="catalog"?"设施图谱":cityPage=="facilities"?"城区营建":cityPage=="construction"?"建设进度":cityPage=="quest-detail"?"事宜目标":"主要事宜";
            HudFittedLabel(new Rect(area.x+4,area.y,area.width-740,26),world.lands[world.homeId].name,heading,16);
            Label(area.x+5,area.y+27,area.width-740,19,"主城  /  "+(cityPage=="overview"?"全景与部队":pageTitle),hudSmall);
            var catalog=new Rect(area.xMax-604,area.y,110,44);RecordCityOverviewTarget("city-catalog",catalog);
            if(CityNavigationAction(catalog,"设施一览","catalog",cityPage=="catalog"))OpenCityFacilityCatalog();
            var overview=new Rect(area.xMax-724,area.y,110,44);RecordCityOverviewTarget("city-overview",overview);
            if(CityNavigationAction(overview,"城内全景","overview",cityPage=="overview")){cityPage="overview";map.CancelPointerInput();}
            var facilities=new Rect(area.xMax-484,area.y,110,44);RecordCityOverviewTarget("city-facilities",facilities);
            if(CityNavigationAction(facilities,"城区管理","facilities",cityPage=="facilities")){OpenCityFacilities();cityFacilitiesOrigin="overview";}
            var quests=new Rect(area.xMax-364,area.y,110,44);RecordCityOverviewTarget("city-quests",quests);
            if(CityNavigationAction(quests,"主要事宜","quests",cityPage=="quests"||cityPage=="quest-detail"))OpenCityQuests();
            var trainingEntry=new Rect(area.xMax-244,area.y,110,44);RecordCityOverviewTarget("city-training",trainingEntry);
            if(CityNavigationAction(trainingEntry,"部队练兵","training",false))OpenTraining();
            var back=new Rect(area.xMax-124,area.y,124,44);RecordCityOverviewTarget("city-back",back);
            if(HudTextAction(back,cityPage=="quest-detail"?"返回任务列表":cityPage=="facilities"&&cityFacilitiesOrigin=="quest-detail"?"返回任务目标":cityPage=="overview"?(cityOverviewPlanning?"结束选址":cityOverviewExpanded?"收起城景":"返回领地"):cityPage=="facilities"&&cityFacilitiesOrigin=="catalog"?"返回设施一览":cityPage=="facilities"&&cityFacilitiesOrigin=="construction"?"返回建设进度":"返回主城"))ReturnFromCityPage();
            var content=new Rect(area.x,area.y+52,area.width,area.height-52);
            cityContentArea=content;
            if(cityPage=="overview"){DrawCityOverview(content);return;}
            if(cityPage=="quests"){DrawCityQuestPage(content);return;}
            if(cityPage=="quest-detail"){DrawCityQuestObjective(content);return;}
            if(cityPage=="catalog"){DrawCityFacilityCatalog(content);return;}
            if(cityPage=="construction"){DrawCityConstructionPage(content);return;}
            float detailWidth=area.width*.66f;
            var district=new Rect(area.x,area.y+52,area.width-detailWidth-18,area.height-52);
            var detail=new Rect(district.xMax+18,district.y,detailWidth,district.height);
            HudSurface(district);HudSurface(detail);
            DrawCityFacilityPortrait(CityGalleryRect(district,CityFacilityGalleryLayout.Stage(district.width,district.height)));
            GUI.Label(CityGalleryRect(district,CityFacilityGalleryLayout.LocatorHeading(district.width,district.height)),"城 区 定 位  ·  九格占地",hudSmall);
            DrawCityDistrictGrid(CityGalleryRect(district,CityFacilityGalleryLayout.Grid(district.width,district.height)));
            DrawCityFacilityQueue(new Rect(district.x+16,district.yMax-100,district.width-32,88));
            DrawCityBuildingPanel(detail);
        }
        private static Rect CityLayoutRect(CityPanoramaBounds bounds)
        { return new Rect(bounds.X,bounds.Y,bounds.Width,bounds.Height); }

        private void DrawCityNavigationDock()
        {
            InitCityDockDesign();
            cityDockTextFits=true;
            string[] pages={"overview","catalog","facilities","quests","training"};
            string[] titles={"城内全景","设施一览","城区管理","主要事宜","部队练兵"};
            for(int index=0;index<pages.Length;index++)
            {
                var rect=CityLayoutRect(CityOverviewLayout.Navigation(width,height,index));
                string page=pages[index];
                RecordCityOverviewTarget("city-"+page,rect);
                if(!CityDockNavigationAction(rect,titles[index],page,page=="overview"))continue;
                if(page=="overview"){cityPage="overview";map.CancelPointerInput();}
                else if(page=="catalog")OpenCityFacilityCatalog();
                else if(page=="facilities"){OpenCityFacilities();cityFacilitiesOrigin="overview";}
                else if(page=="quests")OpenCityQuests();
                else OpenTraining();
            }
            var back=CityLayoutRect(CityOverviewLayout.Back(width,height));
            RecordCityOverviewTarget("city-back",back);
            if(CityDockBackAction(back,cityOverviewPlanning?"结束选址":cityOverviewExpanded?"收起城景":"返回领地"))ReturnFromCityPage();
        }

        private bool CityNavigationAction(Rect area,string title,string page,bool selected)
        {
            bool hover=GUI.enabled&&area.Contains(Event.current.mousePosition);
            if(selected||hover)RectFill(area,new Color(.30f,.26f,.18f,selected?.8f:.4f));
            RectFill(new Rect(area.x+5,area.yMax-2,area.width-10,selected?2:1),selected?Gold:Border*.7f);
            var art=new Rect(area.x+4,area.y+6,28,30);
            DrawCityNavigationArt(art,page,false);
            var caption=new Rect(area.x+36,area.y+1,area.width-38,40);
            GUI.Label(caption,title,new GUIStyle(hudCaption){fontSize=16});
            RecordCityOverviewTarget("city-nav-"+page+"-art",art);
            RecordCityOverviewTarget("city-nav-"+page+"-caption",caption);
            return ContentTileClicked(area,"city-nav-"+page,-1);
        }

        private void DrawCityNavigationArt(Rect art,string page,bool dock)
        {
            if(page=="overview")DrawHudNavigationArt(art,5);
            else if(page=="catalog")DrawCityFacilityArt(art,CityBuilding.TownHall);
            else if(page=="training")DrawCityFacilityArt(art,CityBuilding.DrillGround);
            else if(page=="quests")DrawCityDocumentArt(art);
            else
            {
                // Nine plots denote district management, not the interior scene.
                for(int row=0;row<3;row++)for(int col=0;col<3;col++)
                {
                    float unit=dock?12:9;
                    var tile=new Rect(art.center.x-unit*1.5f+col*unit,art.center.y-unit*1.5f+row*unit,unit-2,unit-2);
                    RectFill(tile,row==1&&col==1?Gold:C("667463"));
                }
            }
        }
        private void DrawCityDocumentArt(Rect area)
        {
            // A proportional scroll shared by the compact navigation and chapter
            // entry; the wide footer artwork uses fixed insets and cannot fit here.
            var paper=new Rect(area.x+area.width*.19f,area.y+area.height*.12f,area.width*.62f,area.height*.76f);
            RectFill(paper,C("C5BB96"));
            RectFill(new Rect(area.x+area.width*.1f,paper.y-2,area.width*.8f,4),C("9A8050"));
            RectFill(new Rect(area.x+area.width*.1f,paper.yMax-2,area.width*.8f,4),C("9A8050"));
            for(int row=0;row<3;row++)RectFill(new Rect(paper.x+4,paper.y+6+row*5,paper.width-8-row,1),C("464A3C"));
            RectFill(new Rect(paper.xMax-9,paper.yMax-9,6,6),C("87534A"));
        }
        private void DrawCityDistrictGrid(Rect area)
        {
            float cellWidth=(area.width-12)/3,cellHeight=(area.height-12)/3;
            for(int i=0;i<9;i++)
            {
                var plot=world.cityPlots[i];bool working=world.CityJob!=null&&world.CityJob.plotIndex==i;var def=CityCatalog.Get(working?world.CityJob.building:plot.building);bool available=world.IsCityDistrict(plot.landId);
                var rect=new Rect(area.x+(i%3)*(cellWidth+6),area.y+(i/3)*(cellHeight+6),cellWidth,cellHeight);
                RecordCityOverviewTarget("city-slot-"+i,rect);
                Color accent=def==null?Border:C(def.color);
                RectFill(rect,!available?C("171B1B"):i==citySelectedPlot?C("3C4137"):C("222A29"));
                if(i==citySelectedPlot)Outline(rect,Gold,2);
                RectFill(new Rect(rect.x,rect.y,3,rect.height),accent);
                Label(rect.x+8,rect.y+2,18,22,(i+1).ToString(),hudSmall);
                string name=!available?"地形保留":def==null?"待建设":def.name;
                HudFittedLabel(new Rect(rect.x+29,rect.y+1,rect.width-34,23),name,small,13);
                string state=def==null?(available?"选择设施":"不可建设"):"Lv. "+plot.level;
                if(world.CityJob!=null&&world.CityJob.plotIndex==i)state="施工中  "+CityTime(world.CityJob.remaining);
                CityGalleryText(new Rect(rect.x+6,rect.y+26,rect.width-12,20),state,new GUIStyle(tiny){fontSize=13,alignment=TextAnchor.MiddleCenter,normal={textColor=working?Gold:Muted}},12);
                if(GUI.Button(rect,"",GUIStyle.none)){citySelectedPlot=i;cityDetailScroll=Vector2.zero;citySelectedBuilding=def!=null?def.kind:DefaultCityBuilding();}
            }
        }
        private void DrawCityBuildingGlyph(Rect rect,Color color,string symbol)
        {
            // Simple editable facility symbols: matching silhouettes and clear per-resource accents.
            RectFill(new Rect(rect.x+rect.width*.13f,rect.y+rect.height*.34f,rect.width*.74f,rect.height*.62f),new Color(color.r,color.g,color.b,.35f));
            RectFill(new Rect(rect.x,rect.y+rect.height*.26f,rect.width,rect.height*.12f),color);
            RectFill(new Rect(rect.x+rect.width*.15f,rect.y+rect.height*.12f,rect.width*.7f,rect.height*.14f),color);
            RectFill(new Rect(rect.x+rect.width*.35f,rect.y,rect.width*.3f,rect.height*.12f),color);
            GUI.Label(new Rect(rect.x,rect.y+rect.height*.32f,rect.width,rect.height*.64f),symbol,new GUIStyle(heading){fontSize=(int)Mathf.Clamp(rect.height*.43f,18,32),alignment=TextAnchor.MiddleCenter,normal={textColor=Text}});
        }
        private void DrawCityQueue(Rect area)
        {
            var job=world.CityJob;Frame(area,C("25374A"));
            if(job==null){Label(area.x+14,area.y+13,area.width-28,25,"工匠待命  ·  选择城区开始建设",small);return;}
            float progress=(float)Math.Max(0,Math.Min(1,1-job.remaining/job.duration));
            Label(area.x+12,area.y+4,area.width-138,24,CityCatalog.Get(job.building).name+" → Lv. "+job.targetLevel,small);
            Label(area.xMax-116,area.y+4,104,24,CityTime(job.remaining),new GUIStyle(small){alignment=TextAnchor.MiddleRight,normal={textColor=Gold}});
            RectFill(new Rect(area.x+12,area.y+34,area.width-24,4),Border);
            RectFill(new Rect(area.x+12,area.y+34,(area.width-24)*progress,4),Gold);
        }
        private void DrawCityResources(Rect area,Supplies supplies)
        {
            double[] amounts={supplies.grain,supplies.grass,supplies.stone,supplies.iron,supplies.gold};
            int[] icons={1,0,2,3,4};float cell=area.width/5;
            for(int i=0;i<5;i++)
            {
                var r=new Rect(area.x+i*cell,area.y,cell-4,area.height);Frame(r,Ink);
                ResourceIcon(new Rect(r.center.x-12,r.y+4,24,20),icons[i]);
                GUI.Label(new Rect(r.x,r.y+28,r.width,24),amounts[i].ToString("0"),new GUIStyle(tiny){alignment=TextAnchor.MiddleCenter,normal={textColor=Text}});
            }
        }
        private void DrawQuestTracker(Rect area)
        {
            if(world==null||!world.IsYouzhou)return;
            DrawCityQuestLandReturn();
            Block(area);Frame(area,Ink);RectFill(new Rect(area.x,area.y,3,area.height),Gold);
            var openQuests=new Rect(area.x+8,area.y+4,area.width-16,53);RecordReviewTarget("quest-open",openQuests);
            if(GUI.Button(openQuests,GUIContent.none,GUIStyle.none))OpenCityQuests();
            var quest=world.CurrentCityQuest;
            if(quest==null){Label(area.x+12,area.y+12,area.width-24,56,"初定幽州\n引导已完成",small);return;}
            bool ready=world.CityQuestReady(quest.id);int goal=world.CityQuestTarget(quest.id);
            Label(area.x+12,area.y+6,area.width-24,22,"主要事宜  ·  "+quest.title,small);
            Label(area.x+12,area.y+32,area.width-24,20,quest.objective,tiny);
            Label(area.x+12,area.y+64,70,22,Math.Min(goal,world.CityQuestProgress(quest.id))+" / "+goal,tiny);
            var questButton=new Rect(area.xMax-143,area.yMax-36,131,27);RecordReviewTarget(ready?"quest-claim":"quest-go",questButton);
            if(Button(questButton,ready?"领取奖励":"前往",ready))
            {if(ready)ClaimCityQuestReward(quest.id);else GuideCityQuest(quest);}
        }
        private void GuideCityQuest(CityQuestDefinition quest)
        {
            if(quest==null)return;
            bool fromObjective=tab=="主城"&&cityPage=="quest-detail";
            ClearCityQuestLandOrigin();
            world.EnsureCityState();
            var route=CityQuestNavigation.Resolve(world,quest);
            switch(route.destination)
            {
                case CityQuestDestination.Facilities:
                    OpenCityFacilities(route.plotIndex);citySelectedBuilding=route.building;break;
                case CityQuestDestination.Formation:OpenCityFormation();break;
                case CityQuestDestination.Army:OpenReinforcement();break;
                case CityQuestDestination.Training:OpenTraining();break;
                case CityQuestDestination.Land:
                    tab="领地";map.selected=route.landId;
                    if(fromObjective){cityQuestLandWorld=world;cityQuestLandId=route.landId;}
                    var target=world.lands[route.landId];map.focus=new Vector3(target.x,0,target.z);break;
            }
            status=route.hint;map.CancelPointerInput();
        }
        private static string CityTime(double seconds)
        { int total=Math.Max(0,(int)Math.Ceiling(seconds));return total>=3600?(total/3600)+"时"+(total%3600/60)+"分":total>=60?(total/60)+"分"+(total%60).ToString("00")+"秒":total+"秒"; }
        private CityBuilding DefaultCityBuilding()
        { var def=CityCatalog.Buildings.FirstOrDefault(d=>d.kind!=CityBuilding.TownHall&&world.CityLevel(d.kind)==0&&world.CityLevel(CityBuilding.TownHall)>=d.unlockLevel);return def==null?CityBuilding.Mill:def.kind; }
    }
}
