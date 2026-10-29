using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

public static class BattleOutcomeSummaryChecks
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
    private static int Experience(GeneralCard card)
    { return card.level * (card.level - 1) * 40 + card.experience; }

    private static World Fight(int troops, int targetLevel, bool recovery, bool capped)
    {
        var world = World.CreateYouzhou(); world.troops = troops;
        foreach (var general in world.ActiveGenerals)
        {
            general.learnedSkill = recovery ? "抚军" : "";
            if (capped) { general.level = general.LevelCap; general.experience = 0; }
        }
        var experienceBefore = world.ActiveGenerals.ToDictionary(g => g.uid, g => Experience(g));
        var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = targetLevel;
        world.March(target.id, true);
        Check(world.order != null && world.reports.Count == 0, "fixture did not start an actual march");
        world.Tick(world.order.remaining / 2);
        Check(world.reports.Count == 0 && !target.owned, "battle result appeared before arrival");
        world.Tick(world.order.remaining);
        Check(world.reports.Count == 1 && BattleReplay.IsValid(world.reports[0]), "arrival did not settle a valid frozen record");
        var summary = BattleOutcomeSummary.Create(world.reports[0]);
        Check(summary != null, "actual arrival has no outcome");
        foreach (var participant in summary.participants)
        {
            var card = world.ActiveGenerals.First(g => g.uid == participant.uid);
            Check(participant.awardedExperience == Experience(card) - experienceBefore[card.uid],
                "participant XP differs from actual credited progress");
        }
        return world;
    }

    private static void CheckSummary(BattleRecord record, BattleOutcomeSummary summary)
    {
        Check(summary != null && summary.battleId == record.battleId && summary.targetName == record.targetName
            && summary.targetId == record.targetId && summary.targetX == record.targetX && summary.targetZ == record.targetZ,
            "battle identity/target differs from frozen facts");
        Check(summary.attackerName == record.attacker.name && summary.defenderName == record.defender.name
            && summary.attackerInitial == record.attacker.troops && summary.defenderInitial == record.defender.troops,
            "initial armies differ from frozen facts");
        Check(summary.attackerFinal == record.attackerFinal && summary.defenderFinal == record.defenderFinal
            && summary.rounds == record.events.Last().round && summary.won == record.won && summary.captured == record.captured,
            "settled outcome differs from frozen facts");
        Check(summary.damageTotal == record.events.Where(e => e.kind == BattleReplayEventKind.Attack).Sum(e => e.amount)
            && summary.lossTotal == record.events.Where(e => e.kind == BattleReplayEventKind.Loss).Sum(e => e.amount)
            && summary.recoveryTotal == record.events.Where(e => e.kind == BattleReplayEventKind.Recovery).Sum(e => e.amount),
            "outcome totals do not match complete recorded events");
        Check(summary.attackerInitial - summary.lossTotal + summary.recoveryTotal == summary.attackerFinal
            && summary.defenderInitial - summary.damageTotal == summary.defenderFinal,
            "outcome troop conservation failed");
        Check(summary.goldReward == record.goldReward
            && summary.experienceReward == record.attacker.generals.Sum(g => g.awardedExperience),
            "outcome rewards differ from recorded actual credits");
        Check(summary.participants.Count == record.attacker.generals.Count, "participant count differs from frozen army");
        for (int i = 0; i < summary.participants.Count; i++)
        {
            var actual = summary.participants[i]; var frozen = record.attacker.generals[i];
            Check(actual.uid == frozen.uid && actual.definitionId == frozen.definitionId && actual.name == frozen.name
                && actual.level == frozen.level && actual.rank == frozen.rank && actual.awardedExperience == frozen.awardedExperience,
                "participant identity, order, initial level/rank or credit differs at " + i);
        }
    }

    private static void CheckViewing(World world, string label)
    {
        string before = Json(world);
        for (int visit = 0; visit < 5; visit++)
            CheckSummary(world.reports[0], BattleOutcomeSummary.Create(world.reports[0]));
        Check(Json(world) == before, "repeated viewing changed troops, land, resources, XP or record");
        var loaded = Parse<World>(before);
        CheckSummary(loaded.reports[0], BattleOutcomeSummary.Create(loaded.reports[0]));
        Check(Json(loaded) == before, "viewing a JSON-restored record changed world state");
        Console.WriteLine("PASS " + label + ": real march arrival, actual rewards, event totals, JSON and repeated read-only viewing");
    }

    private static void CheckIsolation(World world)
    {
        var record = world.reports[0]; string frozenJson = Json(record);
        var projection = BattleOutcomeSummary.Create(record);
        world.ActiveGenerals[0].level++;
        world.ActiveGenerals[0].learnedSkill = "坚守";
        world.formation.Reverse(); world.lands[record.targetId].name = "后来改名";
        Check(Json(record) == frozenJson, "current formation/level/land edits rewrote history");
        CheckSummary(record, projection);
        CheckSummary(record, BattleOutcomeSummary.Create(record));

        var source = Parse<BattleRecord>(frozenJson);
        var detached = BattleOutcomeSummary.Create(source);
        var first = source.attacker.generals[0];
        first.uid++; first.definitionId = "changed"; first.name = "后来改名";
        first.level++; first.rank++; first.awardedExperience++;
        source.attacker.generals.Clear(); source.attacker.name = "new army"; source.attacker.troops++;
        source.defender.name = "new defender"; source.defender.troops++;
        source.battleId = "new battle"; source.targetName = "new target";
        source.targetId++; source.targetX++; source.targetZ++; source.attackerFinal++; source.defenderFinal++;
        source.goldReward++; source.experienceReward++; source.won = !source.won; source.captured = !source.captured;
        source.events[1].amount++; source.events.Clear();
        CheckSummary(Parse<BattleRecord>(frozenJson), detached);

        var exposedList = detached.participants as IList<BattleOutcomeParticipant>;
        Check(exposedList == null || exposedList.IsReadOnly, "participant projection exposes a mutable list");
        if (exposedList != null)
        {
            bool rejected = false;
            try { exposedList.Clear(); } catch (NotSupportedException) { rejected = true; }
            Check(rejected && detached.participants.Count > 0, "caller can remove a projected participant");
        }
        Check(typeof(BattleOutcomeSummary).GetFields().All(f => f.IsInitOnly), "summary exposes writable fields");
        Check(typeof(BattleOutcomeParticipant).GetFields().All(f => f.IsInitOnly
            && (f.FieldType.IsValueType || f.FieldType == typeof(string))), "participant contains mutable fields/references");
        Console.WriteLine("PASS immutable projection: later live changes and later source-object edits preserve historical display facts");
    }

    public static int Main()
    {
        try
        {
            Check(BattleOutcomeSummary.Create(null) == null, "null report manufactured an outcome");
            var legacy = Parse<BattleRecord>("{\"title\":\"旧战报\",\"text\":\"造成 9999 伤害，获得 999 铜钱，经验 888\",\"won\":true,\"time\":0}");
            string oldJson = Json(legacy);
            Check(BattleOutcomeSummary.Create(legacy) == null && Json(legacy) == oldJson,
                "legacy Chinese prose was parsed into an outcome");

            var won = Fight(2000, 1, true, false);
            Check(won.reports[0].won && won.reports[0].events.Any(e => e.kind == BattleReplayEventKind.Recovery),
                "victory/recovery fixture did not produce both facts");
            CheckViewing(won, "victory with recovery");
            var lost = Fight(50, 5, false, false);
            Check(!lost.reports[0].won && lost.reports[0].attackerFinal == 0 && lost.reports[0].goldReward == 0,
                "defeat fixture did not lose all troops without gold");
            CheckViewing(lost, "defeat");
            var capped = Fight(2000, 1, false, true);
            Check(capped.reports[0].experienceReward > 0 && BattleOutcomeSummary.Create(capped.reports[0]).experienceReward == 0,
                "level-capped outcome displayed nominal XP never credited");
            CheckViewing(capped, "level-capped actual XP");

            string validJson = Json(won.reports[0]);
            var broken = Parse<BattleRecord>(validJson); broken.events[1].amount++;
            Check(BattleOutcomeSummary.Create(broken) == null, "inconsistent damage ledger supplied an outcome");
            broken = Parse<BattleRecord>(validJson); broken.replayVersion = 2;
            Check(BattleOutcomeSummary.Create(broken) == null, "unknown replay version supplied an outcome");
            broken = Parse<BattleRecord>(validJson); broken.attacker.generals[0] = null;
            Check(BattleOutcomeSummary.Create(broken) == null, "missing participant supplied an outcome");
            broken = Parse<BattleRecord>(validJson); broken.events.RemoveAt(broken.events.Count - 1);
            Check(BattleOutcomeSummary.Create(broken) == null, "unfinished record supplied a settled outcome");
            CheckIsolation(won);
            Console.WriteLine("BATTLE_OUTCOME_SUMMARY_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
