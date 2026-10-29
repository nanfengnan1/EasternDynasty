using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D hudHeaderVeil,hudHeaderCurrency,hudHeaderIdentity,hudHeaderLedger,hudHeaderCrest,hudHeaderCrestHover;
        private GUIStyle hudHeaderStock,hudHeaderRate,hudHeaderSecondary,hudHeaderRuler;
        private bool hudHeaderTextFits;

        private void DrawDesignedMapHeader()
        {
            InitMapHudStyles();
            if(hudHeaderVeil==null)
            {
                hudHeaderVeil=GameAssets.LoadTexture("ui.youzhou.header_veil");
                hudHeaderCurrency=GameAssets.LoadTexture("ui.youzhou.header_currency");
                hudHeaderIdentity=GameAssets.LoadTexture("ui.youzhou.header_identity");
                hudHeaderLedger=GameAssets.LoadTexture("ui.youzhou.header_ledger");
                hudHeaderCrest=GameAssets.LoadTexture("ui.youzhou.city_marker_crest");
                hudHeaderCrestHover=GameAssets.LoadTexture("ui.youzhou.city_marker_selected");
                hudHeaderStock=new GUIStyle(hudNumber){fontSize=21,alignment=TextAnchor.MiddleLeft};
                hudHeaderRate=new GUIStyle(hudSmall){fontSize=13,alignment=TextAnchor.MiddleLeft};
                hudHeaderRate.normal.textColor=C("D4C797");
                hudHeaderSecondary=new GUIStyle(hudSmall){fontSize=13,alignment=TextAnchor.MiddleLeft};
                hudHeaderSecondary.normal.textColor=C("C8C7B7");
                hudHeaderRuler=new GUIStyle(hudHeaderStock){fontSize=20};
            }
            hudHeaderTextFits=true;
            var bar=new Rect(0,0,width,70);Block(bar);
            GUI.DrawTexture(bar,hudHeaderVeil,ScaleMode.StretchToFill,true);
            var city=HudDesignRect(MapHudHeaderLayout.Identity);
            var crest=HudDesignRect(MapHudHeaderLayout.Crest);
            GUI.DrawTexture(city,hudHeaderIdentity,ScaleMode.StretchToFill,true);
            bool hover=GUI.enabled&&city.Contains(Event.current.mousePosition);
            GUI.DrawTexture(crest,hover?hudHeaderCrestHover:hudHeaderCrest,ScaleMode.ScaleToFit,true);
            var cityArt=HudDesignRect(MapHudHeaderLayout.IdentityArt);
            GUI.DrawTexture(cityArt,hudCity,ScaleMode.ScaleToFit,true);
            bool wrapped=hudHeaderRuler.CalcSize(new GUIContent(world.ruler)).x>174;
            var name=HudDesignRect(MapHudHeaderLayout.Ruler(wrapped));
            DrawHeaderText(name,world.ruler,wrapped?new GUIStyle(hudHeaderRuler){fontSize=14}:hudHeaderRuler,12,wrapped);
            var identityDetail=HudDesignRect(MapHudHeaderLayout.IdentityDetail(wrapped));
            DrawHeaderText(identityDetail,"幽州 · 领地 "+world.OwnedCount,hudHeaderSecondary,12,false);
            RecordMapHudTarget("hud-ruler-city",city);
            RecordMapHudTarget("hud-ruler-crest",crest);
            RecordMapHudTarget("hud-ruler-art",cityArt);
            RecordMapHudTarget("hud-ruler-name",name);
            RecordMapHudTarget("hud-ruler-detail",identityDetail);
            if(GUI.Button(city,GUIContent.none,GUIStyle.none))OpenCity();
            DrawDesignedMapLedger();
        }

        private void DrawDesignedMapLedger()
        {
            string[] names={"木材","粮食","石料","铁矿","金币","灵玉"};
            double[] amounts={world.resources.grass,world.resources.grain,world.resources.stone,world.resources.iron,world.resources.gold,world.resources.jade};
            var rates=world.HourlyIncome();double[] income={rates.grass,rates.grain,rates.stone,rates.iron,rates.gold,0};
            GUI.DrawTexture(HudDesignRect(MapHudHeaderLayout.Production(width)),hudHeaderLedger,ScaleMode.StretchToFill,true);
            for(int i=0;i<6;i++)
            {
                var cell=MapHudHeaderLayout.Resource(width,i);
                Rect area=HudDesignRect(cell),icon=HudDesignRect(MapHudHeaderLayout.Icon(cell,i));
                Rect stock=HudDesignRect(MapHudHeaderLayout.Stock(cell,i)),detail=HudDesignRect(MapHudHeaderLayout.Detail(cell,i));
                if(i>=4)GUI.DrawTexture(area,hudHeaderCurrency,ScaleMode.StretchToFill,true);
                ResourceIcon(icon,i); // Existing small source icons; no new procedural artwork.
                DrawHeaderText(stock,HudQuantity(amounts[i]),hudHeaderStock,14,false);
                string description=i<5?names[i]+" +"+HudQuantity(income[i])+"/时":"试玩灵玉";
                DrawHeaderText(detail,description,hudHeaderRate,12,false);
                RecordMapHudTarget("hud-resource-"+i,area);
                RecordMapHudTarget("hud-resource-icon-"+i,icon);
                RecordMapHudTarget("hud-resource-stock-"+i,stock);
                RecordMapHudTarget("hud-resource-detail-"+i,detail);
            }
        }

        private void DrawHeaderText(Rect bounds,string text,GUIStyle source,int minimumSize,bool wrap)
        {
            var fitted=new GUIStyle(source){wordWrap=wrap,clipping=TextClipping.Clip};
            var content=new GUIContent(text);
            while(fitted.fontSize>minimumSize&&!HeaderTextFits(bounds,content,fitted,wrap))fitted.fontSize--;
            hudHeaderTextFits&=HeaderTextFits(bounds,content,fitted,wrap);
            GUI.Label(bounds,content,fitted);
        }
        private static bool HeaderTextFits(Rect bounds,GUIContent content,GUIStyle style,bool wrap)
        {
            if(wrap)return style.CalcHeight(content,bounds.width)<=bounds.height+.1f;
            Vector2 actual=style.CalcSize(content);
            return actual.x<=bounds.width+.1f&&actual.y<=bounds.height+.1f;
        }
    }
}
