using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;

internal static class CityFacilityNavigationChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    { if (!condition) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static string State(World world)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return Convert.ToBase64String(stream.ToArray()); } }
    private static CityFacilityRoute Resolve(World world, CityBuilding building)
    {
        string before = State(world);
        var route = CityFacilityNavigation.Resolve(world, building);
        Check(State(world) == before, "facility navigation preserves all serialized state: " + building);
        Check(route.requestedBuilding == building && !string.IsNullOrEmpty(route.label) && !string.IsNullOrEmpty(route.reason),
            "route preserves request and explains destination: " + building);
        if (route.canNavigate)
            Check(route.plotIndex >= 0 && route.plotIndex < world.cityPlots.Count && world.IsCityDistrict(world.cityPlots[route.plotIndex].landId),
                "enabled route points to a real operable city plot: " + building);
        else Check(route.plotIndex == -1 && route.building == CityBuilding.None, "disabled route never defaults to town hall: " + building);
        return route;
    }
    private static void Complete(World world, int index, CityBuilding building)
    {
        Check(world.CityBuildProblem(index, building) == "", "fixture construction allowed: " + building);
        world.StartCityConstruction(index, building);
        Check(world.CityJob != null && world.CityJob.plotIndex == index, "fixture starts real construction: " + building);
        world.Tick(world.CityJob.remaining);
        Check(world.CityJob == null && world.cityPlots[index].building == building, "fixture construction completes: " + building);
    }
    private static void Rich(World world)
    { world.resources = new Supplies { grain = 10000000, grass = 10000000, stone = 10000000, iron = 10000000, gold = 10000000 }; }

    public static int Main()
    {
        try
        {
            for (int spawn = 0; spawn < 3; spawn++)
            {
                var world = World.CreateYouzhou(spawn); Rich(world);
                var route = Resolve(world, CityBuilding.Mill);
                Check(route.canNavigate && route.state == CityFacilityRouteState.ReadyToBuild && route.plotIndex == 0 && route.building == CityBuilding.Mill,
                    "new mill chooses lowest usable plot at spawn " + spawn);
                Check(world.CityBuildProblem(route.plotIndex, route.building) == "", "new route is accepted by actual construction rule");
                route = Resolve(world, CityBuilding.Barracks);
                Check(route.canNavigate && route.state == CityFacilityRouteState.HallRequired && route.plotIndex == 4 && route.building == CityBuilding.TownHall,
                    "locked facility points to actual town hall");

                world.StartCityConstruction(2, CityBuilding.Mill);
                route = Resolve(world, CityBuilding.Mill);
                Check(route.state == CityFacilityRouteState.UnderConstruction && route.plotIndex == 2 && route.building == CityBuilding.Mill,
                    "requested construction routes to actual site instead of lowest empty plot");
                route = Resolve(world, CityBuilding.TownHall);
                Check(route.state == CityFacilityRouteState.QueueBlocked && route.plotIndex == 2 && route.building == CityBuilding.Mill && route.label.Contains("磨坊"),
                    "existing hall upgrade is blocked by real named work site");
                route = Resolve(world, CityBuilding.Barracks);
                Check(route.state == CityFacilityRouteState.QueueBlocked && route.plotIndex == 2,
                    "new facility queue prerequisite precedes locked hall requirement");
                world.CancelCityConstruction();
                route = Resolve(world, CityBuilding.Mill);
                Check(route.state == CityFacilityRouteState.ReadyToBuild && route.plotIndex == 0 && world.CityLevel(CityBuilding.Mill) == 0,
                    "cancelling real new build returns navigation to usable empty plot");
                Complete(world, 2, CityBuilding.Mill);
                route = Resolve(world, CityBuilding.Mill);
                Check(route.state == CityFacilityRouteState.Existing && route.plotIndex == 2,
                    "completed facility stays at original plot despite earlier empty plots");
                world.StartCityConstruction(2, CityBuilding.Mill); world.CancelCityConstruction();
                route = Resolve(world, CityBuilding.Mill);
                Check(route.state == CityFacilityRouteState.Existing && route.plotIndex == 2 && world.CityLevel(CityBuilding.Mill) == 1,
                    "cancelled real upgrade still opens completed original facility");

                Complete(world, 4, CityBuilding.TownHall);
                route = Resolve(world, CityBuilding.Barracks);
                Check(route.state == CityFacilityRouteState.ReadyToBuild && route.plotIndex == 0 && route.building == CityBuilding.Barracks,
                    "actual hall upgrade unlocks barracks location");
                Complete(world, route.plotIndex, route.building);
                Check(Resolve(world, CityBuilding.Barracks).plotIndex == 0, "built barracks resolves to its actual plot");
                world.resources = new Supplies();
                route = Resolve(world, CityBuilding.Mill);
                Check(route.canNavigate && route.plotIndex == 2 && route.reason == world.CityBuildProblem(route.plotIndex, route.building),
                    "insufficient upgrade resources preserve detail access and Core reason");
                route = Resolve(world, CityBuilding.Residence);
                Check(route.canNavigate && route.plotIndex == 1 && route.reason == world.CityBuildProblem(route.plotIndex, route.building) && route.reason.Contains("资源不足"),
                    "insufficient new build resources preserve cost panel access");
            }

            var levels = World.CreateYouzhou(); Rich(levels);
            Complete(levels, 0, CityBuilding.Mill);
            Complete(levels, 0, CityBuilding.Mill);
            Complete(levels, 0, CityBuilding.Mill);
            var prerequisite = Resolve(levels, CityBuilding.Mill);
            Check(prerequisite.state == CityFacilityRouteState.HallRequired && prerequisite.plotIndex == 4 && prerequisite.reason.Contains("2"),
                "existing facility upgrade obeys shared hall-level prerequisite");
            Complete(levels, 4, CityBuilding.TownHall);
            Check(Resolve(levels, CityBuilding.Mill).plotIndex == 0, "actual prerequisite upgrade restores original facility route");
            while (levels.CityLevel(CityBuilding.TownHall) < CityCatalog.Get(CityBuilding.TownHall).maximumLevel)
                Complete(levels, 4, CityBuilding.TownHall);
            while (levels.CityLevel(CityBuilding.Mill) < CityCatalog.Get(CityBuilding.Mill).maximumLevel)
                Complete(levels, 0, CityBuilding.Mill);
            levels.StartCityConstruction(1, CityBuilding.Quarry);
            var maximum = Resolve(levels, CityBuilding.Mill);
            Check(maximum.canNavigate && maximum.plotIndex == 0 && maximum.state == CityFacilityRouteState.MaximumLevel,
                "real maximum-level facility remains inspectable while another construction runs");
            maximum = Resolve(levels, CityBuilding.TownHall);
            Check(maximum.canNavigate && maximum.plotIndex == 4 && maximum.state == CityFacilityRouteState.MaximumLevel,
                "maximum-level hall remains inspectable");

            var blocked = World.CreateYouzhou();
            foreach (var plot in blocked.cityPlots.Where(p => p.index != 4)) blocked.lands[plot.landId].kind = LandKind.Mountain;
            var missing = Resolve(blocked, CityBuilding.Mill);
            Check(!missing.canNavigate && missing.state == CityFacilityRouteState.NoAvailablePlot, "no usable empty district disables route");
            var unblocked = blocked.cityPlots[7]; blocked.lands[unblocked.landId].kind = LandKind.Grain;
            Check(Resolve(blocked, CityBuilding.Mill).plotIndex == 7, "only genuine buildable district is selected");
            Complete(blocked, 7, CityBuilding.Mill);
            missing = Resolve(blocked, CityBuilding.Residence);
            Check(!missing.canNavigate && missing.state == CityFacilityRouteState.NoAvailablePlot, "occupied usable district is not offered to another facility");
            Check(Resolve(blocked, CityBuilding.Mill).plotIndex == 7, "existing facility remains reachable with no empty district");

            var old = World.CreateYouzhou(); old.cityVersion = 0; old.cityPlots = null; old.cityConstruction = null;
            Check(!Resolve(old, CityBuilding.Mill).canNavigate && old.cityPlots == null, "navigation never migrates old save on read");
            var legacy = World.Create(Homeland.Han, 4, "旧州");
            Check(!Resolve(legacy, CityBuilding.TownHall).canNavigate, "legacy world city navigation remains isolated");
            Check(!Resolve(levels, CityBuilding.None).canNavigate && !Resolve(levels, (CityBuilding)99).canNavigate,
                "invalid requested facilities cannot navigate");
            Console.WriteLine("CITY_FACILITY_NAVIGATION_OK: " + checks + " assertions."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
