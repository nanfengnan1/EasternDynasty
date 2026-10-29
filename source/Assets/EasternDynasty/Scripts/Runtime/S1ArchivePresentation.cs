using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private GUIStyle archiveName, archiveSmall, archiveStars, archiveFaction, archiveSkill;

        private List<GeneralDefinition> ArchiveDefinitions()
        {
            string query = s1Search.Trim();
            return world.S1Pool(s1Faction, s1Archives).Where(g => query.Length == 0 ||
                g.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                g.id.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        private void ResetArchiveSelection()
        { s1Scroll = Vector2.zero; s1DetailsScroll = Vector2.zero; s1Selected = ""; }

        private void RecordArchiveTarget(string id, Rect rect)
        {
            // A modal still paints its disabled background; it must not expose
            // that occluded archive as an actionable review target.
            if (GUI.enabled) RecordReviewTarget(id, rect);
        }

        private void InitArchiveStyles()
        {
            if (archiveName != null) return;
            archiveName = new GUIStyle(cardName) { alignment = TextAnchor.MiddleCenter, fontSize = 24, wordWrap = false };
            archiveSmall = new GUIStyle(small) { fontSize = 15, wordWrap = false, alignment = TextAnchor.MiddleLeft };
            archiveSmall.normal.textColor = C("C6BFAE");
            archiveStars = new GUIStyle(archiveSmall) { alignment = TextAnchor.MiddleRight, fontSize = 17 };
            archiveStars.normal.textColor = Gold;
            archiveFaction = new GUIStyle(centered) { fontSize = 22 };
            archiveSkill = new GUIStyle(archiveSmall) { fontSize = 16 };
            archiveSkill.normal.textColor = Paper;
        }

        private void S1Archive()
        {
            InitArchiveStyles(); InitArchiveDesign(); archiveDesignNodes.Clear(); archiveDesignTextFits=true;
            Rect content = new Rect(32, 96, width - 64, height - 214);
            RectFill(content, C("181D1D"));
            RectFill(new Rect(content.x, content.y, content.width, 86), C("252B29"));
            DrawCardSeal(new Rect(content.x + 16, content.y + 18, 42, 42), "将", Gold);
            Label(content.x + 74, content.y + 10, 240, 40, "武将图鉴", heading);
            Label(content.x + 76, content.y + 50, 290, 25, "S1 幽州  /  立绘与战法资料", archiveSmall);

            Rect back = new Rect(content.xMax - 176, content.y + 16, 160, 54);
            RecordArchiveTarget("library-back", back);
            if (Button(back, "返回招募")) tab = "招募";
            Rect scope = new Rect(back.x - 230, back.y, 214, 54);
            RecordArchiveTarget("archive-scope", scope);
            if (Button(scope, s1Archives ? "全部资料 · 含历史" : "当前试玩可招募", !s1Archives))
            { s1Archives = !s1Archives; ResetArchiveSelection(); }
            Rect clear = new Rect(scope.x - 66, back.y, 54, 54);
            Rect searchRect = new Rect(clear.x - 266, back.y, 254, 54);
            RecordArchiveTarget("archive-search", searchRect);
            GUI.SetNextControlName("archive-search-field");
            string search = GUI.TextField(searchRect, s1Search, 24);
            if (search != s1Search) { s1Search = search; ResetArchiveSelection(); }
            if (s1Search.Length == 0) Label(searchRect.x + 12, searchRect.y + 14, searchRect.width - 24, 27, "搜索姓名 / 卡片 ID", archiveSmall);
            RecordArchiveTarget("archive-search-clear", clear);
            if (Button(clear, "清除", false, s1Search.Length > 0))
            { s1Search = ""; ResetArchiveSelection(); GUI.FocusControl(null); }

            Rect rail = new Rect(content.x + 12, content.y + 100, 92, content.height - 112);
            RectFill(rail, C("222725"));
            Label(rail.x, rail.y + 8, rail.width, 28, "阵营", centered);
            float factionHeight = Mathf.Min(60, (rail.height - 48) / Catalog.S1Factions.Length);
            for (int i = 0; i < Catalog.S1Factions.Length; i++)
            {
                string faction = Catalog.S1Factions[i];
                Rect item = new Rect(rail.x + 4, rail.y + 44 + i * factionHeight, rail.width - 8, factionHeight - 4);
                bool selected = s1Faction == faction;
                if (selected || (GUI.enabled && item.Contains(Event.current.mousePosition)))
                    RectFill(item, selected ? C("494536") : C("303630"));
                Color accent = faction == "全部" ? Gold : FactionColor(faction);
                RectFill(new Rect(item.x, item.y + 8, selected ? 3 : 1, item.height - 16), accent);
                GUI.Label(item, faction, archiveFaction);
                RecordArchiveTarget("faction-" + faction, item);
                if (faction == "魏") RecordArchiveTarget("archive-wei", item);
                if (GUI.Button(item, GUIContent.none, GUIStyle.none))
                { s1Faction = faction; ResetArchiveSelection(); poolScroll = Vector2.zero; GUI.FocusControl(null); }
            }

            // Filtering can change during this IMGUI event. Use the same result for
            // the count, displayed cards, scrolling and their click targets.
            var list = ArchiveDefinitions();
            float gridX = rail.xMax + 20, gridWidth = content.xMax - gridX - 12;
            Label(gridX, content.y + 96, gridWidth - 310, 28,
                s1Faction + "  ·  " + list.Count + " 张" + (s1Archives ? "资料卡" : "试玩招募卡"), archiveSmall);
            Label(content.xMax - 306, content.y + 96, 290, 28, "点选立绘查看详情  /  向下浏览", archiveSmall);
            Rect area = new Rect(gridX, content.y + 134, gridWidth, content.height - 146);
            RecordArchiveTarget("archive-scroll", area);
            float innerWidth = area.width - 24, gap = ArchiveCardLayout.Gap;
            int columns = ArchiveCardLayout.Columns(area.width,area.height);
            float cardWidth = ArchiveCardLayout.Width(area.width,area.height);
            float cardHeight = ArchiveCardLayout.Height(area.height);
            archiveDesignColumns=columns; archiveDesignCardHeight=cardHeight;
            float left = ArchiveCardLayout.Left(area.width,area.height);
            float rowHeight = cardHeight + gap;
            float scrollHeight = ArchiveCardLayout.ContentHeight(area.height,list.Count,columns);
            s1Scroll = BeginContentScroll(area, s1Scroll, new Rect(0, 0, innerWidth, scrollHeight));
            for (int i = 0; i < list.Count; i++)
            {
                GeneralDefinition g = list[i];
                Rect card = new Rect(left + i % columns * (cardWidth + gap), i / columns * rowHeight, cardWidth, cardHeight);
                if (card.yMax < s1Scroll.y || card.y > s1Scroll.y + area.height)
                { GUI.Button(card, GUIContent.none, GUIStyle.none); continue; }
                Rect visible = new Rect(area.x + card.x, area.y + card.y - s1Scroll.y, card.width, card.height);
                if (visible.y >= area.y && visible.yMax <= area.yMax)
                {
                    RecordArchiveTarget("archive-card-" + g.id, visible); RecordArchiveDesignNode(visible,g);
                    if (i == 0) RecordArchiveTarget("card-first", visible);
                }
                if (DrawArchiveCard(card, g, s1Selected == g.id))
                { s1Selected = g.id; GUI.FocusControl(null); ShowCardDetails(g.id); }
            }
            GUI.EndScrollView(GUI.enabled);
            if (list.Count == 0)
            {
                Rect empty = new Rect(area.center.x - 250, area.y + 48, 500, 220);
                DrawCardSeal(new Rect(empty.center.x - 28, empty.y, 56, 56), "将", Gold);
                Label(empty.x, empty.y + 76, empty.width, 38, "未找到匹配的武将", centered);
                Label(empty.x, empty.y + 122, empty.width, 52, "清除搜索，或切换阵营和资料范围后继续浏览。", centered);
            }
        }

        private bool DrawArchiveCard(Rect r, GeneralDefinition g, bool selected)
        { return DrawArchiveDesignCard(r,g,selected); }
    }
}
