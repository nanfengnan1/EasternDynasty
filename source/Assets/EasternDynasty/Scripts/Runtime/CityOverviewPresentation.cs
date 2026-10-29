using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private Texture2D cityOverviewTexture;
        private int cityOverviewSignature = int.MinValue;
        private GUIStyle cityOverviewLabel;
        private GUIStyle cityOverviewLevelLabel;
        private bool cityOverviewPlanning;
        private bool cityOverviewExpanded;
        private readonly CityPanoramaCamera cityPanoramaCamera = new CityPanoramaCamera();
        private bool cityPanoramaPressed, cityPanoramaDragging;
        private Vector2 cityPanoramaPointerStart, cityPanoramaPointerPrevious;
        private Rect cityPanoramaViewport, cityPanoramaImage, cityPanoramaLandscape, cityPanoramaCanvas;
        private string cityPanoramaTitleText;
        private Rect cityPanoramaTitleArea;
        private float cityPanoramaTitleMeasuredHeight;
        private readonly System.Collections.Generic.List<Rect> cityPanoramaVisibleControls = new System.Collections.Generic.List<Rect>();
        private readonly System.Collections.Generic.List<CityPanoramaBuildingControl> cityPanoramaBuildings = new System.Collections.Generic.List<CityPanoramaBuildingControl>();
        private int cityPanoramaVisibleSites;
        private Rect cityOverviewSceneArea, cityOverviewAffairsArea, cityOverviewArmyArea;
        private readonly System.Collections.Generic.List<Rect> cityOverviewGeneralRects = new System.Collections.Generic.List<Rect>();
        private readonly System.Collections.Generic.List<Rect> cityOverviewGeneralArtRects = new System.Collections.Generic.List<Rect>();
        private readonly System.Collections.Generic.List<Rect> cityOverviewGeneralNameRects = new System.Collections.Generic.List<Rect>();
        private Rect cityAffairsArtArea, cityAffairsSummaryArea, cityAffairsQuestArtArea;
        private CityBuilding cityAffairsDisplayedBuilding;
        private int cityAffairsDisplayedLevel;
        private bool cityAffairsWorking;
        private Rect cityArmyStrengthArea, cityArmySupplyArea;
        private readonly System.Collections.Generic.List<Rect> cityArmyActionArtRects = new System.Collections.Generic.List<Rect>();
        private readonly System.Collections.Generic.List<Rect> cityArmyActionCaptionRects = new System.Collections.Generic.List<Rect>();
        private readonly System.Collections.Generic.List<Rect> cityArmySupplyIconRects = new System.Collections.Generic.List<Rect>();
        private int cityArmyDisplayedTroops, cityArmyDisplayedCap, cityArmyDisplayedRecruit;
        private readonly double[] cityArmyDisplayedCosts = new double[3];
        private bool cityArmyBusy;
        private struct CityPanoramaBuildingControl { internal int index; internal Rect label, hit; }

        private void RecordCityOverviewTarget(string id, Rect rect)
        {
            // Modal cards and reward sheets cover the underlying city controls.
            // Their disabled background must not be advertised as clickable evidence.
            if (GUI.enabled) RecordReviewTarget(id, rect);
        }

        private void ResetCityPanorama()
        { cityOverviewExpanded = false; cityPanoramaCamera.Reset(); CancelCityPanoramaPointer(); }

        private void CancelCityPanoramaPointer()
        { cityPanoramaPressed = cityPanoramaDragging = false; }

        private void SetCityPanoramaExpanded(bool expanded)
        { cityOverviewExpanded = expanded; CancelCityPanoramaPointer(); map.CancelPointerInput(); }

        private bool CityOverviewPlotAvailable(CityPlot plot)
        {
            return plot != null && plot.building == CityBuilding.None && world.IsCityDistrict(plot.landId)
                && (world.CityJob == null || world.CityJob.plotIndex != plot.index);
        }

        private void SetCityOverviewPlanning(bool planning)
        { cityOverviewPlanning = planning; map.CancelPointerInput(); }

        private void SelectCityOverviewPlot(int index)
        {
            // Recheck the live plot at the click. Selection itself never reserves a queue or pays.
            if (index < 0 || index >= world.cityPlots.Count || !CityOverviewPlotAvailable(world.cityPlots[index])) return;
            OpenCityFacilities(index);
        }

        // An original, schematic interior. The historical reference establishes the panorama /
        // army-strip hierarchy only; neither its buildings nor its extra armies are game state.
        private void DrawCityOverview(Rect area)
        {
            InitMapHudStyles();
            cityOverviewGeneralRects.Clear();
            cityOverviewGeneralArtRects.Clear(); cityOverviewGeneralNameRects.Clear();
            if (cityOverviewExpanded) { DrawCityPanorama(area); return; }
            // Keep the city at full content height. The former full-width army strip
            // reduced the landscape to a small letterbox on wide mobile screens.
            // Taller screens have room for wider complete paintings, not just taller
            // empty card frames. The mobile minimum keeps the full-height city dominant.
            float railWidth = CityOverviewLayout.ArmyRailWidth(area.height);
            cityOverviewSceneArea = new Rect(area.x, area.y, area.width - railWidth - 12, area.height);
            cityOverviewAffairsArea = new Rect(cityOverviewSceneArea.xMax + 12, area.y, railWidth, 218);
            cityOverviewArmyArea = new Rect(cityOverviewAffairsArea.x, area.y + 230, railWidth, CityArmyLayout.Height(railWidth, area.height - 230));
            DrawCityPanorama(cityOverviewSceneArea);
            DrawCityOverviewAffairs(cityOverviewAffairsArea);
            DrawCityOverviewArmy(cityOverviewArmyArea);
        }

        private int CityOverviewStateSignature()
        {
            unchecked
            {
                int signature = 17;
                foreach (var plot in world.cityPlots)
                    signature = signature * 31 + plot.index * 101 + (int)plot.building * 11 + plot.level;
                var job = world.CityJob;
                return signature * 31 + (job == null ? 0 : 1 + job.plotIndex * 11 + (int)job.building * 101);
            }
        }

        private void DrawCityPanorama(Rect area)
        {
            int signature = CityOverviewStateSignature();
            if (cityOverviewTexture == null || cityOverviewSignature != signature)
            {
                if (cityOverviewTexture != null) Destroy(cityOverviewTexture);
                cityOverviewTexture = BuildCityOverviewTexture(); cityOverviewSignature = signature;
            }
            cityPanoramaCanvas = area;
            // Artwork and facility input share the raised viewport. The surrounding
            // landscape continues behind its floating edge controls; FitCanvas also
            // covers narrow/tall screens without stretching the town or its targets.
            var viewport = new Rect(area.x + 1, area.y + 60, area.width - 2, area.height - 90);
            HandleCityPanoramaPointer(viewport);
            var bounds = cityPanoramaCamera.FitCanvas(viewport.width, viewport.height,60,30);
            var image = new Rect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            cityPanoramaViewport = viewport;
            cityPanoramaImage = new Rect(viewport.x + image.x, viewport.y + image.y, image.width, image.height);
            var landscape = CityOverviewRect(image, -CitySceneRaster.OverviewOffsetX, -CitySceneRaster.OverviewOffsetY,
                CitySceneRaster.OverviewWidth, CitySceneRaster.OverviewHeight);
            cityPanoramaLandscape = CityPanoramaScreenRect(landscape, viewport);
            cityPanoramaVisibleControls.Clear(); cityPanoramaBuildings.Clear(); cityPanoramaVisibleSites = 0;
            RecordCityOverviewTarget("city-panorama-viewport", viewport);
            GUI.BeginGroup(area);
            GUI.DrawTexture(new Rect(cityPanoramaLandscape.x-area.x,cityPanoramaLandscape.y-area.y,
                cityPanoramaLandscape.width,cityPanoramaLandscape.height),cityOverviewTexture,ScaleMode.StretchToFill,false);
            GUI.EndGroup();
            DrawCityPanoramaToolbar(new Rect(area.x + 10, area.y + 2, area.width - 20, 56));
            GUI.BeginGroup(viewport);
            if (cityOverviewLabel == null)
            {
                cityOverviewLabel = new GUIStyle(tiny) { alignment = TextAnchor.MiddleLeft, fontSize = 14, normal = { textColor = Text } };
                cityOverviewLevelLabel = new GUIStyle(tiny) { alignment = TextAnchor.MiddleCenter, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Gold } };
            }

            foreach (var plot in world.cityPlots.OrderBy(p => p.index / 3 + p.index % 3))
            {
                bool building = plot.building != CityBuilding.None;
                bool construction = world.CityJob != null && world.CityJob.plotIndex == plot.index;
                if (!building && !construction) continue;
                var kind = building ? plot.building : world.CityJob.building;
                var point = CityOverviewPlotPoint(plot.index);
                bool hall = kind == CityBuilding.TownHall;
                float buildingHeight = hall ? 119 : 85;
                float buildingWidth = hall ? 180 : 148;
                var hit = CityOverviewRect(image, point.x - buildingWidth / 2, point.y - buildingHeight, buildingWidth, buildingHeight + 25);
                // A narrow name ribbon and a level seal leave the facade visible. The
                // complete 44px plaque is also the real touch target, independent of zoom.
                float labelWidth = hall ? 142 : 126;
                var label = new Rect(hit.center.x - labelWidth / 2, hit.yMax - 18, labelWidth, 44);
                DrawCityFacilityPlaque(label, kind, plot.level, construction);
                // Register only the same visible geometry that receives the pointer input.
                hit = cityOverviewPlanning ? label : Rect.MinMaxRect(Mathf.Max(0, Mathf.Min(hit.x, label.x)), Mathf.Max(0, hit.y), Mathf.Min(viewport.width, Mathf.Max(hit.xMax, label.xMax)), Mathf.Min(viewport.height, label.yMax));
                if (CityPanoramaControlVisible(label, viewport))
                {
                    cityPanoramaVisibleControls.Add(label);
                    cityPanoramaVisibleControls.Add(hit);
                    cityPanoramaBuildings.Add(new CityPanoramaBuildingControl { index = plot.index, label = label, hit = hit });
                    RecordCityOverviewTarget("city-building-" + plot.index, CityPanoramaScreenRect(label, viewport));
                }
            }
            // Labels always win over neighbouring artwork. Overlapping bodies then follow
            // the raster's painter order: the frontmost visible building receives the click.
            for (int i = cityPanoramaBuildings.Count - 1; i >= 0; i--)
            {
                var control = cityPanoramaBuildings[i];
                if (ContentTileClicked(control.label, "city-building-label-" + control.index, -1)) OpenCityFacilities(control.index);
            }
            if (!cityOverviewPlanning)
                for (int i = cityPanoramaBuildings.Count - 1; i >= 0; i--)
                {
                    var control = cityPanoramaBuildings[i];
                    if (ContentTileClicked(control.hit, "city-building-body-" + control.index, -1)) OpenCityFacilities(control.index);
                }
            if (cityOverviewPlanning) DrawCityOverviewSites(image, viewport);
            GUI.EndGroup();
            var legend=new Rect(area.x+10,area.yMax-28,Mathf.Min(522,area.width-20),24);
            RectFill(legend,new Color(.035f,.055f,.055f,.75f));
            Label(legend.x+8,legend.y+2,legend.width-16,20,
                cityOverviewPlanning ? "点击空地查看费用与前置 · 拖动浏览 · 总览可找回全部位置" : "菱章为设施等级 · 工字为施工进度 · 点击名称管理 · 拖动浏览", new GUIStyle(tiny) { normal = { textColor = C("D1D8C5") } });
            // Corner inlays frame the landscape without restoring a heavy panel.
            foreach(var corner in new[]{new Rect(area.x,area.y,20,2),new Rect(area.x,area.y,2,14),
                new Rect(area.xMax-20,area.yMax-2,20,2),new Rect(area.xMax-2,area.yMax-14,2,14)})RectFill(corner,Gold*.7f);
        }

        private static bool CityPanoramaControlVisible(Rect rect, Rect viewport)
        { return rect.x >= 0 && rect.y >= 0 && rect.xMax <= viewport.width && rect.yMax <= viewport.height; }

        private static Rect CityPanoramaScreenRect(Rect rect, Rect viewport)
        { return new Rect(rect.x + viewport.x, rect.y + viewport.y, rect.width, rect.height); }

        private void DrawCityFacilityPlaque(Rect rect, CityBuilding kind, int level, bool construction)
        {
            bool hall = kind == CityBuilding.TownHall;
            bool hover = GUI.enabled && !mobileMode && rect.Contains(Event.current.mousePosition);
            Color accent = construction ? C("E7AF65") : hall ? Gold : C("C7C7AE");
            // The faint backing occupies the entire clickable plaque; the heavier ink
            // stays behind the name, instead of blanketing a full building-width slab.
            RectFill(rect, new Color(.05f, .07f, .07f, hover ? .42f : .12f));
            var ribbon = new Rect(rect.x + 22, rect.y + 9, rect.width - 22, 26);
            RectFill(ribbon, new Color(.035f, .055f, .055f, hover ? .95f : .79f));
            RectFill(new Rect(ribbon.x, ribbon.y, ribbon.width - 14, 1), new Color(accent.r, accent.g, accent.b, .65f));
            RectFill(new Rect(ribbon.xMax - 14, ribbon.y, 14, ribbon.height), new Color(accent.r, accent.g, accent.b, .06f));
            var seal = new Rect(rect.x + 1, rect.y + 6, 32, 32);
            for (int row = 0; row < 32; row++)
            {
                float inset = Math.Abs(15.5f - row);
                float span = seal.width - inset * 2;
                RectFill(new Rect(seal.x + inset, seal.y + row, span, 1), C(hall ? "4E3926" : "182625"));
                // Local scanlines stay inside BeginGroup's clip on desktop and mobile.
                // Rotating GUI.matrix here applied the group translation twice and sent
                // detached diamond edges into the toolbar and even outside the panorama.
                float edge = Math.Min(1.5f, span);
                RectFill(new Rect(seal.x + inset, seal.y + row, edge, 1), accent);
                RectFill(new Rect(seal.x + inset + span - edge, seal.y + row, edge, 1), accent);
            }
            cityOverviewLevelLabel.normal.textColor = accent;
            GUI.Label(seal, construction ? "工" : level.ToString(), cityOverviewLevelLabel);
            GUI.Label(new Rect(rect.x + 39, rect.y + 8, rect.width - 43, 28), CityCatalog.Get(kind).name, cityOverviewLabel);
            if (construction)
            {
                var job = world.CityJob;
                float progress = Mathf.Clamp01((float)(1 - job.remaining / Math.Max(1, job.duration)));
                var track = new Rect(rect.x + 36, rect.yMax - 6, rect.width - 40, 3);
                RectFill(track, C("3C3E32"));
                RectFill(new Rect(track.x, track.y, track.width * progress, track.height), accent);
            }
            else if (hall)
            {
                RectFill(new Rect(rect.x + 39, rect.yMax - 6, 26, 1), accent);
            }
        }

        private void HandleCityPanoramaPointer(Rect viewport)
        {
            var pointer = Event.current;
            if (!GUI.enabled || mobileMultiTouch || (mobileMode && Time.frameCount <= mobileSuppressUiThrough) || pointer.type == EventType.Ignore)
            { CancelCityPanoramaPointer(); return; }
            if (pointer.type == EventType.ScrollWheel && viewport.Contains(pointer.mousePosition))
            { cityPanoramaCamera.SetZoom(cityPanoramaCamera.Zoom - pointer.delta.y * .08f); pointer.Use(); return; }
            if (pointer.type == EventType.MouseDown && pointer.button == 0 && viewport.Contains(pointer.mousePosition))
            { cityPanoramaPressed = true; cityPanoramaDragging = false; cityPanoramaPointerStart = cityPanoramaPointerPrevious = pointer.mousePosition; }
            if (!cityPanoramaPressed) return;
            if (pointer.rawType == EventType.MouseDrag)
            {
                float threshold = Mathf.Clamp(Screen.dpi > 0 ? Screen.dpi / 160f * 8f : 12f, 10f, 30f) / Mathf.Max(.01f, scale);
                if ((pointer.mousePosition - cityPanoramaPointerStart).sqrMagnitude >= threshold * threshold) cityPanoramaDragging = true;
                if (cityPanoramaDragging)
                {
                    var delta = pointer.mousePosition - cityPanoramaPointerPrevious;
                    cityPanoramaCamera.Pan(delta.x, delta.y);
                    GUIUtility.hotControl = 0; tilePointerPressed = false; pointer.Use();
                }
                cityPanoramaPointerPrevious = pointer.mousePosition;
            }
            if (pointer.rawType == EventType.MouseUp)
            {
                if (cityPanoramaDragging) { GUIUtility.hotControl = 0; tilePointerPressed = false; pointer.Use(); }
                CancelCityPanoramaPointer();
            }
        }

        private void DrawCityOverviewSites(Rect scene, Rect viewport)
        {
            foreach (var plot in world.cityPlots)
            {
                if (plot.building != CityBuilding.None || (world.CityJob != null && world.CityJob.plotIndex == plot.index)) continue;
                var point = CityOverviewPlotPoint(plot.index);
                var anchor = CityOverviewRect(scene, point.x, point.y, 0, 0);
                var site = new Rect(anchor.x - 60, anchor.y - 18, 120, 38);
                bool available = CityOverviewPlotAvailable(plot);
                Frame(site, available ? C("263F34") : C("313C39"));
                Outline(site, available ? Green : Border, 1);
                GUI.Label(site, available ? "＋ 空地 · " + (plot.index + 1) : "地形保留", new GUIStyle(small) { alignment = TextAnchor.MiddleCenter });
                // Only the visible, legal plot receives an actionable target. Reserved terrain is inert.
                if (!available || !CityPanoramaControlVisible(site, viewport)) continue;
                cityPanoramaVisibleControls.Add(site); cityPanoramaVisibleSites++;
                RecordCityOverviewTarget("city-site-" + plot.index, CityPanoramaScreenRect(site, viewport));
                if (ContentTileClicked(site, "city-site-" + plot.index, -1)) SelectCityOverviewPlot(plot.index);
            }
        }

        private static Rect CityOverviewRect(Rect scene, float x, float y, float w, float h)
        { return new Rect(scene.x + x / 1000 * scene.width, scene.y + y / 440 * scene.height, w / 1000 * scene.width, h / 440 * scene.height); }

        private static Vector2 CityOverviewPlotPoint(int index)
        { return CitySceneRaster.PlotPoint(index); }

        private void DrawCityOverviewArmy(Rect area)
        {
            // Inspected Inkscape design: independent cards and two short ledgers.
            // Fit the actual paintings instead of stretching empty portrait mats.
            InitCityArmyDesign(); cityArmyTextFits = true;
            float x = area.x + 8, w = area.width - 16;
            cityArmyActionArtRects.Clear(); cityArmyActionCaptionRects.Clear(); cityArmySupplyIconRects.Clear();
            cityArmyBusy = world.order != null;
            cityArmyDisplayedTroops = world.troops; cityArmyDisplayedCap = world.TroopCap;
            GUI.DrawTexture(new Rect(x, area.y, w, 32), cityArmyStripSkin, ScaleMode.StretchToFill, true);
            CityArmyText(new Rect(x + 8, area.y + 3, 151, 27), "壹  第一部队", new GUIStyle(body) { fontSize = 16 }, 13);
            CityArmyText(new Rect(x + 159, area.y + 4, w - 165, 25), world.ArmyState,
                new GUIStyle(hudSmall) { alignment = TextAnchor.MiddleRight, normal = { textColor = world.order == null ? Green : Gold } }, 11);
            var active = world.ActiveGenerals;
            for (int i = 0; i < 3; i++)
            {
                var layout = CityArmyLayout.Card(area.width, area.height, i);
                var card = CityArmyRect(area, layout);
                cityOverviewGeneralRects.Add(card);
                var portrait = CityArmyRect(area, CityArmyLayout.Portrait(layout));
                var identity = CityArmyRect(area, CityArmyLayout.Identity(layout));
                cityOverviewGeneralArtRects.Add(portrait); cityOverviewGeneralNameRects.Add(identity);
                var seat = new Rect(card.x, card.y - 19, card.width, 18);
                CityArmyText(seat, Slots[i], new GUIStyle(hudSmall) { fontSize = 13, alignment = TextAnchor.MiddleCenter, normal = { textColor = Gold } }, 11);
                bool hover = GUI.enabled && card.Contains(Event.current.mousePosition) && (i < active.Count || world.order == null);
                GUI.DrawTexture(card, hover ? cityArmyCardHoverSkin : cityArmyCardSkin, ScaleMode.StretchToFill, true);
                if (i < active.Count)
                {
                    var general = active[i];
                    // A portrait window and a separate identity plinth preserve the full
                    // source painting. Names and levels never cover a face or weapon.
                    PortraitComplete(portrait, general.definition);
                    CityArmyText(new Rect(card.x + 3, card.yMax - 41, card.width - 6, 22), general.Definition.name,
                        new GUIStyle(body) { fontSize = 16, alignment = TextAnchor.MiddleCenter }, 13);
                    CityArmyText(new Rect(card.x + 3, card.yMax - 21, card.width - 6, 20), "Lv." + general.level,
                        new GUIStyle(hudSmall) { fontSize = 13, alignment = TextAnchor.MiddleCenter, normal = { textColor = Gold } }, 11);
                    RecordCityOverviewTarget("city-general-" + i, card);
                    RecordCityOverviewTarget("city-general-art-" + i, portrait);
                    RecordCityOverviewTarget("city-general-name-" + i, identity);
                    if (ContentTileClicked(card, "city-general-" + i, general.uid)) ShowCardDetails(general.definition, general.uid);
                }
                else
                {
                    CityArmyText(portrait, "＋", new GUIStyle(heading) { fontSize = 30, alignment = TextAnchor.MiddleCenter, normal = { textColor = Gold } }, 20);
                    CityArmyText(identity, "待任命", new GUIStyle(small) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = Muted } }, 13);
                    if (world.order == null)
                    {
                        RecordCityOverviewTarget("city-empty-" + i, card);
                        if (ContentTileClicked(card, "city-empty-" + i, -1)) OpenCityFormation();
                    }
                }
            }
            cityArmyStrengthArea = CityArmyRect(area, CityArmyLayout.Strength(area.width, area.height));
            var strength = cityArmyStrengthArea;
            GUI.DrawTexture(new Rect(x, strength.y, w, 74), cityArmyStripSkin, ScaleMode.StretchToFill, true);
            CityArmyText(new Rect(strength.x + 8, strength.y, 42, 23), "兵力", hudSmall, 11);
            CityArmyText(new Rect(strength.x + 52, strength.y - 2, w - 60, 26),
                cityArmyDisplayedTroops + " / " + cityArmyDisplayedCap, new GUIStyle(hudNumber) { fontSize = 21 }, 16);
            RectFill(new Rect(strength.x, strength.yMax - 3, w, 3), Border);
            RectFill(new Rect(strength.x, strength.yMax - 3, w * Mathf.Clamp01((float)cityArmyDisplayedTroops / Math.Max(1, cityArmyDisplayedCap)), 3), Green);
            cityArmySupplyArea = CityArmyRect(area, CityArmyLayout.Supply(area.width, area.height));
            DrawCityArmySupply(cityArmySupplyArea);
            var formation = CityArmyRect(area, CityArmyLayout.Action(area.width, area.height, 0));
            var recruiting = CityArmyRect(area, CityArmyLayout.Action(area.width, area.height, 1));
            var training = CityArmyRect(area, CityArmyLayout.Action(area.width, area.height, 2));
            RecordCityOverviewTarget("city-formation", formation);
            RecordCityOverviewTarget("city-recruit", recruiting);
            RecordCityOverviewTarget("city-army-training", training);
            if (CityArmyAction(formation, "编队", 0, world.order == null)) OpenCityFormation();
            if (CityArmyAction(recruiting, "征兵", 1, true)) OpenReinforcement();
            if (CityArmyAction(training, "练兵", 2, true)) OpenTraining();
        }

        private void DrawCityArmySupply(Rect area)
        {
            cityArmyDisplayedRecruit = cityArmyBusy ? 0 : Math.Min(200, Math.Max(0, world.TroopCap - world.troops));
            var plan = ReinforcementPlan.Create(world, cityArmyDisplayedRecruit);
            cityArmyDisplayedCosts[0] = plan.cost.grain; cityArmyDisplayedCosts[1] = plan.cost.grass; cityArmyDisplayedCosts[2] = plan.cost.iron;
            if (cityArmyBusy)
            {
                // remaining is the active stage, not the entire return journey.
                DrawHudNavigationIcon(new Rect(area.x, area.y + 3, 29, 32), 1, Gold);
                CityArmyText(new Rect(area.x + 39, area.y, area.width - 39, 19), "执行军令 · 返城后可整编", hudSmall, 11);
                CityArmyText(new Rect(area.x + 39, area.y + 20, area.width - 39, 19),
                    world.order.phase == (int)ArmyPhase.Stationed && !world.order.returning
                        ? "驻守持续至手动撤回" : "当前阶段 " + CityTime(world.order.remaining), hudSmall, 11);
                return;
            }
            string note = world.ActiveGenerals.Count == 0 ? "未编队 · 先任命武将" : cityArmyDisplayedRecruit == 0 ? "兵力已满 · 项目即时征兵"
                : "补兵 +" + cityArmyDisplayedRecruit + (plan.CanExecute ? " · 项目即时征兵" : " · 资源不足");
            CityArmyText(new Rect(area.x, area.y - 1, area.width, 19), note, hudSmall, 11);
            int[] icons = { 1, 0, 3 };
            float cell = area.width / 3;
            for (int i = 0; i < 3; i++)
            {
                var icon = new Rect(area.x + i * cell, area.y + 20, 22, 20);
                cityArmySupplyIconRects.Add(icon);
                ResourceIcon(icon, icons[i]);
                CityArmyText(new Rect(icon.xMax + 5, icon.y, cell - 30, 20), cityArmyDisplayedCosts[i].ToString("0.##"), hudSmall, 11);
            }
        }

        private bool CityArmyAction(Rect area, string title, int index, bool available)
        {
            bool enabled = GUI.enabled && available;
            bool hover = enabled && area.Contains(Event.current.mousePosition);
            Color accent = index == 1 ? Gold : C("B5BBA6");
            GUI.DrawTexture(area, hover ? cityArmyCommandHoverSkin : cityArmyCommandSkin, ScaleMode.StretchToFill, true);
            var art = new Rect(area.x + 5, area.y + 6, 32, 32);
            var caption = new Rect(area.x + 42, area.y + 1, area.width - 45, 40);
            cityArmyActionArtRects.Add(art); cityArmyActionCaptionRects.Add(caption);
            if (index == 2) DrawCityFacilityArt(art, CityBuilding.DrillGround);
            else if (index == 1) DrawCityFacilityArt(art, CityBuilding.Barracks);
            else DrawHudNavigationIcon(art, 1, enabled ? accent : Muted);
            CityArmyText(caption, title, new GUIStyle(hudCaption) { fontSize = 16, normal = { textColor = enabled ? Text : Muted } }, 13);
            if (enabled)
            {
                RecordCityOverviewTarget("city-army-action-" + index + "-art", art);
                RecordCityOverviewTarget("city-army-action-" + index + "-caption", caption);
            }
            return enabled && ContentTileClicked(area, "city-army-action-" + index, -1);
        }

        private Texture2D BuildCityOverviewTexture()
        { return CitySceneRaster.Overview(world).Texture(); }

    }
}
