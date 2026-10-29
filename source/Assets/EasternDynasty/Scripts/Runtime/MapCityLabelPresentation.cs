using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityMarkerCrest,cityMarkerSelected,cityMarkerRibbon,cityMarkerImage;
        private GUIStyle cityMarkerName,cityMarkerDetail;
        private bool mapCityLabelTextFits=true;
        private bool MapCityLabelDesignReady { get { return cityMarkerCrest!=null&&cityMarkerCrest.width==104&&cityMarkerCrest.height==104
            &&cityMarkerSelected!=null&&cityMarkerSelected.width==104&&cityMarkerSelected.height==104
            &&cityMarkerRibbon!=null&&cityMarkerRibbon.width==280&&cityMarkerRibbon.height==104&&cityMarkerImage!=null; } }

        private void DrawDesignedMapCityLabels()
        {
            cityLabels.Clear();mapCityLabelTextFits=true;
            if(BlocksMapInput)return;
            if(cityMarkerCrest==null)
            {
                cityMarkerCrest=GameAssets.LoadTexture("ui.youzhou.city_marker_crest");
                cityMarkerSelected=GameAssets.LoadTexture("ui.youzhou.city_marker_selected");
                cityMarkerRibbon=GameAssets.LoadTexture("ui.youzhou.city_marker_ribbon");
                cityMarkerImage=GameAssets.LoadTexture("map.youzhou.city");
                cityMarkerName=new GUIStyle(body){fontSize=16,alignment=TextAnchor.MiddleLeft,wordWrap=false,clipping=TextClipping.Clip};
                cityMarkerName.normal.textColor=C("EEE4C9");
                cityMarkerDetail=new GUIStyle(cityMarkerName){fontSize=12};cityMarkerDetail.normal.textColor=C("CBBB94");
            }
            foreach(var land in world.lands.Where(t=>t.capital||t.home).OrderByDescending(t=>t.home))
            {
                var projection=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(land)+Vector3.up*.5f);
                if(projection.z<0)continue;
                var anchor=ScreenPointToGui(projection);
                var content=new GUIContent(land.name);
                var box=MapCityLabelLayout.Panel(anchor.x,anchor.y,cityMarkerName.CalcSize(content).x);
                var panel=HudDesignRect(box);
                if(panel.x<0||panel.xMax>width||panel.y<82||panel.yMax>height-119||cityLabels.Any(q=>q.Overlaps(panel)))continue;
                if(hudBlocks.Any(q=>q.Overlaps(GuiRectToPixels(panel))))continue;
                var crest=HudDesignRect(MapCityLabelLayout.Crest(box));
                var imageBox=HudDesignRect(MapCityLabelLayout.Image(box));
                float imageScale=Mathf.Min(imageBox.width/cityMarkerImage.width,imageBox.height/cityMarkerImage.height);
                var image=new Rect(imageBox.center.x-cityMarkerImage.width*imageScale/2,imageBox.center.y-cityMarkerImage.height*imageScale/2,
                    cityMarkerImage.width*imageScale,cityMarkerImage.height*imageScale);
                var name=HudDesignRect(MapCityLabelLayout.Name(box));var detail=HudDesignRect(MapCityLabelLayout.Detail(box));
                cityLabels.Add(panel);Block(panel);
                GUI.DrawTexture(HudDesignRect(MapCityLabelLayout.Ribbon(box)),cityMarkerRibbon,ScaleMode.StretchToFill,true);
                GUI.DrawTexture(crest,map.selected==land.id?cityMarkerSelected:cityMarkerCrest,ScaleMode.StretchToFill,true);
                GUI.DrawTexture(image,cityMarkerImage,ScaleMode.StretchToFill,true);
                var fitted=new GUIStyle(cityMarkerName){wordWrap=box.H==72};
                while(fitted.fontSize>14&&!HeaderTextFits(name,content,fitted,fitted.wordWrap))fitted.fontSize--;
                mapCityLabelTextFits&=HeaderTextFits(name,content,fitted,fitted.wordWrap);
                GUI.Label(name,content,fitted);
                var state=new GUIContent(land.home?"主城 · 幽州":(land.owned?"己方城池":"中立城池")+" · Lv."+land.level);
                mapCityLabelTextFits&=HeaderTextFits(detail,state,cityMarkerDetail,false);GUI.Label(detail,state,cityMarkerDetail);
                RecordReviewTarget("city-label-"+land.id,panel);
                if(land.capital&&!land.home)RecordReviewTarget("capital-label-"+land.id,panel);
                RecordReviewTarget("city-label-art-"+land.id,image);
                RecordReviewTarget("city-label-name-"+land.id,name);
                RecordReviewTarget("city-label-detail-"+land.id,detail);
                if(ContentTileClicked(panel,"city-marker:"+land.id,-1))
                {map.selected=land.id;if(land.home)OpenCity(land.id);return;}
            }
        }
    }
}
