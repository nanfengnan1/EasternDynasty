using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

public static class BattleFormationSummaryChecks
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

    private static void CheckSummary(BattleRecord record, BattleFormationSummary summary)
    {
        Check(summary != null && summary.battleId == record.battleId && summary.targetName == record.targetName
            && summary.targetX == record.targetX && summary.targetZ == record.targetZ,
            "formation identity/target differs from frozen facts");
        Check(summary.attackerName == record.attacker.name && summary.defenderName == record.defender.name
            && summary.attackerInitial == record.attacker.troops && summary.defenderInitial == record.defender.troops,
            "formation army names or initial troop totals differ from frozen facts");
        Check(summary.participants.Count == record.attacker.generals.Count, "formation participant count differs");
        for (int i = 0; i < summary.participants.Count; i++)
        {
            var participant = summary.participants[i]; var frozen = record.attacker.generals[i];
            Check(participant.uid == frozen.uid && participant.definitionId == frozen.definitionId && participant.name == frozen.name
                && participant.level == frozen.level && participant.rank == frozen.rank && participant.attack == frozen.attack
                && participant.skill == frozen.skill && participant.learnedSkill == frozen.learnedSkill,
                "formation participant order, identity, attributes or carried skills differs at " + i);
        }
    }

    private static World Fight(int count)
    {
        var world = World.CreateYouzhou(); world.troops = 2000;
        world.formation.Reverse(); world.formation.RemoveRange(count, world.formation.Count - count);
        for (int i = 0; i < count; i++)
        {
            var card = world.ActiveGenerals[i]; card.level = 2 + i; card.rank = i;
            card.learnedSkill = i == 0 ? "抚军" : i == 1 ? "" : "坚守";
        }
        var original = world.ActiveGenerals.Select(c => new BattleParticipantSnapshot {
            uid = c.uid, definitionId = c.definition, name = c.Definition.name, level = c.level,
            rank = c.rank, attack = c.Definition.attack, skill = c.Definition.skill, learnedSkill = c.learnedSkill }).ToList();
        var target = world.lands.First(t => world.AttackProblem(t.id) == ""); target.level = 1;
        world.March(target.id, true);
        Check(world.order != null && world.reports.Count == 0, "fixture did not begin an actual march");
        world.Tick(world.order.remaining / 2);
        Check(world.reports.Count == 0 && !target.owned, "formation appeared before arrival");
        world.Tick(world.order.remaining);
        Check(world.reports.Count == 1 && BattleReplay.IsValid(world.reports[0]), "arrival did not settle a valid record");
        var record = world.reports[0];
        Check(Json(record.attacker.generals.Select(g => new BattleParticipantSnapshot {
            uid = g.uid, definitionId = g.definitionId, name = g.name, level = g.level, rank = g.rank,
            attack = g.attack, skill = g.skill, learnedSkill = g.learnedSkill }).ToList()) == Json(original),
            "settlement snapshot did not capture actual pre-reward formation order/skills");
        return world;
    }

    private static void CheckViewing(World world)
    {
        string before = Json(world);
        CheckSummary(world.reports[0], BattleFormationSummary.Create(world.reports[0]));
        CheckSummary(world.reports[0], BattleFormationSummary.Create(world.reports[0]));
        Check(Json(world) == before, "viewing formation changed troops, resources, land, XP or history");
        var loaded = Parse<World>(before);
        CheckSummary(loaded.reports[0], BattleFormationSummary.Create(loaded.reports[0]));
        Check(Json(loaded) == before, "viewing JSON-restored formation changed world state");
    }

    private static void CheckIsolation(World world)
    {
        var record = world.reports[0]; string frozen = Json(record);
        var summary = BattleFormationSummary.Create(record);
        world.ActiveGenerals[0].level++; world.ActiveGenerals[0].rank++;
        world.ActiveGenerals[0].learnedSkill = "突击"; world.formation.Reverse();
        world.lands[record.targetId].name = "后来改名";
        Check(Json(record) == frozen, "current formation/upgrade/skill edits rewrote the record");
        CheckSummary(record, summary);
        CheckSummary(record, BattleFormationSummary.Create(record));

        // Once created the projection also survives edits to the mutable source record itself.
        var source = Parse<BattleRecord>(frozen); var detached = BattleFormationSummary.Create(source);
        var member = source.attacker.generals[0];
        member.uid++; member.definitionId = "new-definition"; member.name = "后来改名";
        member.level++; member.rank++; member.attack++; member.skill = "突击"; member.learnedSkill = null;
        source.attacker.generals.Clear(); source.attacker.name = "new army"; source.attacker.troops++;
        source.defender.name = "new defender"; source.defender.troops++;
        source.battleId = "new battle"; source.targetName = "new target"; source.targetX++; source.targetZ++;
        source.events.Clear();
        CheckSummary(Parse<BattleRecord>(frozen), detached);
        var collection = detached.participants as IList<BattleFormationParticipant>;
        Check(collection == null || collection.IsReadOnly, "formation projection exposes a mutable collection");
        if (collection != null)
        {
            bool rejected = false;
            try { collection.Clear(); } catch (NotSupportedException) { rejected = true; }
            Check(rejected && detached.participants.Count == 3, "caller can remove frozen participants");
        }
    }

    private static void CheckMissingSkillsAndRejectedRecords(BattleRecord actual)
    {
        string frozen = Json(actual);
        var missing = Parse<BattleRecord>(frozen);
        missing.attacker.generals[0].skill = null; missing.attacker.generals[0].learnedSkill = null;
        missing.attacker.generals[1].skill = ""; missing.attacker.generals[1].learnedSkill = "";
        var summary = BattleFormationSummary.Create(missing);
        Check(summary != null && summary.participants[0].skill == null && summary.participants[0].learnedSkill == null
            && summary.participants[1].skill == "" && summary.participants[1].learnedSkill == "",
            "missing skills were invented or collapsed into explicit empty slots");
        var loaded = Parse<BattleRecord>(Json(missing));
        CheckSummary(missing, BattleFormationSummary.Create(loaded));
        // Archived identities can no longer be in today's catalog; their snapshot still stands.
        missing.attacker.generals[0].definitionId = "historical-not-in-catalog";
        Check(BattleFormationSummary.Create(missing).participants[0].definitionId == "historical-not-in-catalog",
            "formation depended on the current catalog");

        Check(BattleFormationSummary.Create(null) == null, "null report invented a formation");
        var legacy = Parse<BattleRecord>("{\"title\":\"旧战报\",\"text\":\"张飞统领9999兵，突击发动3次\",\"won\":true,\"time\":0}");
        string oldJson = Json(legacy);
        Check(BattleFormationSummary.Create(legacy) == null && Json(legacy) == oldJson, "legacy prose invented a structured formation");
        var invalid = Parse<BattleRecord>(frozen); invalid.replayVersion = 2;
        Check(BattleFormationSummary.Create(invalid) == null, "unknown replay version exposed formation facts");
        invalid = Parse<BattleRecord>(frozen); invalid.ruleVersion = "unknown-rule";
        Check(BattleFormationSummary.Create(invalid) == null, "unknown rule version exposed formation facts");
        invalid = Parse<BattleRecord>(frozen); invalid.events[1].amount++;
        Check(BattleFormationSummary.Create(invalid) == null, "invalid event ledger exposed formation facts");
        invalid = Parse<BattleRecord>(frozen); invalid.attacker.generals[0] = null;
        Check(BattleFormationSummary.Create(invalid) == null, "missing participant exposed a partial formation");
        invalid = Parse<BattleRecord>(frozen); invalid.events.RemoveAt(invalid.events.Count - 1);
        Check(BattleFormationSummary.Create(invalid) == null, "unfinished settlement exposed formation facts");
    }

    public static int Main()
    {
        try
        {
            World three = null;
            for (int count = 1; count <= 3; count++)
            {
                var world = Fight(count); CheckViewing(world);
                if (count == 3) three = world;
                Console.WriteLine("PASS " + count + " participants: actual march settlement, frozen order/skills, JSON and read-only viewing");
            }
            CheckIsolation(three);
            CheckMissingSkillsAndRejectedRecords(three.reports[0]);
            Console.WriteLine("PASS immutable formation: later upgrades, source edits, absent/empty skills, legacy and invalid records");
            Console.WriteLine("BATTLE_FORMATION_SUMMARY_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
