using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityGalleryStage,cityGalleryFooter,cityGallerySelected;
        private bool cityGalleryTextFits;
        private string cityGalleryTextFailure;
        private Rect cityGalleryImageRect,cityGalleryFooterRect;

        private void InitCityFacilityGallery()
        {
            if(cityGalleryStage!=null)return;
            cityGalleryStage=GameAssets.LoadTexture("ui.youzhou.facility_stage");
            cityGalleryFooter=GameAssets.LoadTexture("ui.youzhou.facility_stage_footer");
            cityGallerySelected=GameAssets.LoadTexture("ui.youzhou.facility_stage_selected");
        }
        private static Rect CityGalleryRect(Rect parent,CityFacilityGalleryLayout.Box box)
        {return new Rect(parent.x+box.X,parent.y+box.Y,box.W,box.H);}

        private void CityGalleryText(Rect rect,string value,GUIStyle style,int minimum=13)
        {
            var fitted=new GUIStyle(style){wordWrap=false,clipping=TextClipping.Clip};var content=new GUIContent(value);
            while(fitted.fontSize>minimum&&!CityGalleryLabelFits(rect,content,fitted))fitted.fontSize--;
            bool fits=CityGalleryLabelFits(rect,content,fitted);
            cityGalleryTextFits&=fits;
            if(!fits&&string.IsNullOrEmpty(cityGalleryTextFailure))
            {
                Vector2 measured=fitted.CalcSize(content);
                cityGalleryTextFailure=string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "text='{0}'; font='{1}'; size={2}; minimum={3}; bounds={4:0.###}x{5:0.###}; measured={6:0.###}x{7:0.###}; lineHeight={8:0.###}; plot={9}; building={10}",
                    value,fitted.font==null?"GUI default":fitted.font.name,fitted.fontSize,minimum,rect.width,rect.height,
                    measured.x,measured.y,fitted.lineHeight,citySelectedPlot,citySelectedBuilding);
            }
            GUI.Label(rect,content,fitted);
        }
        private static bool CityGalleryLabelFits(Rect rect,GUIContent content,GUIStyle style)
        {return HeaderTextFits(rect,content,style,false)&&style.lineHeight<=rect.height+.1f;}

        private void DrawCityFacilityPortrait(Rect area)
        {
            InitCityFacilityGallery();cityGalleryTextFits=true;cityGalleryTextFailure="";
            cityGalleryImageRect=cityGalleryFooterRect=new Rect();
            if(!world.IsCityDistrict(world.cityPlots[citySelectedPlot].landId))
            {
                cityFacilityPortraitKind=CityBuilding.None;cityFacilityPortraitRect=new Rect();
                GUI.Label(area,"地形保留\n此处不可建设",new GUIStyle(body){alignment=TextAnchor.MiddleCenter});return;
            }
            var kind=SelectedCityFacilityKind();var definition=CityCatalog.Get(kind);
            cityFacilityPortraitKind=kind;if(definition==null)return;
            cityFacilityPortraitRect=area;
            cityGalleryImageRect=CityGalleryRect(area,CityFacilityGalleryLayout.Image(area.width,area.height));
            cityGalleryFooterRect=CityGalleryRect(area,CityFacilityGalleryLayout.Footer(area.width,area.height));
            GUI.DrawTexture(area,cityGalleryStage,ScaleMode.StretchToFill,true);
            GUI.DrawTexture(cityGalleryFooterRect,cityGalleryFooter,ScaleMode.StretchToFill,true);
            // Reuse the existing complete project silhouette. No new raster art is generated here.
            DrawCityFacilityArt(cityGalleryImageRect,kind);
            CityGalleryText(new Rect(area.x+10,area.y+2,area.width-20,18),"建筑示意",new GUIStyle(tiny){fontSize=12},12);
            bool working=world.CityJob!=null&&world.CityJob.plotIndex==citySelectedPlot;
            int level=world.CityLevel(kind);
            string name=definition.name+(working?" · 施工中":level==0?" · 未建":"");
            string levels=(working?level+" → "+world.CityJob.targetLevel:level.ToString())+" / "+definition.maximumLevel;
            var footer=cityGalleryFooterRect;
            CityGalleryText(new Rect(footer.x+14,footer.y+1,footer.width-154,32),name,new GUIStyle(body){fontSize=18,alignment=TextAnchor.MiddleLeft});
            CityGalleryText(new Rect(footer.xMax-134,footer.y+1,120,32),levels,new GUIStyle(small){fontSize=16,alignment=TextAnchor.MiddleRight,normal={textColor=C("D8C68F")}});
        }

        private void DrawCityGalleryOption(Rect item,CityBuildingDefinition option,int level,bool locked,bool selected)
        {
            InitCityFacilityGallery();
            GUI.DrawTexture(item,cityGalleryStage,ScaleMode.StretchToFill,true);
            var art=CityGalleryRect(item,CityFacilityGalleryLayout.OptionImage(item.width));
            var name=CityGalleryRect(item,CityFacilityGalleryLayout.OptionName(item.width));
            var state=CityGalleryRect(item,CityFacilityGalleryLayout.OptionState(item.width));
            GUI.DrawTexture(CityGalleryRect(item,CityFacilityGalleryLayout.OptionFooter(item.width)),cityGalleryFooter,ScaleMode.StretchToFill,true);
            if(selected)GUI.DrawTexture(item,cityGallerySelected,ScaleMode.StretchToFill,true);
            DrawCityFacilityArt(art,option.kind);
            CityGalleryText(name,option.name,new GUIStyle(small){fontSize=15,alignment=TextAnchor.MiddleCenter});
            CityGalleryText(state,level>0?"Lv. "+level:locked?"府 "+option.unlockLevel+" 解锁":"可建",
                new GUIStyle(tiny){fontSize=12,alignment=TextAnchor.MiddleCenter,normal={textColor=locked&&level==0?Gold:Muted}},12);
            // These visible subregions belong to the existing full-tile command.
            RecordCityOverviewTarget("city-facility-image-"+(int)option.kind,art);
            RecordCityOverviewTarget("city-facility-title-"+(int)option.kind,name);
        }
    }
}
