using System;
using System.Collections;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public string mapCityLabelStyle;
            public bool mapCityLabelTextFits,mapCityLabelDesignReady;
        }
        private void FillMapCityLabelReview(ReviewState review)
        {
            review.mapCityLabelStyle=MapCityLabelLayout.Style;
            review.mapCityLabelTextFits=mapCityLabelTextFits;
            review.mapCityLabelDesignReady=MapCityLabelDesignReady;
        }
        private void AssertMapCityLabels()
        {
            if(!MapCityLabelDesignReady||!mapCityLabelTextFits)throw new Exception("City marker exported textures or actual-font fit failed.");
            foreach(var land in world.lands)
            {
                ReviewTarget target;
                if(!reviewTargets.TryGetValue("city-label-"+land.id,out target))continue;
                Rect panel=MapHudReviewRect(target),art=RequiredMapCommandRect("city-label-art-"+land.id);
                Rect name=RequiredMapCommandRect("city-label-name-"+land.id),detail=RequiredMapCommandRect("city-label-detail-"+land.id);
                float w=panel.width/scale,h=panel.height/scale;
                if(w<183.9f||w>304.1f||(Mathf.Abs(h-52)>.1f&&Mathf.Abs(h-72)>.1f)
                    ||!MapCommandContains(uiPixels,panel)||!MapCommandContains(panel,art)||!MapCommandContains(panel,name)||!MapCommandContains(panel,detail)
                    ||art.Overlaps(name)||art.Overlaps(detail)||name.Overlaps(detail)||!map.IsBlocked(panel.center))
                    throw new Exception("Visible city marker image/name/status overlaps, clips or leaks terrain input.");
                var ground=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(land));
                if(panel.Contains(new Vector2(ground.x,Screen.height-ground.y)))throw new Exception("City marker obscures the actual city ground pick anchor.");
            }
        }
        private IEnumerator MapCityLabelSmoke()
        {
            string before=JsonUtility.ToJson(world);var home=world.lands[world.homeId];string originalName=home.name;
            int previousSelected=map.selected;var previousFocus=map.focus;float previousZoom=map.zoom;
            bool previousReview=reviewMode;
            // CaptureMapHudState clears its observed targets when it restores a
            // non-review caller. Keep observation active through both captures
            // and their assertions; never manufacture missing marker targets.
            reviewMode=true;
            try
            {
                map.selected=-1;map.CenterHome();
                yield return CaptureMapHudState("smoke-map-city-markers.png");
                if(!reviewTargets.ContainsKey("city-label-"+home.id))throw new Exception("City marker fixture must render the actual home marker.");
                home.name="界面压力验收用长名称主城完整显示";
                yield return CaptureMapHudState("smoke-map-city-markers-long.png");
                var panel=RequiredMapCommandRect("city-label-"+home.id);
                if(Mathf.Abs(panel.height/scale-72)>.1f)throw new Exception("Long city name fixture did not exercise the full two-line design.");
            }
            finally
            {
                home.name=originalName;map.selected=previousSelected;map.focus=previousFocus;map.zoom=previousZoom;
                reviewMode=previousReview;if(!reviewMode)reviewTargets.Clear();
            }
            if(JsonUtility.ToJson(world)!=before)throw new Exception("City marker fixture changed the campaign.");
            Debug.Log("MAP_CITY_MARKER_DESIGN_RUNTIME_OK: Inkscape assets, complete city image, independent live name/status, long-name font fit, safe area and unobscured city ground. Visual review and actual clicks remain publisher checks.");
        }
    }
}
