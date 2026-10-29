using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private readonly Dictionary<CityBuilding,Texture2D> cityFacilityArt=new Dictionary<CityBuilding,Texture2D>();
        private void DrawCityFacilityArt(Rect area,CityBuilding kind)
        {
            Texture2D texture;
            if(!cityFacilityArt.TryGetValue(kind,out texture))
            {
                // Reuse the editable city silhouettes, without historical UI text or numbers.
                var raster=new CitySceneRaster(240,180);
                raster.Building(new Vector2(120,137),kind);
                texture=raster.Texture();cityFacilityArt.Add(kind,texture);
            }
            GUI.DrawTexture(area,texture,ScaleMode.ScaleToFit,true);
        }

        private void DrawCityFacilityQueue(Rect area)
        {
            DrawCityQueue(new Rect(area.x,area.y,area.width,44));
            var job=world.CityJob;
            if(job==null)
            { Label(area.x+12,area.y+58,area.width-24,24,"项目配置 · 单一施工队列",tiny);return; }
            var locate=new Rect(area.x,area.y+44,area.width,44);
            RecordCityOverviewTarget("city-queue-locate",locate);
            if(Button(locate,"查看施工 · "+CityCatalog.Get(job.building).name))OpenCityFacilities(job.plotIndex);
        }

        private void DrawCityBuildingPanel(Rect area)
        {
            InitCityFacilitySheet();citySheetTextFits=true;
            var plot=world.cityPlots[citySelectedPlot];
            if(!world.IsCityDistrict(plot.landId))
            { Label(area.x+20,area.y+22,area.width-40,90,"此处保留原有地形。\n请选择其余可建设城区。",body);return; }
            var job=world.CityJob;
            bool working=job!=null&&job.plotIndex==plot.index;
            if(working)citySelectedBuilding=job.building;
            else if(plot.building!=CityBuilding.None)citySelectedBuilding=plot.building;
            var definition=CityCatalog.Get(citySelectedBuilding);if(definition==null)return;
            bool empty=plot.building==CityBuilding.None;
            var existing=world.cityPlots.FirstOrDefault(p=>p.building==definition.kind);
            bool elsewhere=existing!=null&&existing.index!=plot.index;
            int current=world.CityLevel(definition.kind),next=Math.Min(definition.maximumLevel,current+1);
            bool maximum=current>=definition.maximumLevel;
            float x=area.x+20,w=area.width-40,y=area.y+14;
            Label(x,y,w-200,34,definition.name,heading);
            GUI.Label(new Rect(x+w-196,y,196,30),"城区 "+(plot.index+1)+"  /  Lv. "+current+" · "+definition.maximumLevel,new GUIStyle(small){alignment=TextAnchor.MiddleRight});
            Label(x,y+38,w,24,working?"施工中 → Lv. "+job.targetLevel:maximum?"已达最高等级":elsewhere?"已建成 · 前往原位置升级":empty?"选择要在此处兴建的设施":"设施升级 · 完工后生效",small);
            RectFill(new Rect(x,y+67,w,1),Border);
            y+=78;cityFacilityOptionsRect=new Rect();
            if(empty&&!working)
            {
                var options=CityCatalog.Buildings.Where(d=>d.kind!=CityBuilding.TownHall).ToArray();
                float cell=w/options.Length;
                cityFacilityOptionsRect=new Rect(x,y,w,CityFacilitySheetLayout.OptionHeight);
                for(int i=0;i<options.Length;i++)
                {
                    var option=options[i];int level=world.CityLevel(option.kind);
                    bool locked=world.CityLevel(CityBuilding.TownHall)<option.unlockLevel;
                    var item=CitySheetRect(area,CityFacilitySheetLayout.Options(area.width,i));
                    bool selected=citySelectedBuilding==option.kind;
                    DrawCityGalleryOption(item,option,level,locked,selected);
                    RecordCityOverviewTarget("city-option-"+(int)option.kind,item);
                    if(GUI.Button(item,GUIContent.none,GUIStyle.none)){citySelectedBuilding=option.kind;cityDetailScroll=Vector2.zero;}
                }
                y+=108;
            }
            // Details scroll; the actual action and its reason stay visible on all target screens.
            var view=CitySheetRect(area,CityFacilitySheetLayout.View(area.width,area.height,empty&&!working));
            cityFacilityLedgerRect=view;
            RecordCityOverviewTarget("city-detail-scroll",view);
            cityDetailScroll=BeginContentScroll(view,cityDetailScroll,new Rect(0,0,w-20,Mathf.Max(CityFacilitySheetLayout.ContentHeight,view.height)));
            float inner=CityFacilitySheetLayout.InnerWidth(area.width);
            var description=CitySheetRect(new Rect(),CityFacilitySheetLayout.Description(inner));
            CitySheetText(description,definition.kind==CityBuilding.DrillGround?"项目校场提供兵力加成；本城仍为一支部队。":definition.description,
                new GUIStyle(body){fontSize=18},13,true);
            citySheetDescriptionRect=new Rect(view.x+description.x,view.y+description.y-cityDetailScroll.y,description.width,description.height);
            DrawCitySheetEffects(inner,definition,current,next,maximum);
            citySheetEffectRect=new Rect(view.x,view.y+44-cityDetailScroll.y,inner,76);
            int required=CityCatalog.RequiredHallLevel(definition.kind,next);
            bool prerequisite=world.CityLevel(CityBuilding.TownHall)<required;
            string requirement=definition.kind==CityBuilding.TownHall?"城主府升级可解锁设施，并提高设施等级上限。":"前置：城主府 "+required+" 级  /  当前 "+world.CityLevel(CityBuilding.TownHall)+" 级";
            cityFacilityCostRect=new Rect();
            if(!maximum)
            {
                CitySheetText(new Rect(4,126,inner-8,24),working?"已投入资源 · 中断后全额返还":"建设费用 / 当前库存",new GUIStyle(small){fontSize=16,normal={textColor=Gold}});
                var costs=CitySheetRect(new Rect(),CityFacilitySheetLayout.Costs(inner));
                DrawCityCostAvailability(costs,working?job.paid:world.CityBuildCost(definition.kind,next),working);
                cityFacilityCostRect=new Rect(view.x+costs.x,view.y+costs.y-cityDetailScroll.y,costs.width,costs.height);
                CitySheetText(CitySheetRect(new Rect(),CityFacilitySheetLayout.Duration(inner)),working?"剩余工期  "+CityTime(job.remaining):"建设用时  "+CityTime(world.CityBuildSeconds(definition.kind,next)),new GUIStyle(body){fontSize=18});
            }
            else CitySheetText(new Rect(8,154,inner-16,88),"该设施已完成全部升级\n不再消耗建设资源",new GUIStyle(body){fontSize=18},13,true);
            CitySheetText(CitySheetRect(new Rect(),CityFacilitySheetLayout.Requirement(inner)),requirement,new GUIStyle(small){fontSize=16,normal={textColor=prerequisite?Gold:Muted}},13,true);
            CitySheetText(CitySheetRect(new Rect(),CityFacilitySheetLayout.Note(inner)),"项目设施配置 · 收益于完工后生效",new GUIStyle(tiny){fontSize=13});
            GUI.EndScrollView();

            var action=CitySheetRect(area,CityFacilitySheetLayout.Action(area.width,area.height));
            cityFacilityActionRect=action;
            string problem=world.CityBuildProblem(plot.index,definition.kind);
            string reason=working?"施工完成后自动生效；离开页面仍继续建设。":elsewhere?"同类设施仅一座，前往已建设施继续管理。":maximum?"已达最高等级。":problem.Length==0?"工匠空闲 · 资源充足 · 可立即开工":problem;
            CitySheetText(new Rect(x,area.yMax-104,w,34),reason,new GUIStyle(small){fontSize=16,alignment=TextAnchor.MiddleCenter,normal={textColor=problem.Length==0?Green:Muted}},13,true);
            if(working)
            {
                RecordCityOverviewTarget("city-cancel",action);
                if(CitySheetAction(action,"city-cancel","中断施工 · 查看返还明细"))ConfirmCityCancellation(job);
            }
            else if(elsewhere)
            {
                RecordCityOverviewTarget("city-existing",action);
                if(CitySheetAction(action,"city-existing","前往 "+definition.name+" · Lv. "+current))OpenCityFacilities(existing.index);
            }
            else if(!maximum&&prerequisite)
            {
                RecordCityOverviewTarget("city-prerequisite",action);
                if(CitySheetAction(action,"city-prerequisite","前往城主府 · 需要 "+required+" 级"))OpenCityFacilities(4);
            }
            else
            {
                if(problem.Length==0)RecordCityOverviewTarget("city-build",action);
                string caption=maximum?"已达最高等级":CityTime(world.CityBuildSeconds(definition.kind,next))+" / "+(empty?"开始建设":"升级设施");
                if(CitySheetAction(action,"city-build",caption,problem.Length==0))
                { Act(world.StartCityConstruction(plot.index,definition.kind));Save(false);cityDetailScroll=Vector2.zero; }
            }
        }

        private CityConstruction cityCancellation;
        private void ConfirmCityCancellation(CityConstruction expected)
        {
            Ask("中断"+CityCatalog.Get(expected.building).name+"施工，返还全部已投入资源？",()=>
            {
                // The world continues advancing while a confirmation is open.
                if(!ReferenceEquals(world.CityJob,expected)){status="该施工已结束，未取消或返还其他建设。";return;}
                Act(world.CancelCityConstruction());Save(false);
            });
            cityCancellation=expected;
        }
        private void DrawCityCancellationDialog()
        {
            var r=new Rect((width-620)/2,(height-354)/2,620,354);Frame(r,Ink);
            bool active=ReferenceEquals(world.CityJob,cityCancellation);
            Label(r.x+28,r.y+22,r.width-56,35,"中断施工 · "+CityCatalog.Get(cityCancellation.building).name,heading);
            Label(r.x+28,r.y+70,r.width-56,28,active?"目标 Lv. "+cityCancellation.targetLevel+"  ·  剩余 "+CityTime(cityCancellation.remaining):"该施工已结束，不再返还或取消其他建设。",body);
            Label(r.x+28,r.y+110,r.width-56,26,active?"以下已支付资源将全额返还；当前设施等级保持不变。":"关闭此页后可查看当前设施。",small);
            var refund=active?world.CityConstructionRefund():new Supplies();
            double[] amounts={refund.grain,refund.grass,refund.stone,refund.iron,refund.gold};
            int[] icons={1,0,2,3,4};float cell=(r.width-56)/5;
            for(int i=0;i<5;i++)
            {
                var box=new Rect(r.x+28+i*cell,r.y+151,cell-6,83);Frame(box,Panel);
                ResourceIcon(new Rect(box.center.x-14,box.y+10,28,24),icons[i]);
                GUI.Label(new Rect(box.x,box.y+46,box.width,28),"+"+amounts[i].ToString("0.##"),new GUIStyle(body){alignment=TextAnchor.MiddleCenter,normal={textColor=Green}});
            }
            Label(r.x+28,r.y+243,r.width-56,23,"项目采用 100% 中断返还 · 施工完成后不再可取消",tiny);
            var yes=new Rect(r.x+28,r.y+292,270,44);var no=new Rect(r.x+322,r.y+292,270,44);
            RecordReviewTarget("confirm-yes",yes);RecordReviewTarget("confirm-no",no);
            if(Button(yes,active?"确认中断并返还":"查看当前设施",true)){Action action=confirmation;confirmation=null;action();}
            if(Button(no,active?"继续建设":"关闭"))confirmation=null;
        }
        private static string CityFacilityEffectUnit(CityBuilding kind)
        {
            switch(kind)
            {
                case CityBuilding.TownHall:return " 级 · 设施等级上限";
                case CityBuilding.Mill:return " 粮食 / 小时";
                case CityBuilding.Lumberyard:return " 木材 / 小时";
                case CityBuilding.Quarry:return " 石料 / 小时";
                case CityBuilding.Ironworks:return " 铁矿 / 小时";
                case CityBuilding.Residence:return " 金币 / 小时";
                case CityBuilding.Workshop:return "% · 新建设工期缩短";
                default:return " · 部队带兵上限加成";
            }
        }
        private void DrawCityCostAvailability(Rect area,Supplies cost,bool paid)
        {
            double[] amounts={cost.grain,cost.grass,cost.stone,cost.iron,cost.gold};
            var inventory=world.resources;double[] available={inventory.grain,inventory.grass,inventory.stone,inventory.iron,inventory.gold};
            int[] icons={1,0,2,3,4};string[] names={"粮食","木材","石料","铁矿","金币"};
            GUI.DrawTexture(area,citySheetCost,ScaleMode.StretchToFill,true);
            for(int i=0;i<5;i++)
            {
                var box=CityFacilitySheetLayout.Cost(area.width,i);
                var rect=new Rect(area.x+box.X,area.y,box.W,box.H);
                bool enough=available[i]>=amounts[i];
                ResourceIcon(new Rect(rect.x+10,rect.y+5,28,24),icons[i]);
                CitySheetText(new Rect(rect.x+44,rect.y+3,rect.width-50,26),names[i],new GUIStyle(small){fontSize=16,normal={textColor=Gold}});
                string value=amounts[i].ToString("0.##")+(paid?"":" / "+Math.Floor(available[i]).ToString("0"));
                CitySheetText(new Rect(rect.x+10,rect.y+30,rect.width-16,28),value,new GUIStyle(body){fontSize=17,normal={textColor=paid||enough?Text:Gold}},12);
                CitySheetText(new Rect(rect.x+10,rect.y+60,rect.width-16,24),paid?"已付 · 返 "+amounts[i].ToString("0.##"):enough?"充足":"缺 "+Math.Ceiling(amounts[i]-available[i]).ToString("0"),new GUIStyle(tiny){fontSize=14,normal={textColor=paid||enough?Muted:Gold}},12);
            }
        }
    }
}
