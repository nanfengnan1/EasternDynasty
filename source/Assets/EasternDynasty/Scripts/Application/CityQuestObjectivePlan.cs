using System;
using System.Linq;

namespace EasternDynasty
{
    // Transient presentation data. Creating or inspecting a plan never performs its action,
    // initializes a city, claims a reward, advances time or retains a live campaign reference.
    public sealed class CityQuestObjectivePlan
    {
        public int QuestId { get; private set; }
        public int ChapterIndex { get; private set; }
        public int CurrentQuestId { get; private set; }
        public string Title { get; private set; }
        public string Objective { get; private set; }
        public int Progress { get; private set; }
        public int Target { get; private set; }
        public bool Claimed { get; private set; }
        public bool Current { get; private set; }
        public bool Ready { get; private set; }
        public Supplies Reward { get; private set; }
        public Supplies Cost { get; private set; }
        public string CostLabel { get; private set; }
        public string TimeLabel { get; private set; }
        public CityQuestRoute Route { get; private set; }
        public CityBuilding Building { get; private set; }
        public double EstimatedSeconds { get; private set; }
        public string ActionLabel { get; private set; }
        public string StatusLabel { get; private set; }
        public string ActionHint { get; private set; }
        public string RequirementTitle { get; private set; }
        public string RequirementDetail { get; private set; }

        private CityQuestObjectivePlan() { }

        public static CityQuestObjectivePlan Create(World world, int questId)
        {
            if (world == null || !world.IsYouzhou) return null;
            var quest = CityCatalog.Quests.FirstOrDefault(q => q.id == questId);
            if (quest == null) return null;
            var current = world.CurrentCityQuest;
            var plan = new CityQuestObjectivePlan {
                QuestId = quest.id, ChapterIndex = CityQuestClaimReceipt.ChapterIndex(quest.id),
                CurrentQuestId = current == null ? 0 : current.id, Title = quest.title,
                Objective = quest.objective, Target = world.CityQuestTarget(quest.id),
                Claimed = world.cityClaimedQuests != null && world.cityClaimedQuests.Contains(quest.id),
                Current = current != null && current.id == quest.id, Reward = Copy(quest.reward),
                Building = quest.building, ActionHint = "", RequirementTitle = "", RequirementDetail = ""
            };
            plan.Progress = Math.Min(plan.Target, Math.Max(0, world.CityQuestProgress(quest.id)));
            plan.Ready = plan.Current && !plan.Claimed && world.CityQuestReady(quest.id);
            if (plan.Claimed)
            {
                plan.StatusLabel = "已领取"; plan.ActionLabel = "返回任务列表";
                plan.RequirementTitle = "奖励已入库";
                plan.RequirementDetail = "该任务已领取，重复查看不会再次发奖。";
                plan.ActionHint = current == null ? "幽州纪事已全部完成。" : "当前目标：「" + current.title + "」。";
            }
            else if (!plan.Current)
            {
                plan.StatusLabel = "待解锁"; plan.ActionLabel = "查看当前任务";
                plan.RequirementTitle = "先完成当前任务";
                plan.RequirementDetail = current == null ? "当前没有待领取任务。" : "「" + current.title + "」领取后，依次推进后续任务。";
                plan.ActionHint = "目标进度可提前累积；奖励仍按任务顺序领取。";
            }
            else if (plan.Ready)
            {
                plan.StatusLabel = "可领取"; plan.ActionLabel = "领取奖励";
                plan.RequirementTitle = "目标已达成";
                plan.RequirementDetail = "领取后奖励入库，并开放下一项主要事宜。";
                plan.ActionHint = "核对任务奖励后领取；查看详情不会自动领取。";
            }
            else if (world.cityVersion != 1 || world.cityPlots == null)
            {
                plan.StatusLabel = "城区待初始化"; plan.ActionLabel = "返回任务列表";
                plan.RequirementTitle = "城区尚未就绪";
                plan.RequirementDetail = "城区信息尚未就绪，请返回主城后重试。";
                plan.ActionHint = "任务进度保持不变。";
            }
            else
            {
                plan.StatusLabel = "进行中";
                plan.Route = CityQuestNavigation.Resolve(world, quest);
                plan.DescribeNextStep(world, quest);
            }
            return plan;
        }

        private void DescribeNextStep(World world, CityQuestDefinition quest)
        {
            ActionHint = Route.hint;
            if (Route.destination == CityQuestDestination.Facilities)
            {
                Building = Route.building;
                var definition = CityCatalog.Get(Building);
                var job = world.CityJob;
                if (job != null)
                {
                    Cost = Copy(job.paid); EstimatedSeconds = Math.Max(0, job.remaining);
                    CostLabel = "已投入资源"; TimeLabel = "剩余";
                    ActionLabel = "查看施工进度";
                    RequirementTitle = definition.name + "施工中";
                    RequirementDetail = "当前 " + world.CityLevel(Building) + " 级 → " + job.targetLevel + " 级\n已投入费用；剩余 " + Seconds(EstimatedSeconds) + "（游戏内时间）";
                    return;
                }
                if (quest.id == 1)
                {
                    ActionLabel = "查看主城"; RequirementTitle = "城主府 " + world.CityLevel(Building) + " 级";
                    RequirementDetail = "进入主城，查看城池与城区即可完成。";
                    ActionHint = "查看主城会记录访问；建设与任务领奖需分别确认。";
                    return;
                }
                int nextLevel = world.CityLevel(Building) + 1;
                Cost = Copy(world.CityBuildCost(Building, nextLevel));
                EstimatedSeconds = world.CityBuildSeconds(Building, nextLevel);
                CostLabel = "建设费用"; TimeLabel = "工期";
                ActionLabel = Building == quest.building ? "前往建设" : "查看建设前置";
                RequirementTitle = definition.name + " " + world.CityLevel(Building) + " → " + nextLevel + " 级";
                int hall = CityCatalog.RequiredHallLevel(quest.building, world.CityLevel(quest.building) + 1);
                string problem = world.CityBuildProblem(Route.plotIndex, Building);
                RequirementDetail = Building != quest.building && quest.building != CityBuilding.None
                    ? CityCatalog.Get(quest.building).name + "需要城主府 " + hall + " 级；当前 " + world.CityLevel(CityBuilding.TownHall) + " 级。"
                    : "当前城主府 " + world.CityLevel(CityBuilding.TownHall) + " 级；本次工期 " + Seconds(EstimatedSeconds) + "。";
                if (problem.Length > 0) RequirementDetail += "\n" + problem;
                return;
            }
            Building = CityBuilding.None;
            if (world.order != null)
            {
                var order = world.order;
                var target = world.lands[order.target];
                EstimatedSeconds = Math.Max(0, order.remaining);
                ActionLabel = "查看部队进度"; RequirementTitle = "第一部队 · " + world.ArmyState;
                RequirementDetail = LandLabel(target) + "\n" + (order.phase == (int)ArmyPhase.Stationed && !order.returning
                    ? "持续驻守；撤回后继续任务。" : "当前阶段剩余 " + Seconds(EstimatedSeconds) + "（游戏内时间）。");
                return;
            }
            if (Route.destination == CityQuestDestination.Formation)
            {
                ActionLabel = "前往配置部队"; RequirementTitle = "上阵武将 " + world.ActiveGenerals.Count + " / 3";
                RequirementDetail = "第一部队 · " + world.ArmyState + "\n在实际编队位置选择已有武将。";
                return;
            }
            if (Route.destination == CityQuestDestination.Army)
            {
                ActionLabel = "查看部队征兵"; RequirementTitle = "整队兵力 " + world.troops + " / " + world.TroopCap;
                int amount = Math.Min(200, Math.Max(0, world.TroopCap - world.troops));
                var recruit = ReinforcementPlan.Create(world, amount);
                Cost = Copy(recruit.cost);
                CostLabel = "默认补兵费用"; TimeLabel = "即时完成";
                RequirementDetail = "默认补充 " + amount + " 人 · 项目即时征兵\n" + (recruit.problem.Length == 0 ? "进入征兵后可调整人数并确认费用。" : recruit.problem);
                return;
            }
            if (Route.destination == CityQuestDestination.Land && Route.landId >= 0 && Route.landId < world.lands.Count)
            {
                var target = world.lands[Route.landId];
                RequirementTitle = LandLabel(target);
                var path = world.FindMarchPath(world.homeId, target.id);
                EstimatedSeconds = path == null ? 0 : world.MarchDurationSeconds(path);
                if (quest.action == "farm" && target.owned)
                {
                    ActionLabel = "前往屯田土地";
                    RequirementDetail = "政令 " + world.AvailableCommands + " / 所需 " + World.FarmCommandCost + "\n行军 " + Seconds(EstimatedSeconds) + "；屯田 " + Seconds(World.FarmSeconds) + "，结束后返城。";
                }
                else
                {
                    ActionLabel = "前往目标土地";
                    RequirementDetail = "守军 " + world.Defenders(target) + "；我军 " + world.troops + "\n预计行军 " + Seconds(EstimatedSeconds) + "，抵达后结算战斗。";
                    ActionHint = quest.action == "farm" ? "先占领此地，再返回任务选择屯田；出征费用在土地面板核对。" : "已定位可出征土地；核对守军、费用与编队后确认派兵。";
                }
                return;
            }
            ActionLabel = "前往完成"; RequirementTitle = "查看操作位置"; RequirementDetail = Route.hint;
        }

        private static string LandLabel(Land target)
        { return target.name + " · " + target.level + " 级（" + target.x + "," + target.z + "）"; }
        private static string Seconds(double seconds)
        { return Math.Ceiling(Math.Max(0, seconds)).ToString("0", System.Globalization.CultureInfo.InvariantCulture) + " 秒"; }
        private static Supplies Copy(Supplies value)
        { return value == null ? null : new Supplies { grain = value.grain, grass = value.grass, stone = value.stone,
            iron = value.iron, gold = value.gold, jade = value.jade }; }
    }
}
