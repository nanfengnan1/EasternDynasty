using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using EasternDynasty;

public static class BattleReplayChecks
{
    private static int checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static string Json<T>(T value)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value); return Encoding.UTF8.GetString(stream.ToArray()); } }
    private static T Parse<T>(string value)
    { using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream); }
    private static void Valid(World world) { string problem; Check(world.IsValid(out problem), problem); }
    private static void ArrivalIdentityChecks()
    {
        foreach (bool youzhou in new[] { false, true })
        {
            var world = youzhou ? World.CreateYouzhou() : World.Create(Homeland.Han, 4, "Arrival identity");
            world.troops = 2000;
            var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = 1;
            world.March(target.id, true);
            string id = world.order.battleId;
            Check(!string.IsNullOrEmpty(id) && world.order.watchOnArrival, "watched departure lacks persisted identity or intent");
            string issued = Json(world); world.March(target.id, false);
            Check(Json(world) == issued, "rejected duplicate departure replaced identity or charged resources");
            world.Tick(world.order.remaining / 2);
            world = Parse<World>(Json(world)); Valid(world);
            Check(world.order.battleId == id && world.order.watchOnArrival && world.reports.Count == 0,
                "outbound reload lost identity/intent or disclosed a premature result");
            world.EnsureSeasonState(); world.EnsureSeasonState();
            Check(world.order.battleId == id, "normalization replaced an issued battle identity");
            world.Tick(world.order.remaining);
            Check(world.reports.Count == 1 && world.reports[0].battleId == id && world.order.returning
                && world.order.battleId == id, "settled record is not linked to its departure and return");
            string settled = Json(world.reports[0]); int troops = world.troops;
            world = Parse<World>(Json(world)); world.Tick(1000);
            Check(world.order == null && world.reports.Count == 1 && Json(world.reports[0]) == settled && world.troops == troops,
                "saved return settled the linked battle again");
            Valid(world);
        }

        var old = World.CreateYouzhou();
        var oldTarget = old.lands.First(t => old.AttackProblem(t.id) == "");
        old.March(oldTarget.id);
        Check(!old.order.watchOnArrival && !string.IsNullOrEmpty(old.order.battleId), "legacy March signature opted into automatic watching");
        string oldJson = Regex.Replace(Json(old), "\"(?:battleId|watchOnArrival)\":(?:\"[^\"]*\"|true|false|null),?", "").Replace(",}", "}");
        Check(!oldJson.Contains("battleId") && !oldJson.Contains("watchOnArrival"), "pre-watch fixture still has optional fields");
        old = Parse<World>(oldJson); Valid(old);
        Check(string.IsNullOrEmpty(old.order.battleId) && !old.order.watchOnArrival, "missing fields did not use old-save defaults");
        old.EnsureSeasonState(); string migratedId = old.order.battleId;
        old = Parse<World>(Json(old)); old.EnsureSeasonState();
        Check(!string.IsNullOrEmpty(migratedId) && old.order.battleId == migratedId && !old.order.watchOnArrival,
            "pre-watch order migration was not stable or enabled unsolicited playback");
        old.Tick(old.order.remaining);
        Check(old.reports.Count == 1 && old.reports[0].battleId == migratedId, "migrated order did not settle under its identity");
        Valid(old);

        var repeated = World.CreateYouzhou(); repeated.troops = 50;
        var repeatedTarget = repeated.lands.First(t => repeated.AttackProblem(t.id) == ""); repeatedTarget.level = 5;
        repeated.March(repeatedTarget.id, true); string firstId = repeated.order.battleId;
        repeated.Tick(1000); Check(!repeatedTarget.owned && repeated.order == null, "repeat-location fixture did not lose and return");
        repeated.troops = 50; repeated.March(repeatedTarget.id, true); string secondId = repeated.order.battleId;
        Check(firstId != secondId && repeated.reports.Count == 1, "second departure reused a location's old battle identity");
        repeated.Tick(1000);
        Check(repeated.reports.Count == 2 && repeated.reports[0].battleId == secondId && repeated.reports[1].battleId == firstId
            && repeated.reports.All(r => r.targetId == repeatedTarget.id), "same-location reports cannot be matched to their departures");
        Valid(repeated);
    }
    private static World Fight(int troops, int level, bool healing)
    {
        var world = World.CreateYouzhou(); world.troops = troops;
        var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = level;
        foreach (var card in world.ActiveGenerals) card.learnedSkill = healing ? "抚军" : "";
        world.March(target.id); Check(world.order != null && world.reports.Count == 0, "battle settled before departure");
        world.Tick(world.order.remaining / 2); Check(world.reports.Count == 0 && !target.owned, "battle settled before arrival");
        world.Tick(world.order.remaining);
        Check(world.reports.Count == 1 && world.reports[0].HasReplay, "arrival did not record real battle");
        var record = world.reports[0];
        Check(record.time == world.elapsed && record.targetId == target.id && record.targetName == target.name
            && record.attacker.troops == troops && record.defender.troops == world.Defenders(target), "initial snapshot differs from battle");
        Check(record.attackerFinal == world.troops && record.captured == target.owned && BattleReplay.IsValid(record), "record differs from settled state");
        Check(record.events.Count(e => e.kind == BattleReplayEventKind.Attack) == record.events.Count(e => e.kind == BattleReplayEventKind.Loss), "round does not contain both whole-army actions");
        Check(record.events.Count == 2 + 2 * record.events.Last().round + record.events.Count(e => e.kind == BattleReplayEventKind.Recovery), "event count differs from actual rounds");
        Check(record.attackerFinal == record.attacker.troops - record.events.Where(e => e.kind == BattleReplayEventKind.Loss).Sum(e => e.amount)
            + record.events.Where(e => e.kind == BattleReplayEventKind.Recovery).Sum(e => e.amount), "army totals differ from event ledger");
        Check(record.defenderFinal == record.defender.troops - record.events.Where(e => e.kind == BattleReplayEventKind.Attack).Sum(e => e.amount), "defender total differs from event ledger");
        Valid(world); return world;
    }
    public static int Main()
    {
        try
        {
            ArrivalIdentityChecks();
            var won = Fight(2000, 1, true); var record = won.reports[0];
            Check(record.won && record.goldReward == 35 && record.experienceReward == 100 && record.attacker.generals.All(g => g.awardedExperience == 100), "victory rewards not recorded");
            Check(record.events.Any(e => e.kind == BattleReplayEventKind.Recovery), "healing fixture did not exercise recorded restoration");
            Check(record.defender.generals.Count == 0, "resolver fabricated defender generals");
            var lost = Fight(50, 5, false);
            Check(!lost.reports[0].won && lost.reports[0].attackerFinal == 0 && lost.reports[0].goldReward == 0 && lost.reports[0].experienceReward == 125, "defeat result or rewards wrong");
            World draw = null;
            for (int count = 1000; count <= 1800 && draw == null; count += 5)
            {
                var candidate = World.CreateYouzhou(); candidate.troops = count;
                var target = candidate.lands.First(t => candidate.AttackProblem(t.id) == ""); target.level = 5;
                candidate.March(target.id); candidate.Tick(candidate.order.remaining);
                if (!candidate.reports[0].won && candidate.troops > 0) draw = candidate;
            }
            Check(draw != null, "eight-round draw fixture missing");
            var drawn = draw.reports[0];
            Check(drawn.events.Last().round == 8 && drawn.defenderFinal > 0 && drawn.attackerFinal > 0 && !drawn.captured && drawn.goldReward == 0, "eight-round stop misreported");
            Check(BattleReplay.IsValid(drawn), "eight-round trace invalid"); Valid(draw);

            string frozen = Json(record);
            won.ActiveGenerals[0].level++; won.ActiveGenerals[0].learnedSkill = "坚守";
            won.formation.Reverse(); won.lands[record.targetId].name = "later name";
            Check(Json(record) == frozen, "later formation/card/target changes mutated recorded snapshot");
            Check(record.attacker.generals[0].level == 1 && record.attacker.generals[0].learnedSkill == "抚军", "snapshot did not preserve pre-battle state");
            var loaded = Parse<World>(Json(won)); Valid(loaded);
            Check(Json(loaded.reports[0]) == frozen, "JSON changed frozen events or rewards");
            string beforePlayback = Json(loaded);
            var player = new BattleReplayPlayer(loaded.reports[0]);
            for (int replay = 0; replay < 10; replay++)
            {
                player.Seek(0); Check(player.Current.kind == BattleReplayEventKind.Start, "restart failed");
                for (int i = 1; i < player.Count; i++) { player.Step(1); Check(player.Current.sequence == i, "playback skipped event"); }
                player.Seek(int.MaxValue); Check(player.Current.kind == BattleReplayEventKind.Result, "seek to end failed");
                player.Seek(-1); Check(player.Position == 0, "seek bounds failed");
            }
            Check(Json(loaded) == beforePlayback, "playback settled battle twice or mutated world");
            loaded.Tick(500); Check(loaded.reports.Count == 1 && loaded.troops == record.attackerFinal, "return settled battle again");

            var legacy = Parse<BattleRecord>("{\"title\":\"旧战报\",\"text\":\"只读历史文本\",\"won\":true,\"time\":0}");
            Check(legacy.replayVersion == 0 && !legacy.HasReplay && legacy.events == null && BattleReplay.IsValid(legacy), "old text report was not compatible");
            var legacyPlayer = new BattleReplayPlayer(legacy); legacyPlayer.Step(1);
            Check(legacyPlayer.Count == 0 && legacyPlayer.Current == null, "old text report manufactured replay");
            loaded.reports.Add(legacy); Valid(loaded);

            var broken = Parse<BattleRecord>(frozen); broken.events[1].amount++;
            Check(!BattleReplay.IsValid(broken), "invalid event totals accepted");
            broken = Parse<BattleRecord>(frozen); broken.events.RemoveAt(2);
            Check(!BattleReplay.IsValid(broken), "missing retaliation accepted");
            broken = Parse<BattleRecord>(frozen); broken.attacker.generals[0].awardedExperience = -1;
            Check(!BattleReplay.IsValid(broken), "negative experience accepted");
            broken = Parse<BattleRecord>(frozen); broken.replayVersion = 2;
            Check(!BattleReplay.IsValid(broken), "unknown replay version accepted");
            Check(record.battleId != lost.reports[0].battleId && record.ruleVersion == BattleReplay.RuleVersion, "battle identity or rules missing");
            Console.WriteLine("BATTLE_REPLAY_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
