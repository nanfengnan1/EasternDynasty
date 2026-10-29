using System;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private GUIStyle landPanelTitle, landPanelValue, landPanelCaption, landPanelNote, landPanelSeal, landPanelIdentity;
        private Texture2D landDesignSurface, landFactSurface, landCommandSurface, landCommandActiveSurface;
        private bool landCommandTextFits = true;

        private void InitLandPanelStyles()
        {
            InitMapHudStyles();
            if (landPanelTitle != null) return;
            landPanelTitle = new GUIStyle(cardName) { fontSize = 28, wordWrap = false, clipping = TextClipping.Clip };
            landPanelValue = new GUIStyle(cardName) { fontSize = 22, wordWrap = false };
            landPanelCaption = new GUIStyle(small) { fontSize = 15, wordWrap = false };
            landPanelNote = new GUIStyle(small) { fontSize = 15, wordWrap = true };
            landPanelSeal = new GUIStyle(cardName) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = false };
            landPanelIdentity = new GUIStyle(landPanelCaption) { alignment = TextAnchor.MiddleCenter };
        }

        // All rectangles are in the same logical safe-area space as map hit blocking.
        // The artwork is a preview of existing project assets, never a captured UI.
        private void DrawYouzhouLandPanel(Land tile)
        {
            InitLandPanelStyles();
            landCommandTextFits = true;
            landCommandTextFailure = "";
            var projected = map.viewCamera.WorldToScreenPoint(map.SurfacePoint(tile));
            var target = ScreenPointToGui(projected);
            string menuState = LandVisualState(tile);
            var placement = LandSelectionLayout.Place(width, height, target.x, target.y,
                projected.z > 0 && target.x >= 0 && target.x <= width && target.y >= 0 && target.y <= height, menuState);
            var layout = placement.Facts;
            Rect panel = HudDesignRect(layout);
            Rect commandSurface = HudDesignRect(placement.Commands);
            Block(panel); Block(commandSurface); DrawLandDesignSurfaces(panel, commandSurface, menuState);
            RecordLandPanelTarget("land-panel", panel);
            RecordLandPanelTarget("land-command-surface", commandSurface);
            RecordLandPanelTarget("land-order-information", HudDesignRect(LandCommandMenuLayout.Information(placement.Commands, menuState)));
            bool district = world.IsCityDistrict(tile.id);
            string terrain = tile.kind == LandKind.Pasture ? "林地" : Catalog.LandName(tile.kind);
            string name = tile.home || tile.capital || tile.bridge ? tile.name : district ? "主城城区" : terrain;
            LandFittedLabel(new Rect(panel.x + 18, panel.y + 11, 194, 34), name, landPanelTitle, 17);
            LandFittedLabel(new Rect(panel.x + 18, panel.y + 47, 194, 21), world.RegionName(tile.region) + "  /  " + tile.x + ", " + tile.z, landPanelCaption, 12);
            Rect close = HudDesignRect(LandSelectionLayout.Close(layout));
            RecordLandPanelTarget("land-close", close); RecordLandPanelTarget("map-close", close);
            if (HudTextAction(close, "×")) { map.selected = -1; return; }

            Rect art = HudDesignRect(LandSelectionLayout.Art(layout));
            DrawLandPreview(HudDesignRect(LandSelectionLayout.Image(layout)), HudDesignRect(LandSelectionLayout.Identity(layout)), tile, district);
            RecordLandPanelTarget("land-art", art);

            Rect summary = HudDesignRect(LandSelectionLayout.Summary(layout));
            RecordLandPanelTarget("land-summary", summary);
            var defenseRow = LandSelectionLayout.Defense(layout);
            var yieldRow = LandSelectionLayout.Yield(layout);
            Rect defense = HudDesignRect(defenseRow);
            Rect yield = HudDesignRect(yieldRow);
            RecordLandPanelTarget("land-fact-defense", defense); RecordLandPanelTarget("land-fact-yield", yield);
            DrawCardAttributeEmblem(HudDesignRect(LandSelectionLayout.FactIcon(defenseRow)), 2, tile.owned ? Green : Gold);
            LandFittedLabel(HudDesignRect(LandSelectionLayout.FactLabel(defenseRow)), tile.owned ? "控制状态" : "守军兵力", landPanelCaption, 13);
            LandFittedLabel(HudDesignRect(LandSelectionLayout.FactValue(defenseRow, true)), tile.owned ? "已占领" : tile.Walkable ? world.Defenders(tile).ToString("N0") : "—", landPanelValue, 16);
            if (district || tile.capital || tile.bridge || !tile.Walkable)
            {
                LandFittedLabel(HudDesignRect(LandSelectionLayout.FactLabel(yieldRow)), district ? "城内设施" : tile.capital ? "占领增益" : "地形作用", landPanelCaption, 13);
                LandFittedLabel(HudDesignRect(LandSelectionLayout.FactValue(yieldRow, false)), district ? "九格城区" : tile.capital ? "全资源 +15%" : tile.bridge ? "连通两岸" : "不可通行", body, 14);
            }
            else
            {
                // This is the base land yield; city/capital bonuses belong to total income.
                LandFittedLabel(HudDesignRect(LandSelectionLayout.FactLabel(yieldRow)), "基础产量 / 时", landPanelCaption, 13);
                ResourceIcon(HudDesignRect(LandSelectionLayout.FactIcon(yieldRow)), LandResourceIcon(tile));
                LandFittedLabel(HudDesignRect(LandSelectionLayout.FactValue(yieldRow, true)), "+" + (tile.level * 180), landPanelValue, 16);
            }

            Rect actions = HudDesignRect(LandSelectionLayout.Actions(placement.Commands));
            RecordLandPanelTarget("land-actions", actions);
            if (district)
            {
                Label(actions.x + 4, actions.y + 7, 200, 52, "主城全景与设施建设\n部队、练兵和城建任务", landPanelNote);
                Rect enter = LandMenuCommand(actions, "city", "land-enter-city");
                RecordLandPanelTarget("land-enter-city", enter);
                if (LandCommandButton(enter, "进入主城", 4, "land-enter-city", true, true)) OpenCity(tile.id);
            }
            else if (tile.owned) DrawOwnedLandCommands(actions, tile);
            else DrawNeutralLandCommands(actions, tile);
        }

        private void DrawLandDesignSurfaces(Rect panel, Rect commands, string state)
        {
            if (landDesignSurface == null) landDesignSurface = GameAssets.LoadTexture("ui.youzhou.land_surface");
            if (landFactSurface == null) landFactSurface = GameAssets.LoadTexture("ui.youzhou.land_facts");
            // The inspected single information leaf owns its separators; Runtime
            // only places the exported surface and live rows. Center gap stays map.
            GUI.DrawTexture(panel, landFactSurface, ScaleMode.StretchToFill, true);
            // Only the factual note has a background; independent exported action
            // silhouettes reveal the terrain between rows, as in the reference.
            GUI.DrawTexture(new Rect(commands.x, commands.y, commands.width, LandCommandMenuLayout.InformationHeight(state)), landDesignSurface, ScaleMode.StretchToFill, true);
        }

        private static Rect LandMenuCommand(Rect content, string state, string id)
        { return HudDesignRect(LandCommandMenuLayout.Command(new MapHudNavigationLayout.Box(content.x, content.y, content.width, content.height), state, id)); }

        private void RecordLandPanelTarget(string id, Rect rect)
        {
            if (GUI.enabled && !BlocksMapInput) RecordReviewTarget(id, rect);
        }

        private static int LandResourceIcon(Land tile)
        { return tile.kind == LandKind.Pasture ? 0 : tile.kind == LandKind.Grain ? 1 : tile.kind == LandKind.Stone ? 2 : 3; }

        private void DrawLandPreview(Rect image, Rect identity, Land tile, bool district)
        {
            // Complete registered tile, without the former disc/survey diamond.
            string asset = district || tile.capital || tile.home ? "city" : tile.bridge || tile.kind == LandKind.Water ? null
                : tile.kind == LandKind.Grain ? "grain" : tile.kind == LandKind.Pasture ? "forest"
                : tile.kind == LandKind.Stone ? "stone" : tile.kind == LandKind.Iron ? "iron" : "mountain";
            if (asset != null)
            {
                var texture = GameAssets.LoadTexture("map.youzhou." + asset);
                GUI.DrawTexture(image, texture, ScaleMode.ScaleToFit, true);
            }
            else
            {
                // There is no registered river/bridge sprite. Use a labelled original
                // diagram instead of borrowing another terrain's identity.
                for (int i = 0; i < 7; i++)
                    HudLine(image, .12f, .2f + i * .09f, .88f, .33f + i * .07f, new Color(.35f, .56f, .61f, .75f));
                if (tile.bridge)
                {
                    Rect bridge = new Rect(image.center.x - 22, image.y + image.height * .17f, 44, image.height * .55f);
                    RectFill(bridge, C("8E8165")); Outline(bridge, Gold);
                    for (int i = 1; i < 8; i++) RectFill(new Rect(bridge.x + 4, bridge.y + i * bridge.height / 8, bridge.width - 8, 1), C("514B3E"));
                }
                GUI.Label(new Rect(image.x, image.yMax - 23, image.width, 21), tile.bridge ? "桥梁示意" : "水域示意", landPanelIdentity);
            }
            RecordLandPanelTarget("land-art-image", image);
            Rect seal = new Rect(identity.x, identity.y, identity.width, 28);
            LandFittedLabel(seal, tile.level + "级", landPanelSeal, 18);
            LandFittedLabel(new Rect(identity.x, identity.y + 28, identity.width, 20), district ? "主城辖区" : tile.capital ? "战略城池" : tile.bridge ? "两岸通道" : !tile.Walkable ? "山河地形" : "土地等级", landPanelIdentity, 13);
            Rect owner = new Rect(identity.x, identity.y + 48, identity.width, 22);
            LandFittedLabel(owner, tile.owned ? "己方领地" : tile.Walkable ? "中立土地" : "不可通行", landPanelIdentity, 13);
            RecordLandPanelTarget("land-art-identity", identity);
        }

        private void DrawNeutralLandCommands(Rect area, Land tile)
        {
            Label(area.x + 4, area.y + 3, 200, 24, "出征消耗", landPanelCaption);
            ResourceIcon(new Rect(area.x + 4, area.y + 34, 22, 22), 1);
            Label(area.x + 36, area.y + 34, 168, 25, "30 粮食", landPanelCaption);
            ResourceIcon(new Rect(area.x + 4, area.y + 64, 22, 22), 0);
            Label(area.x + 36, area.y + 64, 168, 25, "20 木材", landPanelCaption);
            string problem = world.AttackProblem(tile.id);
            Label(area.x + 4, area.y + 104, 200, 84, problem.Length == 0 ? "抵达后结算，可观看本场战斗过程。" : problem, landPanelNote);
            Rect attack = LandMenuCommand(area, "neutral", "army-attack");
            RecordLandPanelTarget("army-attack", attack);
            if (LandCommandButton(attack, !tile.Walkable ? "不可通行" : tile.capital ? "派兵攻城" : "派兵占领", 0, "army-attack", true, problem.Length == 0)) BeginAttack(tile.id);
        }

        private void DrawOwnedLandCommands(Rect area, Land tile)
        {
            string state = world.order == null ? "owned" : "busy";
            Rect first = LandMenuCommand(area, state, "army-garrison");
            Rect second = LandMenuCommand(area, state, world.order == null ? "army-farm" : "army-recall");
            Rect training = LandMenuCommand(area, state, "land-training");
            if (world.order != null)
            {
                Label(area.x + 4, area.y + 1, 200, 26, world.ArmyState, body);
                Label(area.x + 4, area.y + 28, 200, 56, world.order.phase == 1 ? "驻守保持至主动撤回" : "本阶段剩余 " + Math.Ceiling(world.order.remaining) + " 秒", landPanelNote);
                Rect recall = second;
                RecordLandPanelTarget("army-recall", recall);
                if (LandCommandButton(recall, "撤回部队", 3, "army-recall", false, !world.order.returning)) Act(world.RecallArmy());
            }
            else
            {
                string guard = world.TerritoryOrderProblem(tile.id, false), farm = world.TerritoryOrderProblem(tile.id, true);
                var gain = world.FarmYield(tile.id);
                LandFittedLabel(new Rect(area.x + 4, area.y, 200, 26), "屯田预计收益 · 政令 " + world.AvailableCommands + "/" + World.CommandCap, landPanelCaption, 13);
                double[] amounts = { gain.grass, gain.grain, gain.stone, gain.iron };
                for (int i = 0; i < 4; i++)
                {
                    ResourceIcon(new Rect(area.x + 2 + (i % 2) * 104, area.y + 34 + (i / 2) * 24, 20, 20), i);
                    LandFittedLabel(new Rect(area.x + 26 + (i % 2) * 104, area.y + 33 + (i / 2) * 24, 74, 23), HudQuantity(amounts[i]), landPanelCaption, 12);
                }
                RecordLandPanelTarget("army-garrison", first); RecordLandPanelTarget("army-farm", second);
                if (LandCommandButton(first, "驻守", 1, "army-garrison", false, guard.Length == 0)) Act(world.Garrison(tile.id));
                if (LandCommandButton(second, "屯田·" + World.FarmCommandCost + "令", 2, "army-farm", true, farm.Length == 0)) Act(world.Farm(tile.id));
                LandFittedLabel(new Rect(area.x, area.y + 84, 208, 20), farm.Length > 0 ? farm : guard.Length > 0 ? guard
                    : "作业 " + World.FarmSeconds + " 秒 · 完成后返城", tiny, 12);
            }
            RecordLandPanelTarget("land-training", training);
            if (LandCommandButton(training, "部队练兵", 5, "land-training", false, true)) OpenTraining(tile.id);
        }

        private void LandFittedLabel(Rect bounds, string text, GUIStyle source, int minimum)
        {
            var style = new GUIStyle(source); var content = new GUIContent(text);
            var fit = LandCommandTextLayout.Fit(bounds.width, bounds.height, style.fontSize, minimum, size => {
                style.fontSize = size; var measured = style.CalcSize(content);
                return new LandCommandTextMetrics { Width = measured.x, Height = measured.y, LineHeight = style.lineHeight };
            });
            style.fontSize = fit.FontSize; landCommandTextFits &= fit.Fits;
            if (!fit.Fits) landCommandTextFailure += "fact text=" + text + " size=" + fit.FontSize
                + " actual=" + fit.Width + "x" + fit.Height + " budget=" + bounds.width + "x" + bounds.height + "; ";
            GUI.Label(bounds, content, style);
        }

        private bool LandCommandButton(Rect r, string text, int symbol, string id, bool active, bool enabled)
        {
            bool hover = GUI.enabled && enabled && r.Contains(Event.current.mousePosition);
            if (landCommandSurface == null) landCommandSurface = GameAssets.LoadTexture("ui.youzhou.land_order");
            if (landCommandActiveSurface == null) landCommandActiveSurface = GameAssets.LoadTexture("ui.youzhou.land_order_active");
            var surfaceColor = GUI.color;
            GUI.color = !enabled ? new Color(.65f,.65f,.65f,1) : hover ? new Color(1.1f,1.1f,1.1f,1) : Color.white;
            GUI.DrawTexture(r, enabled && active ? landCommandActiveSurface : landCommandSurface, ScaleMode.StretchToFill, true);
            GUI.color = surfaceColor;
            Color ink = enabled ? Gold : C("6E7975");
            var command = new MapHudNavigationLayout.Box(r.x, r.y, r.width, r.height);
            Rect icon = HudDesignRect(LandSelectionLayout.CommandIcon(command));
            if (symbol == 0) DrawCardAttributeEmblem(icon, 0, ink);
            else if (symbol == 1) DrawCardAttributeEmblem(icon, 2, ink);
            else if (symbol == 2) ResourceIcon(icon, 1);
            else if (symbol == 3)
            {
                HudLine(icon, .82f, .23f, .28f, .23f, ink, 2);
                HudLine(icon, .28f, .23f, .08f, .45f, ink, 2);
                HudLine(icon, .08f, .45f, .28f, .66f, ink, 2);
                HudLine(icon, .28f, .45f, .8f, .45f, ink, 2);
                HudLine(icon, .8f, .45f, .8f, .9f, ink, 2);
            }
            else if (symbol == 4) DrawHudNavigationArt(icon, 5);
            else
            {
                HudLine(icon, .21f, .12f, .21f, .92f, ink, 2);
                HudLine(icon, .21f, .12f, .84f, .2f, ink, 2);
                HudLine(icon, .84f, .2f, .7f, .53f, ink, 2);
                HudLine(icon, .7f, .53f, .21f, .47f, ink, 2);
                HudLine(icon, .38f, .75f, .87f, .75f, ink, 2);
                HudLine(icon, .48f, .9f, .78f, .9f, ink, 2);
            }
            RecordLandPanelTarget(id + "-icon", icon);
            Rect caption = HudDesignRect(LandSelectionLayout.CommandLabel(command));
            var beforeColor = GUI.color; if (!enabled) GUI.color = new Color(.55f, .55f, .55f, 1);
            var captionStyle = new GUIStyle(hudCaption) { fontSize = 18, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            captionStyle.normal.textColor = enabled && active ? C("252D29") : C("E8E2CD");
            var content = new GUIContent(text);
            var fit = LandCommandTextLayout.Fit(caption.width, caption.height, 18, 14, size => {
                captionStyle.fontSize = size; var measured = captionStyle.CalcSize(content);
                return new LandCommandTextMetrics { Width = measured.x, Height = measured.y, LineHeight = captionStyle.lineHeight };
            });
            captionStyle.fontSize = fit.FontSize; landCommandTextFits &= fit.Fits;
            if (!fit.Fits) landCommandTextFailure += id + " text=" + text + " font=" + (captionStyle.font == null ? "default" : captionStyle.font.name)
                + " size=" + fit.FontSize + " actual=" + fit.Width + "x" + fit.Height + " budget=" + caption.width + "x" + caption.height + "; ";
            GUI.Label(caption, content, captionStyle); GUI.color = beforeColor;
            RecordLandPanelTarget(id + "-label", caption);
            bool before = GUI.enabled; GUI.enabled = before && enabled;
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none); GUI.enabled = before;
            return clicked;
        }
    }
}
