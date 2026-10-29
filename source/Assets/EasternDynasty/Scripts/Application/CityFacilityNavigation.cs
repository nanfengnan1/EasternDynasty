using System;
using System.Linq;

namespace EasternDynasty
{
    public enum CityFacilityRouteState
    {
        Existing, ReadyToBuild, MaximumLevel, UnderConstruction,
        QueueBlocked, HallRequired, NoAvailablePlot, Unavailable
    }

    // A detached, read-only decision for opening an actual facility, prerequisite or work site.
    // This is UI state, not a saved construction request; navigating never pays or starts work.
    public sealed class CityFacilityRoute
    {
        public CityBuilding requestedBuilding, building;
        public int plotIndex = -1;
        public bool canNavigate;
        public string label, reason;
        public CityFacilityRouteState state;
    }

    public static class CityFacilityNavigation
    {
        public static CityFacilityRoute Resolve(World world, CityBuilding wanted)
        {
            if (world == null) throw new ArgumentNullException("world");
            var definition = CityCatalog.Get(wanted);
            if (definition == null)
                return Disabled(wanted, CityFacilityRouteState.Unavailable, "设施不可用", "请选择有效设施。");
            if (!world.IsYouzhou)
                return Disabled(wanted, CityFacilityRouteState.Unavailable, "幽州开放", "城建在幽州开放。");
            // CityBuildProblem migrates old city state. Loading/migration belongs to the session,
            // so a navigation query must not invoke it for an uninitialised save.
            if (world.cityVersion != 1 || world.cityPlots == null)
                return Disabled(wanted, CityFacilityRouteState.Unavailable, "城区尚未就绪", "请完成主城初始化后查看设施。");

            var existing = world.cityPlots.FirstOrDefault(p => p != null && p.building == wanted);
            var job = world.CityJob;
            if (job != null && job.building == wanted)
                return WorkSite(world, wanted, job, CityFacilityRouteState.UnderConstruction,
                    definition.name + "正在施工；可查看进度或取消建设。");

            // A capped facility remains inspectable even when another facility is being built.
            if (existing != null && existing.level >= definition.maximumLevel)
                return At(world, wanted, wanted, existing, CityFacilityRouteState.MaximumLevel,
                    "查看" + definition.name, "设施已达最高等级。");

            if (job != null)
                return WorkSite(world, wanted, job, CityFacilityRouteState.QueueBlocked,
                    definition.name + "的建设需等待当前施工；可前往实际工地。");

            int targetLevel = existing == null ? 1 : existing.level + 1;
            int requiredHall = CityCatalog.RequiredHallLevel(wanted, targetLevel);
            int hallLevel = world.CityLevel(CityBuilding.TownHall);
            if (hallLevel < requiredHall)
            {
                var hall = world.cityPlots.FirstOrDefault(p => p != null && p.building == CityBuilding.TownHall);
                return At(world, wanted, CityBuilding.TownHall, hall, CityFacilityRouteState.HallRequired,
                    "前往城主府", definition.name + "需要城主府 " + requiredHall + " 级；当前 " + hallLevel + " 级。");
            }

            var target = existing ?? world.cityPlots.Where(p => p != null && p.index != 4 &&
                p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).OrderBy(p => p.index).FirstOrDefault();
            if (target == null)
                return Disabled(wanted, CityFacilityRouteState.NoAvailablePlot, "暂无可用城区", "没有可建设的空城区。");

            // Use Core's actual prerequisite/cost check. Resource shortage does not prevent
            // opening the cost panel, and the query does not spend or queue construction.
            string problem = world.CityBuildProblem(target.index, wanted);
            return At(world, wanted, wanted, target,
                existing == null ? CityFacilityRouteState.ReadyToBuild : CityFacilityRouteState.Existing,
                existing == null ? "查看建造位置" : "查看" + definition.name,
                problem.Length > 0 ? problem : existing == null ? "前往可用城区，核对费用后开始建设。" : "前往原设施，核对升级效果与费用。");
        }

        private static CityFacilityRoute WorkSite(World world, CityBuilding wanted, CityConstruction job,
            CityFacilityRouteState state, string reason)
        {
            var plot = world.cityPlots.FirstOrDefault(p => p != null && p.index == job.plotIndex);
            var definition = CityCatalog.Get(job.building);
            if (definition == null)
                return Disabled(wanted, CityFacilityRouteState.Unavailable, "工地不可用", "施工设施无效。");
            return At(world, wanted, job.building, plot, state, "前往" + definition.name + "工地", reason);
        }

        private static CityFacilityRoute At(World world, CityBuilding wanted, CityBuilding building,
            CityPlot plot, CityFacilityRouteState state, string label, string reason)
        {
            if (plot == null || plot.index < 0 || plot.index >= world.cityPlots.Count ||
                !ReferenceEquals(world.cityPlots[plot.index], plot) || !world.IsCityDistrict(plot.landId))
                return Disabled(wanted, CityFacilityRouteState.Unavailable, "位置不可用", "未找到可操作的设施位置。");
            return new CityFacilityRoute { requestedBuilding = wanted, building = building, plotIndex = plot.index,
                canNavigate = true, label = label, reason = reason, state = state };
        }

        private static CityFacilityRoute Disabled(CityBuilding wanted, CityFacilityRouteState state, string label, string reason)
        {
            return new CityFacilityRoute { requestedBuilding = wanted, building = CityBuilding.None,
                canNavigate = false, label = label, reason = reason, state = state };
        }
    }
}
