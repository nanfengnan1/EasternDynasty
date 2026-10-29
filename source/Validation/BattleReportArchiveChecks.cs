using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using EasternDynasty;

public static class BattleReportArchiveChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static string Json<T>(T value)
    {
        using (var stream = new MemoryStream())
        { new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value); return Encoding.UTF8.GetString(stream.ToArray()); }
    }
    private static T Parse<T>(string value)
    { using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream); }
    private static string Hash(string value)
    { using (var hash = SHA256.Create()) return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(value))); }
    private static string EntryFacts(BattleReportArchiveEntry entry)
    {
        return string.Join("|", typeof(BattleReportArchiveEntry).GetProperties()
            .OrderBy(p => p.Name).Select(p => p.Name + "=" + p.GetValue(entry, null)));
    }

    private static World Fight(int troops, int level, bool capped = false)
    {
        var world = World.CreateYouzhou(); world.troops = troops;
        foreach (var general in world.ActiveGenerals)
        {
            general.learnedSkill = "";
            if (capped) { general.level = general.LevelCap; general.experience = 0; }
        }
        var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = level;
        world.March(target.id, true);
        Check(world.order != null && world.reports.Count == 0, "fixture must issue an actual march without showing a result");
        world.Tick(world.order.remaining / 2);
        Check(world.reports.Count == 0 && !target.owned, "archive result appeared before arrival");
        world.Tick(world.order.remaining);
        Check(world.reports.Count == 1 && BattleReplay.IsValid(world.reports[0]), "arrival failed to settle a valid record");
        return world;
    }

    private static void CheckEntry(BattleReportArchiveEntry entry, BattleRecord record, int sourceIndex)
    {
        var summary = BattleOutcomeSummary.Create(record);
        Check(summary != null && entry.HasReplay && entry.SourceIndex == sourceIndex,
            "structured archive row lost its source identity");
        Check(entry.Title == record.title && entry.Text == record.text && entry.TargetName == summary.targetName
            && entry.TargetX == summary.targetX && entry.TargetZ == summary.targetZ,
            "archive row target/title differs from frozen report");
        Check(entry.Captured == summary.captured && entry.Rounds == summary.rounds
            && entry.AttackerInitial == summary.attackerInitial && entry.AttackerFinal == summary.attackerFinal
            && entry.DefenderInitial == summary.defenderInitial && entry.DefenderFinal == summary.defenderFinal,
            "archive row outcome differs from recorded events");
        Check(entry.Gold == summary.goldReward && entry.Experience == summary.experienceReward,
            "archive rewards must use actual frozen credits");
    }

    private static void CheckLegacy(BattleReportArchiveEntry entry)
    {
        Check(!entry.HasReplay && !entry.Captured && entry.TargetName == "历史文字记录"
            && entry.TargetX == -1 && entry.TargetZ == -1, "legacy text manufactured a capture or target");
        Check(entry.Rounds == 0 && entry.AttackerInitial == 0 && entry.AttackerFinal == 0
            && entry.DefenderInitial == 0 && entry.DefenderFinal == 0 && entry.Gold == 0 && entry.Experience == 0,
            "legacy/invalid report supplied structured numbers");
    }

    private static void CheckFilters(IList<BattleRecord> records)
    {
        var all = BattleReportArchive.Create(records, BattleReportFilter.All);
        Check(all.Entries.Select(e => e.SourceIndex).SequenceEqual(new[] { 0, 2, 3, 4 }),
            "archive sorted source records or renumbered after a null record");
        foreach (BattleReportFilter filter in Enum.GetValues(typeof(BattleReportFilter)))
        {
            var archive = BattleReportArchive.Create(records, filter);
            Check(archive.AllCount == 4 && archive.CapturedCount == 1 && archive.NotCapturedCount == 2
                && archive.LegacyCount == 1, "filter changed total category counts");
            int[] expected = filter == BattleReportFilter.All ? new[] { 0, 2, 3, 4 }
                : filter == BattleReportFilter.Captured ? new[] { 3 }
                : filter == BattleReportFilter.NotCaptured ? new[] { 0, 2 } : new[] { 4 };
            Check(archive.Entries.Select(e => e.SourceIndex).SequenceEqual(expected), "wrong capture/legacy filter membership");
            foreach (int selected in expected)
                Check(archive.ResolveSelection(selected) == selected, "filter lost a surviving selected source index");
            Check(archive.ResolveSelection(1000) == expected[0] && archive.ResolveSelection(-1) == expected[0],
                "removed/invalid selection did not fall back to newest visible report");
        }
        var invalidFilter = BattleReportArchive.Create(records, (BattleReportFilter)77);
        Check(invalidFilter.Filter == BattleReportFilter.All && invalidFilter.Entries.Count == 4,
            "unknown filter did not safely fall back to All");
        CheckEntry(all.Entries[0], records[0], 0);
        CheckEntry(all.Entries[1], records[2], 2);
        CheckEntry(all.Entries[2], records[3], 3);
        CheckLegacy(all.Entries[3]);
        Check(all.Entries[3].Title == records[4].title && all.Entries[3].Text == records[4].text,
            "legacy title/text was not preserved verbatim");
        Check(!all.Entries[0].Captured && all.Entries[0].Rounds == 8 && all.Entries[0].AttackerFinal > 0
            && all.Entries[0].DefenderFinal > 0, "eight-round stop was collapsed into a wiped-out army");
    }

    private static void CheckDetached(World world)
    {
        var record = world.reports[0]; string frozen = Json(record);
        var archive = BattleReportArchive.Create(world.reports, BattleReportFilter.All);
        string row = EntryFacts(archive.Entries[0]);
        world.ActiveGenerals[0].level++; world.ActiveGenerals[0].learnedSkill = "坚守";
        world.formation.Reverse(); world.lands[record.targetId].name = "后来改名";
        Check(Json(record) == frozen, "live upgrades/formation/land rename rewrote frozen battle");
        Check(EntryFacts(archive.Entries[0]) == row
            && EntryFacts(BattleReportArchive.Create(world.reports, BattleReportFilter.All).Entries[0]) == row,
            "archive consulted live upgrades or current target names");

        var copied = Parse<BattleRecord>(frozen);
        var input = new List<BattleRecord> { copied };
        var detached = BattleReportArchive.Create(input, BattleReportFilter.All);
        copied.title = copied.text = copied.targetName = "changed";
        copied.targetX++; copied.targetZ++; copied.captured = !copied.captured;
        copied.attacker.troops++; copied.attacker.generals[0].awardedExperience++;
        copied.attackerFinal++; copied.defenderFinal++; copied.goldReward++; copied.events.Clear(); input.Clear();
        Check(EntryFacts(detached.Entries[0]) == row && detached.AllCount == 1,
            "archive holds a mutable source/list reference instead of copied display facts");
        Check(typeof(BattleReportArchiveEntry).GetProperties().All(p => p.GetSetMethod(true) == null
            && (p.PropertyType.IsValueType || p.PropertyType == typeof(string))), "entry exposes writable properties or mutable references");
        var exposed = detached.Entries as IList<BattleReportArchiveEntry>;
        Check(exposed != null && exposed.IsReadOnly, "entry collection exposes writes");
        bool rejected = false;
        try { exposed.Clear(); } catch (NotSupportedException) { rejected = true; }
        Check(rejected && detached.Entries.Count == 1, "consumer could clear immutable entry collection");
        Console.WriteLine("PASS detached archive: upgrades, live formation, renamed target and source edits preserve recorded display facts");
    }

    private static void CheckPresentationFixture()
    {
        var world = World.CreateYouzhou();
        for (int i = 0; i < 7; i++)
        {
            world.troops = i < 6 ? 2000 : 50;
            var target = world.lands.First(t => world.AttackProblem(t.id) == "");
            target.level = i < 6 ? 1 : 5;
            world.March(target.id);
            Check(world.order != null, "presentation fixture could not issue battle " + i);
            world.Tick(world.order.remaining + .01); world.Tick(1000);
            Check(world.reports.Count == i + 1 && world.order == null,
                "presentation fixture failed to settle and return from battle " + i);
        }
        Check(world.reports.Count == 7 && world.reports.Select(r => r.battleId).Distinct().Count() == 7,
            "presentation fixture must have seven independent battle identities");
        var archive = BattleReportArchive.Create(world.reports, BattleReportFilter.All);
        Check(archive.CapturedCount == 6 && archive.NotCapturedCount == 1 && archive.LegacyCount == 0
            && !archive.Entries[0].Captured && archive.Entries[0].AttackerFinal == 0,
            "presentation fixture does not contain six captures and newest defeated army");
        Console.WriteLine("PASS presentation fixture: seven real marches, unique identities, six captures and one defeated army");
    }

    public static int Main()
    {
        try
        {
            foreach (var empty in new IList<BattleRecord>[] { null, new List<BattleRecord>(), new List<BattleRecord> { null } })
            {
                var archive = BattleReportArchive.Create(empty, BattleReportFilter.All);
                Check(archive.Entries.Count == 0 && archive.AllCount == 0 && archive.ResolveSelection(0) == -1,
                    "empty/missing source manufactured an entry or selection");
            }
            var won = Fight(2000, 1); var lost = Fight(50, 5);
            Check(won.reports[0].captured && !lost.reports[0].captured && lost.reports[0].attackerFinal == 0,
                "actual capture/defeat fixtures missing");
            World draw = null;
            for (int count = 1000; count <= 1800 && draw == null; count += 5)
            {
                var candidate = Fight(count, 5);
                if (!candidate.reports[0].captured && candidate.reports[0].attackerFinal > 0) draw = candidate;
            }
            Check(draw != null && draw.reports[0].events.Last().round == 8, "actual eight-round stop fixture missing");
            var legacy = Parse<BattleRecord>("{\"title\":\"旧胜利战报\",\"text\":\"击败守军，造成9999伤害，获得999铜钱、888经验\",\"won\":true,\"time\":0,\"captured\":true,\"targetX\":17,\"targetZ\":18,\"targetName\":\"旧记录中的地名\",\"goldReward\":999}");
            var records = new List<BattleRecord> { draw.reports[0], null, lost.reports[0], won.reports[0], legacy };
            CheckFilters(records);
            var emptyCapture = BattleReportArchive.Create(new List<BattleRecord> { legacy }, BattleReportFilter.Captured);
            Check(emptyCapture.Entries.Count == 0 && emptyCapture.ResolveSelection(0) == -1,
                "legacy victory prose was treated as a capture");

            won.reports.Add(lost.reports[0]); won.reports.Add(draw.reports[0]); won.reports.Add(legacy);
            string before = Json(won), beforeHash = Hash(before);
            for (int visit = 0; visit < 5; visit++)
                foreach (BattleReportFilter filter in Enum.GetValues(typeof(BattleReportFilter)))
                {
                    var archive = BattleReportArchive.Create(won.reports, filter);
                    foreach (var entry in archive.Entries) archive.ResolveSelection(entry.SourceIndex);
                    archive.ResolveSelection(-999);
                }
            Check(Json(won) == before && Hash(Json(won)) == beforeHash,
                "archive browsing/filtering/selection changed World JSON or SHA256");
            var loaded = Parse<World>(before);
            var restored = BattleReportArchive.Create(loaded.reports, BattleReportFilter.All);
            Check(restored.Entries.Count == 4 && restored.LegacyCount == 1, "JSON restore lost structured/legacy archive membership");
            for (int i = 0; i < 3; i++) CheckEntry(restored.Entries[i], won.reports[i], i);
            Check(Json(loaded) == before && Hash(Json(loaded)) == beforeHash, "archive changed restored World JSON or SHA256");
            Console.WriteLine("PASS actual march outcomes: capture, defeated army, eight-round stop; mixed legacy filters and selection; unchanged World JSON/SHA256");

            var capped = Fight(2000, 1, true);
            var cappedEntry = BattleReportArchive.Create(capped.reports, BattleReportFilter.All).Entries[0];
            Check(capped.reports[0].experienceReward > 0 && cappedEntry.Experience == 0,
                "archive advertised nominal experience that capped generals never received");

            string valid = Json(won.reports[0]);
            var corruptions = new Action<BattleRecord>[] {
                r => r.replayVersion = 2, r => r.attacker = null, r => r.defender = null,
                r => r.events = null, r => r.events[1].amount++, r => r.events[1] = null,
                r => r.attacker.generals = null, r => r.attacker.generals[0] = null,
                r => r.targetName = null, r => r.events.RemoveAt(r.events.Count - 1), r => r.time = double.NaN
            };
            foreach (var corrupt in corruptions)
            {
                var broken = Parse<BattleRecord>(valid); corrupt(broken);
                var archive = BattleReportArchive.Create(new List<BattleRecord> { broken }, BattleReportFilter.All);
                Check(archive.LegacyCount == 1 && archive.CapturedCount == 0 && archive.NotCapturedCount == 0,
                    "malformed record was classified using invalid structured data");
                CheckLegacy(archive.Entries[0]);
            }
            var missing = BattleReportArchive.Create(new List<BattleRecord> { new BattleRecord() }, BattleReportFilter.Legacy);
            Check(missing.Entries[0].Title == "历史文字记录" && missing.Entries[0].Text == "", "missing fields have no safe display fallback");
            CheckDetached(won);
            CheckPresentationFixture();
            Console.WriteLine("BATTLE_REPORT_ARCHIVE_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
