using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private BattleReportFilter reportFilter;
        private World reportArchiveWorld;
        private BattleRecord reportArchiveSelection;
        private int reportArchiveSelectionIndex = -1;
        private float reportArchiveScrollMaximum;

        private BattleReportArchive RefreshReportArchive()
        {
            if (reportArchiveWorld != world)
            {
                reportArchiveWorld = world; reportFilter = BattleReportFilter.All;
                reportArchiveSelection = null; reportArchiveSelectionIndex = -1;
                reportScroll = battleTextScroll = Vector2.zero;
            }
            // Preserve the actual selected battle when a newly settled report is inserted at index zero.
            if (reportArchiveSelection != null && selectedReport == reportArchiveSelectionIndex)
            {
                int currentIndex = world.reports.IndexOf(reportArchiveSelection);
                selectedReport = currentIndex;
            }
            var archive = BattleReportArchive.Create(world.reports, reportFilter);
            selectedReport = archive.ResolveSelection(selectedReport);
            var next = selectedReport < 0 ? null : world.reports[selectedReport];
            if (next != reportArchiveSelection)
            {
                battleTextScroll = Vector2.zero;
                if (reportArchiveSelection != null && selectedReport == 0) reportScroll = Vector2.zero;
            }
            reportArchiveSelection = next; reportArchiveSelectionIndex = selectedReport;
            return archive;
        }

        private void SelectReportFilter(BattleReportFilter filter)
        {
            reportFilter = filter; reportScroll = battleTextScroll = Vector2.zero;
            RefreshReportArchive(); map.CancelPointerInput();
        }

        private void SelectArchivedReport(int sourceIndex)
        {
            selectedReport = sourceIndex;
            reportArchiveSelectionIndex = sourceIndex;
            reportArchiveSelection = sourceIndex >= 0 && sourceIndex < world.reports.Count ? world.reports[sourceIndex] : null;
            battleTextScroll = Vector2.zero;
        }

        private void RestoreArchivedReport(BattleRecord record)
        {
            if (record == null) return;
            int index = world.reports.IndexOf(record);
            if (index < 0) return;
            RefreshReportArchive();
            var archive = BattleReportArchive.Create(world.reports, reportFilter);
            if (!archive.Entries.Any(e => e.SourceIndex == index))
            { reportFilter = BattleReportFilter.All; reportScroll = Vector2.zero; }
            var previousTextScroll = battleTextScroll; bool sameRecord = reportArchiveSelection == record;
            SelectArchivedReport(index);
            if (sameRecord) battleTextScroll = previousTextScroll;
        }

        private void DrawBattleReports()
        {
            var page = new Rect(24, 94, width - 48, height - 198); Block(page);
            var archive = RefreshReportArchive();
            Label(page.x, page.y, 300, 36, "战地军报", heading);
            Label(page.x + 310, page.y + 7, page.width - 310, 28,
                "最近 " + archive.AllCount + " 场  ·  选择记录后观看过程、核对阵容与战果", small);
            string[] names = { "全部", "已占领", "未占领", "历史文字" };
            int[] counts = { archive.AllCount, archive.CapturedCount, archive.NotCapturedCount, archive.LegacyCount };
            float filterWidth = Mathf.Min(220, (page.width - 36) / 4);
            for (int i = 0; i < names.Length; i++)
            {
                var control = new Rect(page.x + i * (filterWidth + 12), page.y + 48, filterWidth, 40);
                RecordReviewTarget("report-filter-" + i, control);
                if (Button(control, names[i] + "  " + counts[i], (int)reportFilter == i))
                { SelectReportFilter((BattleReportFilter)i); return; }
            }
            float listWidth = Mathf.Clamp(page.width * .24f, 292, 360);
            var list = new Rect(page.x, page.y + 106, listWidth, page.height - 106);
            var detail = new Rect(list.xMax + 16, list.y, page.width - list.width - 16, list.height);
            Frame(list, Ink); Frame(detail, Ink);
            Label(list.x + 14, list.y + 8, list.width - 28, 28, names[(int)reportFilter] + " · " + archive.Entries.Count + " 份记录", body);
            var viewport = new Rect(list.x + 8, list.y + 46, list.width - 16, list.height - 54);
            RecordReviewTarget("report-list", viewport);
            const float stride = 110;
            reportArchiveScrollMaximum = Mathf.Max(0, archive.Entries.Count * stride - viewport.height);
            reportScroll.y = Mathf.Clamp(reportScroll.y, 0, reportArchiveScrollMaximum);
            reportScroll = BeginContentScroll(viewport, reportScroll, new Rect(0, 0, viewport.width - 22, Mathf.Max(viewport.height, archive.Entries.Count * stride)));
            foreach (var entry in archive.Entries.Select((item, index) => new { item, index }))
            {
                var row = new Rect(0, entry.index * stride, viewport.width - 24, stride - 8);
                var item = entry.item;
                bool chosen = selectedReport == item.SourceIndex;
                if (row.y >= reportScroll.y && row.yMax <= reportScroll.y + viewport.height)
                    RecordReviewTarget("report-row-" + item.SourceIndex, new Rect(viewport.x, viewport.y + row.y - reportScroll.y, row.width, row.height));
                Frame(row, chosen ? Red : Panel);
                if (chosen) RectFill(new Rect(row.x, row.yMax - 2, row.width, 2), Gold);
                Color accent = item.HasReplay ? (item.Captured ? Green : Red) : Gold;
                RectFill(new Rect(row.x + 1, row.y + 1, 4, row.height - 2), accent);
                Label(row.x + 14, row.y + 8, row.width - 28, 25,
                    item.HasReplay ? (item.Captured ? "已占领" : "未占领") + "  ·  " + item.Rounds + " 回合" : "历史文字 · 无过程记录", small);
                Label(row.x + 14, row.y + 37, row.width - 28, 26,
                    item.HasReplay ? item.TargetName + " (" + item.TargetX + "," + item.TargetZ + ")" : item.Title, body);
                Label(row.x + 14, row.y + 70, row.width - 28, 23,
                    item.HasReplay ? "我军 " + item.AttackerInitial + " → " + item.AttackerFinal + " 兵" : "保留原文，不推断胜负与奖励", tiny);
                if (ContentTileClicked(row, "report-row-" + item.SourceIndex, item.SourceIndex)) SelectArchivedReport(item.SourceIndex);
            }
            GUI.EndScrollView(GUI.enabled);
            if (selectedReport < 0)
            {
                Label(detail.x + 32, detail.y + 54, detail.width - 64, 40,
                    archive.AllCount == 0 ? "尚无军报" : "此分类暂无军报", heading);
                Label(detail.x + 32, detail.y + 112, detail.width - 64, 74,
                    archive.AllCount == 0 ? "部队抵达目标并完成结算后，记录会出现在这里。" : "选择其他分类查看已有记录。未占领表示本场未取得地块；旧文字记录单独保留。", body);
                var reset = new Rect(detail.x + 32, detail.y + 218, 190, 42);
                RecordReviewTarget("report-show-all", reset);
                if (Button(reset, "查看全部军报", true)) SelectReportFilter(BattleReportFilter.All);
                return;
            }
            var selected = archive.Entries.First(e => e.SourceIndex == selectedReport);
            DrawArchivedReportDetail(detail, world.reports[selectedReport], selected);
        }

        private void DrawArchivedReportDetail(Rect detail, BattleRecord record, BattleReportArchiveEntry entry)
        {
            Label(detail.x + 18, detail.y + 12, detail.width - 36, 32, entry.Title, heading);
            float contentTop = detail.y + 78;
            if (entry.HasReplay)
            {
                Label(detail.x + 18, detail.y + 46, detail.width - 36, 24,
                    (entry.Captured ? "已占领" : "未占领") + "  ·  (" + entry.TargetX + "," + entry.TargetZ + ")  ·  " + entry.Rounds + " 回合 · 本场冻结记录", small);
                float half = (detail.width - 48) / 2;
                DrawBattleArmy(new Rect(detail.x + 16, contentTop, half, 174), record.attacker, entry.AttackerFinal, Green, false);
                DrawBattleArmy(new Rect(detail.x + 32 + half, contentTop, half, 174), record.defender, entry.DefenderFinal, Red, false);
                float actionY = contentTop + 186, actionWidth = (detail.width - 62) / 4;
                string[] ids = { "report-replay", "report-outcome", "report-formation", "report-location" };
                string[] labels = { "观看战斗过程", "战果总览", "参战阵容", "战斗地点" };
                for (int i = 0; i < ids.Length; i++)
                {
                    var action = new Rect(detail.x + 16 + i * (actionWidth + 10), actionY, actionWidth, 42);
                    RecordReviewTarget(ids[i], action);
                    if (!Button(action, labels[i], i == 0)) continue;
                    if (i == 0) OpenBattleReplay(record);
                    else if (i == 1) { OpenBattleReplay(record); OpenBattleOutcome(); }
                    else if (i == 2) { OpenBattleReplay(record); OpenBattleFormation(0, true); }
                    else { tab = "领地"; map.selected = record.targetId; map.focus = new Vector3(record.targetX, 0, record.targetZ); map.CancelPointerInput(); }
                }
                Label(detail.x + 18, actionY + 50, detail.width - 36, 24,
                    "本场实得  金币 +" + entry.Gold + "  ·  经验 +" + entry.Experience + "   |   观看不会再次结算", small);
                contentTop = actionY + 84;
            }
            else Label(detail.x + 18, detail.y + 48, detail.width - 36, 25, "历史文字战报 · 未记录冻结阵容与顺序事件", small);
            var textArea = new Rect(detail.x + 18, contentTop, detail.width - 36, Mathf.Max(40, detail.yMax - contentTop - 16));
            RecordReviewTarget("report-text", textArea);
            float textHeight = body.CalcHeight(new GUIContent(record.text ?? ""), textArea.width - 28) + 20;
            battleTextScroll = BeginContentScroll(textArea, battleTextScroll, new Rect(0, 0, textArea.width - 22, Mathf.Max(textArea.height, textHeight)));
            Label(0, 0, textArea.width - 28, textHeight, record.text ?? "", body); GUI.EndScrollView(GUI.enabled);
        }
    }
}
