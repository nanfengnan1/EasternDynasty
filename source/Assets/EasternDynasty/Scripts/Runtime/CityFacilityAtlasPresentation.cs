using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityCatalogSheet,cityCatalogRule,cityCatalogCategorySkin,cityCatalogCategoryActive;
        private bool cityCatalogDesignTextFits,cityCatalogDesignReady;
        private void InitCityCatalogDesign()
        {
            InitCityFacilitySheet();
            if(cityCatalogSheet==null)
            {
                cityCatalogSheet=GameAssets.LoadTexture("ui.youzhou.catalog_sheet");
                cityCatalogRule=GameAssets.LoadTexture("ui.youzhou.catalog_rule");
                cityCatalogCategorySkin=GameAssets.LoadTexture("ui.youzhou.catalog_category");
                cityCatalogCategoryActive=GameAssets.LoadTexture("ui.youzhou.catalog_category_active");
            }
            cityCatalogDesignReady=cityCatalogSheet.width==1024&&cityCatalogSheet.height==640
                &&cityCatalogRule.width==1024&&cityCatalogRule.height==2
                &&cityCatalogCategorySkin.width==368&&cityCatalogCategorySkin.height==88
                &&cityCatalogCategoryActive.width==368&&cityCatalogCategoryActive.height==88
                &&citySheetCommand.width==720&&citySheetCommand.height==88
                &&citySheetCommandHover.width==720&&citySheetCommandHover.height==88;
        }
        private static Rect CityAtlasRect(Rect parent,CityFacilityAtlasLayout.Box box)
        {return new Rect(parent.x+box.X,parent.y+box.Y,box.W,box.H);}
        private void CityCatalogText(Rect rect,string value,int size,Color color,TextAnchor alignment=TextAnchor.MiddleLeft,int minimum=13)
        {
            var style=new GUIStyle(body){fontSize=size,alignment=alignment,wordWrap=false,clipping=TextClipping.Clip,normal={textColor=color}};
            var content=new GUIContent(value);
            while(style.fontSize>minimum&&!HeaderTextFits(rect,content,style,false))style.fontSize--;
            cityCatalogDesignTextFits&=HeaderTextFits(rect,content,style,false);
            GUI.Label(rect,content,style);
        }
        private bool CityCatalogAction(Rect area,string id,string caption,bool enabled=true,bool category=false,bool active=false)
        {
            bool previous=GUI.enabled;GUI.enabled=previous&&enabled;
            bool hover=GUI.enabled&&area.Contains(Event.current.mousePosition);
            var texture=category?(active||hover?cityCatalogCategoryActive:cityCatalogCategorySkin):(hover?citySheetCommandHover:citySheetCommand);
            var previousColor=GUI.color;
            if(!enabled)GUI.color=new Color(previousColor.r,previousColor.g,previousColor.b,previousColor.a*.4f);
            GUI.DrawTexture(area,texture,ScaleMode.StretchToFill,true);GUI.color=previousColor;
            CityCatalogText(new Rect(area.x+8,area.y+1,area.width-16,42),caption,category?17:16,
                C(category?"E3E2D9":enabled?"202526":"D1D4D4"),TextAnchor.MiddleCenter);
            bool clicked=ContentTileClicked(area,id,-1);
            GUI.enabled=previous;return clicked;
        }
        private Rect CityCatalogAbsolute(Rect view,Rect local)
        {return new Rect(view.x+local.x,view.y+local.y-cityCatalogScroll.y,local.width,local.height);}
    }
}
