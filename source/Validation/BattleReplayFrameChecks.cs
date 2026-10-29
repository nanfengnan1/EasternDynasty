using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

public static class BattleReplayFrameChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static string Json<T>(T value)
    {
        using (var stream = new MemoryStream())
        { new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value); return Encoding.UTF8.GetString(stream.ToArray()); }
    }
    private static T Parse<T>(string value)
    { using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(value))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream); }
    private static World Fight(int troops, int targetLevel, bool recovery, bool capped)
    {
        var world = World.CreateYouzhou(); world.troops = troops;
        foreach (var general in world.ActiveGenerals)
        {
            general.learnedSkill = recovery ? "抚军" : "";
            if (capped) { general.level = general.LevelCap; general.experience = 0; }
        }
        var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = targetLevel;
        world.March(target.id, true);
        Check(world.order != null && world.reports.Count == 0, "fixture did not issue a real march");
        world.Tick(world.order.remaining);
        Check(world.reports.Count == 1 && BattleReplay.IsValid(world.reports[0]), "real arrival did not produce a valid record");
        return world;
    }
    private static void CheckPosition(BattleRecord record, int requested)
    {
        var frame = BattleReplayFrame.Create(record, requested);
        int position = Math.Max(0, Math.Min(record.events.Count - 1, requested));
        var item = record.events[position];
        var prefix = record.events.Take(position + 1).ToArray();
        Check(frame != null && frame.position == position && frame.round == item.round && frame.kind == item.kind
            && frame.amount == item.amount, "cursor or event identity differs at " + requested);
        int damage = prefix.Where(e => e.kind == BattleReplayEventKind.Attack).Sum(e => e.amount);
        int loss = prefix.Where(e => e.kind == BattleReplayEventKind.Loss).Sum(e => e.amount);
        int recovery = prefix.Where(e => e.kind == BattleReplayEventKind.Recovery).Sum(e => e.amount);
        Check(frame.damageTotal == damage && frame.lossTotal == loss && frame.recoveryTotal == recovery,
            "jump accumulated future or previously viewed events at " + requested);
        Check(frame.attackerAfter == record.attacker.troops - loss + recovery
            && frame.defenderAfter == record.defender.troops - damage, "prefix troop conservation failed at " + requested);
        int attackerDelta = item.kind == BattleReplayEventKind.Loss ? -item.amount
            : item.kind == BattleReplayEventKind.Recovery ? item.amount : 0;
        int defenderDelta = item.kind == BattleReplayEventKind.Attack ? -item.amount : 0;
        Check(frame.attackerAfter == frame.attackerBefore + attackerDelta
            && frame.defenderAfter == frame.defenderBefore + defenderDelta, "current event before/after mismatch at " + requested);
        Check(frame.attackerBefore == (position == 0 ? record.attacker.troops : record.events[position - 1].attackerTroops)
            && frame.defenderBefore == (position == 0 ? record.defender.troops : record.events[position - 1].defenderTroops),
            "current event does not start at preceding event at " + requested);
        bool result = item.kind == BattleReplayEventKind.Result;
        Check(frame.isResult == result && frame.won == (result && record.won) && frame.captured == (result && record.captured),
            "result flags leaked before Result at " + requested);
        Check(frame.goldReward == (result ? record.goldReward : 0)
            && frame.experienceReward == (result ? record.attacker.generals.Sum(g => g.awardedExperience) : 0),
            "rewards leaked or differed from recorded credit at " + requested);
    }
    private static void CheckNavigation(World world, string label)
    {
        var record = world.reports[0];
        string beforeWorld = Json(world), beforeRecord = Json(record);
        foreach (int requested in new[] { int.MinValue, -1, 0, int.MaxValue, 1, 1, 0, record.events.Count, 2, 0 })
            CheckPosition(record, requested);
        // Simulates selecting any round/event directly, then stepping back and revisiting it.
        for (int i = record.events.Count - 1; i >= 0; i--)
        { CheckPosition(record, i); CheckPosition(record, Math.Max(0, i - 1)); CheckPosition(record, i); }
        var final = BattleReplayFrame.Create(record, int.MaxValue);
        Check(final.attackerAfter == record.attackerFinal && final.defenderAfter == record.defenderFinal,
            "result frame differs from settled troops");
        Check(Json(record) == beforeRecord && Json(world) == beforeWorld,
            "navigation changed the frozen record, troops, ownership, resources or experience");
        var loaded = Parse<World>(beforeWorld);
        CheckPosition(loaded.reports[0], 0); CheckPosition(loaded.reports[0], int.MaxValue);
        Check(Json(loaded) == beforeWorld, "projecting a reloaded report changed the campaign");
        Console.WriteLine("PASS " + label + ": " + record.events.Count + " recorded events; arbitrary forward/back/repeated seeks conserve troops and preserve campaign");
    }
    public static int Main()
    {
        try
        {
            Check(BattleReplayFrame.Create(null, 0) == null, "null report manufactured a frame");
            var legacy = Parse<BattleRecord>("{\"title\":\"旧战报\",\"text\":\"造成 9999 伤害，获得 999 铜钱\",\"won\":true,\"time\":0}");
            string legacyBefore = Json(legacy);
            Check(BattleReplayFrame.Create(legacy, int.MaxValue) == null && Json(legacy) == legacyBefore,
                "legacy Chinese text was interpreted as recorded events");
            Check(typeof(BattleReplayFrame).GetFields().All(field => field.IsInitOnly && field.FieldType.IsValueType),
                "frame exposes mutable fields or references into a battle");

            var won = Fight(2000, 1, true, false);
            Check(won.reports[0].won && won.reports[0].events.Any(e => e.kind == BattleReplayEventKind.Recovery),
                "winning fixture did not record real recovery");
            CheckNavigation(won, "victory with recovery");
            var lost = Fight(50, 5, false, false);
            Check(!lost.reports[0].won && lost.reports[0].attackerFinal == 0, "defeat fixture did not lose");
            CheckNavigation(lost, "defeat");
            var capped = Fight(2000, 1, false, true);
            Check(capped.reports[0].experienceReward > 0 && capped.reports[0].attacker.generals.All(g => g.awardedExperience == 0),
                "level-cap fixture did not distinguish nominal and actual XP");
            CheckNavigation(capped, "level-capped actual reward");
            Check(BattleReplayFrame.Create(capped.reports[0], int.MaxValue).experienceReward == 0,
                "result displays nominal XP that was never credited");

            World draw = null;
            for (int troops = 1000; troops <= 1800 && draw == null; troops += 5)
            {
                var candidate = World.CreateYouzhou(); candidate.troops = troops;
                var target = candidate.lands.First(t => candidate.AttackProblem(t.id) == ""); target.level = 5;
                candidate.March(target.id); candidate.Tick(candidate.order.remaining);
                if (!candidate.reports[0].won && candidate.troops > 0) draw = candidate;
            }
            Check(draw != null && draw.reports[0].events.Last().round == 8, "eight-round fixture not found");
            CheckNavigation(draw, "eight-round withdrawal");

            var record = won.reports[0]; string frozen = Json(record);
            won.ActiveGenerals[0].level++; won.formation.Reverse(); won.lands[record.targetId].name = "changed later";
            CheckPosition(record, 1); CheckPosition(record, int.MaxValue);
            Check(Json(record) == frozen, "later live army/land changes rewrote display facts");
            var broken = Parse<BattleRecord>(frozen); broken.events[1].amount++;
            Check(BattleReplayFrame.Create(broken, 1) == null, "inconsistent event ledger was presented as valid");
            broken = Parse<BattleRecord>(frozen); broken.replayVersion = 2;
            Check(BattleReplayFrame.Create(broken, 1) == null, "unknown replay version was interpreted");
            Console.WriteLine("BATTLE_REPLAY_FRAME_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
