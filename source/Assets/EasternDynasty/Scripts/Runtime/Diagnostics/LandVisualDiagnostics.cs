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
        private string landCommandTextFailure = "";
        private sealed partial class ReviewState
        {
            public bool landPanelVisible, landCommandTextFits;
            public string landPanelStyle, landPanelState, landPanelKind;
            public string landCommandTextFailure;
            public int landPanelLandId;
            public float landTouchMinimum, landTileX, landTileY;
            public bool landHasTerrainPatch;
            public float landMeadowX, landMeadowY;
        }

        private void FillLandVisualReview(ReviewState review)
        {
            // Geometry is supplied only by the visible renderer. The map position is
            // an observed world projection, never a synthetic clickable UI target.
            review.landPanelVisible = world.IsYouzhou && tab == "领地" && !BlocksMapInput
                && map.selected >= 0 && reviewTargets.ContainsKey("land-panel");
            review.landPanelStyle = review.landPanelVisible ? LandSelectionLayout.Style : "";
            review.landPanelLandId = review.landPanelVisible ? map.selected : -1;
            review.landPanelState = review.landPanelVisible ? LandVisualState(world.lands[map.selected]) : "";
            review.landPanelKind = review.landPanelVisible ? world.lands[map.selected].kind.ToString() : "";
            review.landTouchMinimum = 44 * scale;
            review.landCommandTextFits = review.landPanelVisible && landCommandTextFits;
            review.landCommandTextFailure = review.landPanelVisible ? landCommandTextFailure : "";
            if (review.landPanelVisible)
            {
                var pixel = map.viewCamera.WorldToScreenPoint(map.SurfacePoint(world.lands[map.selected]));
                review.landTileX = pixel.x; review.landTileY = Screen.height - pixel.y;
                YouzhouTerrainLayout.Patch patch;
                var land=world.lands[map.selected];
                review.landHasTerrainPatch=YouzhouTerrainLayout.TryPatch(world,land,out patch);
                if(review.landHasTerrainPatch)
                {
                    // Observe the exposed meadow inside this actual cell. It is
                    // not a control/target and must still pick the same land.
                    var meadow=map.viewCamera.WorldToScreenPoint(new Vector3(YouzhouTerrainLayout.MeadowX(patch,land.x),0,land.z));
                    review.landMeadowX=meadow.x;review.landMeadowY=Screen.height-meadow.y;
                }
            }
        }

        private string LandVisualState(Land land)
        {
            return world.IsCityDistrict(land.id) ? "city" : !land.Walkable ? "blocked"
                : land.owned ? world.order == null ? "owned" : "busy" : "neutral";
        }

        private static bool IsLandBackgroundTarget(string id)
        {
            return id == "land-panel" || id == "land-command-surface" || id == "land-order-information" || id == "land-art" || id == "land-summary" || id == "land-actions"
                || id == "land-art-image" || id == "land-art-identity" || id == "land-fact-defense" || id == "land-fact-yield"
                || id == "land-close" || id == "map-close" || id == "land-enter-city" || id == "land-training"
                || id == "army-attack" || id == "army-garrison" || id == "army-farm"
                || id == "land-enter-city-icon" || id == "land-training-icon" || id == "army-attack-icon"
                || id == "army-garrison-icon" || id == "army-farm-icon" || id == "army-recall-icon"
                || id == "land-enter-city-label" || id == "land-training-label" || id == "army-attack-label"
                || id == "army-garrison-label" || id == "army-farm-label" || id == "army-recall-label";
        }

        private bool HasLandRecallTarget()
        {
            // army-recall is shared with the left army rail. Only a rectangle that
            // differs from that rail's explicit alias belongs to the land panel.
            ReviewTarget recall, hud;
            return reviewTargets.TryGetValue("army-recall", out recall)
                && (!reviewTargets.TryGetValue("hud-army-recall", out hud)
                    || MapHudReviewRect(recall) != MapHudReviewRect(hud));
        }

        private static bool LandRectContains(Rect outer, Rect inner)
        {
            return inner.width > 0 && inner.height > 0 && inner.xMin >= outer.xMin - .1f
                && inner.yMin >= outer.yMin - .1f && inner.xMax <= outer.xMax + .1f && inner.yMax <= outer.yMax + .1f;
        }

        private bool LandMatchesDesignRect(Rect actual, Rect parent, float x, float y, float w, float h)
        {
            return Mathf.Abs(actual.x - parent.x - x * scale) < .1f
                && Mathf.Abs(actual.y - parent.y - y * scale) < .1f
                && Mathf.Abs(actual.width - w * scale) < .1f && Mathf.Abs(actual.height - h * scale) < .1f;
        }

        private LandSelectionLayout.Placement ObservedLandPlacement()
        {
            var projected = map.viewCamera.WorldToScreenPoint(map.SurfacePoint(world.lands[map.selected]));
            var point = ScreenPointToGui(projected);
            return LandSelectionLayout.Place(width, height, point.x, point.y,
                projected.z > 0 && point.x >= 0 && point.x <= width && point.y >= 0 && point.y <= height, LandVisualState(world.lands[map.selected]));
        }

        private void AssertLandVisualTargets()
        {
            if (!world.IsYouzhou || tab != "领地" || BlocksMapInput || map.selected < 0)
                throw new Exception("Land panel verification requires a visible S1 selection.");
            string state = LandVisualState(world.lands[map.selected]);
            var actions = new List<string> { "land-close" };
            if (state == "city") actions.Add("land-enter-city");
            else if (state == "owned") actions.AddRange(new[] { "army-garrison", "army-farm", "land-training" });
            else if (state == "busy") actions.AddRange(new[] { "army-recall", "land-training" });
            else actions.Add("army-attack");
            var required = new[] { "land-panel", "land-command-surface", "land-order-information", "land-art", "land-summary", "land-actions", "map-close",
                "land-art-image", "land-art-identity", "land-fact-defense", "land-fact-yield" }
                .Concat(actions).Concat(actions.Where(id => id != "land-close").Select(id => id + "-icon"))
                .Concat(actions.Where(id => id != "land-close").Select(id => id + "-label")).ToArray();
            foreach (string id in required)
            {
                ReviewTarget target;
                if (!reviewTargets.TryGetValue(id, out target)) throw new Exception("Visible land target missing: " + id);
                Rect rect = MapHudReviewRect(target);
                if (!LandRectContains(uiPixels, rect)) throw new Exception("Land target exceeds safe area: " + id);
                if (!map.IsBlocked(rect.center)) throw new Exception("Land target leaks input into terrain: " + id);
            }
            if (reviewTargets.Keys.Where(IsLandBackgroundTarget).Any(id => !required.Contains(id))
                || HasLandRecallTarget() && !actions.Contains("army-recall"))
                throw new Exception("Land panel exposed an action belonging to another ownership/order state.");
            Rect panel = MapHudReviewRect(reviewTargets["land-panel"]);
            Rect art = MapHudReviewRect(reviewTargets["land-art"]);
            Rect summary = MapHudReviewRect(reviewTargets["land-summary"]);
            Rect commands = MapHudReviewRect(reviewTargets["land-actions"]);
            Rect commandSurface = MapHudReviewRect(reviewTargets["land-command-surface"]);
            var observedPlacement = ObservedLandPlacement();
            Rect expectedFacts = GuiRectToPixels(HudDesignRect(observedPlacement.Facts));
            Rect expectedCommands = GuiRectToPixels(HudDesignRect(observedPlacement.Commands));
            if ((panel.position - expectedFacts.position).sqrMagnitude > .02f
                || (commandSurface.position - expectedCommands.position).sqrMagnitude > .02f)
                throw new Exception("Land surfaces must follow the actual selected tile projection or the specified safe edge fallback.");
            if (!LandRectContains(panel, art) || !LandRectContains(panel, summary) || !LandRectContains(commandSurface, commands)
                || panel.Overlaps(commandSurface) || art.Overlaps(summary) || art.Overlaps(commands) || summary.Overlaps(commands))
                throw new Exception("Land facts and commands must remain separate visible surfaces with independent input blocking.");
            if (Mathf.Abs(panel.width - 280 * scale) > .1f || Mathf.Abs(panel.height - 228 * scale) > .1f
                || Mathf.Abs(commandSurface.width - 240 * scale) > .1f || Mathf.Abs(commandSurface.height - LandCommandMenuLayout.Height(state) * scale) > .1f
                || Mathf.Abs(art.width - 256 * scale) > .1f || Mathf.Abs(art.height - 84 * scale) > .1f
                || Mathf.Abs(summary.width - 248 * scale) > .1f || Mathf.Abs(summary.height - 60 * scale) > .1f
                || Mathf.Abs(commands.width - 208 * scale) > .1f || Mathf.Abs(commands.height - (LandCommandMenuLayout.Height(state) - 24) * scale) > .1f)
                throw new Exception("Land facts and commands must retain the inspected flanking composition dimensions.");
            if (landFactSurface == null || landFactSurface.width != 560 || landFactSurface.height != 456
                || !LandMatchesDesignRect(art, panel, 12, 64, 256, 84)
                || !LandMatchesDesignRect(summary, panel, 16, 156, 248, 60)
                || !LandMatchesDesignRect(MapHudReviewRect(reviewTargets["land-close"]), panel, 224, 12, 44, 44))
                throw new Exception("Compact land facts must use the actual 560x456 exported surface and inspected art, summary and close geometry.");
            Rect information = MapHudReviewRect(reviewTargets["land-order-information"]);
            if (!LandRectContains(commandSurface, information) || Mathf.Abs(information.height - LandCommandMenuLayout.InformationHeight(state) * scale) > .1f
                || landCommandSurface == null || landCommandActiveSurface == null
                || landCommandSurface.width != 416 || landCommandSurface.height != 88
                || landCommandActiveSurface.width != 416 || landCommandActiveSurface.height != 88)
                throw new Exception("Detached land commands require the actual exported circular strips and compact information surface.");
            float gapY = (Mathf.Max(panel.yMin, commandSurface.yMin) + Mathf.Min(panel.yMax, commandSurface.yMax)) * .5f;
            if (panel.xMax >= commandSurface.xMin || map.IsBlocked(new Vector2((panel.xMax + commandSurface.xMin) * .5f, gapY)))
                throw new Exception("The map gap between land facts and commands must remain free of UI input blocking.");
            if (!landCommandTextFits)
                throw new Exception("Land facts, values and command captions do not fit their actual rendered font rectangles at the minimum readable size: " + landCommandTextFailure);
            Rect image = MapHudReviewRect(reviewTargets["land-art-image"]);
            Rect identity = MapHudReviewRect(reviewTargets["land-art-identity"]);
            Rect defense = MapHudReviewRect(reviewTargets["land-fact-defense"]);
            Rect yieldFact = MapHudReviewRect(reviewTargets["land-fact-yield"]);
            if (!LandRectContains(art, image) || !LandRectContains(art, identity) || image.Overlaps(identity)
                || image.xMax > identity.xMin || !LandMatchesDesignRect(image, panel, 12, 68, 156, 78)
                || !LandMatchesDesignRect(identity, panel, 178, 72, 90, 70))
                throw new Exception("Visible terrain art and its vertical identity must be separate and uncropped.");
            if (!LandRectContains(summary, defense) || !LandRectContains(summary, yieldFact) || defense.Overlaps(yieldFact)
                || defense.yMax > yieldFact.yMin || !LandMatchesDesignRect(defense, panel, 16, 156, 248, 28)
                || !LandMatchesDesignRect(yieldFact, panel, 16, 188, 248, 28))
                throw new Exception("Defense and yield must occupy two complete aligned factual rows with a four-unit gap.");
            for (int i = 0; i < actions.Count; i++)
            {
                Rect r = MapHudReviewRect(reviewTargets[actions[i]]);
                if (r.width < 44 * scale - .1f || r.height < 44 * scale - .1f || !LandRectContains(actions[i] == "land-close" ? panel : commandSurface, r)
                    || r.Overlaps(art) || r.Overlaps(summary)
                    || actions[i] != "land-close" && !LandRectContains(commands, r))
                    throw new Exception("Land action is too small, clipped or covers display content: " + actions[i]);
                for (int j = 0; j < i; j++)
                    if (r.Overlaps(MapHudReviewRect(reviewTargets[actions[j]])))
                        throw new Exception("Land actions overlap: " + actions[i] + " / " + actions[j]);
                if (actions[i] != "land-close")
                {
                    Rect icon = MapHudReviewRect(reviewTargets[actions[i] + "-icon"]);
                    if (!LandRectContains(r, icon) || icon.width < 34 * scale - .1f || icon.height < 34 * scale - .1f)
                        throw new Exception("Land command icon must be actually drawn inside its owning button: " + actions[i]);
                    Rect caption = MapHudReviewRect(reviewTargets[actions[i] + "-label"]);
                    float expectedWidth = 208;
                    float expectedTop = LandCommandMenuLayout.CommandTop(state, actions[i]) - 12;
                    float inset = 5;
                    if (r.yMin < information.yMax + 12 * scale - .1f)
                        throw new Exception("Detached command must not overlap the factual note: " + actions[i]);
                    if (Mathf.Abs(r.width - expectedWidth * scale) > .1f || Mathf.Abs(r.height - 44 * scale) > .1f
                        || Mathf.Abs(r.y - commands.y - expectedTop * scale) > .1f
                        || Mathf.Abs(icon.x - r.x - inset * scale) > .1f || Mathf.Abs(icon.center.y - r.center.y) > .1f
                        || Mathf.Abs(icon.width - 34 * scale) > .1f || Mathf.Abs(icon.height - 34 * scale) > .1f)
                        throw new Exception("Land commands must retain the inspected 44-high row and centered icon: " + actions[i]);
                    if (!LandRectContains(r, caption) || caption.Overlaps(icon) || icon.xMax > caption.xMin
                        || Mathf.Abs(caption.x - r.x - 48 * scale) > .1f
                        || Mathf.Abs(caption.y - r.y - 4 * scale) > .1f
                        || Mathf.Abs(caption.width - 156 * scale) > .1f
                        || Mathf.Abs(caption.height - 36 * scale) > .1f)
                        throw new Exception("Actual land command label must be readable beside its icon inside the real button: " + actions[i]);
                }
            }
            if (MapHudReviewRect(reviewTargets["map-close"]) != MapHudReviewRect(reviewTargets["land-close"]))
                throw new Exception("Land close aliases no longer identify the same visible control.");
        }

        private IEnumerator CaptureLandVisualState(string filename)
        {
            bool previousReview = reviewMode; float previousSpeed = speed;
            reviewMode = true; speed = 0; string unchanged = JsonUtility.ToJson(world);
            try
            {
                for (int i = 0; i < 5; i++) yield return null;
                // Compare the actually drawn targets with the camera that drew
                // them. CaptureState resumes after the next Update, when edge
                // scrolling can already have moved the camera but OnGUI has not
                // yet refreshed these targets. Keep strict geometry validation
                // and the screenshot together at this rendered frame boundary.
                yield return new WaitForEndOfFrame();
                try { AssertLandVisualTargets(); }
                catch (Exception failure)
                {
                    throw new Exception("Land visual fixture failed: capture=" + (filename ?? "assert-only")
                        + "; state=" + LandVisualState(world.lands[map.selected]) + "; land=" + map.selected
                        + "; mobile=" + mobileMode + "; " + failure.Message, failure);
                }
                if (JsonUtility.ToJson(world) != unchanged) throw new Exception("Land panel rendering changed the campaign: " + filename);
                if (filename != null)
                    ScreenCapture.CaptureScreenshot(Path.Combine(Application.persistentDataPath, filename));
                yield return null;
            }
            finally
            {
                reviewMode = previousReview; speed = previousSpeed;
                if (!reviewMode) reviewTargets.Clear();
            }
        }

        private IEnumerator LandVisualSmoke()
        {
            var previousSession = session; string previousTab = tab, previousStatus = status;
            int previousSelected = map.selected; float previousSpeed = speed, previousZoom = map.zoom;
            var previousFocus = map.focus; bool previousReview = reviewMode;
            var previousConfirmation = confirmation; string previousConfirmText = confirmText;
            string previousCurrentBattle = currentBattleId, previousPendingBattle = pendingBattleWatchId;
            string previousDismissedBattle = dismissedBattleWatchId;
            string original = JsonUtility.ToJson(world);
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath,
                "smoke-land-panel-unused")), () => DateTime.UtcNow, true);
            try
            {
                session.StartYouzhou(0, "土地面板只读验收"); map.Setup(world); tab = "领地"; speed = 0; reviewMode = true;
                confirmation = null; currentBattleId = pendingBattleWatchId = dismissedBattleWatchId = "";
                map.CenterHome();
                var target = world.lands.First(t => world.AttackProblem(t.id).Length == 0);
                map.selected = target.id;
                // Capture real neutral defenders and yield in the compact facts
                // design; every prior state, modal and World assertion remains.
                yield return CaptureLandVisualState("smoke-land-facts.png");
                foreach (LandKind resource in new[] { LandKind.Grain, LandKind.Pasture, LandKind.Stone, LandKind.Iron })
                {
                    var resourceLand = world.lands.First(t => t.kind == resource && !world.IsCityDistrict(t.id));
                    map.selected = resourceLand.id;
                    yield return CaptureLandVisualState("smoke-land-" + resource.ToString().ToLowerInvariant() + ".png");
                }
                map.selected = target.id;
                yield return CaptureLandVisualState(null);
                string unchanged = JsonUtility.ToJson(world);
                confirmation = () => { }; confirmText = "只读土地面板遮挡验收";
                for (int i = 0; i < 5; i++) yield return null;
                if (!BlocksMapInput || reviewTargets.Keys.Any(IsLandBackgroundTarget) || HasLandRecallTarget() || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Land confirmation exposes background targets or changes the campaign.");
                confirmation = null;
                world.March(target.id);
                if (world.order == null) throw new Exception("Land fixture failed to issue a real capture order.");
                world.Tick(300);
                if (!target.owned || world.order != null || world.reports.Count == 0)
                    throw new Exception("Land fixture did not capture territory through actual combat and return.");
                map.RefreshOwnership();
                yield return CaptureLandVisualState("smoke-land-owned.png");
                yield return CaptureLandVisualState("smoke-land-design.png");
                yield return CaptureLandVisualState("smoke-land-orders.png");
                var ownedFocus = map.focus;
                map.focus = new Vector3(target.x, 0, target.z);
                for (int i = 0; i < 5; i++) yield return null;
                var desiredPixel = GuiPointToPixels(new Vector2((718 + width - 332) * .5f, height * .5f));
                var targetSurface = map.SurfacePoint(target);
                Ray centerRay = map.viewCamera.ScreenPointToRay(new Vector3(desiredPixel.x, Screen.height - desiredPixel.y, 0));
                float distance;
                if (!new Plane(Vector3.up, targetSurface).Raycast(centerRay, out distance))
                    throw new Exception("Flanking fixture could not project the real terrain plane.");
                var offset = targetSurface - centerRay.GetPoint(distance); offset.y = 0; map.focus += offset;
                for (int i = 0; i < 5; i++) yield return null;
                if (!ObservedLandPlacement().Flanking) throw new Exception("Flanking screenshot must show a real centered target between both surfaces.");
                yield return CaptureLandVisualState("smoke-land-flanking.png");
                map.focus = ownedFocus;
                for (int i = 0; i < 5; i++) yield return null;
                world.Garrison(target.id);
                if (world.order == null || world.order.action != (int)ArmyAction.Garrison)
                    throw new Exception("Land fixture failed to start an actual marching garrison.");
                yield return CaptureLandVisualState("smoke-land-busy.png");
                unchanged = JsonUtility.ToJson(world);
                confirmation = () => { }; confirmText = "只读行军面板遮挡验收";
                for (int i = 0; i < 5; i++) yield return null;
                if (!BlocksMapInput || reviewTargets.Keys.Any(IsLandBackgroundTarget) || HasLandRecallTarget() || JsonUtility.ToJson(world) != unchanged)
                    throw new Exception("Busy land modal exposes panel actions or confuses the independent army rail recall alias.");
                confirmation = null;
                world.RecallArmy(); world.Tick(300);
                foreach (LandKind kind in new[] { LandKind.Mountain, LandKind.Water })
                {
                    var blocked = world.lands.First(t => t.kind == kind && !t.Walkable && !world.IsCityDistrict(t.id));
                    map.selected = blocked.id;
                    if (world.AttackProblem(blocked.id).Length == 0) throw new Exception("Blocked terrain allows an attack.");
                    yield return CaptureLandVisualState(kind == LandKind.Water ? "smoke-land-blocked.png" : null);
                }
                map.selected = world.homeId;
                yield return CaptureLandVisualState("smoke-land-city-orders.png");
                map.CenterHome(); map.selected = world.lands.First(t => t.x == 0 && t.z == 0).id;
                for (int i = 0; i < 5; i++) yield return null;
                if (ObservedLandPlacement().Flanking) throw new Exception("Edge-dock screenshot must exercise the real offscreen selection fallback.");
                yield return CaptureLandVisualState("smoke-land-edge-dock.png");
                Debug.Log("LAND_VISUAL_RUNTIME_OK: real visible illustration/summary/actions and 44-unit controls fit safe area without overlaps; neutral, actual captured owned, marching, mountain/water and city states; modal suppresses background targets; rendering preserves complete World; actual clicks checked separately.");
                Debug.Log("LAND_WARRANT_RUNTIME_OK: separate actual terrain image and identity; two factual rows; illustrated owned/busy actions; Grain/Pasture/Stone/Iron screenshots select existing real world tiles; all original state/modal/unchanged-world assertions retained.");
                Debug.Log("LAND_FLOATING_DESIGN_RUNTIME_OK: inspected independent land surfaces; 44-high actions with separate actual icon/label targets and real GUI font fit; owned design capture; neutral/owned/busy/blocked/city and modal checks preserve World. Actual image/label clicks require the publisher interaction suite.");
                Debug.Log("LAND_FLANKING_DESIGN_RUNTIME_OK: independent 280x228 facts and state-sized 240-wide commands, visible complete terrain art, 208x44 commands with measured readable captions, unblocked map gap, original ownership/order/modal/unchanged-world gates retained. Actual clicks and appearance remain publisher checks.");
                Debug.Log("LAND_DETACHED_ORDERS_RUNTIME_OK: actual 416x88 exported circle/strip skins, separate factual note, five state-specific heights, full actual labels and retained 44-unit image/text controls; original state/modal/World assertions preserved. Runtime appearance and clicks require publisher review.");
                Debug.Log("LAND_COMPACT_FACTS_RUNTIME_OK: actual 560x456 exported skin, 280x228 facts with complete 156x78 terrain art, separate 90x70 identity, exact 248x28 defense/yield rows and measured actual label/value fonts; new neutral facts capture and all original state/modal/World gates retained. Appearance and real clicks require publisher review.");
            }
            finally
            {
                session = previousSession; map.Setup(world); map.selected = previousSelected; map.focus = previousFocus; map.zoom = previousZoom;
                tab = previousTab; status = previousStatus; speed = previousSpeed; reviewMode = previousReview;
                confirmation = previousConfirmation; confirmText = previousConfirmText;
                currentBattleId = previousCurrentBattle; pendingBattleWatchId = previousPendingBattle; dismissedBattleWatchId = previousDismissedBattle;
                if (!reviewMode) reviewTargets.Clear();
                if (JsonUtility.ToJson(world) != original) throw new Exception("Land fixture modified the original campaign.");
            }
        }
    }
}
