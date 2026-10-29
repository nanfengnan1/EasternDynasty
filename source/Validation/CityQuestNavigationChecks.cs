using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;

internal static class CityQuestNavigationChecks
{
    private static int checks;
    private static void Check(bool condition,string name)
    { if(!condition)throw new Exception("FAIL "+name);checks++;Console.WriteLine("PASS "+name); }
    private static CityQuestDefinition Quest(int id) { return CityCatalog.Quests.First(q=>q.id==id); }
    private static string State(World world)
    { using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(World)).WriteObject(stream,world);return Convert.ToBase64String(stream.ToArray());} }
    private static CityQuestRoute Resolve(World world,int id)
    {
        string before=State(world);var result=CityQuestNavigation.Resolve(world,Quest(id));
        Check(State(world)==before,"navigation preserves all serialized state for quest "+id);return result;
    }
    private static void Complete(World world,int plot,CityBuilding building)
    { world.StartCityConstruction(plot,building);Check(world.CityJob!=null,"fixture construction starts");world.Tick(world.CityJob.remaining); }
    public static int Main()
    {
        try
        {
            for(int spawn=0;spawn<3;spawn++)
            {
                var world=World.CreateYouzhou(spawn);
                var route=Resolve(world,1);
                Check(route.destination==CityQuestDestination.Facilities&&route.plotIndex==4&&route.building==CityBuilding.TownHall,"visit opens real town hall at spawn "+spawn);
                route=Resolve(world,2);
                Check(route.building==CityBuilding.Mill&&world.CityBuildProblem(route.plotIndex,route.building)=="","mill task opens a buildable plot");
                world.StartCityConstruction(2,CityBuilding.Mill);route=Resolve(world,2);
                Check(route.plotIndex==2&&route.building==CityBuilding.Mill,"in-progress task locates the real mill queue instead of an empty plot");
                route=Resolve(world,7);
                Check(route.plotIndex==2&&route.building==CityBuilding.Mill,"other queued construction is the actionable prerequisite");
                world.CancelCityConstruction();route=Resolve(world,7);
                Check(route.plotIndex==4&&route.building==CityBuilding.TownHall,"locked barracks routes to actual prerequisite");
                Complete(world,4,CityBuilding.TownHall);route=Resolve(world,7);
                Check(route.building==CityBuilding.Barracks&&world.CityBuildProblem(route.plotIndex,route.building)=="","unlocked barracks routes to an actionable empty plot");
                route=Resolve(world,5);Check(route.destination==CityQuestDestination.Army,"recruit task opens real army controls");
                world.troops=world.TroopCap;route=Resolve(world,5);
                Check(route.destination==CityQuestDestination.Facilities&&route.building==CityBuilding.Barracks,"full army routes to capacity improvement instead of disabled recruiting");
                route=Resolve(world,8);
                Check(route.destination==CityQuestDestination.Land&&world.AttackProblem(route.landId)=="","farm without owned land selects legal attack prerequisite");
                int land=route.landId;world.lands[land].owned=true;route=Resolve(world,8);
                Check(route.landId==land&&world.TerritoryOrderProblem(land,true)=="","farm reaches reachable owned resource with enabled action");
                world.commands=0;route=Resolve(world,8);
                Check(route.landId==land&&world.TerritoryOrderProblem(land,true).Length>0,"command shortage keeps the owned land cost panel reachable");
                world.commands=World.CommandCap;world.Farm(land);route=Resolve(world,8);
                Check(route.destination==CityQuestDestination.Land&&route.landId==world.order.target,"busy army routes to actual order rather than dead-end city hint");
                route=Resolve(world,4);
                Check(route.destination==CityQuestDestination.Land&&route.landId==world.order.target,"busy formation task reaches order controls before a locked roster");
            }
            var isolated=World.CreateYouzhou();isolated.troops=49;
            Check(Resolve(isolated,6).destination==CityQuestDestination.Army,"weak army routes to replenishment before attack");
            isolated.formation.Clear();
            Check(Resolve(isolated,6).destination==CityQuestDestination.Formation,"empty army routes to formation before recruit or attack");
            var unreachable=World.CreateYouzhou();
            var far=unreachable.lands.First(t=>!t.owned&&t.Walkable&&!t.capital&&!t.bridge&&t.x>30&&t.z>30);
            far.owned=true;
            foreach(var tile in unreachable.lands.Where(t=>Math.Abs(t.x-far.x)<=1&&Math.Abs(t.z-far.z)<=1&&t.id!=far.id))tile.kind=LandKind.Mountain;
            var fallback=Resolve(unreachable,8);
            Check(fallback.landId!=far.id&&unreachable.AttackProblem(fallback.landId)=="","unreachable owned land does not trap farm navigation");
            Console.WriteLine("PASS: "+checks+" city quest navigation checks.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
