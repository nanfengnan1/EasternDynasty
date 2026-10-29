using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EasternDynasty;

internal static class CityQuestObjectivePlanChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static string Json(World world)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return Encoding.UTF8.GetString(stream.ToArray()); } }
    private static World Restore(World world)
    { using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Json(world)))) return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream); }
    private static CityQuestObjectivePlan Plan(World world, int id)
    {
        string before = Json(world);
        var plan = CityQuestObjectivePlan.Create(world, id);
        Check(Json(world) == before, "detail preserves complete serialized World for task " + id);
        if (plan != null)
        {
            Check(plan.Route == null || plan.Current && !plan.Ready && !plan.Claimed, "navigation belongs only to incomplete current task " + id);
            Check(plan.Cost == null || plan.Route != null, "inactive task has no executable quote " + id);
            Check(!string.IsNullOrEmpty(plan.RequirementTitle) && !string.IsNullOrEmpty(plan.RequirementDetail)
                && !string.IsNullOrEmpty(plan.ActionLabel) && plan.ActionHint.Length <= 100, "task has concise usable detail " + id);
        }
        return plan;
    }
    private static double[] Values(Supplies value)
    { return new[] { value.grain, value.grass, value.stone, value.iron, value.gold, value.jade }; }
    private static void Same(Supplies actual, Supplies expected, string name)
    { Check(actual != null && expected != null && Values(actual).SequenceEqual(Values(expected)), name); }
    private static void Claim(World world, int id)
    {
        string message;
        var receipt = CityQuestClaimReceipt.Claim(world, id, out message);
        Check(receipt != null && receipt.questId == id, "real claim advances task " + id);
    }
    private static void Build(World world, CityBuilding building)
    {
        var existing = world.cityPlots.FirstOrDefault(p => p.building == building);
        int plot = existing == null ? world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index : existing.index;
        world.StartCityConstruction(plot, building);
        Check(world.CityJob != null && world.CityJob.building == building, "real construction starts " + building);
        world.Tick(world.CityJob.remaining + .01);
    }
    private static int Capture(World world)
    {
        world.Recruit(world.TroopCap);
        var target = world.lands.Where(t => !t.capital && !t.bridge && !world.IsCityDistrict(t.id) && world.AttackProblem(t.id) == "").OrderBy(t => t.level).First();
        world.March(target.id); world.Tick(1000);
        Check(target.owned && world.order == null, "actual capture and return at " + target.id);
        return target.id;
    }

    public static int Main()
    {
        try
        {
            Check(CityQuestObjectivePlan.Create(null, 1) == null, "missing campaign has no task detail");
            Check(Plan(World.Create(Homeland.Han, 4, "Legacy"), 1) == null, "legacy map does not expose S1 task detail");
            for (int spawn = 0; spawn < 3; spawn++)
            {
                var initial = World.CreateYouzhou(spawn);
                var first = Plan(initial, 1);
                Check(first.QuestId == 1 && first.CurrentQuestId == 1 && first.ChapterIndex == 0
                    && first.Current && !first.Ready && !first.Claimed && first.Progress == 0 && first.Target == 1,
                    "initial task identity and actual progress at spawn " + spawn);
                Check(first.Route.plotIndex == 4 && first.Route.building == CityBuilding.TownHall
                    && first.Cost == null && first.EstimatedSeconds == 0, "visit opens actual hall without invented construction payment");
                Same(first.Reward, CityCatalog.Quests[0].reward, "reward comes from project task catalog");
                var future = Plan(initial, 4);
                Check(future.Progress == 3 && !future.Ready && !future.Current && future.Route == null
                    && future.Cost == null && future.CurrentQuestId == 1, "future achieved objective still points to current task");
                Check(Plan(initial, 0) == null && Plan(initial, 11) == null, "invalid stable task IDs have no detail");
            }

            var uninitialised = World.CreateYouzhou();
            uninitialised.cityVersion = 0; uninitialised.cityPlots = null;
            var unmigrated = Plan(uninitialised, 1);
            Check(unmigrated.Route == null && unmigrated.Cost == null && uninitialised.cityVersion == 0
                && uninitialised.cityPlots == null, "read-only detail never migrates a legacy city state");

            var world = World.CreateYouzhou();
            world.RecordCityAction("visit");
            var ready = Plan(world, 1);
            Check(ready.Ready && ready.Progress == 1 && ready.Route == null && ready.Cost == null, "real visit makes reward ready without opening another action");
            string before = Json(world);
            ready.Reward.grain = 999999;
            Check(Json(world) == before && CityCatalog.Quests[0].reward.grain == 300, "reward projection cannot mutate campaign or shared catalog");
            Claim(world, 1);
            var claimed = Plan(world, 1);
            Check(claimed.Claimed && !claimed.Current && !claimed.Ready && claimed.CurrentQuestId == 2
                && claimed.Route == null && claimed.Cost == null, "claimed detail retains history and identifies next objective");
            var mill = Plan(world, 2);
            Check(mill.Building == CityBuilding.Mill && mill.Route.plotIndex != 4
                && world.CityBuildProblem(mill.Route.plotIndex, mill.Building) == "", "current mill detail selects actual usable construction position");
            Same(mill.Cost, world.CityBuildCost(CityBuilding.Mill, 1), "mill quote uses Core construction cost");
            Check(mill.EstimatedSeconds == world.CityBuildSeconds(CityBuilding.Mill, 1)
                && mill.CostLabel == "建设费用" && mill.TimeLabel == "工期", "new construction quote identifies its time and purpose");
            var poor = Restore(world); poor.resources = new Supplies();
            var shortage = Plan(poor, 2);
            Check(shortage.Route.plotIndex == mill.Route.plotIndex && shortage.RequirementDetail.Contains("建设资源不足")
                && shortage.Cost.grain > 0, "resource shortage keeps actual position and honest unpaid quote visible");
            before = Json(world); mill.Cost.grain = -100; mill.Route.plotIndex = 4;
            Check(Json(world) == before && Plan(world, 2).Route.plotIndex != 4, "cost and route copies cannot alter world or later plans");

            mill = Plan(world, 2);
            world.StartCityConstruction(mill.Route.plotIndex, mill.Building); world.Tick(3.125);
            var construction = Plan(world, 2);
            Check(construction.Route.plotIndex == world.CityJob.plotIndex && construction.Building == world.CityJob.building
                && construction.EstimatedSeconds == world.CityJob.remaining && construction.Progress == 0,
                "active construction shows real plot and remaining time without crediting unfinished level");
            Same(construction.Cost, world.CityJob.paid, "construction displays original paid receipt");
            Check(construction.CostLabel == "已投入资源" && construction.TimeLabel == "剩余", "construction quote cannot be mistaken for a new charge");
            double frozenTime = construction.EstimatedSeconds; string frozenDetail = construction.RequirementDetail;
            world.Tick(.25);
            Check(construction.EstimatedSeconds == frozenTime && construction.RequirementDetail == frozenDetail
                && Plan(world, 2).EstimatedSeconds == world.CityJob.remaining, "existing plan stays detached while a new plan reflects advancing construction");
            before = Json(world); construction.Cost.grain = 0;
            Check(Json(world) == before, "mutating construction cost cannot mutate saved receipt");
            world = Restore(world);
            construction = Plan(world, 2);
            Check(construction.EstimatedSeconds == world.CityJob.remaining, "JSON roundtrip retains actual construction progress");
            world.Tick(world.CityJob.remaining + .01);
            Check(Plan(world, 2).Ready, "completed building makes task ready");
            Claim(world, 2);

            world.resources = new Supplies { grain = 1000000, grass = 1000000, stone = 1000000, iron = 1000000, gold = 1000000 };
            var hall = Plan(world, 3);
            Check(hall.Building == CityBuilding.TownHall && hall.Progress == 1 && hall.Target == 2
                && hall.RequirementTitle.Contains("1 → 2"), "hall objective shows actual current and next levels");
            Same(hall.Cost, world.CityBuildCost(CityBuilding.TownHall, 2), "hall upgrade reads level two cost");
            Build(world, CityBuilding.TownHall); Claim(world, 3);
            Check(Plan(world, 4).ChapterIndex == 1 && Plan(world, 3).Claimed, "claim crosses into second chapter with earlier history intact");
            Claim(world, 4);
            var recruitment = Plan(world, 5);
            int amount = Math.Min(200, world.TroopCap - world.troops);
            Same(recruitment.Cost, World.RecruitmentCost(amount), "default reinforcement quote matches actual default request");
            Check(recruitment.Route.destination == CityQuestDestination.Army && recruitment.TimeLabel == "即时完成"
                && recruitment.RequirementDetail.Contains(amount + " 人"), "recruit objective names actual suggested amount and immediate project rule");
            world.Recruit(amount);
            Check(Plan(world, 5).Ready, "actual recruitment makes objective claimable"); Claim(world, 5);

            var land = Plan(world, 6);
            Check(land.Route.destination == CityQuestDestination.Land && world.AttackProblem(land.Route.landId) == ""
                && land.RequirementTitle.Contains(world.lands[land.Route.landId].name), "territory objective identifies actual legal land");
            Check(land.EstimatedSeconds == world.MarchDurationSeconds(world.FindMarchPath(world.homeId, land.Route.landId)),
                "land estimate uses real path duration");
            int firstLand = land.Route.landId;
            world.March(firstLand); world.Tick(1.75);
            var busy = Plan(world, 6);
            Check(busy.Route.landId == firstLand && busy.RequirementTitle.Contains(world.ArmyState)
                && busy.EstimatedSeconds == world.order.remaining && busy.Cost == null && !busy.Ready,
                "marching objective shows actual order without premature victory");
            world = Restore(world);
            Check(Plan(world, 6).EstimatedSeconds == world.order.remaining, "JSON roundtrip retains ongoing order in detail");
            world.Tick(1000);
            Check(world.lands[firstLand].owned && Plan(world, 6).Ready, "actual battle occupation satisfies territory objective"); Claim(world, 6);

            world.StartCityConstruction(world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index, CityBuilding.Lumberyard);
            var blocked = Plan(world, 7);
            Check(blocked.Building == CityBuilding.Lumberyard && blocked.Route.plotIndex == world.CityJob.plotIndex
                && blocked.EstimatedSeconds == world.CityJob.remaining, "another construction queue resolves to actual prerequisite worksite");
            world.CancelCityConstruction();
            Build(world, CityBuilding.Barracks); Claim(world, 7);
            Check(Plan(world, 8).ChapterIndex == 2, "seventh reward advances third chapter");
            var farm = Plan(world, 8);
            Check(farm.Route.landId == firstLand && world.TerritoryOrderProblem(farm.Route.landId, true) == ""
                && farm.RequirementDetail.Contains("所需 " + World.FarmCommandCost), "farm task locates actual owned land and shared command requirement");
            world.Garrison(firstLand); world.Tick(1000);
            var stationed = Plan(world, 8);
            Check(stationed.EstimatedSeconds == 0 && stationed.RequirementDetail.Contains("持续驻守")
                && !stationed.Ready, "stationed army is not presented as completed farming or zero-second arrival");
            world.RecallArmy();
            Check(Plan(world, 8).RequirementTitle.Contains("返回主城"), "returning army remains the actionable current order");
            world.Tick(1000); world.Farm(firstLand);
            world.Tick(world.order.remaining + .01);
            var working = Plan(world, 8);
            Check(working.RequirementTitle.Contains("屯田中") && working.EstimatedSeconds == world.order.remaining && !working.Ready,
                "working phase shows real remaining farming time");
            world.Tick(1000);
            Check(Plan(world, 8).Ready, "actual completed farming makes objective ready"); Claim(world, 8);
            Build(world, CityBuilding.TownHall); Claim(world, 9);
            Capture(world); Capture(world);
            Check(Plan(world, 10).Ready && Plan(world, 10).Progress == 3, "three real captures complete final objective");
            Claim(world, 10); world = Restore(world);
            var finished = Plan(world, 10);
            Check(finished.Claimed && finished.CurrentQuestId == 0 && !finished.Current && !finished.Ready
                && finished.Route == null && finished.Cost == null && finished.ActionHint.Contains("全部完成"),
                "completed JSON save has historical detail with no remaining action or reward");
            string problem;
            Check(world.IsValid(out problem), "real task sequence preserves valid campaign " + problem);
            Console.WriteLine("PASS: " + checks + " city quest objective plan checks."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
