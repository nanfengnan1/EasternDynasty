using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Rect cityFacilityPortraitRect, cityFacilityLedgerRect, cityFacilityCostRect;
        private Rect cityFacilityActionRect, cityFacilityOptionsRect;
        private CityBuilding cityFacilityPortraitKind;
        private Texture2D cityFacilityMedallion;


        private CityBuilding SelectedCityFacilityKind()
        {
            var plot=world.cityPlots[citySelectedPlot];
            if(world.CityJob!=null&&world.CityJob.plotIndex==plot.index)return world.CityJob.building;
            return plot.building==CityBuilding.None?citySelectedBuilding:plot.building;
        }

        private void DrawCityFacilityMedallion(Rect area,CityBuilding kind,bool large)
        {
            // One cached procedural backing, drawn in the caller's GUI group and
            // mobile matrix. It contains no official artwork, text or rule values.
            if(cityFacilityMedallion==null)
            {
                const int size=256;var pixels=new Color32[size*size];
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float dx=(x-127.5f)/128,dy=(y-127.5f)/128;
                    float distance=Mathf.Sqrt(dx*dx+dy*dy);
                    Color color=Color.Lerp(C("4C5540"),C("172520"),y/255f);
                    if(distance>.947f&&distance<.97f)color=C("A9976C");
                    else if(distance>.91f&&distance<.918f)color=C("6E7457");
                    color.a=Mathf.Clamp01((.985f-distance)*128);pixels[y*size+x]=color;
                }
                cityFacilityMedallion=new Texture2D(size,size,TextureFormat.RGBA32,false)
                    {name="FacilityMedallionProcedural",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.DontSave};
                cityFacilityMedallion.SetPixels32(pixels);cityFacilityMedallion.Apply(false,true);
            }
            GUI.DrawTexture(area,cityFacilityMedallion,ScaleMode.ScaleToFit,true);
            // Existing original production artwork, including the full roof and yard.
            var image=new Rect(area.center.x-area.width*.57f,area.y+area.height*.04f,area.width*1.14f,area.height*.855f);
            DrawCityFacilityArt(image,kind);
            if(large)
            {
                for(int i=0;i<4;i++)
                {
                    float y=area.y+area.height*(.18f+i*.18f);
                    RectFill(new Rect(area.x-12,y,7,1),Gold*.55f);
                    RectFill(new Rect(area.xMax+5,y,7,1),Gold*.55f);
                }
            }
        }

        private void DrawCityFacilityEffectLedger(Rect area,CityBuildingDefinition definition,int current,int next,bool maximum)
        {
            RectFill(area,C("202A28"));
            RectFill(new Rect(area.x,area.y,area.width,1),C("756B51"));
            float half=(area.width-64)/2;
            Label(area.x+16,area.y+8,half-16,22,"当前 Lv. "+current,hudSmall);
            Label(area.x+half+64,area.y+8,half-16,22,maximum?"最高等级":"完工 Lv. "+next,hudSmall);
            Label(area.x+16,area.y+34,half-16,35,CityCatalog.EffectAtLevel(definition.kind,current).ToString(),new GUIStyle(heading){fontSize=29,normal={textColor=Text}});
            Label(area.x+half+64,area.y+34,half-16,35,CityCatalog.EffectAtLevel(definition.kind,next).ToString(),new GUIStyle(heading){fontSize=29,normal={textColor=Gold}});
            GUI.Label(new Rect(area.center.x-30,area.y+26,60,40),maximum?"—":"››",new GUIStyle(heading){alignment=TextAnchor.MiddleCenter});
            Label(area.x+16,area.y+72,area.width-32,22,CityFacilityEffectUnit(definition.kind).Trim(),hudSmall);
        }
    }
}
