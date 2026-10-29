using System.Collections.Generic;

namespace EasternDynasty
{
    public enum BattleReportFilter { All, Captured, NotCaptured, Legacy }

    // A detached display row. It never retains a mutable record, army or World reference.
    public sealed class BattleReportArchiveEntry
    {
        public int SourceIndex { get; }
        public string Title { get; }
        public string Text { get; }
        public string TargetName { get; }
        public int TargetX { get; }
        public int TargetZ { get; }
        public bool HasReplay { get; }
        public bool Captured { get; }
        public int Rounds { get; }
        public int AttackerInitial { get; }
        public int AttackerFinal { get; }
        public int DefenderInitial { get; }
        public int DefenderFinal { get; }
        public int Gold { get; }
        public int Experience { get; }

        internal BattleReportArchiveEntry(int sourceIndex, BattleRecord record)
        {
            SourceIndex = sourceIndex;
            Text = record.text ?? "";
            var outcome = BattleOutcomeSummary.Create(record);
            HasReplay = outcome != null;
            Title = string.IsNullOrWhiteSpace(record.title)
                ? (HasReplay ? outcome.targetName + "战报" : "历史文字记录") : record.title;
            if (!HasReplay)
            {
                // The old title/text is available verbatim. Its prose and optional numeric fields
                // cannot establish a target, capture, casualty or reward for filtering/display.
                TargetName = "历史文字记录";
                TargetX = TargetZ = -1;
                return;
            }
            TargetName = outcome.targetName;
            TargetX = outcome.targetX; TargetZ = outcome.targetZ;
            Captured = outcome.captured; Rounds = outcome.rounds;
            AttackerInitial = outcome.attackerInitial; AttackerFinal = outcome.attackerFinal;
            DefenderInitial = outcome.defenderInitial; DefenderFinal = outcome.defenderFinal;
            Gold = outcome.goldReward; Experience = outcome.experienceReward;
        }
    }

    // Input order is already newest first; preserve it and the source index for navigation.
    // NotCaptured includes an eight-round stop with survivors, not just an army defeat.
    public sealed class BattleReportArchive
    {
        public IReadOnlyList<BattleReportArchiveEntry> Entries { get; }
        public BattleReportFilter Filter { get; }
        public int AllCount { get; }
        public int CapturedCount { get; }
        public int NotCapturedCount { get; }
        public int LegacyCount { get; }

        private BattleReportArchive(IList<BattleRecord> records, BattleReportFilter filter)
        {
            Filter = filter >= BattleReportFilter.All && filter <= BattleReportFilter.Legacy
                ? filter : BattleReportFilter.All;
            var entries = new List<BattleReportArchiveEntry>();
            if (records != null)
            {
                for (int i = 0; i < records.Count; i++)
                {
                    // A missing list element has no report to open. Keep later source indices intact.
                    if (records[i] == null) continue;
                    var entry = new BattleReportArchiveEntry(i, records[i]);
                    AllCount++;
                    if (!entry.HasReplay) LegacyCount++;
                    else if (entry.Captured) CapturedCount++;
                    else NotCapturedCount++;
                    if (Filter == BattleReportFilter.All
                        || Filter == BattleReportFilter.Legacy && !entry.HasReplay
                        || Filter == BattleReportFilter.Captured && entry.HasReplay && entry.Captured
                        || Filter == BattleReportFilter.NotCaptured && entry.HasReplay && !entry.Captured)
                        entries.Add(entry);
                }
            }
            Entries = entries.AsReadOnly();
        }

        public static BattleReportArchive Create(IList<BattleRecord> records, BattleReportFilter filter)
        {
            return new BattleReportArchive(records, filter);
        }

        public int ResolveSelection(int sourceIndex)
        {
            foreach (var entry in Entries)
                if (entry.SourceIndex == sourceIndex) return sourceIndex;
            return Entries.Count == 0 ? -1 : Entries[0].SourceIndex;
        }
    }
}
