using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

internal static class ReinforcementChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static string Json(World world)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return Encoding.UTF8.GetString(stream.ToArray()); } }
    private static World Copy(World world)
    { using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json(world)))) return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream); }
    private static double[] Values(Supplies value)
    { return new[] { value.grain, value.grass, value.stone, value.iron, value.gold, value.jade }; }
    private static void Same(Supplies actual, Supplies expected, string context)
    { Check(Values(actual).Zip(Values(expected), (a, b) => Math.Abs(a - b) < 0.00000001).All(v => v), context); }
    private static void Rejected(ReinforcementPlan plan, World world, string context)
    {
        string before = Json(world), message; ReinforcementReceipt receipt;
        Check(!plan.TryExecute(world, out receipt, out message) && receipt == null && message.Length > 0,
            context + " has a reason and no receipt");
        Check(Json(world) == before, context + " cannot charge, recruit or progress a task");
    }
    private static void Deployed(World world)
    {
        var target = world.lands.First(t => world.AttackProblem(t.id) == "");
        world.March(target.id);
        Check(world.order != null, "real march creates busy state");
    }

    public static int Main()
    {
        try
        {
            string message; ReinforcementReceipt receipt;
            var absent = ReinforcementPlan.Create(null, 10);
            Check(!absent.CanExecute && !absent.TryExecute(null, out receipt, out message) && receipt == null,
                "no campaign cannot execute a confirmation");
            foreach (int requested in new[] { 1, 7, 99, 137, 200, int.MaxValue })
            {
                var world = World.CreateYouzhou(); string before = Json(world);
                var plan = ReinforcementPlan.Create(world, requested);
                int actual = Math.Min(requested, world.TroopCap - world.troops);
                Check(Json(world) == before, "preview is read only for " + requested);
                Check(plan.CanExecute && plan.requested == requested && plan.amount == actual
                    && plan.beforeTroops == world.troops && plan.capacity == world.TroopCap
                    && plan.affordable == world.TroopCap - world.troops, "exact frozen preview for " + requested);
                Same(plan.cost, World.RecruitmentCost(actual), "shared cost for " + requested);
                var legacyCommand = Copy(world); legacyCommand.Recruit(requested);
                Check(plan.TryExecute(world, out receipt, out message), "confirmed command executes " + requested);
                Check(receipt.beforeTroops == plan.beforeTroops && receipt.afterTroops == world.troops
                    && receipt.amount == actual && receipt.capacity == world.TroopCap, "receipt reflects actual soldiers " + requested);
                Same(receipt.cost, World.RecruitmentCost(actual), "receipt reflects actual resources " + requested);
                Check(Json(world) == Json(legacyCommand), "existing Recruit behavior and quest accounting preserved " + requested);
                Check(!plan.CanExecute, "successful confirmation disables itself " + requested);
                Rejected(plan, world, "replayed confirmation " + requested);
                Check(world.cityRecruitActions == 1, "only one recruit action counted " + requested);
                string problem; Check(world.IsValid(out problem), "recruited campaign remains valid " + problem);
            }
            {
                var world = World.CreateYouzhou(); world.troops = world.TroopCap - 37;
                var plan = ReinforcementPlan.Create(world, 200);
                Check(plan.amount == 37 && plan.affordable == 37 && plan.cost.grain == 37, "partial gap crops request and fee together");
                Check(plan.TryExecute(world, out receipt, out message) && world.troops == world.TroopCap
                    && receipt.amount == 37, "partial gap fills exactly without overcharging");
                foreach (int count in new[] { 0, 1, int.MaxValue })
                {
                    plan = ReinforcementPlan.Create(world, count);
                    Check(!plan.CanExecute && plan.amount == 0 && plan.affordable == 0
                        && plan.problem.Contains("兵力已达上限"), "full army has full-capacity reason " + count);
                    Rejected(plan, world, "full army " + count);
                }
            }
            foreach (int count in new[] { 0, -1, int.MinValue })
            {
                var world = World.CreateYouzhou(); var plan = ReinforcementPlan.Create(world, count);
                Check(!plan.CanExecute && plan.amount == 0 && plan.problem.Contains("大于零"), "nonpositive request explained " + count);
                Rejected(plan, world, "invalid requested count " + count);
            }
            {
                var world = World.CreateYouzhou(); world.resources.grain = 25;
                var plan = ReinforcementPlan.Create(world, 100);
                Check(!plan.CanExecute && plan.affordable == 25 && plan.amount == 100 && plan.cost.grain == 100,
                    "insufficient preview preserves full requested cost and affordable alternative");
                Rejected(plan, world, "insufficient resources");
                world.resources.grain = 200;
                Check(plan.TryExecute(world, out receipt, out message) && receipt.amount == 100,
                    "current balance may recover without silently changing approved count");
            }
            {
                var world = World.CreateYouzhou(); var plan = ReinforcementPlan.Create(world, 100);
                world.resources.iron = 0; Rejected(plan, world, "resources depleted after confirmation");
                world.resources.iron = 900; world.Tick(1);
                Check(plan.TryExecute(world, out receipt, out message) && receipt.amount == 100,
                    "ordinary income does not invalidate an unchanged army");
            }
            {
                var world = World.CreateYouzhou(); var plan = ReinforcementPlan.Create(world, 100);
                Deployed(world); Rejected(plan, world, "army departs after preview");
                Check(!plan.CanExecute, "changed army permanently expires confirmation");
                world.RecallArmy(); world.Tick(1000); Rejected(plan, world, "expired confirmation after army returns");
                var current = ReinforcementPlan.Create(world, 100); Deployed(world);
                current = ReinforcementPlan.Create(world, 0);
                Check(!current.CanExecute && current.problem.Contains("军令"), "busy reason precedes zero requested count");
                Rejected(current, world, "preview created during real march");
                world.RecallArmy(); world.Tick(1000); Rejected(current, world, "busy preview cannot become approval upon return");
            }
            {
                var world = World.CreateYouzhou(); world.Recruit(int.MaxValue); Deployed(world);
                var plan = ReinforcementPlan.Create(world, 0);
                Check(!plan.CanExecute && plan.problem.Contains("军令"), "busy reason precedes full army and zero count");
                Rejected(plan, world, "full deployed army");
            }
            {
                var world = World.CreateYouzhou(); var plan = ReinforcementPlan.Create(world, 100);
                world.Recruit(1); Rejected(plan, world, "troops changed after preview");
                world = World.CreateYouzhou(); plan = ReinforcementPlan.Create(world, 100);
                world.ActiveGenerals[0].level++; Rejected(plan, world, "general growth changes approved capacity");
                world = World.CreateYouzhou(); plan = ReinforcementPlan.Create(world, 100);
                world.formation.Reverse(); Rejected(plan, world, "same-capacity formation order changed");
                world = World.CreateYouzhou(); plan = ReinforcementPlan.Create(world, 100);
                var replacement = world.AddCard(world.ActiveGenerals[0].definition);
                world.formation[0] = replacement.uid;
                Check(world.TroopCap == plan.capacity, "replacement test retains same numerical capacity");
                Rejected(plan, world, "different participant identity with same capacity");
                world = World.CreateYouzhou(); plan = ReinforcementPlan.Create(world, 100);
                Rejected(plan, Copy(world), "same save reloaded into different session");
            }
            {
                var world = World.CreateYouzhou(); var plan = ReinforcementPlan.Create(world, 7);
                plan.cost.grain = 0; plan.cost.grass = 999999;
                Check(plan.TryExecute(world, out receipt, out message), "presentation mutation does not alter payment authority");
                Same(receipt.cost, World.RecruitmentCost(7), "receipt is computed from real resource change");
                string before = Json(world); receipt.cost.grain = 999;
                Check(Json(world) == before, "receipt resource snapshot has no reference into campaign");
            }
            // The shared existing Spend command accepts a 0.00001 tolerance.
            // Check each resource boundary against that command, not division-rounded estimates.
            foreach (int resource in new[] { 0, 1, 3 })
            foreach (int count in new[] { 1, 3, 7, 137 })
            foreach (double offset in new[] { -0.00002, -0.000005, 0, 0.000005 })
            {
                var world = World.CreateYouzhou(); var cost = World.RecruitmentCost(count);
                double balance = Values(cost)[resource] + offset;
                if (resource == 0) world.resources.grain = balance;
                if (resource == 1) world.resources.grass = balance;
                if (resource == 3) world.resources.iron = balance;
                string before = Json(world); var plan = ReinforcementPlan.Create(world, count);
                int expected = offset < -0.00001 ? count - 1 : count;
                Check(plan.affordable == expected && plan.CanExecute == (count == expected)
                    && Json(world) == before, "fractional affordable boundary " + resource + "/" + count + "/" + offset);
                var direct = Copy(world); int troops = direct.troops; direct.Recruit(count);
                bool success = plan.TryExecute(world, out receipt, out message);
                Check(success == (direct.troops > troops) && Json(world) == Json(direct),
                    "preview and execution match existing Spend at boundary " + resource + "/" + count + "/" + offset);
            }
            {
                var legacy = World.Create(Homeland.Han, 4, "Legacy"); var direct = Copy(legacy);
                var plan = ReinforcementPlan.Create(legacy, 123); direct.Recruit(123);
                Check(plan.TryExecute(legacy, out receipt, out message) && Json(legacy) == Json(direct),
                    "legacy map retains previous recruitment behavior");
                var world = World.CreateYouzhou(); string before = Json(world);
                plan = ReinforcementPlan.Create(world, 17);
                Check(Json(Copy(world)) == before && !before.Contains("Reinforcement")
                    && !typeof(World).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Any(f => f.FieldType == typeof(ReinforcementPlan) || f.FieldType == typeof(ReinforcementReceipt)),
                    "preview adds no persistent fields and roundtrip shape is unchanged");
                Check(!typeof(ReinforcementPlan).IsSerializable && !typeof(ReinforcementReceipt).IsSerializable,
                    "confirmation and receipt are explicitly transient application models");
            }
            Console.WriteLine("PASS: " + checks + " reinforcement checks."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
