using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using EasternDynasty;

public static class TrainingChecks
{
    private static int checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static void Valid(World world) { string problem; Check(world.IsValid(out problem), problem); }
    private static World Fresh() { return World.CreateYouzhou(); }
    private static int Target(World world)
    { var tile = world.lands.First(t => world.AttackProblem(t.id) == ""); tile.owned = true; return tile.id; }
    private static int Experience(GeneralCard card)
    { return card.experience + 40 * card.level * (card.level - 1); }
    private static string Json(World world)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return Encoding.UTF8.GetString(stream.ToArray()); } }
    private static World Parse(string json)
    { using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream); }
    private static World Copy(World world) { return Parse(Json(world)); }
    private static void Rejected(World world, int target, string context)
    {
        string before = Json(world);
        Check(world.TrainingProblem(target).Length > 0, context + " should explain rejection");
        world.StartTraining(target);
        Check(Json(world) == before, context + " changed state or charged a cost");
    }

    public static int Main()
    {
        try
        {
            Check((int)ArmyAction.Attack == 0 && (int)ArmyAction.Garrison == 1 && (int)ArmyAction.Farm == 2 && (int)ArmyAction.Training == 3, "persisted action values changed");
            {
                var world = Fresh(); int target = Target(world); int xp = world.TrainingExperience(target);
                var ids = world.formation.ToList(); var before = ids.Select(id => Experience(world.cards.Find(c => c.uid == id))).ToList();
                var resources = world.resources; double grain = resources.grain, gold = resources.gold;
                Check(world.TrainingProblem(target) == "" && xp == world.lands[target].level * 100, "legal training target rejected or project reward wrong");
                world.StartTraining(target); double travel = world.order.duration;
                Check(world.AvailableCommands == World.CommandCap - World.TrainingCommandCost && grain == resources.grain && gold == resources.gold, "training cost is not exactly two commands");
                Check(world.order.action == (int)ArmyAction.Training && world.order.phase == 0 && world.order.trainingParticipants.SequenceEqual(ids), "outbound training snapshot missing");
                Check(ids.Select(id => Experience(world.cards.Find(c => c.uid == id))).SequenceEqual(before), "training awarded instant XP");
                Rejected(world, target, "duplicate submit"); Valid(world);
                world.Tick(travel);
                Check(world.order.phase == (int)ArmyPhase.Working && world.order.remaining == World.TrainingSeconds && world.cityTrainingCompletions == 0, "arrival did not start timed work");
                world.Tick(World.TrainingSeconds - 1);
                Check(world.LastTrainingReceipt == null && ids.Select(id => Experience(world.cards.Find(c => c.uid == id))).SequenceEqual(before), "unfinished work awarded XP");
                world.Tick(1);
                Check(world.order.returning && world.order.trainingSettled && !world.order.trainingCancelled && world.cityTrainingCompletions == 1, "completion did not settle and return");
                var receipt = world.LastTrainingReceipt;
                Check(receipt != null && !receipt.cancelled && receipt.target == target && receipt.experiencePerGeneral == xp && receipt.participantIds.SequenceEqual(ids) && receipt.awardedExperience.All(x => x == xp), "completion receipt incorrect");
                for (int i = 0; i < ids.Count; i++) Check(Experience(world.cards.Find(c => c.uid == ids[i])) == before[i] + xp, "participant XP mismatch");
                Valid(world); world = Copy(world); world.Tick(travel + 10);
                Check(world.order == null && world.cityTrainingCompletions == 1 && world.trainingHistory.Count == 1, "return or reload settled twice"); Valid(world);
            }
            foreach (bool whileWorking in new[] { false, true })
            {
                var world = Fresh(); int target = Target(world); int before = world.cards.Sum(Experience);
                world.StartTraining(target); world.Tick(whileWorking ? world.order.remaining + 20 : world.order.remaining / 2);
                double x, z, rx, rz; world.ArmyPosition(out x, out z); world.RecallArmy(); world.ArmyPosition(out rx, out rz);
                Check(Math.Abs(x-rx) < .00001 && Math.Abs(z-rz) < .00001, "recall teleported training army");
                Check(world.LastTrainingReceipt.cancelled && world.LastTrainingReceipt.awardedExperience.All(n => n == 0), "cancellation receipt missing or granted reward");
                world.RecallArmy(); Valid(world); world = Copy(world); world.Tick(1000);
                Check(world.order == null && world.cards.Sum(Experience) == before && world.cityTrainingCompletions == 0 && world.trainingHistory.Count == 1 && world.AvailableCommands == 18, "cancel/refetch duplicated reward or refunded command"); Valid(world);
            }
            {
                var world = Fresh(); int target = Target(world);
                Rejected(world, -1, "negative target"); Rejected(world, world.lands.Count, "out-of-range target");
                Rejected(world, world.homeId, "home target");
                Rejected(world, world.lands.First(t => world.IsCityDistrict(t.id) && !t.home).id, "city district");
                world.lands[target].owned = false; Rejected(world, target, "unowned target"); world.lands[target].owned = true;
                var city = world.lands.First(t => t.capital); city.owned = true; Rejected(world, city.id, "capital");
                var bridge = world.lands.First(t => t.bridge); bridge.owned = true; Rejected(world, bridge.id, "bridge");
                var water = world.lands.First(t => !t.Walkable); water.owned = true; Rejected(world, water.id, "water or mountain");
                world.troops = 49; Rejected(world, target, "low troops"); world.troops = 450;
                var formation = world.formation.ToList(); world.formation.Clear(); Rejected(world, target, "no generals"); world.formation = formation;
                world.EnsureSeasonState(); world.commands = 1; Rejected(world, target, "low commands"); world.commands = 20;
                foreach (var card in world.ActiveGenerals) { card.level = card.LevelCap; card.experience = 0; }
                Rejected(world, target, "all generals at cap");
                world = World.Create(Homeland.Han, 4, "Legacy"); Rejected(world, world.homeId, "legacy campaign");
            }
            {
                var world = Fresh(); int target = Target(world); var home = world.lands[world.homeId];
                foreach (var tile in world.lands.Where(t => world.AreAdjacent(home.id, t.id))) tile.kind = LandKind.Mountain;
                Rejected(world, target, "unreachable target");
            }
            {
                var world = Fresh(); int target = Target(world); var first = world.ActiveGenerals[0];
                first.level = first.LevelCap - 1; first.experience = first.level * 80 - 3;
                foreach (var card in world.ActiveGenerals.Skip(1)) { card.level = card.LevelCap; card.experience = 0; }
                world.StartTraining(target); world.Tick(1000);
                Check(first.level == first.LevelCap && first.experience == 0 && world.LastTrainingReceipt.awardedExperience[0] == 3, "training overshot cap or receipt overstated gain");
                Check(world.LastTrainingReceipt.awardedExperience.Skip(1).All(x => x == 0), "capped participant gained XP"); Valid(world);
            }
            {
                var world = Fresh(); int target = Target(world); world.StartTraining(target);
                var participants = world.order.trainingParticipants.ToList(); int frozenXp = world.order.trainingExperience;
                var replacement = world.AddCard(world.cards[0].definition); world.formation = new List<int> { replacement.uid };
                var original = world.cards.Find(c => c.uid == participants[0]); int before = Experience(original); double gold = world.resources.gold;
                world.Train(original.uid); world.Promote(original.uid); world.Dismantle(original.uid); world.Teach(original.uid, "unknown");
                Check(Experience(original) == before && world.resources.gold == gold && world.cards.Contains(original), "frozen participant allowed mutation");
                world.lands[target].level = world.lands[target].level == 5 ? 1 : 5;
                world.Tick(1000);
                Check(world.LastTrainingReceipt.participantIds.SequenceEqual(participants) && world.LastTrainingReceipt.experiencePerGeneral == frozenXp && Experience(original) == before + frozenXp && Experience(replacement) == 0, "formation/land mutation redirected training reward"); Valid(world);
            }
            foreach (bool whileWorking in new[] { false, true })
            {
                var world = Fresh(); int target = Target(world); int before = world.cards.Sum(Experience);
                world.StartTraining(target); if (whileWorking) world.Tick(world.order.remaining + 1);
                world.lands[target].owned = false; world.Tick(1000);
                Check(world.LastTrainingReceipt.cancelled && world.cards.Sum(Experience) == before && world.cityTrainingCompletions == 0 && world.order == null, "lost target awarded training or stranded army"); Valid(world);
            }
            {
                var world = Fresh(); int target = Target(world); world.StartTraining(target);
                world.Tick(world.order.duration / 2); var restored = Copy(world); Valid(restored);
                Check(restored.order.trainingParticipants.SequenceEqual(world.order.trainingParticipants) && restored.order.trainingExperience == world.order.trainingExperience, "outbound JSON snapshot changed");
                world.Tick(world.order.remaining + 19); restored = Copy(world); Valid(restored);
                Check(restored.order.phase == (int)ArmyPhase.Working && restored.order.remaining == world.order.remaining, "working JSON timer changed");
                var stepped = Copy(restored); restored.Tick(500); for (int i = 0; i < 1000; i++) stepped.Tick(.5);
                Check(restored.cards.Select(Experience).SequenceEqual(stepped.cards.Select(Experience)) && restored.cityTrainingCompletions == 1 && stepped.cityTrainingCompletions == 1 && restored.order == null && stepped.order == null, "large offline tick differs from small ticks");
                Check(restored.LastTrainingReceipt.completedAt == stepped.LastTrainingReceipt.completedAt, "offline completion timestamp differs"); Valid(restored); Valid(stepped);
            }
            {
                var world = Fresh(); int target = Target(world); world.Garrison(target);
                string old = Json(world);
                old = Regex.Replace(old, ",?\"trainingHistory\":\\[\\]", "");
                old = Regex.Replace(old, ",?\"cityTrainingCompletions\":0", "");
                old = Regex.Replace(old, ",?\"training(?:Version|Experience|Participants|Settled|Cancelled)\":(?:false|0|null|\\[\\])", "");
                Check(!old.Contains("training") && !old.Contains("cityTraining"), "old-save fixture did not remove new fields");
                var restored = Parse(old); Valid(restored);
                Check(restored.trainingHistory == null && restored.order.trainingParticipants == null, "old-save missing optional collections were not exercised");
                restored.EnsureSeasonState();
                Check(restored.trainingHistory != null && restored.trainingHistory.Count == 0 && restored.order.trainingParticipants != null && restored.order.trainingParticipants.Count == 0, "old-save collection normalization failed");
                restored.Tick(1000); restored.RecallArmy(); restored.Tick(1000);
                Check(restored.LastTrainingReceipt == null && restored.cityTrainingCompletions == 0, "old save fabricated training state");
                restored.StartTraining(target); restored.Tick(1000); Check(restored.LastTrainingReceipt != null && restored.cityTrainingCompletions == 1, "old save cannot start new training"); Valid(restored);
            }
            {
                var world = Fresh(); int target = Target(world); world.StartTraining(target);
                var broken = Copy(world); broken.order.trainingVersion = 0; string problem; Check(!broken.IsValid(out problem), "training missing snapshot accepted");
                broken = Copy(world); broken.order.trainingParticipants.Add(broken.order.trainingParticipants[0]); Check(!broken.IsValid(out problem), "duplicate participants accepted");
                broken = Copy(world); broken.order.trainingSettled = true; Check(!broken.IsValid(out problem), "premature settlement accepted");
                broken = Copy(world); broken.order.trainingExperience = -1; Check(!broken.IsValid(out problem), "negative reward accepted");
                world.Tick(1000); broken = Copy(world); broken.LastTrainingReceipt.awardedExperience[0] = 501; Check(!broken.IsValid(out problem), "invalid receipt accepted");
                for (int i = 0; i < 10; i++) { world.commands = 20; world.StartTraining(target); world.Tick(1000); }
                Check(world.trainingHistory.Count == 10 && world.cityTrainingCompletions == 11, "training history bound or completion count wrong"); Valid(world);
            }
            Console.WriteLine("TRAINING_CHECKS_OK: " + checks); return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}
