using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public bool mapHudVisible;
            public string mapHudStyle;
            public string mapRecruitStyle;
            public bool mapRecruitTextFits;
            public string mapHudHeaderStyle;
            public bool mapHudHeaderTextFits;
            public string mapHudArmyStyle;
            public bool mapHudArmyTextFits;
            public int[] mapHudArmyUids;
            public float mapHudSafeX, mapHudSafeY, mapHudSafeWidth, mapHudSafeHeight;
            public float mapHudTouchMinimum;
            public string mapQuestStyle, mapQuestArtKind;
            public int mapQuestId, mapQuestChapter, mapQuestProgress, mapQuestTarget;
            public bool mapQuestReady, mapQuestTextFits, mapQuestHovered;
            public string minimapStyle;
            public string minimapDesignStyle;
            public bool minimapTextFits;
            public float[] minimapViewportPoints;
            public int minimapNavigationSequence;
            public float minimapNavigationPointerX, minimapNavigationPointerY;
            public string terrainStyle;
            public string riverStyle;
            public int riverBridgeId = -1, riverBankId = -1;
            public float riverBridgeX, riverBridgeY, riverRailX, riverRailY, riverBankX, riverBankY;
        }

        private int minimapNavigationSequence;
        private Vector2 minimapNavigationPointer;

        private void RecordMinimapNavigation(Vector2 logicalPointer)
        {
            if(!reviewMode)return;
            // Observe only the visible atlas event that really changed focus.
            // IMGUI coordinates are converted back through the active safe-area
            // transform, independently of the external Windows pointer request.
            minimapNavigationPointer=GuiPointToPixels(logicalPointer);
            minimapNavigationSequence++;
        }

        private void FillMapHudReview(ReviewState review)
        {
            FillMapCityLabelReview(review);
            // Targets come exclusively from the visible presentation controls. This
            // observer never creates a target or turns resource cells into buttons.
            review.mapHudVisible = world.IsYouzhou && tab == "领地" && !BlocksMapInput
                && reviewTargets.ContainsKey("hud-ruler-city");
            review.mapHudStyle = review.mapHudVisible ? MapHudNavigationLayout.Style : "";
            bool recruitSealVisible=review.mapHudVisible&&map.selected<0&&reviewTargets.ContainsKey("recruit-tab-art");
            review.mapRecruitStyle=recruitSealVisible?MapHudNavigationLayout.RecruitStyle:"";
            review.mapRecruitTextFits=recruitSealVisible&&hudMapRecruitTextFits;
            review.mapHudHeaderStyle = review.mapHudVisible ? MapHudHeaderLayout.Style : "";
            review.mapHudHeaderTextFits = review.mapHudVisible && hudHeaderTextFits;
            review.mapHudArmyStyle = review.mapHudVisible ? MapHudArmyLayout.Style : "";
            review.mapHudArmyTextFits = review.mapHudVisible && hudArmyTextFits;
            review.mapHudArmyUids = world.ActiveGenerals.Take(3).Select(c => c.uid).ToArray();
            review.mapHudSafeX = uiPixels.x; review.mapHudSafeY = uiPixels.y;
            review.mapHudSafeWidth = uiPixels.width; review.mapHudSafeHeight = uiPixels.height;
            review.mapHudTouchMinimum = 44 * scale;
            if(review.mapHudVisible&&reviewTargets.ContainsKey("hud-quest-panel"))
            {
                var quest=world.CurrentCityQuest;
                review.mapQuestStyle=MapHudQuestLayout.Style;
                review.mapQuestTextFits=hudQuestTextFits;
                review.mapQuestHovered=hudQuestHovered;
                review.mapQuestId=quest==null?0:quest.id;
                review.mapQuestChapter=quest==null?2:CityQuestChapter(quest.id);
                review.mapQuestTarget=quest==null?CityCatalog.Quests.Length:world.CityQuestTarget(quest.id);
                review.mapQuestProgress=quest==null?world.cityClaimedQuests.Count:Math.Min(review.mapQuestTarget,world.CityQuestProgress(quest.id));
                review.mapQuestReady=quest!=null&&world.CityQuestReady(quest.id);
                review.mapQuestArtKind="task-book";
            }
            bool atlasVisible=review.mapHudVisible&&reviewTargets.ContainsKey("minimap-area");
            review.minimapStyle=atlasVisible?"youzhou-atlas-v1":"";
            review.minimapDesignStyle=atlasVisible?MapHudAtlasLayout.Style:"";
            review.minimapTextFits=atlasVisible&&AtlasDesignReady&&atlasTextFits;
            review.minimapViewportPoints=atlasVisible?minimapViewport.SelectMany(p=>new[]{p.x,p.y}).ToArray():new float[0];
            review.minimapNavigationSequence=minimapNavigationSequence;
            review.minimapNavigationPointerX=minimapNavigationPointer.x;
            review.minimapNavigationPointerY=minimapNavigationPointer.y;
            review.terrainStyle=review.mapHudVisible&&map.RenderStatistics.Contains(YouzhouTerrainLayout.Style)?YouzhouTerrainLayout.Style:"";
            review.riverStyle=review.mapHudVisible&&map.RenderStatistics.Contains(YouzhouRiverLayout.Style)?YouzhouRiverLayout.Style:"";
            if(review.mapHudVisible) FillRiverVisualReview(review);
        }

        private void FillRiverVisualReview(ReviewState review)
        {
            // These are observations of unobstructed world geometry, never hidden
            // controls or targets. Real mouse tests still go through map picking.
            foreach(var land in world.lands.OrderBy(t=>(t.x-map.focus.x)*(t.x-map.focus.x)+(t.z-map.focus.z)*(t.z-map.focus.z)))
            {
                Vector2 pixel;
                float start,end;
                if(review.riverBridgeId<0&&YouzhouRiverLayout.TryBridgeSpan(world,land,out start,out end))
                {
                    float deck=YouzhouRiverLayout.BridgeDeckHeight(land.x,start,end);Vector2 railPixel;
                    if(VisibleRiverPoint(new Vector3(land.x,deck+.009f,land.z),land.id,out pixel)
                        &&VisibleRiverPoint(new Vector3(land.x,deck+YouzhouRiverLayout.BridgePostHeight,
                            land.z+YouzhouRiverLayout.BridgeRailOffset),land.id,out railPixel))
                    {
                        review.riverBridgeId=land.id;review.riverBridgeX=pixel.x;review.riverBridgeY=pixel.y;
                        review.riverRailX=railPixel.x;review.riverRailY=railPixel.y;
                    }
                }
                if(review.riverBankId<0&&land.kind==LandKind.Water&&!land.bridge)
                {
                    foreach(int direction in new[]{-1,1})
                    {
                        var neighbor=world.TryGetLand(land.x+direction,land.z);
                        if(neighbor==null||YouzhouRiverLayout.IsWater(neighbor))continue;
                        if(VisibleRiverPoint(new Vector3(land.x+direction*.32f,0,land.z),land.id,out pixel))
                        { review.riverBankId=land.id;review.riverBankX=pixel.x;review.riverBankY=pixel.y;break; }
                    }
                }
                if(review.riverBridgeId>=0&&review.riverBankId>=0)break;
            }
        }

        private bool VisibleRiverPoint(Vector3 position,int landId,out Vector2 pixel)
        {
            Vector3 screen=map.viewCamera.WorldToScreenPoint(position); pixel=new Vector2(screen.x,Screen.height-screen.y);
            Rect safe=uiPixels; safe.xMin+=10; safe.xMax-=10; safe.yMin+=80*scale;safe.yMax-=88*scale;
            int picked;
            return screen.z>0&&safe.Contains(pixel)&&!map.IsBlocked(pixel)&&map.TryPickLand(screen,out picked)&&picked==landId;
        }

        private IEnumerator CaptureMapHudState(string filename)
        {
            if (!world.IsYouzhou) { yield return CaptureState(filename); yield break; }
            bool previousReview = reviewMode; float previousSpeed = speed;
            reviewMode = true; speed = 0;
            string unchanged = JsonUtility.ToJson(world);
            try
            {
                yield return CaptureState(filename);
                AssertMapHudTargets();
                AssertMapCityLabels();
                map.AssertYouzhouTerrainGeometry();
                map.AssertYouzhouRiverGeometry(false);
                if(map.selected<0)AssertMinimapAtlas();
                if (JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Map HUD rendering changed the paused campaign: " + filename);
            }
            finally
            {
                reviewMode = previousReview; speed = previousSpeed;
                if (!reviewMode) reviewTargets.Clear();
            }
        }

        private void AssertMapHudTargets()
        {
            if (tab != "领地" || BlocksMapInput) throw new Exception("Map HUD capture is not a visible map page.");
            var expected = new List<string> { "hud-ruler-city", "hud-save", "city-open", "map-tab", "roster-tab",
                "recruit-tab", "reports-tab", "hud-help", "speed-cycle", "army-reinforcement", "hud-army-header", "hud-army-status" };
            expected.AddRange(Enumerable.Range(0, 6).Select(i => "hud-resource-" + i));
            int armyCount = Math.Min(3, world.ActiveGenerals.Count);
            expected.AddRange(Enumerable.Range(0, armyCount).Select(i => "hud-army-card-" + i));
            expected.Add(world.order == null ? "hud-army-formation" : "hud-army-recall");
            if (TrainingActive) expected.Add("army-training-progress");
            if (reviewTargets.ContainsKey("army-battle-watch")) expected.Add("army-battle-watch");
            for (int i = armyCount; i < 3; i++)
                foreach(string part in new[]{"card","art","name","identity"})
                    if (reviewTargets.ContainsKey("hud-army-" + part + "-" + i))
                        throw new Exception("Empty HUD slot exposed a fabricated general " + part + ".");
            if (reviewTargets.ContainsKey(world.order == null ? "hud-army-recall" : "hud-army-formation"))
                throw new Exception("Map HUD exposed the wrong idle/marching army action.");
            for (int i = 0; i < expected.Count; i++)
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(expected[i], out target))
                    throw new Exception("Map HUD control was not actually drawn: " + expected[i]);
                Rect rect = MapHudReviewRect(target);
                if (rect.width <= 0 || rect.height <= 0 || rect.xMin < uiPixels.xMin - .1f
                    || rect.yMin < uiPixels.yMin - .1f || rect.xMax > uiPixels.xMax + .1f || rect.yMax > uiPixels.yMax + .1f)
                    throw new Exception("Map HUD control is clipped by the real safe area: " + expected[i]);
                if (!map.IsBlocked(rect.center)) throw new Exception("Map HUD control leaks input to terrain: " + expected[i]);
                for (int j = 0; j < i; j++)
                {
                    Rect previous=MapHudReviewRect(reviewTargets[expected[j]]);
                    if (rect.Overlaps(previous))
                        throw new Exception("Map HUD controls overlap: " + expected[i] + " / " + expected[j]
                            + "; intersection pixels=" + (Math.Min(rect.xMax,previous.xMax)-Math.Max(rect.xMin,previous.xMin)).ToString("R")
                            + "x" + (Math.Min(rect.yMax,previous.yMax)-Math.Max(rect.yMin,previous.yMin)).ToString("R")
                            + "; scale=" + scale.ToString("R"));
                }
            }
            ReviewTarget surface;
            if (!reviewTargets.TryGetValue("map-surface", out surface)) throw new Exception("Map surface target is missing.");
            Rect actual = MapHudReviewRect(surface), viewport = GuiRectToPixels(new Rect(0, 70, width, height - 148));
            if (Math.Abs(actual.yMin - viewport.yMin) > .1f || Math.Abs(actual.yMax - viewport.yMax) > .1f)
                throw new Exception("Map HUD changed the established header/footer viewport boundaries.");
            AssertMapCommandRegions();
            AssertMapQuestRegions();
            AssertMapHeaderRegions();
        }

        private void AssertMapHeaderRegions()
        {
            if(!hudHeaderTextFits)throw new Exception("Map header actual font metrics exceed the designed text bounds.");
            Rect identity=RequiredMapCommandRect("hud-ruler-city");
            Rect art=RequiredMapCommandRect("hud-ruler-art"),name=RequiredMapCommandRect("hud-ruler-name");
            Rect crest=RequiredMapCommandRect("hud-ruler-crest"),identityDetail=RequiredMapCommandRect("hud-ruler-detail");
            if(!MapCommandContains(identity,art)||!MapCommandContains(identity,name)||art.Overlaps(name)||identity.height/scale<44)
                throw new Exception("Map header city art/name or touch area is clipped.");
            if(!MapCommandContains(identity,crest)||!MapCommandContains(crest,art)||!MapCommandContains(identity,identityDetail)
                ||crest.Overlaps(name)||crest.Overlaps(identityDetail)||name.Overlaps(identityDetail))
                throw new Exception("Map header crest, complete city image and identity labels overlap or clip.");
            AssertMapHeaderDesignRect(identity,new Rect(16,4,246,62),"identity");
            AssertMapHeaderDesignRect(crest,new Rect(16,4,62,62),"crest");
            AssertMapHeaderDesignRect(art,new Rect(22,13,50,44),"city image");
            AssertMapHeaderDesignRect(identityDetail,new Rect(84,44,174,20),"identity detail");
            if(hudHeaderIdentity.width!=492||hudHeaderIdentity.height!=124||hudHeaderLedger.width!=1344||hudHeaderLedger.height!=116)
                throw new Exception("Map header no longer uses the exact exported identity and ledger textures.");
            float pitch=Mathf.Min(168,(width-604)/4);
            for(int i=0;i<6;i++)
            {
                Rect cell=RequiredMapCommandRect("hud-resource-"+i);
                Rect icon=RequiredMapCommandRect("hud-resource-icon-"+i);
                Rect stock=RequiredMapCommandRect("hud-resource-stock-"+i);
                Rect detail=RequiredMapCommandRect("hud-resource-detail-"+i);
                if(!MapCommandContains(cell,icon)||!MapCommandContains(cell,stock)||!MapCommandContains(cell,detail)
                    ||icon.Overlaps(stock)||icon.Overlaps(detail)||stock.Overlaps(detail)||!map.IsBlocked(stock.center))
                    throw new Exception("Map header resource layers overlap or leak map input: "+i);
                float x=i<4?280+i*pitch:280+pitch*4+16+(i-4)*150;
                float cellWidth=i<4?pitch-6:144;
                AssertMapHeaderDesignRect(cell,new Rect(x,5,cellWidth,58),"resource "+i);
                AssertMapHeaderDesignRect(icon,new Rect(x+(i<4?6:7),19,i<4?32:26,i<4?26:28),"icon "+i);
                AssertMapHeaderDesignRect(stock,new Rect(x+(i<4?44:40),30,i<4?cellWidth-48:100,26),"stock "+i);
                AssertMapHeaderDesignRect(detail,new Rect(x+(i<4?44:40),6,i<4?cellWidth-48:100,23),"detail "+i);
            }
            // Render terrain under the transparent veil without making it pickable.
            Rect camera=map.viewCamera.pixelRect;
            Rect expected=GuiRectToPixels(HudDesignRect(MapHudHeaderLayout.Camera(width,height)));
            Rect actual=new Rect(camera.x,Screen.height-camera.yMax,camera.width,camera.height);
            if(Mathf.Abs(actual.yMin-expected.yMin)>.1f||Mathf.Abs(actual.yMax-expected.yMax)>.1f
                ||Mathf.Abs(actual.xMin-expected.xMin)>.1f||Mathf.Abs(actual.width-expected.width)>.1f)
                throw new Exception("Map camera does not cover the designed transparent header inside the safe area.");
            Rect input=GuiRectToPixels(new Rect(0,70,width,height-148));
            if(Mathf.Abs(map.screenArea.yMin-input.yMin)>.1f||Mathf.Abs(map.screenArea.yMax-input.yMax)>.1f)
                throw new Exception("Transparent header moved the map input/edge-scroll area under blocked UI.");
        }

        private void AssertMapHeaderDesignRect(Rect actual,Rect design,string part)
        {
            Rect expected=GuiRectToPixels(design);
            if(Mathf.Abs(actual.x-expected.x)>.1f||Mathf.Abs(actual.y-expected.y)>.1f
                ||Mathf.Abs(actual.width-expected.width)>.1f||Mathf.Abs(actual.height-expected.height)>.1f)
                throw new Exception("Map header differs from inspected ledger design: "+part);
        }

        private void AssertMapQuestRegions()
        {
            if(hudQuestSleeve==null||hudQuestSleeveHover==null||hudQuestSleeve.width!=528||hudQuestSleeve.height!=136
                ||hudQuestSleeveHover.width!=528||hudQuestSleeveHover.height!=136)
                throw new Exception("Task sleeve must use both inspected Inkscape exports at their actual dimensions.");
            if(!hudQuestTextFits)throw new Exception("Map task actual font metrics exceed the exported design text bounds.");
            Rect panel=RequiredMapCommandRect("hud-quest-panel"),open=RequiredMapCommandRect("quest-open");
            Rect art=RequiredMapCommandRect("hud-quest-art"),titleRect=RequiredMapCommandRect("hud-quest-title");
            Rect objective=RequiredMapCommandRect("hud-quest-objective"),progress=RequiredMapCommandRect("hud-quest-progress");
            Rect sleeve=RequiredMapCommandRect("hud-quest-body"),chapter=RequiredMapCommandRect("hud-quest-chapter");
            if(!MapCommandContains(open,sleeve)||art.Overlaps(sleeve)||!MapCommandContains(sleeve,chapter)
                ||!MapCommandContains(sleeve,titleRect)||!MapCommandContains(sleeve,objective)||chapter.Overlaps(titleRect))
                throw new Exception("Task book must remain detached from the information sleeve and its live text.");
            var quest=world.CurrentCityQuest;
            string actionId=quest==null?"hud-quest-history":world.CityQuestReady(quest.id)?"quest-claim":"quest-go";
            Rect action=RequiredMapCommandRect(actionId);
            if(!MapCommandContains(uiPixels,panel)||!MapCommandContains(panel,open)||!MapCommandContains(panel,art)
                ||!MapCommandContains(open,titleRect)||!MapCommandContains(open,objective)||!MapCommandContains(panel,progress)
                ||!MapCommandContains(panel,action)||art.Overlaps(titleRect)||art.Overlaps(objective)||titleRect.Overlaps(objective)
                ||!MapCommandContains(open,art)||art.Overlaps(progress)||art.Overlaps(action)
                ||open.Overlaps(action)||open.Overlaps(progress)||progress.Overlaps(action)
                ||action.width/scale<43.99f||action.height/scale<43.99f||art.width/scale<43.99f||art.height/scale<43.99f
                ||open.width/scale<43.99f||open.height/scale<43.99f||!map.IsBlocked(art.center)
                ||!map.IsBlocked(open.center)||!map.IsBlocked(progress.center)||!map.IsBlocked(action.center))
                throw new Exception("Map quest banner, objective, progress or touch action is clipped/overlapped or leaks terrain input.");
            // Independent acceptance dimensions from the inspected SVG contract;
            // a stale large charter cannot pass just by relabelling its style.
            AssertMapQuestDesignRect(panel,new Rect(20,90,354,118),"panel");
            AssertMapQuestDesignRect(art,new Rect(20,90,80,68),"book");
            AssertMapQuestDesignRect(open,new Rect(20,90,354,68),"chapter entry");
            AssertMapQuestDesignRect(sleeve,new Rect(110,90,264,68),"information sleeve");
            AssertMapQuestDesignRect(chapter,new Rect(120,90,244,18),"chapter text");
            AssertMapQuestDesignRect(titleRect,new Rect(120,108,244,24),"title");
            AssertMapQuestDesignRect(objective,new Rect(120,132,244,26),"objective");
            AssertMapQuestDesignRect(progress,new Rect(20,164,212,44),"progress");
            AssertMapQuestDesignRect(action,new Rect(242,164,132,44),"action");
            foreach(string id in new[]{"quest-go","quest-claim","hud-quest-history"})
                if(id!=actionId&&reviewTargets.ContainsKey(id))throw new Exception("Map quest displays a stale or duplicate action: "+id);
            ReviewTarget landReturn;
            if(reviewTargets.TryGetValue("quest-land-return",out landReturn)&&panel.Overlaps(MapHudReviewRect(landReturn)))
                throw new Exception("Quest charter obscures the actual land-objective return.");
            if(panel.Overlaps(RequiredMapCommandRect("hud-army-panel")))
                throw new Exception("Quest charter overlaps the map army.");
        }

        private void AssertMapQuestDesignRect(Rect actual,Rect design,string region)
        {
            Rect expected=GuiRectToPixels(design);
            if(Mathf.Abs(actual.x-expected.x)>.1f||Mathf.Abs(actual.y-expected.y)>.1f
                ||Mathf.Abs(actual.width-expected.width)>.1f||Mathf.Abs(actual.height-expected.height)>.1f)
                throw new Exception("Map task does not match the inspected compact design: "+region);
        }

        private IEnumerator CaptureMapQuestCharter(string filename)
        {
            string previousTab=tab;int previousSelected=map.selected;
            var previousLandWorld=cityQuestLandWorld;int previousLandId=cityQuestLandId;
            try
            {
                tab="领地";map.selected=-1;
                yield return CaptureMapHudState(filename);
            }
            finally {tab=previousTab;map.selected=previousSelected;cityQuestLandWorld=previousLandWorld;cityQuestLandId=previousLandId;}
        }

        private void AssertMapRecruitDesign()
        {
            if(!hudMapRecruitTextFits||hudMapRecruitSeal==null||hudMapRecruitSealHover==null
                ||hudMapRecruitSeal.width!=320||hudMapRecruitSeal.height!=280
                ||hudMapRecruitSealHover.width!=320||hudMapRecruitSealHover.height!=280)
                throw new Exception("Map recruitment lost the inspected seal textures or actual Chinese font fit.");
            Rect button=RequiredMapCommandRect("recruit-tab"),art=RequiredMapCommandRect("recruit-tab-art");
            Rect caption=RequiredMapCommandRect("recruit-tab-caption");
            AssertMapHeaderDesignRect(button,new Rect(width-180,height-144,160,140),"recruit seal");
            AssertMapHeaderDesignRect(art,new Rect(width-130,height-134,60,88),"complete recruitment portrait");
            AssertMapHeaderDesignRect(caption,new Rect(width-170,height-30,140,24),"recruitment title");
            if(!MapCommandContains(uiPixels,button)||!MapCommandContains(button,art)||!MapCommandContains(button,caption)
                ||art.Overlaps(caption)||Mathf.Abs((GuiRectToPixels(new Rect(0,height-78,width,78)).yMin-button.yMin)/scale-66)>.01f)
                throw new Exception("Map recruitment seal is clipped, overlaps its separate title or lost its 66-unit rise.");
            // The raised portion is inside the former terrain viewport. Check the
            // entire visible control's blocking independently of recorded targets.
            for(int row=0;row<3;row++)for(int column=0;column<3;column++)
                if(!map.IsBlocked(new Vector2(button.xMin+button.width*(column+.5f)/3,button.yMin+button.height*(row+.5f)/3)))
                    throw new Exception("Raised recruitment seal leaks pointer input into the terrain.");
        }

        private void AssertMapCommandRegions()
        {
            if(!hudNavigationTextFits)throw new Exception("Shared map/city navigation actual font metrics exceed the inspected text regions.");
            Rect save=RequiredMapCommandRect("hud-save"),clock=RequiredMapCommandRect("speed-cycle");
            float utilityGap=(clock.xMin-save.xMax)/scale;
            if(Mathf.Abs(utilityGap-8)>.01f||Mathf.Abs(save.width/scale-82)>.01f||Mathf.Abs(clock.width/scale-180)>.01f
                ||save.height/scale<48.99f||clock.height/scale<48.99f)
                throw new Exception("Map save/time controls lost the inspected eight-unit separation or full touch regions: gap="+utilityGap.ToString("R"));
            foreach(string id in new[]{"city-open","map-tab","roster-tab","recruit-tab","reports-tab","hud-help"})
            {
                Rect button=RequiredMapCommandRect(id),art=RequiredMapCommandRect(id+"-art"),caption=RequiredMapCommandRect(id+"-caption");
                bool placement=art.yMax<=caption.yMin;
                if(!MapCommandContains(button,art)||!MapCommandContains(button,caption)||art.Overlaps(caption)
                    ||!placement||button.height/scale<44)
                    throw new Exception("Illustrated map entry art/caption is clipped or overlaps: "+id);
                if(id!="recruit-tab")
                {
                    if(Mathf.Abs((art.xMin-button.xMin)/scale-4)>.01f||Mathf.Abs((art.yMin-button.yMin)/scale-3)>.01f
                        ||Mathf.Abs(art.width/scale-100)>.01f||Mathf.Abs(art.height/scale-42)>.01f
                        ||Mathf.Abs((caption.xMin-button.xMin)/scale-4)>.01f||Mathf.Abs((caption.yMin-button.yMin)/scale-46)>.01f
                        ||Mathf.Abs(caption.width/scale-100)>.01f||Mathf.Abs(caption.height/scale-22)>.01f)
                        throw new Exception("Map navigation differs from the inspected image-above-caption design: "+id);
                }
            }
            if(map.selected<0)AssertMapRecruitDesign();
            else
            {
                // Selected-land commands can occupy the raised region. Preserve
                // the compact footer control instead of exposing an obscured image.
                AssertMapHeaderDesignRect(RequiredMapCommandRect("recruit-tab"),new Rect(width-152,height-74,132,70),"selected-land compact recruitment");
                AssertMapHeaderDesignRect(RequiredMapCommandRect("recruit-tab-art"),new Rect(width-106,height-67,40,38),"selected-land recruitment portrait");
                AssertMapHeaderDesignRect(RequiredMapCommandRect("recruit-tab-caption"),new Rect(width-144,height-27,116,22),"selected-land recruitment title");
            }
            Rect panel=RequiredMapCommandRect("hud-army-panel");
            Rect header=RequiredMapCommandRect("hud-army-header"),statusRect=RequiredMapCommandRect("hud-army-status");
            Rect reinforce=RequiredMapCommandRect("army-reinforcement");
            Rect action=RequiredMapCommandRect(world.order==null?"hud-army-formation":"hud-army-recall");
            if(!hudArmyTextFits)throw new Exception("Map army actual font metrics exceed the inspected design text bounds.");
            if(!MapCommandContains(uiPixels,panel)||!MapCommandContains(panel,header)||!MapCommandContains(panel,statusRect)
                ||!MapCommandContains(panel,reinforce)||!MapCommandContains(panel,action)
                ||reinforce.width/scale<43.99f||reinforce.height/scale<43.99f||action.width/scale<43.99f||action.height/scale<43.99f
                ||!map.IsBlocked(header.center)||!map.IsBlocked(statusRect.center))
                throw new Exception("Map army actions lost their visible 44-unit foot row.");
            // Independent inspected-SVG geometry. A stale solid panel cannot pass
            // only by changing its style label; image/alpha inspection is separate.
            float armyY=height-396;
            AssertMapArmyDesignRect(panel,new Rect(20,armyY,354,302),"area");
            AssertMapArmyDesignRect(header,new Rect(20,armyY,354,28),"header");
            AssertMapArmyDesignRect(statusRect,new Rect(20,armyY+236,354,20),"status");
            AssertMapArmyDesignRect(reinforce,new Rect(20,armyY+258,171,44),"reinforcement");
            AssertMapArmyDesignRect(action,new Rect(203,armyY+258,171,44),"formation or recall");
            for(int i=0;i<Math.Min(3,world.ActiveGenerals.Count);i++)
            {
                Rect card=RequiredMapCommandRect("hud-army-card-"+i),art=RequiredMapCommandRect("hud-army-art-"+i);
                Rect name=RequiredMapCommandRect("hud-army-name-"+i),identity=RequiredMapCommandRect("hud-army-identity-"+i);
                if(!MapCommandContains(panel,card)||!MapCommandContains(card,art)||!MapCommandContains(card,name)
                    ||!MapCommandContains(card,identity)||art.Overlaps(name)||art.Overlaps(identity)||name.Overlaps(identity)
                    ||name.xMin<art.xMax||identity.xMin<name.xMax||reinforce.yMin<card.yMax||action.yMin<card.yMax
                    ||card.height/scale<63.99f||card.width/scale<43.99f)
                    throw new Exception("Army portrait, identity strip or foot actions overlap: "+i);
                float rowY=armyY+32+i*68;
                AssertMapArmyDesignRect(card,new Rect(20,rowY,354,64),"row "+i);
                AssertMapArmyDesignRect(art,new Rect(24,rowY+1,44,62),"portrait "+i);
                AssertMapArmyDesignRect(name,new Rect(82,rowY+12,164,40),"horizontal name "+i);
                AssertMapArmyDesignRect(identity,new Rect(262,rowY+12,102,40),"identity "+i);
                if(i>0)
                {
                    Rect previous=RequiredMapCommandRect("hud-army-card-"+(i-1));
                    var gap=new Vector2(card.center.x,(previous.yMax+card.yMin)*.5f);
                    if(Mathf.Abs((card.yMin-previous.yMax)/scale-4)>.01f||!map.IsBlocked(gap))
                        throw new Exception("Army row gap lost its inspected spacing or leaked terrain input.");
                }
            }
            foreach(string id in new[]{"army-training-progress","army-battle-watch"})
            {
                ReviewTarget target;if(!reviewTargets.TryGetValue(id,out target))continue;
                Rect entry=MapHudReviewRect(target),quest=RequiredMapCommandRect("hud-quest-panel");
                int offset=id=="army-training-progress"||!TrainingActive?51:102;
                AssertMapArmyDesignRect(entry,new Rect(20,armyY-offset,354,44),id);
                if(!MapCommandContains(uiPixels,entry)||entry.Overlaps(quest)||entry.Overlaps(panel)||!map.IsBlocked(entry.center))
                    throw new Exception("Dynamic training/watch entry clips or overlaps task/army.");
                ReviewTarget objectiveReturn;
                if(reviewTargets.TryGetValue("quest-land-return",out objectiveReturn)&&entry.Overlaps(MapHudReviewRect(objectiveReturn)))
                    throw new Exception("Dynamic training/watch entry obscures the actual quest objective return.");
            }
        }

        private void AssertMapArmyDesignRect(Rect actual,Rect design,string region)
        {
            Rect expected=GuiRectToPixels(design);
            if(Mathf.Abs(actual.x-expected.x)>.1f||Mathf.Abs(actual.y-expected.y)>.1f
                ||Mathf.Abs(actual.width-expected.width)>.1f||Mathf.Abs(actual.height-expected.height)>.1f)
                throw new Exception("Map army does not match the inspected horizontal row design: "+region);
        }

        private Rect RequiredMapCommandRect(string id)
        {
            ReviewTarget target;
            if(!reviewTargets.TryGetValue(id,out target))throw new Exception("Map command region was not visibly drawn: "+id);
            return MapHudReviewRect(target);
        }

        private static bool MapCommandContains(Rect outer,Rect inner)
        { return inner.width>0&&inner.height>0&&inner.xMin>=outer.xMin-.1f&&inner.yMin>=outer.yMin-.1f&&inner.xMax<=outer.xMax+.1f&&inner.yMax<=outer.yMax+.1f; }

        private static Rect MapHudReviewRect(ReviewTarget target)
        { return new Rect(target.x - target.w / 2, target.y - target.h / 2, target.w, target.h); }

        private void AssertMinimapAtlas()
        {
            ReviewTarget areaTarget,homeTarget;
            if(!reviewTargets.TryGetValue("minimap-area",out areaTarget)||!reviewTargets.TryGetValue("map-home",out homeTarget))
                throw new Exception("Atlas did not draw its navigation controls.");
            Rect area=MapHudReviewRect(areaTarget),home=MapHudReviewRect(homeTarget);
            if(area.Overlaps(home)||area.xMin<uiPixels.xMin||area.yMin<uiPixels.yMin||area.xMax>uiPixels.xMax||area.yMax>uiPixels.yMax
                ||home.width/scale<43.99f||home.height/scale<43.99f||!map.IsBlocked(area.center))
                throw new Exception("Atlas is clipped, overlaps home navigation or leaks terrain input.");
            if(minimapTexture==null||minimapTexture.width!=MinimapAtlasRaster.Size||minimapTexture.height!=MinimapAtlasRaster.Size
                ||!ReferenceEquals(minimapWorld,world)||minimapViewport.Count<3)
                throw new Exception("Atlas does not show the current world and a projected camera footprint.");
            float twiceArea=0;
            for(int i=0;i<minimapViewport.Count;i++)
            {
                var p=minimapViewport[i];var next=minimapViewport[(i+1)%minimapViewport.Count];
                if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y)
                    ||p.x<0||p.y<0||p.x>1||p.y>1)
                    throw new Exception("Atlas viewport escaped the map bounds.");
                twiceArea+=p.x*next.y-next.x*p.y;
            }
            if(Mathf.Abs(twiceArea)<.0001f)throw new Exception("Atlas viewport collapsed into a focus marker.");
            AssertMinimapDesign();
        }

        private void AssertMinimapDesign()
        {
            if(!atlasTextFits||!AtlasDesignReady)
                throw new Exception("Atlas export component or actual-font text fit is missing.");
            // Independent measurements from the inspected design, not a second
            // call to the production layout that could repeat the same mistake.
            string[] ids={"minimap-panel","minimap-title","map-home","map-home-art","map-home-caption","minimap-area","minimap-legend"};
            Rect[] expected={new Rect(width-302,90,282,204),new Rect(width-92,90,72,54),
                new Rect(width-86,154,60,60),new Rect(width-66,160,20,28),new Rect(width-86,191,60,20),
                new Rect(width-302,90,204,204),new Rect(width-92,224,72,70)};
            for(int i=0;i<ids.Length;i++)
            {
                Rect drawn=RequiredMapCommandRect(ids[i]),design=GuiRectToPixels(expected[i]);
                if(Mathf.Abs(drawn.x-design.x)+Mathf.Abs(drawn.y-design.y)+Mathf.Abs(drawn.width-design.width)+Mathf.Abs(drawn.height-design.height)>.2f
                    ||!MapCommandContains(uiPixels,drawn)||!map.IsBlocked(drawn.center))
                    throw new Exception("Visible atlas region differs from the design or leaks terrain input: "+ids[i]);
            }
            if(RequiredMapCommandRect("map-home-art").Overlaps(RequiredMapCommandRect("map-home-caption")))
                throw new Exception("Atlas location image overlaps its live caption.");
        }

        private IEnumerator MinimapAtlasSmoke()
        {
            var previousFocus=map.focus;float previousZoom=map.zoom;int previousSelected=map.selected;
            string unchanged=JsonUtility.ToJson(world);
            try
            {
                map.selected=-1;map.focus=new Vector3(23,0,20);map.zoom=7.2f;
                yield return CaptureMapHudState("smoke-minimap-atlas.png");
                yield return CaptureMapHudState("smoke-hud-design-minimap.png");
                yield return CaptureMapHudState("smoke-minimap-sidebar.png");
                var centralFootprint=minimapViewport.ToArray();
                map.focus=new Vector3(0,0,39);map.zoom=9;
                yield return CaptureMapHudState("smoke-minimap-edge.png");
                if(centralFootprint.SequenceEqual(minimapViewport)||!minimapViewport.Any(p=>p.x<.0001f)
                    ||!minimapViewport.Any(p=>p.y<.0001f))
                    throw new Exception("Atlas footprint did not follow the camera or clip at the north-west edge.");
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Atlas navigation changed the campaign.");
                Debug.Log("MINIMAP_ATLAS_RUNTIME_OK: production terrain atlas, actual camera footprint, map-edge clipping, visible navigation targets and safe area; unchanged campaign; real click navigation checked separately.");
                Debug.Log("MINIMAP_DESIGN_RUNTIME_OK: exported sidebar and dark locator, inspected full 204-square terrain/60-unit touch geometry, actual-font fit, map-edge state and unchanged campaign; visual and real-click review remain required.");
                Debug.Log("MINIMAP_SIDEBAR_RUNTIME_OK: full 40x40 terrain beside identity, image/caption home control and vertical legend; 282x204 footprint, actual assets and fonts verified; no cropped or hidden map content.");
            }
            finally{map.focus=previousFocus;map.zoom=previousZoom;map.selected=previousSelected;}
        }

        private IEnumerator MapHudVisualSmoke()
        {
            var previousSession = session; string previousTab = tab, previousStatus = status;
            int previousSelected = map.selected; float previousSpeed = speed, previousZoom = map.zoom;
            var previousFocus = map.focus; string original = JsonUtility.ToJson(world);
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,
                "smoke-map-hud-unused")), () => DateTime.UtcNow, true);
            try
            {
                session.StartYouzhou(0, "界面压力验收用长名称主城"); map.Setup(world); tab = "领地"; speed = 0;
                yield return CaptureMapQuestCharter("smoke-map-quest-active.png");
                yield return CaptureMapHudState("smoke-hud-design-navigation.png");
                yield return CaptureMapHudState("smoke-hud-navigation-captions.png");
                yield return CaptureMapHudState("smoke-hud-recruit-seal.png");
                yield return CaptureMapHudState("smoke-hud-design-header.png");
                yield return CaptureMapHudState("smoke-hud-header-ledger.png");
                yield return CaptureMapHudState("smoke-hud-design-quest.png");
                yield return CaptureMapHudState("smoke-hud-quest-docket.png");
                yield return CaptureMapHudState("smoke-hud-quest-sleeve.png");
                yield return CaptureMapHudState("smoke-hud-design-army.png");
                yield return CaptureMapHudState("smoke-hud-design-army-rows.png");
                yield return MapCityLabelSmoke();
                Debug.Log("MAP_QUEST_DESIGN_RUNTIME_OK: exported compact task banner, separate chapter entry and action, actual-font fit, exact design dimensions, safe area and unchanged campaign; visual review and actual input remain required.");
                Debug.Log("MAP_QUEST_DOCKET_RUNTIME_OK: compatible chapter control with detached book, right-side live objective, independent progress and action; actual font and safe geometry verified, visual review and real clicks remain required.");
                Debug.Log("MAP_QUEST_SLEEVE_RUNTIME_OK: inspected 80x68 detached book, 264x68 information sleeve, 118-high blocked region, both 528x136 Inkscape textures and actual Chinese text metrics. Publisher must inspect active/ready/complete states and real image/text input.");
                Debug.Log("MAP_HUD_DESIGN_RUNTIME_OK: Inkscape-exported image panels, separate horizontal title/image targets, independent recruitment and utility group; safe area and unchanged campaign. Visual review and real clicks remain required.");
                Debug.Log("NAVIGATION_CAPTIONS_RUNTIME_OK: inspected 100x42 complete image region above 100x22 horizontal caption; five exported panels, recruitment and utilities retain all actions and safe geometry. Actual clicks and visual acceptance remain publisher work.");
                Debug.Log("MAP_RECRUIT_SEAL_RUNTIME_OK: inspected 160x140 map-only Inkscape seal, complete 60x88 portrait and separate 140x24 actual-font title; both 320x280 textures, safe-area geometry and raised-region terrain blocking verified without campaign mutation. Publisher must inspect the new capture and test actual upper portrait click, image/title return and drag cancellation.");
                yield return TerrainVisualSmoke();
                yield return MinimapAtlasSmoke();
                // Explicit isolated display stress data; these are not official balances
                // or configuration, and no player's campaign is loaded or saved here.
                world.resources = new Supplies { grass = 999999, grain = 1000000, stone = 100000000,
                    iron = 99999999, gold = 1234567890, jade = 12345 };
                status = "只读界面验收 · 长名称与大数值测试数据";
                yield return CaptureMapHudState("smoke-hud-long-values.png");
                Debug.Log("MAP_HEADER_DESIGN_RUNTIME_OK: exported translucent ledger, grouped resources, drawn text fits actual font, safe camera extension and blocked header input; long-name/large-quantity fixture unchanged. Visual and real-click review remain required.");
                Debug.Log("MAP_HEADER_LEDGER_RUNTIME_OK: exact exported identity ribbon and contiguous production ledger, full city crest, six aligned inventories and actual-font fit. Real crest/name/stock clicks and visual comparison remain publisher checks.");
                // Real, already-recruitable identities only. Isolated display data
                // exercises name length and two-digit levels, never the live pool.
                var longNames=Catalog.Generals.Where(g=>g.s1&&g.recruitable).OrderByDescending(g=>g.name.Length).Take(3).ToArray();
                for(int i=0;i<longNames.Length;i++)
                {
                    var card=world.cards.Find(c=>c.uid==world.formation[i]);
                    card.definition=longNames[i].id;card.level=card.LevelCap;
                }
                status = "只读界面验收 · 真实长姓名与等级底座";
                yield return CaptureMapHudState("smoke-hud-command-cards.png");
                world.formation.Clear();
                world.troops = 0;
                status = "只读界面验收 · 空编队测试数据";
                yield return CaptureMapHudState("smoke-hud-empty-formation.png");
                Debug.Log("MAP_ARMY_DESIGN_RUNTIME_OK: exported horizontal army rows, actual-font fit, independent exact header/row/status/action geometry, safe area, genuine long-name and empty-formation fixtures; unchanged campaign. Visual alpha/portrait review and actual input remain required.");
                Debug.Log("MAP_ARMY_ROWS_RUNTIME_OK: inspected 64-high rows, complete 44x62 portrait containers, horizontal names and separate identities, blocked four-unit gaps and safe dynamic entries; actual image/text/gap input remains publisher work.");
                Debug.Log("MAP_HUD_VISUAL_RUNTIME_OK: real drawn header/resource/army/navigation targets fit safe area, do not overlap and block terrain; long-name/large-balance and empty-formation isolated states; no hidden card targets or campaign effects; actual clicks checked separately.");
                Debug.Log("MAP_COMMAND_VISUAL_RUNTIME_OK: illustrated entry art and captions, complete portrait regions with separate name strips and level/slot bases, 44-unit foot actions and real long-name/empty formation states; unchanged campaign; visual approval and actual clicks remain separate.");
            }
            finally
            {
                session = previousSession; map.Setup(world); map.selected = previousSelected;
                map.focus = previousFocus; map.zoom = previousZoom; speed = previousSpeed;
                tab = previousTab; status = previousStatus;
                if (JsonUtility.ToJson(world) != original) throw new Exception("Map HUD fixture modified the original campaign.");
            }
        }

        private IEnumerator TerrainVisualSmoke()
        {
            Vector3 previousFocus=map.focus; float previousZoom=map.zoom; int previousSelected=map.selected;
            string unchanged=JsonUtility.ToJson(world);
            try
            {
                map.selected=-1; map.CenterHome(); map.zoom=2.7f;
                yield return CaptureMapHudState("smoke-terrain-close.png");
                var river=world.lands.First(t=>t.bridge);
                map.focus=new Vector3(river.x,0,river.z); map.zoom=5.4f;
                yield return CaptureMapHudState("smoke-terrain-river.png");
                map.zoom=2.8f;
                yield return CaptureMapHudState("smoke-river-crossing-close.png");
                map.AssertYouzhouRiverGeometry(true);
                foreach(var land in world.lands.Where(t=>t.bridge||t.kind==LandKind.Water&&Math.Abs(t.z-river.z)<3))
                {
                    var pixel=map.viewCamera.WorldToScreenPoint(map.SurfacePoint(land));int picked;
                    if(map.viewCamera.pixelRect.Contains(pixel)&&(!map.TryPickLand(pixel,out picked)||picked!=land.id))
                        throw new Exception("River bank rendering changed the actual water/bridge pick cell.");
                }
                var bend=world.lands.First(t=>t.kind==LandKind.Water&&t.z>10&&t.z<25
                    &&world.TryGetLand(t.x,t.z+1)!=null&&!YouzhouRiverLayout.IsWater(world.TryGetLand(t.x,t.z+1)));
                map.focus=new Vector3(bend.x,0,bend.z);map.zoom=3.8f;
                yield return CaptureMapHudState("smoke-river-bank-bend.png");
                map.focus=new Vector3(20,0,20); map.zoom=14;
                yield return CaptureMapHudState("smoke-terrain-wide.png");
                if(JsonUtility.ToJson(world)!=unchanged)throw new Exception("Terrain rendering changed the campaign.");
                Debug.Log("MAP_TERRAIN_RUNTIME_OK: actual resource mesh positions/UVs, feather coordinates, supported shader, near/river/wide views and unchanged World; image quality and real picking require publication review.");
                Debug.Log("RIVER_BANKS_RUNTIME_OK: actual uploaded map bank distances, raised bridge geometry and overlay order, crossing/bend/near/wide views and unchanged water/bridge picking; image quality and real input require publication review.");
            }
            finally { map.focus=previousFocus;map.zoom=previousZoom;map.selected=previousSelected; }
        }
    }
}
