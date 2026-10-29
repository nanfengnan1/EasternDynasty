using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D citySheetEffect,citySheetCost,citySheetArrow,citySheetCommand,citySheetCommandHover;
        private bool citySheetTextFits;
        private Rect citySheetDescriptionRect,citySheetEffectRect;

        private void InitCityFacilitySheet()
        {
            if(citySheetEffect!=null)return;
            citySheetEffect=GameAssets.LoadTexture("ui.youzhou.facility_effect");
            citySheetCost=GameAssets.LoadTexture("ui.youzhou.facility_cost");
            citySheetArrow=GameAssets.LoadTexture("ui.youzhou.facility_arrow");
            citySheetCommand=GameAssets.LoadTexture("ui.youzhou.facility_command");
            citySheetCommandHover=GameAssets.LoadTexture("ui.youzhou.facility_command_hover");
        }
        private static Rect CitySheetRect(Rect parent,CityFacilitySheetLayout.Box box)
        { return new Rect(parent.x+box.X,parent.y+box.Y,box.W,box.H); }
        private void CitySheetText(Rect area,string value,GUIStyle style,int minimum=13,bool wrap=false)
        {
            var fitted=new GUIStyle(style){wordWrap=wrap,clipping=TextClipping.Clip};
            var text=new GUIContent(value);
            while(fitted.fontSize>minimum&&!HeaderTextFits(area,text,fitted,wrap))fitted.fontSize--;
            citySheetTextFits&=HeaderTextFits(area,text,fitted,wrap);
            GUI.Label(area,text,fitted);
        }
        private void DrawCitySheetEffects(float width,CityBuildingDefinition definition,int current,int next,bool maximum)
        {
            var origin=new Rect();
            GUI.DrawTexture(CitySheetRect(origin,CityFacilitySheetLayout.Effect(width)),citySheetEffect,ScaleMode.StretchToFill,true);
            CitySheetText(CitySheetRect(origin,CityFacilitySheetLayout.Unit(width)),CityFacilityEffectUnit(definition.kind).Trim(),
                new GUIStyle(body){fontSize=18,alignment=TextAnchor.MiddleLeft,normal={textColor=C("D8C68F")}});
            var before=CitySheetRect(origin,CityFacilitySheetLayout.Current(width));
            var after=CitySheetRect(origin,CityFacilitySheetLayout.Next(width));
            CitySheetText(new Rect(before.x,before.y,before.width,22),"当前 Lv. "+current,new GUIStyle(small){fontSize=14});
            CitySheetText(new Rect(before.x,before.y+24,before.width,40),CityCatalog.EffectAtLevel(definition.kind,current).ToString(),
                new GUIStyle(heading){fontSize=28,normal={textColor=Text}});
            CitySheetText(new Rect(after.x,after.y,after.width,22),maximum?"最高等级":"完工 Lv. "+next,new GUIStyle(small){fontSize=14});
            CitySheetText(new Rect(after.x,after.y+24,after.width,40),maximum?"—":CityCatalog.EffectAtLevel(definition.kind,next).ToString(),
                new GUIStyle(heading){fontSize=28,normal={textColor=C("94C1A2")}});
            if(!maximum)GUI.DrawTexture(CitySheetRect(origin,CityFacilitySheetLayout.Arrow(width)),citySheetArrow,ScaleMode.StretchToFill,true);
        }
        private bool CitySheetAction(Rect area,string id,string caption,bool enabled=true)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool hover=GUI.enabled&&area.Contains(Event.current.mousePosition);
            var old=GUI.color;
            if(!enabled)GUI.color=new Color(old.r,old.g,old.b,old.a*.4f);
            GUI.DrawTexture(area,hover?citySheetCommandHover:citySheetCommand,ScaleMode.StretchToFill,true);
            GUI.color=old;
            CitySheetText(new Rect(area.x+8,area.y+1,area.width-16,42),caption,
                new GUIStyle(body){fontSize=18,alignment=TextAnchor.MiddleCenter,normal={textColor=C(enabled?"202526":"D1D4D4")}});
            bool clicked=ContentTileClicked(area,id,-1);
            GUI.enabled=previous;return clicked;
        }
    }
}
