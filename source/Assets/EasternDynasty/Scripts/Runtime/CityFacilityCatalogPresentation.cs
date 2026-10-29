using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private int cityCatalogCategory;
        private Vector2 cityCatalogScroll;
        private static readonly string[] CityCatalogCategories={"全部设施","城务发展","资源生产","军备建设"};
        private Rect cityCatalogCanvas,cityCatalogHallArt;
        private readonly List<CityCatalogNodeVisual> cityCatalogNodes=new List<CityCatalogNodeVisual>();
        private sealed class CityCatalogNodeVisual
        {
            public CityBuilding kind;
            public Rect card,art,name,state,effect,condition,action;
            public int level,next,requiredHall;
        }

        private void RecordCityCatalogTarget(string id,Rect rect)
        { if(GUI.enabled)RecordReviewTarget(id,rect); }

        private void OpenCityFacilityCatalog()
        {
            world.EnsureCityState();tab="主城";cityPage="catalog";cityOverviewPlanning=false;ResetCityPanorama();
            map.CancelPointerInput();
        }

        private static int CityFacilityCategory(CityBuilding kind)
        {
            if(kind==CityBuilding.TownHall||kind==CityBuilding.Workshop)return 1;
            if(kind==CityBuilding.Barracks||kind==CityBuilding.DrillGround)return 3;
            return 2;
        }

        private void NavigateCityFacility(CityBuilding kind)
        {
            // Re-resolve at the click: construction may complete while this page is open.
            var route=CityFacilityNavigation.Resolve(world,kind);
            if(!route.canNavigate){status=route.reason;return;}
            OpenCityFacilities(route.plotIndex);citySelectedBuilding=route.building;
            cityFacilitiesOrigin="catalog";status=route.reason;
        }

        private void DrawCityFacilityCatalog(Rect area)
        {
            cityCatalogNodes.Clear();InitCityCatalogDesign();cityCatalogDesignTextFits=true;
            const float sidebarWidth=208;
            var sidebar=new Rect(area.x,area.y,sidebarWidth,area.height);
            var panel=new Rect(sidebar.xMax+16,area.y,area.width-sidebarWidth-16,area.height);
            GUI.DrawTexture(area,cityCatalogSheet,ScaleMode.StretchToFill,true);
            CityCatalogText(new Rect(sidebar.x+18,sidebar.y+13,sidebar.width-36,30),"设施一览",24,C("D8C68F"));
            cityCatalogHallArt=new Rect(sidebar.x+12,sidebar.y+56,184,138);
            DrawCityFacilityArt(cityCatalogHallArt,CityBuilding.TownHall);
            CityCatalogText(new Rect(sidebar.x+12,sidebar.y+197,sidebar.width-24,26),"城主府  Lv. "+world.CityLevel(CityBuilding.TownHall),18,C("E3E2D9"),TextAnchor.MiddleCenter);
            CityCatalogText(new Rect(sidebar.x+12,sidebar.y+227,sidebar.width-24,24),"已建 "+world.cityPlots.Count(p=>p.building!=CityBuilding.None)+" / "+CityCatalog.Buildings.Length+" 处",14,C("B8C3C7"),TextAnchor.MiddleCenter);
            for(int i=0;i<CityCatalogCategories.Length;i++)
            {
                var category=new Rect(sidebar.x+12,sidebar.y+276+i*50,sidebar.width-24,44);
                RecordCityCatalogTarget("city-catalog-category-"+i,category);
                if(CityCatalogAction(category,"city-catalog-category-"+i,CityCatalogCategories[i],true,true,cityCatalogCategory==i))
                {cityCatalogCategory=i;cityCatalogScroll=Vector2.zero;}
            }
            CityCatalogText(new Rect(sidebar.x+18,sidebar.yMax-64,sidebar.width-36,20),"项目设施配置 · 建筑示意",12,C("B8C3C7"),TextAnchor.MiddleLeft,12);
            CityCatalogText(new Rect(sidebar.x+18,sidebar.yMax-45,sidebar.width-36,20),"一支部队 · 单施工队列",12,C("B8C3C7"),TextAnchor.MiddleLeft,12);
            CityCatalogText(new Rect(sidebar.x+18,sidebar.yMax-26,sidebar.width-36,20),"图像前往真实位置",12,C("B8C3C7"),TextAnchor.MiddleLeft,12);
            var definitions=CityCatalog.Buildings.Where(d=>cityCatalogCategory==0||CityFacilityCategory(d.kind)==cityCatalogCategory).ToArray();
            var job=world.CityJob;
            CityCatalogText(new Rect(panel.x+22,panel.y+10,panel.width-220,28),CityCatalogCategories[cityCatalogCategory]+"  /  建筑图谱",20,C("E3E2D9"));
            CityCatalogText(new Rect(panel.x+22,panel.y+40,panel.width-220,23),job==null?"工匠待命  ·  选择建筑，核对费用后建设":"施工中  "+CityCatalog.Get(job.building).name+" → Lv. "+job.targetLevel+"  ·  "+CityTime(job.remaining),14,C("B8C3C7"));
            if(job!=null)
            {
                var locate=new Rect(panel.xMax-184,panel.y+14,162,44);RecordCityCatalogTarget("city-catalog-queue",locate);
                if(CityCatalogAction(locate,"city-catalog-queue","查看当前施工"))NavigateCityFacility(job.building);
            }
            GUI.DrawTexture(new Rect(panel.x+22,panel.y+69,panel.width-44,1),cityCatalogRule,ScaleMode.StretchToFill,true);
            var view=new Rect(panel.x+18,panel.y+80,panel.width-36,panel.height-94);
            cityCatalogCanvas=view;
            const float gap=CityFacilityAtlasLayout.Gap,cardHeight=CityFacilityAtlasLayout.Height;
            int columns=CityFacilityAtlasLayout.Columns(view.width);
            float cardWidth=CityFacilityAtlasLayout.Width(view.width);
            int rows=(definitions.Length+columns-1)/columns;
            RecordCityCatalogTarget("city-catalog-scroll",view);
            cityCatalogScroll=BeginContentScroll(view,cityCatalogScroll,new Rect(0,0,view.width-20,Mathf.Max(view.height,rows*(cardHeight+gap)-gap)));
            for(int i=0;i<definitions.Length;i++)
            {
                var definition=definitions[i];
                var card=new Rect((i%columns)*(cardWidth+gap),(i/columns)*(cardHeight+gap),cardWidth,cardHeight);
                var absolute=new Rect(view.x+card.x,view.y+card.y-cityCatalogScroll.y,card.width,card.height);
                bool fullyVisible=GUI.enabled&&CityOverviewContains(view,absolute);
                int level=world.CityLevel(definition.kind),next=Math.Min(definition.maximumLevel,level+1);
                bool working=job!=null&&job.building==definition.kind;
                var route=CityFacilityNavigation.Resolve(world,definition.kind);
                var art=CityAtlasRect(card,CityFacilityAtlasLayout.Art(card.width));
                var name=CityAtlasRect(card,CityFacilityAtlasLayout.Name(card.width));
                var stateArea=CityAtlasRect(card,CityFacilityAtlasLayout.State(card.width));
                var effectArea=CityAtlasRect(card,CityFacilityAtlasLayout.Effect(card.width));
                var conditionArea=CityAtlasRect(card,CityFacilityAtlasLayout.Condition(card.width));
                DrawCityFacilityArt(art,definition.kind);
                GUI.DrawTexture(new Rect(card.x,card.y+204,card.width,1),cityCatalogRule,ScaleMode.StretchToFill,true);
                CityCatalogText(name,definition.name,22,C("E3E2D9"));
                string state=working?"施工至 Lv. "+job.targetLevel:level>0?"Lv. "+level+" / "+definition.maximumLevel:"尚未建成";
                CityCatalogText(stateArea,state,14,C("D8C68F"));
                string effect=CityCatalog.EffectAtLevel(definition.kind,level)+" → "+CityCatalog.EffectAtLevel(definition.kind,next);
                CityCatalogText(effectArea,effect+CityFacilityEffectUnit(definition.kind),14,C("CBD4D5"));
                int requiredHall=CityCatalog.RequiredHallLevel(definition.kind,next);
                string condition=definition.kind==CityBuilding.TownHall?"主城发展核心":requiredHall>world.CityLevel(CityBuilding.TownHall)?"需城主府 Lv. "+requiredHall:"城主府 Lv. "+requiredHall+"  ·  已满足";
                if(level>=definition.maximumLevel)condition="设施已达最高等级";
                if(working)condition="完工剩余  "+CityTime(job.remaining);
                else if(route.state==CityFacilityRouteState.QueueBlocked)condition="等待 "+CityCatalog.Get(job.building).name+"完工";
                CityCatalogText(conditionArea,condition,14,C("B8C3C7"));
                var action=CityAtlasRect(card,CityFacilityAtlasLayout.Action(card.width));
                if(fullyVisible)
                {
                    var visual=new CityCatalogNodeVisual{kind=definition.kind,card=absolute,
                        art=new Rect(view.x+art.x,view.y+art.y-cityCatalogScroll.y,art.width,art.height),
                        name=new Rect(view.x+name.x,view.y+name.y-cityCatalogScroll.y,name.width,name.height),
                        state=CityCatalogAbsolute(view,stateArea),effect=CityCatalogAbsolute(view,effectArea),condition=CityCatalogAbsolute(view,conditionArea),
                        action=new Rect(view.x+action.x,view.y+action.y-cityCatalogScroll.y,action.width,action.height),
                        level=level,next=next,requiredHall=requiredHall};
                    cityCatalogNodes.Add(visual);
                    RecordCityCatalogTarget("city-catalog-card-"+(int)definition.kind,absolute);
                    if(route.canNavigate)
                    {
                        RecordCityCatalogTarget("city-catalog-go-"+(int)definition.kind,visual.action);
                        RecordCityCatalogTarget("city-catalog-image-"+(int)definition.kind,visual.art);
                        RecordCityCatalogTarget("city-catalog-name-"+(int)definition.kind,visual.name);
                    }
                }
                // Image and command share the same freshly resolved navigation; neither builds.
                bool previousEnabled=GUI.enabled;GUI.enabled=previousEnabled&&fullyVisible&&route.canNavigate;
                bool open=ContentTileClicked(art,"city-catalog-image-"+(int)definition.kind,-1);
                bool named=ContentTileClicked(name,"city-catalog-name-"+(int)definition.kind,-1);
                if(CityCatalogAction(action,"city-catalog-go-"+(int)definition.kind,route.label,route.canNavigate)||open||named)NavigateCityFacility(definition.kind);
                GUI.enabled=previousEnabled;
            }
            GUI.EndScrollView(GUI.enabled);
        }
    }
}
