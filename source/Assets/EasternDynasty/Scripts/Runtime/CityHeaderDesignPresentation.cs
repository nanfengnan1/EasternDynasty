using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityHeaderIdentitySkin, cityHeaderCrestSkin, cityHeaderCrestHoverSkin, cityHeaderResourceSkin, cityHeaderCurrencySkin, cityHeaderBackdropSkin;
        private bool cityHeaderTextFits, cityHeaderNameWrapped, cityHeaderBackdropDrawn;
        private GUIStyle cityHeaderNameStyle, cityHeaderStockStyle, cityHeaderRateStyle, cityHeaderDetailStyle;

        private void InitCityHeaderDesign()
        {
            InitMapHudStyles();
            if (cityHeaderIdentitySkin != null) return;
            cityHeaderBackdropSkin = GameAssets.LoadTexture("ui.youzhou.city_header_backdrop");
            cityHeaderIdentitySkin = GameAssets.LoadTexture("ui.youzhou.header_identity");
            cityHeaderCrestSkin = GameAssets.LoadTexture("ui.youzhou.city_marker_crest");
            cityHeaderCrestHoverSkin = GameAssets.LoadTexture("ui.youzhou.city_marker_selected");
            cityHeaderResourceSkin = GameAssets.LoadTexture("ui.youzhou.header_ledger");
            cityHeaderCurrencySkin = GameAssets.LoadTexture("ui.youzhou.header_currency");
            cityHeaderNameStyle = new GUIStyle(hudNumber) { fontSize = 20, alignment = TextAnchor.MiddleLeft };
            cityHeaderStockStyle = new GUIStyle(hudNumber) { fontSize = 21, alignment = TextAnchor.MiddleLeft };
            cityHeaderRateStyle = new GUIStyle(hudSmall) { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = C("D4C797") } };
            cityHeaderDetailStyle = new GUIStyle(hudSmall) { fontSize = 13, alignment = TextAnchor.MiddleLeft, normal = { textColor = C("C8C7B7") } };
        }

        private void CityHeaderText(Rect bounds, string text, GUIStyle style, int minimum, bool wrap = false)
        {
            var fitted = new GUIStyle(style) { wordWrap = wrap, clipping = TextClipping.Clip };
            var content = new GUIContent(text);
            while (fitted.fontSize > minimum && !HeaderTextFits(bounds, content, fitted, wrap)) fitted.fontSize--;
            cityHeaderTextFits &= HeaderTextFits(bounds, content, fitted, wrap);
            GUI.Label(bounds, content, fitted);
        }

        private void DrawDesignedCityHeader()
        {
            InitCityHeaderDesign(); cityHeaderTextFits = true; cityHeaderBackdropDrawn = false;
            var backdrop = HudDesignRect(CityHeaderLayout.Backdrop(width));
            Block(backdrop);
            // On city pages the camera clears only below y=70. Repaint the whole
            // band before translucent widgets, including its otherwise empty gaps.
            // A disabled/modal GUI must never turn this clear into alpha blending.
            Color previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(backdrop, cityHeaderBackdropSkin, ScaleMode.StretchToFill, false);
            GUI.color = previousColor;
            cityHeaderBackdropDrawn = true;
            RecordCityOverviewTarget("city-header-backdrop", backdrop);
            cityHudIdentityArea = HudDesignRect(CityHeaderLayout.Identity);
            cityHudArtArea = HudDesignRect(CityHeaderLayout.Art);
            bool hover = GUI.enabled && cityHudIdentityArea.Contains(Event.current.mousePosition);
            GUI.DrawTexture(cityHudIdentityArea, cityHeaderIdentitySkin, ScaleMode.StretchToFill, true);
            Rect crest = HudDesignRect(CityHeaderLayout.Crest);
            GUI.DrawTexture(crest, hover ? cityHeaderCrestHoverSkin : cityHeaderCrestSkin, ScaleMode.ScaleToFit, true);
            DrawCityFacilityArt(cityHudArtArea, CityBuilding.TownHall);
            cityHudDisplayedName = world.lands[world.homeId].name;
            cityHudDisplayedLevel = world.CityLevel(CityBuilding.TownHall);
            // Retain the existing diagnostic field, now containing the complete visible name.
            cityHudShortName = cityHudDisplayedName;
            cityHeaderNameWrapped = cityHeaderNameStyle.CalcSize(new GUIContent(cityHudDisplayedName)).x > CityHeaderLayout.Name(false).W;
            cityHudNameArea = HudDesignRect(CityHeaderLayout.Name(cityHeaderNameWrapped));
            CityHeaderText(cityHudNameArea, cityHudDisplayedName,
                cityHeaderNameWrapped ? new GUIStyle(cityHeaderNameStyle) { fontSize = 14 } : cityHeaderNameStyle, 12, cityHeaderNameWrapped);
            CityHeaderText(HudDesignRect(CityHeaderLayout.IdentityDetail), "幽州 · 府邸 " + cityHudDisplayedLevel + " 级", cityHeaderDetailStyle, 12);
            RecordCityOverviewTarget("city-hud-home", cityHudIdentityArea);
            RecordCityOverviewTarget("city-hud-home-crest", crest);
            RecordCityOverviewTarget("city-hud-home-art", cityHudArtArea);
            RecordCityOverviewTarget("city-hud-home-name", cityHudNameArea);
            RecordCityOverviewTarget("city-hud-home-detail", HudDesignRect(CityHeaderLayout.IdentityDetail));
            if (ContentTileClicked(cityHudIdentityArea, "city-hud-home", -1)) OpenCity();
            DrawDesignedCityLedger();
        }

        private void DrawDesignedCityLedger()
        {
            string[] names = { "木材", "粮食", "石料", "铁矿", "金币", "灵玉" };
            double[] stocks = { world.resources.grass, world.resources.grain, world.resources.stone, world.resources.iron, world.resources.gold, world.resources.jade };
            var rates = world.HourlyIncome();
            double[] income = { rates.grass, rates.grain, rates.stone, rates.iron, rates.gold, 0 };
            var production = HudDesignRect(CityHeaderLayout.Production(width));
            GUI.DrawTexture(production, cityHeaderResourceSkin, ScaleMode.StretchToFill, true);
            RecordCityOverviewTarget("city-header-production", production);
            for (int i = 0; i < 6; i++)
            {
                var cell = CityHeaderLayout.Resource(width, i);
                Rect area = HudDesignRect(cell), icon = HudDesignRect(CityHeaderLayout.Icon(cell, i));
                Rect stock = HudDesignRect(CityHeaderLayout.Stock(cell, i)), detail = HudDesignRect(CityHeaderLayout.Detail(cell, i));
                if (i >= 4) GUI.DrawTexture(area, cityHeaderCurrencySkin, ScaleMode.StretchToFill, true);
                ResourceIcon(icon, i);
                CityHeaderText(stock, HudQuantity(stocks[i]), cityHeaderStockStyle, 14);
                CityHeaderText(detail, i < 5 ? names[i] + " +" + HudQuantity(income[i]) + "/时" : "试玩灵玉",
                    cityHeaderRateStyle, 12);
                RecordMapHudTarget("hud-resource-" + i, area);
                RecordMapHudTarget("hud-resource-icon-" + i, icon);
                RecordMapHudTarget("hud-resource-stock-" + i, stock);
                RecordMapHudTarget("hud-resource-detail-" + i, detail);
            }
        }

        private void AssertCityHeaderDesign()
        {
            if (!cityHeaderTextFits || !cityHeaderBackdropDrawn || cityHeaderBackdropSkin == null
                || cityHeaderBackdropSkin.width != 1440 || cityHeaderBackdropSkin.height != 70
                || cityHeaderIdentitySkin == null || cityHeaderCrestSkin == null || cityHeaderCrestHoverSkin == null
                || cityHeaderResourceSkin == null || cityHeaderCurrencySkin == null || cityHudShortName != cityHudDisplayedName)
                throw new Exception("City header design assets, complete home name or actual font fit are missing.");
            var identity = HudDesignRect(CityHeaderLayout.Identity);
            if ((cityHudIdentityArea.position - identity.position).sqrMagnitude > .01f || (cityHudIdentityArea.size - identity.size).sqrMagnitude > .01f
                || cityHudArtArea != HudDesignRect(CityHeaderLayout.Art) || cityHudNameArea != HudDesignRect(CityHeaderLayout.Name(cityHeaderNameWrapped)))
                throw new Exception("City header identity geometry differs from the inspected Inkscape draft.");
            AssertCityCompositionTarget("city-header-backdrop", HudDesignRect(CityHeaderLayout.Backdrop(width)), new Rect(0, 0, width, 70));
            AssertCityCompositionTarget("city-hud-home-crest", HudDesignRect(CityHeaderLayout.Crest), identity);
            AssertCityCompositionTarget("city-hud-home-detail", HudDesignRect(CityHeaderLayout.IdentityDetail), identity);
            AssertCityCompositionTarget("city-header-production", HudDesignRect(CityHeaderLayout.Production(width)), new Rect(0, 0, width, 70));
            for (int i = 0; i < 6; i++)
            {
                var cell = CityHeaderLayout.Resource(width, i);
                var area = HudDesignRect(cell);
                AssertCityCompositionTarget("hud-resource-" + i, area, new Rect(0, 0, width, 70));
                AssertCityCompositionTarget("hud-resource-icon-" + i, HudDesignRect(CityHeaderLayout.Icon(cell, i)), area);
                AssertCityCompositionTarget("hud-resource-stock-" + i, HudDesignRect(CityHeaderLayout.Stock(cell, i)), area);
                AssertCityCompositionTarget("hud-resource-detail-" + i, HudDesignRect(CityHeaderLayout.Detail(cell, i)), area);
            }
        }
    }
}
