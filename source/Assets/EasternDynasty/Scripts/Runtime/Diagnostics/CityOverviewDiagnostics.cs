using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private void FillCityOverviewReview(ReviewState review)
        {
            bool headerVisible=tab=="主城"&&reviewTargets.ContainsKey("city-hud-home");
            review.cityShellStyle=headerVisible?"city-crest-landscape-navigation-v1":"";
            review.cityHeaderDesignStyle=headerVisible?CityHeaderLayout.Style:"";
            review.cityHeaderDesignTextFits=headerVisible&&cityHeaderTextFits;
            review.cityHeaderNameWrapped=headerVisible&&cityHeaderNameWrapped;
            review.cityHeaderBackdropDrawn=headerVisible&&cityHeaderBackdropDrawn&&reviewTargets.ContainsKey("city-header-backdrop");
            bool footerVisible=tab=="主城"&&reviewTargets.ContainsKey("city-open-art");
            review.cityFooterDesignStyle=footerVisible?MapHudNavigationLayout.Style:"";
            review.cityFooterDesignTextFits=footerVisible&&hudNavigationTextFits;
            review.cityHudFullName=cityHudDisplayedName;review.cityHudShortName=cityHudShortName;
            review.cityHudLevel=cityHudDisplayedLevel;
            review.cityHudIdentity=CityOverviewReviewBounds(cityHudIdentityArea);
            review.cityHudArt=CityOverviewReviewBounds(cityHudArtArea);
            review.cityHudName=CityOverviewReviewBounds(cityHudNameArea);
            review.cityPanoramaCanvas=CityOverviewReviewBounds(cityPanoramaCanvas);
            bool navigationDockVisible = tab == "主城" && cityPage == "overview" && reviewTargets.ContainsKey("city-overview");
            review.cityNavigationStyle = navigationDockVisible ? "city-edge-dock-v1" : "";
            review.cityDockDesignStyle = navigationDockVisible ? CityDockLayout.Style : "";
            review.cityDockDesignTextFits = navigationDockVisible && cityDockTextFits;
            review.cityNavigationDock = navigationDockVisible ? CityOverviewReviewBounds(cityNavigationDockArea) : new float[0];
            review.cityContentCanvas = navigationDockVisible ? CityOverviewReviewBounds(cityContentArea) : new float[0];
            review.cityPanoramaTitleText = navigationDockVisible ? cityPanoramaTitleText : "";
            review.cityPanoramaTitleBounds = navigationDockVisible ? CityOverviewReviewBounds(cityPanoramaTitleArea) : new float[0];
            review.cityPanoramaTitleMeasuredHeight = navigationDockVisible ? cityPanoramaTitleMeasuredHeight : 0;
            review.cityOverviewVisible = tab == "主城" && cityPage == "overview" && !cityOverviewExpanded
                && cardDetailId == null && confirmation == null && drawResult == null && cityQuestReceipt == null
                && reviewTargets.ContainsKey("city-general-0") == (world.ActiveGenerals.Count > 0)
                && reviewTargets.ContainsKey("city-army-training");
            review.cityOverviewStyle = review.cityOverviewVisible ? "panorama-side-army-v1" : "";
            review.cityOverviewArmyUids = world.ActiveGenerals.Take(3).Select(c => c.uid).ToArray();
            review.cityOverviewScene = CityOverviewReviewBounds(cityOverviewSceneArea);
            review.cityOverviewAffairs = CityOverviewReviewBounds(cityOverviewAffairsArea);
            review.cityOverviewArmy = CityOverviewReviewBounds(cityOverviewArmyArea);
            review.cityAffairsCompositionStyle = review.cityOverviewVisible ? CityAffairsLayout.Style : "";
            review.cityAffairsDesignTextFits = review.cityOverviewVisible && cityAffairsTextFits;
            review.cityAffairsArt = CityOverviewReviewBounds(cityAffairsArtArea);
            review.cityAffairsSummary = CityOverviewReviewBounds(cityAffairsSummaryArea);
            review.cityAffairsQuestArt = CityOverviewReviewBounds(cityAffairsQuestArtArea);
            review.cityAffairsDisplayedKind = (int)cityAffairsDisplayedBuilding;
            review.cityAffairsDisplayedLevel = cityAffairsDisplayedLevel;
            review.cityAffairsWorking = cityAffairsWorking;
            var affairsJob = world.CityJob;
            var affairsKind = affairsJob == null ? CityBuilding.TownHall : affairsJob.building;
            review.cityAffairsActualKind = (int)affairsKind;
            review.cityAffairsActualLevel = affairsJob == null ? world.CityLevel(affairsKind) : affairsJob.targetLevel;
            var affairsHall = world.cityPlots == null ? null : world.cityPlots.FirstOrDefault(p => p != null && p.building == CityBuilding.TownHall);
            review.cityAffairsActualPlot = affairsJob == null ? (affairsHall == null ? -1 : affairsHall.index) : affairsJob.plotIndex;
            review.cityOverviewGeneralSlots = cityOverviewGeneralRects.SelectMany(CityOverviewReviewBounds).ToArray();
            review.cityOverviewGeneralArt = cityOverviewGeneralArtRects.SelectMany(CityOverviewReviewBounds).ToArray();
            review.cityOverviewGeneralNames = cityOverviewGeneralNameRects.SelectMany(CityOverviewReviewBounds).ToArray();
            review.cityArmyCompositionStyle = review.cityOverviewVisible ? "formation-standards-muster-v2" : "";
            review.cityArmyDesignStyle = review.cityOverviewVisible ? CityArmyLayout.Style : "";
            review.cityArmyDesignTextFits = review.cityOverviewVisible && cityArmyTextFits;
            review.cityArmyStrength = CityOverviewReviewBounds(cityArmyStrengthArea);
            review.cityArmySupply = CityOverviewReviewBounds(cityArmySupplyArea);
            review.cityArmyActionArt = cityArmyActionArtRects.SelectMany(CityOverviewReviewBounds).ToArray();
            review.cityArmyActionCaptions = cityArmyActionCaptionRects.SelectMany(CityOverviewReviewBounds).ToArray();
            review.cityArmySupplyIcons = cityArmySupplyIconRects.SelectMany(CityOverviewReviewBounds).ToArray();
            review.cityArmyDisplayedTroops = cityArmyDisplayedTroops; review.cityArmyDisplayedCap = cityArmyDisplayedCap;
            review.cityArmyDisplayedRecruit = cityArmyDisplayedRecruit; review.cityArmyDisplayedCosts = cityArmyDisplayedCosts.ToArray();
            review.cityArmyBusy = cityArmyBusy; review.cityArmyActualCap = world.TroopCap;
            review.cityArmyActualRecruit = world.order == null ? Math.Min(200, Math.Max(0, world.TroopCap - world.troops)) : 0;
            var armyCost = World.RecruitmentCost(review.cityArmyActualRecruit);
            review.cityArmyActualCosts = new[] { armyCost.grain, armyCost.grass, armyCost.iron };
            review.cityPlanning = cityOverviewPlanning;
            review.cityExpanded = cityOverviewExpanded;
            review.cityPanoramaZoom = cityPanoramaCamera.Zoom;
            review.cityPanoramaPanX = cityPanoramaCamera.PanX; review.cityPanoramaPanY = cityPanoramaCamera.PanY;
            review.cityPanoramaImageWidth = cityPanoramaImage.width; review.cityPanoramaImageHeight = cityPanoramaImage.height;
            var imagePixels = GuiRectToPixels(cityPanoramaImage);
            review.cityPanoramaImagePixelX = imagePixels.x; review.cityPanoramaImagePixelY = imagePixels.y;
            review.cityPanoramaImagePixelWidth = imagePixels.width; review.cityPanoramaImagePixelHeight = imagePixels.height;
            review.cityPanoramaViewportWidth = cityPanoramaViewport.width; review.cityPanoramaViewportHeight = cityPanoramaViewport.height;
            review.cityLandscapeStyle = "courtyard-landscape-v2";
            review.cityLandscapeBounds = CityOverviewReviewBounds(cityPanoramaLandscape);
            review.cityLandscapeViewport = CityOverviewReviewBounds(cityPanoramaViewport);
            review.cityLandscapeTextureWidth = cityOverviewTexture == null ? 0 : cityOverviewTexture.width;
            review.cityLandscapeTextureHeight = cityOverviewTexture == null ? 0 : cityOverviewTexture.height;
            review.cityAvailableSites = world.IsYouzhou && world.cityPlots != null ? world.cityPlots.Count(CityOverviewPlotAvailable) : 0;
            review.citySiteIds = reviewTargets.Keys.Where(id => id.StartsWith("city-site-", StringComparison.Ordinal))
                .Select(id => int.Parse(id.Substring("city-site-".Length))).OrderBy(id => id).ToArray();
        }

        private sealed partial class ReviewState
        {
            public string cityShellStyle,cityHudFullName,cityHudShortName;
            public string cityHeaderDesignStyle;
            public bool cityHeaderDesignTextFits,cityHeaderNameWrapped,cityHeaderBackdropDrawn;
            public string cityFooterDesignStyle;
            public bool cityFooterDesignTextFits;
            public int cityHudLevel;
            public float[] cityHudIdentity,cityHudArt,cityHudName,cityPanoramaCanvas;
            public string cityNavigationStyle;
            public string cityDockDesignStyle;
            public bool cityDockDesignTextFits;
            public float[] cityNavigationDock, cityContentCanvas;
            public string cityPanoramaTitleText;
            public float[] cityPanoramaTitleBounds;
            public float cityPanoramaTitleMeasuredHeight;
            public bool cityOverviewVisible;
            public string cityOverviewStyle;
            public int[] cityOverviewArmyUids;
            public float[] cityOverviewScene, cityOverviewAffairs, cityOverviewArmy;
            public string cityAffairsCompositionStyle;
            public bool cityAffairsDesignTextFits;
            public float[] cityAffairsArt, cityAffairsSummary, cityAffairsQuestArt;
            public float[] cityOverviewGeneralSlots, cityOverviewGeneralArt, cityOverviewGeneralNames;
            public int cityAffairsDisplayedKind, cityAffairsDisplayedLevel, cityAffairsActualKind, cityAffairsActualLevel, cityAffairsActualPlot;
            public bool cityAffairsWorking;
            public string cityArmyCompositionStyle;
            public string cityArmyDesignStyle;
            public bool cityArmyDesignTextFits;
            public float[] cityArmyStrength, cityArmySupply, cityArmyActionArt, cityArmyActionCaptions, cityArmySupplyIcons;
            public int cityArmyDisplayedTroops, cityArmyDisplayedCap, cityArmyDisplayedRecruit, cityArmyActualCap, cityArmyActualRecruit;
            public double[] cityArmyDisplayedCosts, cityArmyActualCosts;
            public bool cityArmyBusy;
            public bool cityPlanning;
            public bool cityExpanded;
            public float cityPanoramaZoom, cityPanoramaPanX, cityPanoramaPanY;
            public float cityPanoramaImageWidth, cityPanoramaImageHeight, cityPanoramaViewportWidth, cityPanoramaViewportHeight;
            public float cityPanoramaImagePixelX, cityPanoramaImagePixelY, cityPanoramaImagePixelWidth, cityPanoramaImagePixelHeight;
            public int cityAvailableSites;
            public int[] citySiteIds;
            public string cityLandscapeStyle;
            public float[] cityLandscapeBounds, cityLandscapeViewport;
            public int cityLandscapeTextureWidth, cityLandscapeTextureHeight;
        }

        private float[] CityOverviewReviewBounds(Rect area)
        { var r = GuiRectToPixels(area); return new[] { r.x, r.y, r.width, r.height }; }

        private static bool CityOverviewContains(Rect parent, Rect child)
        { return child.width > 0 && child.height > 0 && child.xMin >= parent.xMin - .1f && child.yMin >= parent.yMin - .1f && child.xMax <= parent.xMax + .1f && child.yMax <= parent.yMax + .1f; }

        private void AssertCityOverviewGeometry()
        {
            if (tab != "主城" || cityPage != "overview" || cityOverviewExpanded || !BlocksMapInput)
                throw new Exception("Compact city geometry was checked outside the actual overview page.");
            var content = cityContentArea;
            var panels = new[] { cityOverviewSceneArea, cityOverviewAffairsArea, cityOverviewArmyArea };
            for (int i = 0; i < panels.Length; i++)
            {
                if (!CityOverviewContains(content, panels[i]) || !CityOverviewContains(uiPixels, GuiRectToPixels(panels[i])))
                    throw new Exception("City overview panel escaped its content or mobile safe area.");
                for (int j = 0; j < i; j++) if (panels[i].Overlaps(panels[j]))
                    throw new Exception("City panorama, affairs and army panels overlap.");
            }
            if (Math.Abs(cityOverviewSceneArea.height - content.height) > .1f
                || cityOverviewSceneArea.width < content.width * .6f
                || cityOverviewAffairsArea.yMax >= cityOverviewArmyArea.yMin)
                throw new Exception("City panorama lost its full-height scene or separated side rail.");
            string[] affairs = { "city-overview-facilities", "city-construction-page", "city-overview-quests" };
            string[] army = { "city-formation", "city-recruit", "city-army-training" };
            var ids = affairs.Concat(army).Concat(Enumerable.Range(0, Math.Min(3, world.ActiveGenerals.Count)).Select(i => "city-general-" + i)).ToArray();
            var seen = new System.Collections.Generic.List<Rect>();
            foreach (string id in ids)
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("City side rail control was not drawn: " + id);
                Rect r = new Rect(target.x - target.w / 2, target.y - target.h / 2, target.w, target.h);
                Rect parent = GuiRectToPixels(affairs.Contains(id) ? cityOverviewAffairsArea : cityOverviewArmyArea);
                if (!CityOverviewContains(parent, r) || !CityOverviewContains(uiPixels, r))
                    throw new Exception("City side rail control is clipped: " + id);
                if (map.inputEnabled && !map.IsBlocked(r.center)) throw new Exception("City control leaks input to the map: " + id);
                if (seen.Any(other => other.Overlaps(r))) throw new Exception("City side rail controls overlap: " + id);
                seen.Add(r);
            }
            if (cityOverviewGeneralRects.Count != 3 || cityOverviewGeneralArtRects.Count != 3 || cityOverviewGeneralNameRects.Count != 3)
                throw new Exception("City army must draw exactly three actual portrait slots with separate art and identity regions.");
            for (int i = 0; i < 3; i++)
            {
                Rect r = cityOverviewGeneralRects[i];
                Rect art = cityOverviewGeneralArtRects[i], identity = cityOverviewGeneralNameRects[i];
                if (!CityOverviewContains(r, art) || !CityOverviewContains(r, identity) || art.Overlaps(identity)
                    || art.height < r.height * .68f || identity.height < 40)
                    throw new Exception("City army portrait painting is covered by its name/level base or loses readable identity space.");
                if (!CityOverviewContains(cityOverviewArmyArea, r) || r.height <= r.width)
                    throw new Exception("City army portrait slot is cropped or no longer vertical.");
                for (int j = 0; j < i; j++) if (r.Overlaps(cityOverviewGeneralRects[j]))
                    throw new Exception("City army portrait slots overlap.");
                ReviewTarget target;
                bool exposed = reviewTargets.TryGetValue("city-general-" + i, out target);
                if (exposed != (i < world.ActiveGenerals.Count)) throw new Exception("City army exposes a nonexistent owned general or hides an actual one.");
                if (exposed)
                {
                    var actual = GuiRectToPixels(r);
                    if (Math.Abs(target.x - actual.center.x) + Math.Abs(target.y - actual.center.y) + Math.Abs(target.w - actual.width) + Math.Abs(target.h - actual.height) > .1f)
                        throw new Exception("City general target does not match its visible portrait slot.");
                }
                bool emptyExposed = reviewTargets.TryGetValue("city-empty-" + i, out target);
                if (emptyExposed != (i >= world.ActiveGenerals.Count && world.order == null))
                    throw new Exception("City army exposes an occupied or busy empty-seat action or hides an available appointment.");
                if (emptyExposed)
                {
                    var actual = GuiRectToPixels(r);
                    if (Math.Abs(target.x - actual.center.x) + Math.Abs(target.y - actual.center.y) + Math.Abs(target.w - actual.width) + Math.Abs(target.h - actual.height) > .1f)
                        throw new Exception("City empty appointment target differs from its visible standard.");
                }
            }
            AssertCityAffairsComposition();
            AssertCityArmyComposition();
            AssertCityShellGeometry();
            AssertCityPanoramaGeometry();
        }

        private void AssertCityShellGeometry()
        {
            AssertCityHeaderDesign();
            var header=new Rect(0,0,width,70);
            if(cityHudDisplayedName!=world.lands[world.homeId].name||cityHudDisplayedLevel!=world.CityLevel(CityBuilding.TownHall)
                ||string.IsNullOrEmpty(cityHudShortName)||cityHudShortName!=cityHudDisplayedName||!cityHeaderTextFits)
                throw new Exception("City crest identity differs from its actual home, truncates its complete name or exceeds the fitted actual font bounds.");
            AssertCityCompositionTarget("city-hud-home",cityHudIdentityArea,header);
            if(!cityHeaderBackdropDrawn)throw new Exception("City header did not draw its opaque full-width backdrop in the current frame.");
            AssertCityCompositionTarget("city-header-backdrop",header,header);
            AssertCityCompositionTarget("city-hud-home-crest",HudDesignRect(CityHeaderLayout.Crest),cityHudIdentityArea);
            AssertCityCompositionTarget("city-hud-home-art",cityHudArtArea,cityHudIdentityArea);
            AssertCityCompositionTarget("city-hud-home-name",cityHudNameArea,cityHudIdentityArea);
            AssertCityCompositionTarget("city-hud-home-detail",HudDesignRect(CityHeaderLayout.IdentityDetail),cityHudIdentityArea);
            AssertCityCompositionTarget("city-header-production",HudDesignRect(CityHeaderLayout.Production(width)),header);
            if(cityHudArtArea.Overlaps(cityHudNameArea)||cityHudArtArea!=HudDesignRect(CityHeaderLayout.Art)
                ||!CityOverviewContains(HudDesignRect(CityHeaderLayout.Crest),cityHudArtArea))
                throw new Exception("City identity lost its complete inset town image, square seal or readable name separation.");
            Rect previous=GuiRectToPixels(cityHudIdentityArea);
            for(int i=0;i<6;i++)
            {
                var cell=CityOverviewTargetBounds("hud-resource-"+i);
                if(!CityOverviewContains(GuiRectToPixels(header),cell)||cell.Overlaps(previous))
                    throw new Exception("City live stock/income column overlaps another header region.");
                var icon=CityOverviewTargetBounds("hud-resource-icon-"+i);
                var stock=CityOverviewTargetBounds("hud-resource-stock-"+i);
                var detail=CityOverviewTargetBounds("hud-resource-detail-"+i);
                if(!CityOverviewContains(cell,icon)||!CityOverviewContains(cell,stock)||!CityOverviewContains(cell,detail)
                    ||icon.Overlaps(stock)||icon.Overlaps(detail)||stock.Overlaps(detail))
                    throw new Exception("City resource icon, stock and income layers are clipped or overlap: "+i);
                previous=cell;
            }
            AssertCityFooterDesignGeometry();
        }

        private void AssertCityFooterDesignGeometry()
        {
            if(tab!="主城"||!BlocksMapInput||!hudNavigationTextFits||hudTicket==null||hudTicketActive==null
                ||hudRecruitTicket==null||hudLetter==null||hudBook==null||hudShelf==null)
                throw new Exception("City footer lost its actual page, six imported design textures or measured Chinese font fit.");
            var footer=new Rect(0,height-MapHudNavigationLayout.FooterHeight,width,MapHudNavigationLayout.FooterHeight);
            var seen=new System.Collections.Generic.List<Rect>();
            string[] ids={"city-open","map-tab","roster-tab","reports-tab","hud-help","recruit-tab"};
            for(int i=0;i<ids.Length;i++)
            {
                var box=MapHudNavigationLayout.Entry(width,height,i);
                Rect parent=HudDesignRect(box),art=HudDesignRect(MapHudNavigationLayout.Art(box,i==5));
                Rect caption=HudDesignRect(MapHudNavigationLayout.Caption(box,i==5));
                AssertCityCompositionTarget(ids[i],parent,footer);
                AssertCityCompositionTarget(ids[i]+"-art",art,parent);
                AssertCityCompositionTarget(ids[i]+"-caption",caption,parent);
                if(parent.width<44||parent.height<44||art.Overlaps(caption)
                    ||art.yMax>caption.yMin||caption.width<100||caption.height!=22||seen.Any(other=>other.Overlaps(parent)))
                    throw new Exception("City footer exported image/caption geometry overlaps or loses its complete touch action: "+ids[i]);
                seen.Add(parent);
            }
            for(int i=0;i<2;i++)
            {
                var utility=HudDesignRect(MapHudNavigationLayout.Utility(width,height,i==1));
                string id=i==0?"hud-save":"speed-cycle";
                AssertCityCompositionTarget(id,utility,footer);
                if(utility.width<44||utility.height<44||seen.Any(other=>other.Overlaps(utility)))
                    throw new Exception("City footer utility overlaps one of its eight real controls or loses touch size: "+id);
                seen.Add(utility);
            }
            // Include the empty shelf gaps: the complete visible 78-unit footer
            // must keep terrain input blocked, not just its registered controls.
            for(int x=0;x<=8;x++)
                if(map.inputEnabled&&!map.IsBlocked(GuiPointToPixels(new Vector2(footer.x+footer.width*(x+.5f)/9,footer.center.y))))
                    throw new Exception("City footer shelf gap leaks a pointer to the map.");
        }

        private void AssertCityEdgeDockGeometry()
        {
            if (tab != "主城" || cityPage != "overview" || !BlocksMapInput)
                throw new Exception("City edge dock was checked outside its actual overview page.");
            Rect dock = cityNavigationDockArea, content = cityContentArea;
            if (!CityOverviewContains(uiPixels, GuiRectToPixels(dock))
                || !CityOverviewContains(uiPixels, GuiRectToPixels(content)) || dock.Overlaps(content)
                || dock.xMax >= content.xMin || Math.Abs(dock.yMin - content.yMin) > .1f
                || Math.Abs(dock.yMax - content.yMax) > .1f || content.yMin >= 100
                || !CityOverviewContains(content, cityPanoramaCanvas))
                throw new Exception("City edge dock overlaps its raised content or escapes the actual safe area.");
            string expectedTitle = world.lands[world.homeId].name + (cityOverviewPlanning ? "\n建设选址" : "\n城内全景");
            if (cityPanoramaTitleText != expectedTitle
                || !CityOverviewContains(cityPanoramaCanvas, cityPanoramaTitleArea)
                || cityPanoramaTitleMeasuredHeight <= 0 || cityPanoramaTitleMeasuredHeight > cityPanoramaTitleArea.height + .1f
                || cityPanoramaTitleMeasuredHeight > 40.1f)
                throw new Exception("City panorama title lost the complete actual home name or clips its measured text.");
            AssertCityPanoramaToolbarDesign();
            var dockPixels = GuiRectToPixels(dock);
            var seen = new System.Collections.Generic.List<Rect>();
            foreach (string page in new[] { "overview", "catalog", "facilities", "quests", "training" })
            {
                var parent = CityOverviewTargetBounds("city-" + page);
                var art = CityOverviewTargetBounds("city-nav-" + page + "-art");
                var caption = CityOverviewTargetBounds("city-nav-" + page + "-caption");
                if (!CityOverviewContains(dockPixels, parent) || !CityOverviewContains(parent, art)
                    || !CityOverviewContains(parent, caption) || art.Overlaps(caption)
                    || art.yMax > caption.yMin + .1f || parent.height < 44 * scale - .1f
                    || parent.width < 44 * scale - .1f || art.width < 48 * scale - .1f
                    || art.height < 40 * scale - .1f || seen.Any(other => other.Overlaps(parent)))
                    throw new Exception("City edge navigation lost its separate visible image/caption or overlaps another action: " + page);
                if (map.inputEnabled && !map.IsBlocked(parent.center))
                    throw new Exception("City edge navigation leaks input to the map: " + page);
                seen.Add(parent);
            }
            var back = CityOverviewTargetBounds("city-back");
            if (!CityOverviewContains(dockPixels, back) || back.width < 44 * scale - .1f
                || back.height < 44 * scale - .1f || seen.Any(other => other.Overlaps(back))
                || back.yMin <= seen.Last().yMax || dockPixels.yMax - back.yMax > 16 * scale + .1f
                || (map.inputEnabled && !map.IsBlocked(back.center)))
                throw new Exception("City edge return is not a separate visible bottom touch action.");
            AssertCityDockDesign();
        }

        private void AssertCityArmyComposition()
        {
            var army = cityOverviewArmyArea;
            AssertCityArmyDesign();
            int expectedRecruit = world.order == null ? Math.Min(200, Math.Max(0, world.TroopCap - world.troops)) : 0;
            var quote = World.RecruitmentCost(expectedRecruit);
            if (cityArmyBusy != (world.order != null) || cityArmyDisplayedTroops != world.troops
                || cityArmyDisplayedCap != world.TroopCap || cityArmyDisplayedRecruit != expectedRecruit
                || cityArmyDisplayedCosts.Length != 3 || Math.Abs(cityArmyDisplayedCosts[0] - quote.grain) > .00001
                || Math.Abs(cityArmyDisplayedCosts[1] - quote.grass) > .00001 || Math.Abs(cityArmyDisplayedCosts[2] - quote.iron) > .00001)
                throw new Exception("City army strength or displayed supply cost differs from the current campaign and Core recruitment quote.");
            if (!CityOverviewContains(army, cityArmyStrengthArea) || !CityOverviewContains(army, cityArmySupplyArea)
                || cityArmyStrengthArea.height < 28 || cityArmySupplyArea.height < 40
                || cityArmyStrengthArea.Overlaps(cityArmySupplyArea)
                || cityOverviewGeneralRects.Any(card => card.yMax > cityArmyStrengthArea.yMin - 5.9f))
                throw new Exception("City army portrait, strength and supply rows overlap or lose their visible space.");
            if (cityArmyActionArtRects.Count != 3 || cityArmyActionCaptionRects.Count != 3
                || cityArmySupplyIconRects.Count != (cityArmyBusy ? 0 : 3))
                throw new Exception("City army action illustrations or actual idle supply icons are missing.");
            string[] parents = { "city-formation", "city-recruit", "city-army-training" };
            for (int i = 0; i < 3; i++)
            {
                Rect art = cityArmyActionArtRects[i], caption = cityArmyActionCaptionRects[i];
                Rect parent = CityOverviewTargetBounds(parents[i]);
                if (!CityOverviewContains(parent, GuiRectToPixels(art)) || !CityOverviewContains(parent, GuiRectToPixels(caption))
                    || art.Overlaps(caption) || art.width < 32 || art.height < 32 || caption.height < 40
                    || parent.height < 44 * scale - .1f || GuiRectToPixels(cityArmySupplyArea).yMax > parent.yMin - 7.9f * scale)
                    throw new Exception("City army illustration and title do not fit their real 44px command button.");
                bool enabled = i != 0 || !cityArmyBusy;
                foreach (string suffix in new[] { "art", "caption" })
                {
                    string id = "city-army-action-" + i + "-" + suffix;
                    if (reviewTargets.ContainsKey(id) != enabled) throw new Exception("City army exposes a disabled formation action or hides an available command: " + id);
                    if (enabled) AssertCityCompositionTarget(id, suffix == "art" ? art : caption, army);
                }
                if (!cityArmyBusy)
                {
                    Rect icon = cityArmySupplyIconRects[i];
                    if (!CityOverviewContains(cityArmySupplyArea, icon) || !CityOverviewContains(uiPixels, GuiRectToPixels(icon))
                        || icon.width < 22 || icon.height < 20)
                        throw new Exception("City army recruitment resource icon is clipped or missing.");
                }
            }
        }

        private Rect CityOverviewTargetBounds(string id)
        {
            ReviewTarget target;
            if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("Visible city composition target missing: " + id);
            return new Rect(target.x - target.w / 2, target.y - target.h / 2, target.w, target.h);
        }

        private void AssertCityCompositionTarget(string id, Rect drawn, Rect parent)
        {
            Rect actual = GuiRectToPixels(drawn), target = CityOverviewTargetBounds(id);
            if (!CityOverviewContains(parent, drawn) || !CityOverviewContains(uiPixels, actual)
                || Math.Abs(actual.x - target.x) + Math.Abs(actual.y - target.y) + Math.Abs(actual.width - target.width) + Math.Abs(actual.height - target.height) > .1f)
                throw new Exception("City composition target differs from its visible artwork: " + id);
            if (map.inputEnabled && !map.IsBlocked(actual.center)) throw new Exception("City illustration leaks a touch to the map: " + id);
        }

        private void AssertCityAffairsComposition()
        {
            AssertCityAffairsDesign();
            var job = world.CityJob;
            var actualKind = job == null ? CityBuilding.TownHall : job.building;
            if (cityAffairsWorking != (job != null) || cityAffairsDisplayedBuilding != actualKind
                || cityAffairsDisplayedLevel != (job == null ? world.CityLevel(actualKind) : job.targetLevel))
                throw new Exception("City affairs illustration or displayed level does not match the actual facility and live construction queue.");
            AssertCityCompositionTarget("city-affairs-art", cityAffairsArtArea, cityOverviewAffairsArea);
            AssertCityCompositionTarget("city-affairs-building", cityAffairsBuildingArea, cityAffairsArtArea);
            AssertCityCompositionTarget("city-affairs-caption", cityAffairsCaptionArea, cityAffairsArtArea);
            if (cityAffairsBuildingArea.Overlaps(cityAffairsCaptionArea))
                throw new Exception("City affairs building obscures its independent visible caption.");
            if (cityAffairsArtArea.width < 100 || cityAffairsArtArea.height < 100
                || !CityOverviewContains(cityOverviewAffairsArea, cityAffairsSummaryArea) || cityAffairsArtArea.Overlaps(cityAffairsSummaryArea))
                throw new Exception("City affairs lost its large facility illustration or separate readable summary.");
            var quest = CityOverviewTargetBounds("city-overview-quests");
            var questArt = GuiRectToPixels(cityAffairsQuestArtArea);
            AssertCityCompositionTarget("city-overview-quests-art", cityAffairsQuestArtArea, cityOverviewAffairsArea);
            AssertCityCompositionTarget("city-overview-quests-caption", cityAffairsQuestCaptionArea, cityOverviewAffairsArea);
            if (!CityOverviewContains(quest, questArt) || cityAffairsQuestArtArea.width < 36 || cityAffairsQuestArtArea.height < 36
                || quest.height < 44 * scale - .1f
                || !CityOverviewContains(quest, GuiRectToPixels(cityAffairsQuestCaptionArea))
                || cityAffairsQuestArtArea.Overlaps(cityAffairsQuestCaptionArea))
                throw new Exception("City chapter scroll is outside its complete 44px task entry.");
            foreach (string page in new[] { "overview", "catalog", "facilities", "quests", "training" })
            {
                var parent = CityOverviewTargetBounds("city-" + page);
                var art = CityOverviewTargetBounds("city-nav-" + page + "-art");
                var caption = CityOverviewTargetBounds("city-nav-" + page + "-caption");
                if (!CityOverviewContains(parent, art) || !CityOverviewContains(parent, caption) || art.Overlaps(caption)
                    || !CityOverviewContains(uiPixels, parent) || parent.height < 44 * scale - .1f
                    || art.width < 28 * scale - .1f || art.height < 30 * scale - .1f)
                    throw new Exception("City navigation illustration and title lost their shared visible touch target: " + page);
            }
        }

        private IEnumerator CityPanoramaSmoke()
        {
            var oldSession = session; string oldTab = tab, oldPage = cityPage, oldStatus = status, oldOrigin = cityFacilitiesOrigin;
            int oldSelected = map.selected, oldPlot = citySelectedPlot;
            bool oldPlanning = cityOverviewPlanning, oldExpanded = cityOverviewExpanded, oldReview = reviewMode;
            var oldBuilding = citySelectedBuilding; var oldScroll = cityDetailScroll;
            var oldFocus = map.focus; float oldZoom = map.zoom, oldSpeed = speed;
            float oldCityZoom = cityPanoramaCamera.Zoom, oldPanX = cityPanoramaCamera.PanX, oldPanY = cityPanoramaCamera.PanY;
            string original = JsonUtility.ToJson(world);
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath, "smoke-city-panorama-unused")), () => DateTime.UtcNow, true);
            try
            {
                session.StartYouzhou(0, "幽州城内全景界面验收用长名称"); map.Setup(world); speed = 0; reviewMode = true;
                world.lands[world.homeId].name = "幽州城内全景界面验收用长名称主城";
                OpenCityOverview();
                string overviewUnchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-overview-long-name.png");
                AssertCityOverviewGeometry();
                yield return CaptureState("smoke-city-panorama-toolbar.png");
                AssertCityEdgeDockGeometry();
                Debug.Log("CITY_PANORAMA_TOOLBAR_RUNTIME_OK: actual Inkscape textures, full two-line title, separate image/caption hit regions and unchanged reserved camera band; visual and actual input acceptance remains publisher work.");
                if(!cityHeaderNameWrapped||cityHudShortName!=world.lands[world.homeId].name)
                    throw new Exception("City header long-name fixture was truncated or did not use the designed wrapped title.");
                // Isolated visual pressure data only; restore before the actual
                // garrison/construction sequence so its resource preconditions stay intact.
                var headerResources=world.resources;
                world.resources=new Supplies {grass=999999,grain=1000000,stone=100000000,iron=99999999,gold=1234567890,jade=12345};
                string headerUnchanged=JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-header-design.png");
                AssertCityShellGeometry();
                yield return CaptureState("smoke-city-header-ledger.png");
                AssertCityShellGeometry();
                // Exercise the problematic prior-page painting order in this
                // isolated fixture. Actual image/title clicks and pixel equality
                // are checked separately by the publisher's experience script.
                tab="武将";
                yield return CaptureState("smoke-city-header-ordinary.png");
                if(reviewTargets.ContainsKey("city-header-backdrop")||reviewTargets.ContainsKey("city-hud-home"))
                    throw new Exception("Ordinary roster page exposed stale city header targets.");
                OpenCityOverview();
                yield return CaptureState("smoke-city-header-ordinary-return.png");
                AssertCityShellGeometry();
                if(!cityHeaderNameWrapped||JsonUtility.ToJson(world)!=headerUnchanged)
                    throw new Exception("City header pressure rendering clipped the full name or changed the isolated campaign.");
                world.resources=headerResources;
                if(JsonUtility.ToJson(world)!=overviewUnchanged)
                    throw new Exception("City header pressure fixture failed to restore its prior campaign.");
                yield return CaptureState("smoke-city-army-design.png");
                AssertCityArmyDesign();
                yield return CaptureState("smoke-city-affairs-design.png");
                AssertCityAffairsComposition();
                yield return CaptureState("smoke-city-dock-design.png");
                AssertCityEdgeDockGeometry();
                yield return CaptureState("smoke-city-footer-design.png");
                AssertCityFooterDesignGeometry();
                yield return CaptureState("smoke-city-footer-captions.png");
                AssertCityFooterDesignGeometry();
                if (JsonUtility.ToJson(world) != overviewUnchanged) throw new Exception("City overview rendering changed the paused campaign.");
                var formation = world.formation.ToArray(); int troops = world.troops;
                world.formation.Clear(); world.troops = 0;
                overviewUnchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-overview-empty-formation.png");
                AssertCityOverviewGeometry();
                if (JsonUtility.ToJson(world) != overviewUnchanged) throw new Exception("Empty city army rendering changed the paused campaign.");
                world.formation.AddRange(formation); world.troops = troops;
                var awayTarget = world.lands.First(t => world.TerritoryOrderProblem(t.id, false).Length == 0);
                world.Garrison(awayTarget.id);
                if (world.order == null || world.order.action != (int)ArmyAction.Garrison || world.order.target != awayTarget.id)
                    throw new Exception("City army away fixture did not issue the actual garrison command.");
                overviewUnchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-army-away.png");
                AssertCityOverviewGeometry();
                if (!cityArmyBusy || JsonUtility.ToJson(world) != overviewUnchanged)
                    throw new Exception("Reading the city army's active command changed the campaign or displayed idle recruitment supply.");
                world.RecallArmy(); world.Tick(world.order.remaining + .01);
                if (world.order != null) throw new Exception("City army away fixture did not finish its actual return.");
                // Reset the isolated fixture through the normal session entry, so this
                // real command cannot change later construction/quest expectations.
                session.StartYouzhou(0, "幽州城内全景界面验收用长名称"); map.Setup(world);
                world.lands[world.homeId].name = "幽州城内全景界面验收用长名称主城";
                OpenCityOverview(); SetCityPanoramaExpanded(true);
                string unchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-expanded.png");
                AssertCityPanoramaGeometry();
                AssertCityShellGeometry();
                if (!cityOverviewExpanded || !BlocksMapInput || JsonUtility.ToJson(world) != unchanged) throw new Exception("Expanding the city changed campaign state.");
                if (cityPanoramaBuildings.Count != 1 || cityPanoramaBuildings[0].index != 4 || cityPanoramaVisibleSites != 0)
                    throw new Exception("Street scenery exposed a nonexistent facility or an unrequested building site.");
                var cachedScenery = cityOverviewTexture;
                cityPanoramaCamera.SetZoom(1.5f); cityPanoramaCamera.Pan(115, -85);
                yield return CaptureState("smoke-city-garden-shore.png");
                AssertCityPanoramaGeometry();
                if (JsonUtility.ToJson(world) != unchanged || cityPanoramaVisibleSites != 0
                    || !ReferenceEquals(cachedScenery, cityOverviewTexture))
                    throw new Exception("Garden/shore viewing changed World, exposed scenery as a site, or regenerated the cached landscape.");
                cityPanoramaCamera.Reset();
                cityPanoramaCamera.SetZoom(2); cityPanoramaCamera.Pan(85, 45);
                yield return CaptureState("smoke-city-zoomed.png");
                AssertCityPanoramaGeometry();
                if (cityPanoramaCamera.Zoom != 2 || JsonUtility.ToJson(world) != unchanged) throw new Exception("City camera zoom spent resources or changed the world.");
                SetCityOverviewPlanning(true);
                yield return CaptureState("smoke-city-zoomed-planning.png");
                AssertCityPanoramaGeometry();
                if (cityPanoramaVisibleSites < 1 || cityPanoramaVisibleSites > world.cityPlots.Count(CityOverviewPlotAvailable)) throw new Exception("Zoomed planning has no real visible site or exposes an unavailable site.");
                cityPanoramaCamera.Reset();
                int chosen = world.cityPlots.First(CityOverviewPlotAvailable).index;
                SelectCityOverviewPlot(chosen);
                yield return CaptureState("smoke-city-expanded-detail.png");
                AssertCityFooterDesignGeometry();
                if (cityPage != "facilities" || citySelectedPlot != chosen || !cityOverviewExpanded || world.CityJob != null) throw new Exception("Expanded site did not reach its real district.");
                unchanged = JsonUtility.ToJson(world);
                HandleMobileBack();
                if (cityPage != "overview" || !cityOverviewPlanning || !cityOverviewExpanded) throw new Exception("District return lost expanded planning origin.");
                HandleMobileBack();
                if (cityOverviewPlanning || !cityOverviewExpanded || tab != "主城") throw new Exception("Planning back skipped the expanded city.");
                HandleMobileBack();
                if (cityOverviewExpanded || tab != "主城") throw new Exception("Expanded city back skipped the compact city.");
                if (JsonUtility.ToJson(world) != unchanged) throw new Exception("City camera return changed the campaign.");
                SetCityPanoramaExpanded(true); cityPanoramaCamera.SetZoom(2);
                tab = "领地"; OpenCityFacilities(4); ReturnFromCityPage();
                if (cityOverviewExpanded || cityPanoramaCamera.Zoom != 1 || cityOverviewPlanning) throw new Exception("External district entry inherited a stale city camera.");
                world.resources = new Supplies { grain = 50000, grass = 50000, stone = 50000, iron = 50000, gold = 50000 };
                CompleteCitySmokeConstruction(world, 4, CityBuilding.TownHall); CompleteCitySmokeConstruction(world, 4, CityBuilding.TownHall);
                foreach (var definition in CityCatalog.Buildings.Where(d => d.kind != CityBuilding.TownHall))
                    CompleteCitySmokeConstruction(world, world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index, definition.kind);
                OpenCityOverview();
                overviewUnchanged = JsonUtility.ToJson(world);
                for (int frame = 0; frame < 5; frame++) yield return null;
                AssertCityOverviewGeometry();
                if (cityPanoramaBuildings.Count != 9) throw new Exception("Compact developed city hides a real facility behind the new side rail.");
                if (JsonUtility.ToJson(world) != overviewUnchanged) throw new Exception("Developed city side rail changed the paused campaign.");
                int workingPlot = world.cityPlots.First(p => p.building == CityBuilding.Mill).index;
                world.StartCityConstruction(workingPlot, CityBuilding.Mill);
                if (world.CityJob == null || world.CityJob.plotIndex != workingPlot || world.CityJob.building != CityBuilding.Mill)
                    throw new Exception("City affairs working fixture failed to start the real mill upgrade.");
                overviewUnchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-affairs-working.png");
                AssertCityOverviewGeometry();
                if (JsonUtility.ToJson(world) != overviewUnchanged) throw new Exception("Reading illustrated city affairs changed the active construction or complete campaign.");
                world.CancelCityConstruction();
                SetCityPanoramaExpanded(true);
                unchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-expanded-developed.png");
                AssertCityPanoramaGeometry();
                if (cityPanoramaBuildings.Count != 9 || JsonUtility.ToJson(world) != unchanged) throw new Exception("Expanded developed city hid a real facility or changed the campaign while viewed.");
                cachedScenery = cityOverviewTexture;
                cityPanoramaCamera.Reset(); cityPanoramaCamera.SetZoom(1.5f); cityPanoramaCamera.Pan(0, -10000);
                yield return CaptureState("smoke-city-gate-masonry.png");
                AssertCityPanoramaGeometry();
                Rect gate = CityOverviewRect(cityPanoramaImage, 437, 318, 126, 110);
                Vector2 gatePassage = CityOverviewRect(cityPanoramaImage, 520, 405, 1, 1).position;
                if (!CityOverviewContains(cityPanoramaViewport, gate) || cityPanoramaVisibleSites != 0
                    || !ReferenceEquals(cachedScenery, cityOverviewTexture) || JsonUtility.ToJson(world) != unchanged
                    || cityPanoramaBuildings.Any(building => building.label.Contains(gatePassage - cityPanoramaViewport.position)
                        || building.hit.Contains(gatePassage - cityPanoramaViewport.position)))
                    throw new Exception("Southern gate masonry is clipped, changes the cached city, or exposes scenery as a facility/site.");
                Debug.Log("CITY_GATE_MASONRY_RUNTIME_OK: full southern gate and its actual passage visible through the production panorama transform; passage has no facility/site action; cached landscape and World unchanged. Masonry, arch and paved approach require publisher visual and input review.");
                cityPanoramaCamera.Reset();
                cityPanoramaCamera.SetZoom(1.5f);
                yield return CaptureState("smoke-city-neighbourhoods.png");
                AssertCityPanoramaGeometry();
                if (JsonUtility.ToJson(world) != unchanged) throw new Exception("Reading the enlarged neighbourhood landscape changed the campaign.");
                cachedScenery = cityOverviewTexture;
                cityPanoramaCamera.Pan(160, -110);
                yield return CaptureState("smoke-city-river-grove.png");
                AssertCityPanoramaGeometry();
                if (JsonUtility.ToJson(world) != unchanged || cityPanoramaVisibleSites != 0
                    || !ReferenceEquals(cachedScenery, cityOverviewTexture))
                    throw new Exception("River/grove viewing changed the developed World, created scenery sites, or rebuilt unchanged art.");
                Debug.Log("CITY_SCENERY_DETAIL_RUNTIME_OK: garden and river/grove captures use the same cached production landscape after zoom/pan; unchanged World and no scenic site targets. New foliage, banks and water require publisher screenshot review; this marker is not visual acceptance.");
                Debug.Log("CITY_LANDSCAPE_RUNTIME_OK: cached courtyard/park/river landscape fills the viewport at compact, expanded and zoomed scales; town and actual facility coordinates stay proportional; new/developed states and original World preserved. Visual quality requires screenshot review.");
                Debug.Log("CITY_STREETSCAPE_RUNTIME_OK: new and developed city captures use production street/house composition; scenery adds no facility targets; all nine real facilities remain accessible; World unchanged.");
                Debug.Log("CITY_PANORAMA_RUNTIME_OK: independent expanded panorama, uniform aspect, zoom/pan clipping, actual district selection, planning/expanded/compact back hierarchy; no new persistence or settlement.");
                Debug.Log("CITY_OVERVIEW_VISUAL_RUNTIME_OK: full-height panorama with separated affairs and vertical army rail; real visible controls and three portrait slots fit safe area without overlap; empty formation has no owned-general targets; long-name and developed fixtures preserve complete World; actual clicks checked separately.");
                Debug.Log("CITY_AFFAIRS_COMPOSITION_RUNTIME_OK: real idle/working facility medallion and live level, separate summary, chapter scroll and five illustrated navigation entries match drawn touch bounds; three portrait paintings remain separate from their readable identity bases; paused World unchanged. Actual clicks and visual quality require publisher review.");
                Debug.Log("CITY_AFFAIRS_DESIGN_RUNTIME_OK: real Inkscape seal/summary/chapter components, exact design geometry, actual-font fit and separate building/name/chapter targets match the idle hall and real mill queue; paused World unchanged. Publisher must compare new runtime captures and actual image/title clicks.");
                Debug.Log("CITY_ARMY_COMPOSITION_RUNTIME_OK: three uncapped vertical standards, separate identity bases, real troop capacity bar and exact Core recruitment supply quote; three illustrated 44px commands match drawn geometry; actual garrison/return fixture displays busy state without spending on view; empty/idle/away World preserved. Actual clicks and screenshot quality require publisher review.");
                Debug.Log("CITY_ARMY_DESIGN_RUNTIME_OK: five Inkscape exports, compact independent full-portrait cards, curved identity plinths, separate strength/supply/44-unit commands, exact design geometry and actual-font fit; original campaign/return/modal checks retained. Visual and real-input acceptance remains required.");
                Debug.Log("CITY_SHELL_RUNTIME_OK: actual town hall crest, full name fitted with actual font metrics, six live stock/income columns and six illustrated footer actions; landscape fills the complete canvas behind separate edge controls with shared artwork/input coordinates; long-name, compact and expanded checks preserve World. Publisher must review new screenshots and real clicks.");
                Debug.Log("CITY_HEADER_DESIGN_RUNTIME_OK: actual Inkscape header assets, complete wrapped home name and town hall identity, independent icon/stock/income targets and actual-font fit match the exported design; isolated large-value fixture restores complete World. Publisher must compare new runtime screenshots and click real crest, name, stock and income regions; this marker is not visual acceptance.");
                Debug.Log("CITY_FOOTER_DESIGN_RUNTIME_OK: imported Inkscape navigation components, all six images above separate horizontal captions and recruitment portrait/base match the shared map layout; all eight controls fit safe area without pairwise overlap, actual Chinese fonts fit and the complete 78-unit shelf blocks terrain. Compact, empty-formation, expanded and facility-detail fixtures preserve World. Publisher must compare new captures and click every real image/caption; this marker is not visual acceptance.");
                Debug.Log("CITY_EDGE_DOCK_RUNTIME_OK: compact and expanded overview use an actual separate left image/caption dock and raised content canvas; all five navigation entries and bottom return fit safe area, remain non-overlapping and block map input; original city, facility and World assertions retained. Publisher must inspect new screenshots and real image/caption clicks.");
                Debug.Log("CITY_DOCK_DESIGN_RUNTIME_OK: five Inkscape exports, separated 72x54 navigation art and 80x24 captions, fitted actual Chinese text and distinct bottom return image/caption match the professional design; compact, expanded and planning views preserve the original touch commands and complete World. Publisher must compare new runtime captures and real image/caption clicks.");
                Debug.Log("CITY_FACILITY_PLAQUES_RUNTIME_OK: real facility name ribbons and level seals retain 44px touch height, separate non-overlapping labels, visible geometry and all nine developed facilities; construction uses the actual queue; World unchanged.");
            }
            finally
            {
                session = oldSession; map.Setup(world); map.selected = oldSelected; map.focus = oldFocus; map.zoom = oldZoom;
                tab = oldTab; cityPage = oldPage; status = oldStatus; cityFacilitiesOrigin = oldOrigin;
                citySelectedPlot = oldPlot; citySelectedBuilding = oldBuilding; cityOverviewPlanning = oldPlanning; cityOverviewExpanded = oldExpanded;
                cityDetailScroll = oldScroll; speed = oldSpeed;
                reviewMode = oldReview; if (!reviewMode) reviewTargets.Clear();
                cityPanoramaCamera.Reset(); cityPanoramaCamera.SetZoom(oldCityZoom); cityPanoramaCamera.Pan(oldPanX, oldPanY); CancelCityPanoramaPointer();
                if (JsonUtility.ToJson(world) != original) throw new Exception("Panorama fixture modified the original campaign.");
            }
        }

        private void AssertCityPanoramaGeometry()
        {
            if (!reviewMode) throw new Exception("City panorama diagnostics require visible target capture to be enabled before rendering.");
            AssertCityEdgeDockGeometry();
            if (cityOverviewTexture == null || cityOverviewTexture.width != CitySceneRaster.OverviewWidth
                || cityOverviewTexture.height != CitySceneRaster.OverviewHeight
                || !CityOverviewContains(cityPanoramaLandscape, cityPanoramaViewport))
                throw new Exception("City landscape leaves a blank band or did not load the complete production backdrop.");
            if(!CityOverviewContains(cityPanoramaLandscape,cityPanoramaCanvas)
                ||!CityOverviewContains(uiPixels,GuiRectToPixels(cityPanoramaCanvas))
                ||Math.Abs(cityPanoramaCanvas.height-cityPanoramaViewport.height-90)>.1f)
                throw new Exception("City landscape no longer fills the full canvas behind the floating controls.");
            if (cityPanoramaImage.width <= 0 || Math.Abs(cityPanoramaImage.width / cityPanoramaImage.height - 1000f / 440) > .0001f)
                throw new Exception("City panorama stretched its source artwork.");
            float sourceScale = cityPanoramaImage.width / 1000f;
            if (Math.Abs(cityPanoramaLandscape.x + CitySceneRaster.OverviewOffsetX * sourceScale - cityPanoramaImage.x) > .1f
                || Math.Abs(cityPanoramaLandscape.y + CitySceneRaster.OverviewOffsetY * sourceScale - cityPanoramaImage.y) > .1f
                || Math.Abs(cityPanoramaLandscape.width / CitySceneRaster.OverviewWidth - sourceScale) > .0001f)
                throw new Exception("Landscape and real facility targets no longer share the town origin and scale.");
            if (cityPanoramaVisibleControls.Count == 0) throw new Exception("City panorama has no visible facility controls.");
            foreach (var rect in cityPanoramaVisibleControls)
            {
                // These are the same local rectangles used by visible buttons, even outside -edReview.
                if (!CityPanoramaControlVisible(rect, cityPanoramaViewport))
                    throw new Exception("Panorama registered a clipped facility target.");
            }
            for (int i = 0; i < cityPanoramaBuildings.Count; i++)
            {
                var plaque = cityPanoramaBuildings[i];
                var plot = world.cityPlots[plaque.index];
                bool construction = world.CityJob != null && world.CityJob.plotIndex == plaque.index;
                if (plot.building == CityBuilding.None && !construction)
                    throw new Exception("A scenic building received a facility plaque.");
                if (plaque.label.height < 44 || plaque.label.width < 126 || plaque.label.width > 142)
                    throw new Exception("Facility name ribbon lost its compact width or usable touch height.");
                for (int j = 0; j < i; j++)
                    if (plaque.label.Overlaps(cityPanoramaBuildings[j].label))
                        throw new Exception("Facility name ribbons overlap and conceal a real touch target.");
            }
        }

        private IEnumerator CityOverviewPlanningSmoke()
        {
            var oldSession = session; string oldTab = tab, oldPage = cityPage, oldStatus = status, oldOrigin = cityFacilitiesOrigin;
            int oldSelected = map.selected, oldPlot = citySelectedPlot;
            bool oldPlanning = cityOverviewPlanning, oldReview = reviewMode;
            var oldBuilding = citySelectedBuilding; var oldScroll = cityDetailScroll;
            var oldFocus = map.focus; float oldZoom = map.zoom, oldSpeed = speed;
            string original = JsonUtility.ToJson(world);
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath, "smoke-city-planning-unused")), () => DateTime.UtcNow, true);
            try
            {
                // Geometry assertions consume targets recorded by actual OnGUI drawing.
                // Each fixture owns capture mode instead of relying on a prior smoke's state.
                session.StartYouzhou(0, "全景选址验收"); map.Setup(world); speed = 0; reviewMode = true;
                OpenCityOverview(); string unchanged = JsonUtility.ToJson(world);
                SetCityOverviewPlanning(true);
                yield return CaptureState("smoke-city-planning.png");
                if (!BlocksMapInput || !cityOverviewPlanning || world.CityQuestReady(1) || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("City planning changed the campaign or completed a facility visit without entering it.");
                int available = world.cityPlots.Count(CityOverviewPlotAvailable);
                if (available == 0) throw new Exception("City planning fixture has no legal empty site.");
                SelectCityOverviewPlot(4); SelectCityOverviewPlot(-1); SelectCityOverviewPlot(9);
                if (cityPage != "overview" || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("City planning accepted an occupied or invalid site.");
                int chosen = world.cityPlots.First(CityOverviewPlotAvailable).index;
                SelectCityOverviewPlot(chosen);
                if (cityPage != "facilities" || citySelectedPlot != chosen || !world.CityQuestReady(1) || world.CityJob != null)
                    throw new Exception("City planning did not reach its actual empty plot and normal district visit.");
                yield return CaptureState("smoke-city-planning-detail.png");
                unchanged = JsonUtility.ToJson(world); HandleMobileBack();
                if (cityPage != "overview" || !cityOverviewPlanning || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("City planning detail return lost the planning layer or changed the campaign.");
                HandleMobileBack();
                if (tab != "主城" || cityOverviewPlanning || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("City planning system back left the city before closing site selection.");
                SetCityOverviewPlanning(true); SelectCityOverviewPlot(chosen);
                if (JsonUtility.ToJson(world) != unchanged) throw new Exception("Repeated site selection changed the campaign.");
                world.resources = new Supplies { grain = 50000, grass = 50000, stone = 50000, iron = 50000, gold = 50000 };
                world.StartCityConstruction(chosen, CityBuilding.Mill);
                if (world.CityJob == null || world.CityJob.plotIndex != chosen) throw new Exception("Selected site did not start the real construction queue.");
                ReturnFromCityPage(); unchanged = JsonUtility.ToJson(world);
                yield return CaptureState("smoke-city-planning-working.png");
                AssertCityPanoramaGeometry();
                if (world.cityPlots.Count(CityOverviewPlotAvailable) != available - 1 || CityOverviewPlotAvailable(world.cityPlots[chosen]))
                    throw new Exception("The active construction site remained available for a second build.");
                SelectCityOverviewPlot(chosen);
                if (cityPage != "overview" || JsonUtility.ToJson(world) != unchanged) throw new Exception("Reserved site selection replaced the real construction queue.");
                OpenCityFacilities(chosen);
                if (citySelectedBuilding != CityBuilding.Mill || JsonUtility.ToJson(world) != unchanged) throw new Exception("Construction panorama did not reach the same live facility.");
                ReturnFromCityPage(); world.Tick(world.CityJob.remaining + .01);
                yield return CaptureState("smoke-city-planning-built.png");
                AssertCityPanoramaGeometry();
                if (world.CityLevel(CityBuilding.Mill) != 1 || world.CityHourlyIncome().grain != 160 || CityOverviewPlotAvailable(world.cityPlots[chosen]))
                    throw new Exception("Completed facility did not replace its construction site and apply its real effect.");
                unchanged = JsonUtility.ToJson(world); OpenCityFacilities(chosen); HandleMobileBack();
                if (cityPage != "overview" || !cityOverviewPlanning || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Built facility navigation repeated construction or lost its panorama origin.");
                tab = "领地"; OpenCityFacilities(chosen); HandleMobileBack();
                if (cityOverviewPlanning || cityPage != "overview" || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("An external facility entry inherited stale panorama planning state.");
                Debug.Log("CITY_PLANNING_RUNTIME_OK: isolated read-only panorama site selection; real empty plot, visit, queue, completed mill income and two-level system back; occupied/invalid/reserved sites rejected; navigation does not spend or build.");
            }
            finally
            {
                session = oldSession; map.Setup(world); map.selected = oldSelected; map.focus = oldFocus; map.zoom = oldZoom;
                tab = oldTab; cityPage = oldPage; status = oldStatus; cityFacilitiesOrigin = oldOrigin;
                citySelectedPlot = oldPlot; citySelectedBuilding = oldBuilding; cityOverviewPlanning = oldPlanning;
                cityDetailScroll = oldScroll; speed = oldSpeed;
                reviewMode = oldReview; if (!reviewMode) reviewTargets.Clear();
                if (JsonUtility.ToJson(world) != original) throw new Exception("Planning fixture modified the original campaign.");
            }
        }
    }
}
