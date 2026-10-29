using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

public static class TrainingPlanPreviewChecks
{
    private static int checks;
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static void Near(double actual, double expected, string message)
    { Check(Math.Abs(actual - expected) < .000001, message + ": " + actual + " / " + expected); }
    private static string Json(World world)
    {
        using (var stream = new MemoryStream())
        { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return Encoding.UTF8.GetString(stream.ToArray()); }
    }
    private static World Copy(World world)
    {
        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json(world))))
            return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream);
    }
    private static int Target(World world, int level)
    {
        var target = world.lands.First(t => world.AttackProblem(t.id) == "");
        target.owned = true; target.level = level; return target.id;
    }
    private static int LifetimeExperience(GeneralCard card)
    { return card.experience + 40 * card.level * (card.level - 1); }
    private static TrainingPlanPreview ReadOnlyPreview(World world, int target)
    {
        string before = Json(world); TrainingPlanPreview preview = null;
        for (int visit = 0; visit < 3; visit++) preview = TrainingPlanPreview.Create(world, target);
        Check(Json(world) == before, "preview changed full World, including logs, timers or optional save fields");
        Check(preview.Target == target && preview.Participants.Select(p => p.Uid).SequenceEqual(world.ActiveGenerals.Select(c => c.uid)),
            "preview lost target or formation ordering");
        Check(preview.TotalExperience == preview.Participants.Sum(p => p.AwardedExperience), "total differs from actual per-general preview");
        return preview;
    }
    private static void CompareExecution(World world, int target, string label)
    {
        var preview = ReadOnlyPreview(world, target);
        Check(preview.Reachable && world.TrainingProblem(target) == "", "execution fixture is ineligible: " + label);
        var actual = Copy(world);
        var before = actual.ActiveGenerals.ToDictionary(c => c.uid, LifetimeExperience);
        actual.StartTraining(target);
        Check(actual.order != null && actual.order.action == (int)ArmyAction.Training, "real command did not start: " + label);
        Near(preview.MarchSeconds, actual.order.duration, "preview does not match real outbound timer");
        actual.Tick(preview.MarchSeconds);
        Check(actual.order.phase == (int)ArmyPhase.Working && actual.LastTrainingReceipt == null, "arrival awarded experience early");
        Near(preview.WorkSeconds, actual.order.remaining, "preview does not match real working timer");
        actual.Tick(preview.WorkSeconds);
        var receipt = actual.LastTrainingReceipt;
        Check(receipt != null && !receipt.cancelled && receipt.experiencePerGeneral == preview.ConfiguredExperience,
            "real receipt differs from configured reward");
        Check(receipt.participantIds.SequenceEqual(preview.Participants.Select(p => p.Uid)), "receipt participants differ from preparation");
        for (int i = 0; i < preview.Participants.Count; i++)
        {
            var participant = preview.Participants[i]; var card = actual.cards.Find(c => c.uid == participant.Uid);
            Check(participant.DefinitionId == card.definition && participant.LevelAfter == card.level && participant.ExperienceAfter == card.experience,
                "projected post-training growth differs from Core settlement: " + label);
            Check(participant.AwardedExperience == receipt.awardedExperience[i]
                && participant.AwardedExperience == LifetimeExperience(card) - before[card.uid], "projected credit differs from actual credit: " + label);
        }
        Near(preview.TotalSeconds, preview.MarchSeconds + preview.WorkSeconds + actual.order.remaining,
            "round trip preview differs from actual outbound, work and return timers");
        actual.Tick(actual.order.remaining);
        Check(actual.order == null && actual.trainingHistory.Count == 1 && actual.cityTrainingCompletions == 1,
            "planned finish did not return exactly once");
        Console.WriteLine("PASS " + label + ": immutable preview compared with real StartTraining, Tick, receipt and return");
    }
    private static World FlatWorld()
    {
        var world = World.CreateYouzhou();
        foreach (var tile in world.lands) { tile.kind = LandKind.Grain; tile.bridge = false; }
        return world;
    }

    public static int Main()
    {
        try
        {
            Check(TrainingPlanPreview.Create(null, 0) == null, "null world produced a preparation");
            foreach (int level in new[] { 1, 2, 3, 4, 5 })
            {
                var world = World.CreateYouzhou(); int target = Target(world, level);
                foreach (var card in world.ActiveGenerals) { card.level = 1; card.experience = 70; }
                var preview = ReadOnlyPreview(world, target);
                Check(preview.ConfiguredExperience == level * 100 && preview.Participants.All(p => p.LevelAfter > p.LevelBefore),
                    "target-level reward or cross-level preview missing");
                if (level == 5) Check(preview.Participants.All(p => p.LevelAfter == 4 && p.ExperienceAfter == 90),
                    "multiple-level preview failed its independent expected growth");
                CompareExecution(world, target, "target level " + level + " and level crossing");
            }
            {
                var world = World.CreateYouzhou(); int target = Target(world, 5);
                var generals = world.ActiveGenerals;
                generals[0].level = generals[0].LevelCap - 1; generals[0].experience = generals[0].level * 80 - 3;
                foreach (var card in generals.Skip(1)) { card.level = card.LevelCap; card.experience = 0; }
                var preview = ReadOnlyPreview(world, target);
                Check(preview.TotalExperience == 3 && preview.Participants[0].AwardedExperience == 3
                    && preview.Participants[0].LevelAfter == preview.Participants[0].LevelCap
                    && preview.Participants[0].ExperienceAfter == 0 && preview.Participants.Skip(1).All(p => p.AwardedExperience == 0),
                    "cap preview overstates award or excludes capped participants");
                CompareExecution(world, target, "mixed near-cap and capped generals");
                foreach (var card in world.ActiveGenerals) { card.level = card.LevelCap; card.experience = 0; }
                preview = ReadOnlyPreview(world, target);
                Check(preview.Reachable && preview.TotalExperience == 0 && world.TrainingProblem(target).Length > 0,
                    "all-capped planning lost route or invented rewards");
                string before = Json(world); world.StartTraining(target);
                Check(Json(world) == before, "all-capped command changed World");
            }
            {
                var world = World.CreateYouzhou(); int target = Target(world, 3);
                world.EnsureSeasonState(); world.commands = 1;
                var preview = ReadOnlyPreview(world, target);
                Check(preview.Reachable && preview.TotalExperience > 0 && world.TrainingProblem(target).Length > 0,
                    "missing commands prevented informative preparation");
                string before = Json(world); world.StartTraining(target);
                Check(Json(world) == before, "preview bypassed command cost");
                world.commands = World.CommandCap;
                CompareExecution(world, target, "prepared while commands were insufficient");
            }
            {
                var world = FlatWorld(); var home = world.lands[world.homeId];
                var target = world.TryGetLand(home.x + 4, home.z); target.owned = true;
                var direct = ReadOnlyPreview(world, target.id);
                for (int z = 0; z < 40; z++) world.TryGetLand(home.x + 2, z).kind = LandKind.Water;
                var bridge = world.TryGetLand(home.x + 2, home.z + 4); bridge.kind = LandKind.Grain; bridge.bridge = true;
                var detour = ReadOnlyPreview(world, target.id);
                Check(detour.Reachable && detour.MarchSeconds > direct.MarchSeconds
                    && world.FindMarchPath(home.id, target.id).Contains(bridge.id), "preview ignores bridge detour");
                CompareExecution(world, target.id, "bridge detour uses actual march time");
                bridge.kind = LandKind.Water; bridge.bridge = false;
                var blocked = ReadOnlyPreview(world, target.id);
                Check(!blocked.Reachable && double.IsPositiveInfinity(blocked.MarchSeconds) && double.IsPositiveInfinity(blocked.TotalSeconds),
                    "unreachable route masquerades as a zero-second trip");
                string before = Json(world); world.StartTraining(target.id);
                Check(world.order == null && Json(world) == before, "unreachable preview enabled a command");
            }
            {
                var world = World.CreateYouzhou(); int target = Target(world, 2);
                foreach (int invalid in new[] { -1, world.lands.Count, world.homeId })
                {
                    var preview = ReadOnlyPreview(world, invalid);
                    Check(!preview.Reachable && preview.ConfiguredExperience == 0 && preview.TotalExperience == 0
                        && double.IsPositiveInfinity(preview.TotalSeconds), "invalid target produced usable training preview");
                }
                world.lands[target].owned = false;
                Check(!ReadOnlyPreview(world, target).Reachable, "unowned target produced usable preview");
                world.lands[target].owned = true;
                // Exercise old-save optional nulls without normalizing or adding save state while browsing.
                world.trainingHistory = null; world.seasonMechanicsVersion = 0;
                ReadOnlyPreview(world, target);
                Check(world.trainingHistory == null && world.seasonMechanicsVersion == 0, "preview migrated old save on read");
            }
            {
                var world = World.CreateYouzhou(); int target = Target(world, 5);
                var preview = ReadOnlyPreview(world, target); var first = preview.Participants[0];
                int previousCount = preview.Participants.Count, previousTotal = preview.TotalExperience;
                world.ActiveGenerals[0].level++; world.ActiveGenerals[0].experience += 50;
                world.ActiveGenerals[0].definition = world.cards.Last().definition;
                world.formation.Clear(); world.lands[target].level = 1;
                Check(preview.Participants.Count == previousCount && preview.TotalExperience == previousTotal
                    && first.LevelBefore == 1 && first.ExperienceBefore == 0 && preview.ConfiguredExperience == 500,
                    "live formation or card edits changed detached preview");
                var list = preview.Participants as IList<TrainingParticipantPreview>;
                Check(list == null || list.IsReadOnly, "participant collection is writable");
                if (list != null)
                {
                    bool rejected = false;
                    try { list.Clear(); } catch (NotSupportedException) { rejected = true; }
                    Check(rejected && preview.Participants.Count == previousCount, "caller removed a preview participant");
                }
                Check(typeof(TrainingPlanPreview).GetFields().All(f => f.IsInitOnly), "preview exposes mutable fields");
                Check(typeof(TrainingParticipantPreview).GetFields().All(f => f.IsInitOnly
                    && (f.FieldType.IsValueType || f.FieldType == typeof(string))), "participant references a mutable live card");
            }
            Console.WriteLine("TRAINING_PLAN_PREVIEW_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
