using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private sealed partial class ReviewState
        {
            public int reportFilter, reportFilteredCount, reportCapturedCount, reportNotCapturedCount, reportLegacyCount;
            public int reportSelectedIndex, reportSelectedRounds, reportSelectedInitial, reportSelectedFinal;
            public bool reportSelectedReplay, reportSelectedCaptured;
            public string reportSelectedBattleId;
            public int[] reportSourceIndices;
            public float reportListScrollY, reportListScrollMaximum;
        }

        private void CaptureReportArchiveReview(ReviewState review)
        {
            var archive = BattleReportArchive.Create(world.reports, reportFilter);
            review.reportFilter = (int)reportFilter; review.reportFilteredCount = archive.Entries.Count;
            review.reportCapturedCount = archive.CapturedCount; review.reportNotCapturedCount = archive.NotCapturedCount;
            review.reportLegacyCount = archive.LegacyCount;
            review.reportSourceIndices = archive.Entries.Select(e => e.SourceIndex).ToArray();
            review.reportSelectedIndex = selectedReport; review.reportListScrollY = reportScroll.y;
            review.reportListScrollMaximum = reportArchiveScrollMaximum;
            var entry = archive.Entries.FirstOrDefault(e => e.SourceIndex == selectedReport);
            review.reportSelectedReplay = entry != null && entry.HasReplay;
            review.reportSelectedCaptured = entry != null && entry.Captured;
            review.reportSelectedRounds = entry == null ? 0 : entry.Rounds;
            review.reportSelectedInitial = entry == null ? 0 : entry.AttackerInitial;
            review.reportSelectedFinal = entry == null ? 0 : entry.AttackerFinal;
            review.reportSelectedBattleId = entry == null || !entry.HasReplay ? "" : world.reports[entry.SourceIndex].battleId;
        }

        private BattleRecord PopulateReportArchiveFixture()
        {
            // Called only by isolated smoke or explicit read-only review setup. Every new
            // structured record is produced by the real resolver, never synthesized events.
            if (!transientRun || world == null || !world.IsYouzhou || world.reports.Count != 0)
                throw new InvalidOperationException("Archive fixture requires a fresh read-only Youzhou campaign.");
            for (int i = 0; i < 7; i++)
            {
                world.troops = i == 6 ? 50 : 2000;
                var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = i == 6 ? 5 : 1;
                world.March(target.id); world.Tick(world.order.remaining + .01);
                if (world.reports.Count != i + 1 || !BattleReplay.IsValid(world.reports[0]))
                    throw new Exception("Archive fixture did not create a real settlement.");
                world.Tick(1000);
            }
            var legacy = new BattleRecord { title = "旧档军报", text = "旧档只保留文字。此页不推造阵容、事件或奖励。", won = true, time = world.elapsed };
            world.reports.Add(legacy); return legacy;
        }

        private void InitializeReportArchiveReview()
        {
            if (!reviewMode || !transientRun || world == null || !world.IsYouzhou)
                throw new InvalidOperationException("Archive review is restricted to an isolated read-only S1 session.");
            PopulateReportArchiveFixture(); speed = 0; tab = "战报"; selectedReport = 0; RefreshReportArchive();
            status = "只读验收场景 · 七次实际结算及一份旧文字记录";
            Debug.Log("BATTLE_REPORT_ARCHIVE_REVIEW_READY: isolated read-only campaign; seven real settlements and one legacy text record; awaiting actual user-input checks.");
        }

        private IEnumerator CaptureReportArchive(string file)
        {
            string before = ReplayWorldHash(); bool oldReview = reviewMode; reviewMode = true;
            try
            {
                yield return CaptureState(file);
                if (tab != "战报" || !BlocksMapInput || ReplayWorldHash() != before)
                    throw new Exception("Report archive rendering changed the campaign or failed to block map input.");
                foreach (string id in new[] { "report-filter-0", "report-filter-1", "report-filter-2", "report-filter-3", "report-list" })
                {
                    if (!reviewTargets.ContainsKey(id)) throw new Exception("Archive control not actually drawn: " + id);
                    var rect = reviewTargets[id];
                    if (rect.w <= 0 || rect.h <= 0 || rect.x - rect.w / 2 < 0 || rect.x + rect.w / 2 > Screen.width
                        || rect.y - rect.h / 2 < 0 || rect.y + rect.h / 2 > Screen.height)
                        throw new Exception("Archive control outside client bounds: " + id);
                }
                var entry = BattleReportArchive.Create(world.reports, reportFilter).Entries.FirstOrDefault(e => e.SourceIndex == selectedReport);
                bool shouldReplay = entry != null && entry.HasReplay;
                if (reviewTargets.ContainsKey("report-replay") != shouldReplay)
                    throw new Exception("Empty or historical report exposed a fabricated replay action.");
                if (shouldReplay)
                    foreach (string id in new[] { "report-replay", "report-outcome", "report-formation", "report-location", "report-text" })
                    {
                        if (!reviewTargets.ContainsKey(id)) throw new Exception("Archive detail action missing: " + id);
                        var rect = reviewTargets[id];
                        if (rect.x - rect.w / 2 < 0 || rect.x + rect.w / 2 > Screen.width || rect.y + rect.h / 2 > Screen.height)
                            throw new Exception("Archive detail clipped: " + id);
                    }
            }
            finally { reviewMode = oldReview; if (!reviewMode) reviewTargets.Clear(); }
        }

        private IEnumerator BattleReportArchiveSmoke()
        {
            var oldSession = session; string oldTab = tab; float oldSpeed = speed;
            var oldFilter = reportFilter; var oldWorld = reportArchiveWorld; var oldSelection = reportArchiveSelection;
            int oldSelectionIndex = reportArchiveSelectionIndex, oldSelected = selectedReport, oldMapSelected = map.selected;
            var oldListScroll = reportScroll; var oldTextScroll = battleTextScroll; float oldMaximum = reportArchiveScrollMaximum;
            var oldFocus = map.focus; float oldZoom = map.zoom; string original = ReplayWorldHash();
            var oldRecord = replayRecord; var oldPlayer = replayPlayer; var oldOutcome = replayOutcome;
            bool oldPlaying = replayPlaying, oldEvents = replayShowEvents, oldOutcomeOpen = replayShowOutcome, oldWatching = watchingCurrentBattle;
            float oldClock = replayClock, oldReplaySpeed = replaySpeed; var oldEventScroll = replayEventScroll;
            bool oldFormationOpen = battleFormationOpen, oldFormationFromReport = battleFormationFromReport;
            session = new CampaignSession(new JsonCampaignRepository(Path.Combine(Application.persistentDataPath, "smoke-report-archive-unused")), () => DateTime.UtcNow, true);
            try
            {
                session.StartYouzhou(0, "军报筛选验收"); map.Setup(world); speed = 0; tab = "战报";
                var legacy = PopulateReportArchiveFixture(); selectedReport = 0; RefreshReportArchive();
                string unchanged = ReplayWorldHash();
                yield return CaptureReportArchive("smoke-report-archive-all.png");
                SelectReportFilter(BattleReportFilter.Captured);
                if (BattleReportArchive.Create(world.reports, reportFilter).Entries.Count != 6)
                    throw new Exception("Captured filter disagrees with real settlements.");
                yield return CaptureReportArchive("smoke-report-archive-captured.png");
                var selected = world.reports[selectedReport];
                reportScroll.y = 80;
                OpenBattleReplay(selected); SeekBattleEvent(1); CloseBattleReplay(); RefreshReportArchive();
                if (reportFilter != BattleReportFilter.Captured || world.reports[selectedReport] != selected || reportScroll.y != 80)
                    throw new Exception("Replay return lost the filtered record or list position.");
                SelectReportFilter(BattleReportFilter.NotCaptured);
                if (selectedReport != 0 || world.reports[selectedReport].captured)
                    throw new Exception("Not-captured filter selected a captured report.");
                yield return CaptureReportArchive("smoke-report-archive-not-captured.png");
                SelectReportFilter(BattleReportFilter.Legacy);
                if (world.reports[selectedReport] != legacy) throw new Exception("Legacy filter did not keep the actual old record.");
                yield return CaptureReportArchive("smoke-report-archive-legacy.png");
                if (ReplayWorldHash() != unchanged) throw new Exception("Filtering, replay and returning mutated the campaign.");
                world.reports.Remove(legacy); RefreshReportArchive();
                yield return CaptureReportArchive("smoke-report-archive-empty.png");
                if (selectedReport != -1) throw new Exception("Empty archive retained an invalid selection.");
                SelectReportFilter(BattleReportFilter.All);
                if (selectedReport != 0) throw new Exception("Show all did not restore the newest actual report.");
                // New reports must not silently replace a user's currently selected battle.
                SelectArchivedReport(2); selected = world.reports[2];
                world.troops = 2000; var next = world.lands.First(t => world.AttackProblem(t.id) == ""); next.level = 1;
                world.March(next.id); world.Tick(world.order.remaining + .01); RefreshReportArchive();
                if (selectedReport != 3 || world.reports[selectedReport] != selected)
                    throw new Exception("New settlement shifted the archive to a different battle.");
                var newest = world.reports[0]; RestoreArchivedReport(newest); RefreshReportArchive();
                if (selectedReport != 0 || reportArchiveSelection != newest)
                    throw new Exception("Explicit replay return was replaced by a stale archive selection.");
                SelectReportFilter(BattleReportFilter.NotCaptured); RestoreArchivedReport(newest); RefreshReportArchive();
                if (reportFilter != BattleReportFilter.All || selectedReport != 0)
                    throw new Exception("Explicit return to a filtered-out report did not restore its actual record.");
                Debug.Log("BATTLE_REPORT_ARCHIVE_RUNTIME_OK: actual seven settlements, all/captured/not-captured/legacy/empty screens, drawn actions, replay return preserves filter/selection/scroll, inserted settlement preserves record identity, no repeated campaign effects.");
            }
            finally
            {
                session = oldSession; map.Setup(world); map.selected = oldMapSelected; map.focus = oldFocus; map.zoom = oldZoom;
                tab = oldTab; speed = oldSpeed; reportFilter = oldFilter; reportArchiveWorld = oldWorld;
                reportArchiveSelection = oldSelection; reportArchiveSelectionIndex = oldSelectionIndex; selectedReport = oldSelected;
                reportScroll = oldListScroll; battleTextScroll = oldTextScroll; reportArchiveScrollMaximum = oldMaximum;
                replayRecord = oldRecord; replayPlayer = oldPlayer; replayOutcome = oldOutcome;
                replayPlaying = oldPlaying; replayShowEvents = oldEvents; replayShowOutcome = oldOutcomeOpen; watchingCurrentBattle = oldWatching;
                replayClock = oldClock; replaySpeed = oldReplaySpeed; replayEventScroll = oldEventScroll;
                battleFormationOpen = oldFormationOpen; battleFormationFromReport = oldFormationFromReport;
                if (ReplayWorldHash() != original) throw new Exception("Archive smoke changed the original campaign.");
            }
        }
    }
}
