using System;
using System.Linq;

namespace EasternDynasty
{
    public enum CityQuestDestination { Facilities, Formation, Army, Land, Training }

    // A read-only navigation decision. Opening a task never starts or pays for an action.
    public sealed class CityQuestRoute
    {
        public CityQuestDestination destination;
        public int plotIndex = -1, landId = -1;
        public CityBuilding building;
        public string hint;
    }

    public static class CityQuestNavigation
    {
        public static CityQuestRoute Resolve(World world, CityQuestDefinition quest)
        {
            if (world == null || quest == null) throw new ArgumentNullException();
            if (quest.action == "city") return Building(world, quest.building);
            if (quest.action == "training") return Route(CityQuestDestination.Training, "选择目标土地后确认练兵。");
            if (world.order != null)
                return new CityQuestRoute { destination = CityQuestDestination.Land, landId = world.order.target,
                    hint = world.ArmyState + "；在部队面板查看进度或撤回，返城后继续任务。" };
            if (quest.action == "formation" || world.ActiveGenerals.Count == 0)
                return Route(CityQuestDestination.Formation, "配置上阵武将后返回主要事宜。");
            if (quest.action == "recruit" && world.troops >= world.TroopCap)
                return Building(world, CityBuilding.Barracks);
            if (quest.action == "recruit" || world.troops < 50)
                return Route(CityQuestDestination.Army, "在第一部队核对征兵费用，确认后补充兵力。");

            var home = world.lands[world.homeId];
            var candidates = world.lands.Where(t => t.Walkable && !t.bridge && !t.capital && !world.IsCityDistrict(t.id))
                .OrderBy(t => t.level).ThenBy(t => Math.Abs(t.x-home.x)+Math.Abs(t.z-home.z)).ThenBy(t => t.id);
            if (quest.action == "farm")
            {
                // Insufficient commands must still reach the owned plot's real cost/reason panel.
                var owned = candidates.FirstOrDefault(t => t.owned && world.FindMarchPath(world.homeId,t.id) != null);
                if (owned != null) return new CityQuestRoute { destination = CityQuestDestination.Land, landId = owned.id,
                    hint = world.AvailableCommands < World.FarmCommandCost ? "政令不足；在此查看屯田费用，恢复后可下令。" : "在土地面板选择屯田，完成后返回主要事宜领取奖励。" };
            }
            var target = candidates.FirstOrDefault(t => !t.owned && world.AttackProblem(t.id).Length == 0);
            if (target != null) return new CityQuestRoute { destination = CityQuestDestination.Land, landId = target.id,
                hint = quest.action == "farm" ? "尚无可达的屯田土地：先派兵占领此地，再返回任务选择屯田。" : "已选可出征资源地；核对守军后选择派兵占领。" };
            return Route(CityQuestDestination.Army, "暂无可出征的资源地；先检查部队兵力与编队。");
        }

        private static CityQuestRoute Building(World world, CityBuilding wanted)
        {
            // A pending queue is the actionable prerequisite, including the requested building itself.
            var job = world.CityJob;
            if (job != null) return new CityQuestRoute { destination = CityQuestDestination.Facilities,
                plotIndex = job.plotIndex, building = job.building,
                hint = "已定位实际工地；完成或取消当前施工后继续任务。" };
            int next = world.CityLevel(wanted)+1;
            if (world.CityLevel(CityBuilding.TownHall) < CityCatalog.RequiredHallLevel(wanted,next)) wanted = CityBuilding.TownHall;
            var plot = world.cityPlots.FirstOrDefault(p => p.building == wanted)
                ?? world.cityPlots.FirstOrDefault(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId));
            return new CityQuestRoute { destination = CityQuestDestination.Facilities, plotIndex = plot == null ? 4 : plot.index,
                building = wanted, hint = "已定位" + CityCatalog.Get(wanted).name + "；核对前置、费用和工期后开始建设。" };
        }

        private static CityQuestRoute Route(CityQuestDestination destination, string hint)
        { return new CityQuestRoute { destination = destination, hint = hint }; }
    }
}
