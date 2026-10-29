using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public enum ArmyAction { Attack = 0, Garrison = 1, Farm = 2, Training = 3 }
    public enum ArmyPhase { Marching = 0, Stationed = 1, Working = 2 }
    public partial class World
    {
        // Explicit prototype tuning, not asserted as current official S1 numeric rules.
        public const int CommandCap = 20, FarmCommandCost = 2;
        public const double FarmSeconds = 90, CommandRecoverySeconds = 3600;
        public const int TrainingCommandCost = 2;
        public const double TrainingSeconds = 60;
        public void EnsureSeasonState()
        {
            // Older serializers leave optional collections null; normalize empty state
            // before the next save so JsonUtility preserves its shape on roundtrip.
            if (trainingHistory == null) trainingHistory = new List<TrainingReceipt>();
            if (order != null && order.action != (int)ArmyAction.Training && order.trainingParticipants == null)
                order.trainingParticipants = new List<int>();
            if (order != null && order.action == (int)ArmyAction.Attack && !order.returning
                && string.IsNullOrEmpty(order.battleId))
                order.battleId = Guid.NewGuid().ToString("N");
            if (IsYouzhou && seasonMechanicsVersion == 0)
            { seasonMechanicsVersion = 1; commands = CommandCap; commandRecovery = 0; }
        }
        public int AvailableCommands { get { return seasonMechanicsVersion == 0 ? CommandCap : commands; } }
        public string ArmyState
        {
            get
            {
                if (order == null) return "驻城待命";
                if (order.returning) return "返回主城";
                if (order.phase == (int)ArmyPhase.Stationed) return "驻守 · " + lands[order.target].name;
                if (order.phase == (int)ArmyPhase.Working) return order.action == (int)ArmyAction.Training ? "练兵中" : "屯田中";
                if (order.action == (int)ArmyAction.Training) return "前往练兵";
                return order.action == (int)ArmyAction.Garrison ? "前往驻守" : order.action == (int)ArmyAction.Farm ? "前往屯田" : "出征行军";
            }
        }
        public List<int> FindMarchPath(int from, int to)
        {
            if (from < 0 || to < 0 || from >= lands.Count || to >= lands.Count || !lands[from].Walkable || !lands[to].Walkable) return null;
            // A* uses actual segment distances. Diagonals are sqrt(2), and cannot
            // cut across a blocked corner or shortcut from a bank onto a bridge.
            var prev = new int[lands.Count]; var distance = new double[lands.Count];
            var closed = new bool[lands.Count];
            for (int i = 0; i < prev.Length; i++) { prev[i] = -1; distance[i] = double.PositiveInfinity; }
            var open = new SortedSet<MarchNode>(new MarchNodeComparer());
            int serial = 0; distance[from] = 0; prev[from] = from;
            open.Add(new MarchNode(from, MarchHeuristic(lands[from], lands[to]), serial++));
            while (open.Count > 0)
            {
                var next = open.Min; open.Remove(next); if (closed[next.id]) continue;
                int current = next.id; closed[current] = true; if (current == to) break;
                var t = lands[current];
                for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0) continue;
                    var n = TryGetLand(t.x + dx, t.z + dz);
                    if (n == null || closed[n.id] || !CanMarchStep(current, n.id, IsYouzhou ? 2 : 1)) continue;
                    double candidate = distance[current] + (dx != 0 && dz != 0 ? Math.Sqrt(2) : 1);
                    if (candidate >= distance[n.id]) continue;
                    prev[n.id] = current; distance[n.id] = candidate;
                    open.Add(new MarchNode(n.id, candidate + MarchHeuristic(n, lands[to]), serial++));
                }
            }
            if (prev[to] < 0) return null;
            var path = new List<int>(); for (int p=to;;p=prev[p]) { path.Add(p); if(p==from)break; }
            path.Reverse(); return path;
        }
        private struct MarchNode
        {
            public int id, serial; public double score;
            public MarchNode(int id, double score, int serial) { this.id = id; this.score = score; this.serial = serial; }
        }
        private sealed class MarchNodeComparer : IComparer<MarchNode>
        {
            public int Compare(MarchNode a, MarchNode b) { int result = a.score.CompareTo(b.score); return result != 0 ? result : a.serial.CompareTo(b.serial); }
        }
        private double MarchHeuristic(Land a, Land b)
        {
            int dx = Math.Abs(a.x - b.x), dz = Math.Abs(a.z - b.z);
            return IsYouzhou ? Math.Max(dx, dz) + (Math.Sqrt(2) - 1) * Math.Min(dx, dz) : dx + dz;
        }
        public bool AreAdjacent(int first, int second)
        {
            if (first < 0 || second < 0 || first >= lands.Count || second >= lands.Count) return false;
            var a = lands[first]; var b = lands[second];
            int dx = Math.Abs(a.x - b.x), dz = Math.Abs(a.z - b.z);
            return IsYouzhou ? Math.Max(dx, dz) == 1 : dx + dz == 1;
        }
        public bool HasAdjacentOwnedLand(int target)
        {
            if (target < 0 || target >= lands.Count) return false;
            var t = lands[target];
            for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dz == 0 || !IsYouzhou && Math.Abs(dx) + Math.Abs(dz) != 1) continue;
                var n = TryGetLand(t.x + dx, t.z + dz); if (n != null && n.owned) return true;
            }
            return false;
        }
        // routeVersion 1 is persisted four-direction S1 movement. Version 2 adds
        // weighted eight-direction movement without changing old route progress.
        public bool CanMarchStep(int from, int to, int routeVersion)
        {
            if (from < 0 || to < 0 || from >= lands.Count || to >= lands.Count) return false;
            var a = lands[from]; var b = lands[to]; if (!a.Walkable || !b.Walkable) return false;
            int dx = Math.Abs(a.x - b.x), dz = Math.Abs(a.z - b.z);
            if (dx + dz == 1) return true;
            if (routeVersion != 2 || !IsYouzhou || dx != 1 || dz != 1 || a.bridge || b.bridge) return false;
            var sideA = TryGetLand(a.x, b.z); var sideB = TryGetLand(b.x, a.z);
            return sideA != null && sideB != null && sideA.Walkable && sideB.Walkable && !sideA.bridge && !sideB.bridge;
        }
        public double MarchPathLength(IList<int> path, int routeVersion = 2)
        {
            if (path == null || path.Count < 2) return 0;
            if (routeVersion == 1) return path.Count - 1;
            double length = 0;
            for (int i = 1; i < path.Count; i++)
            {
                var a = lands[path[i - 1]]; var b = lands[path[i]];
                length += Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
            }
            return length;
        }
        public double MarchDurationSeconds(IList<int> path, int routeVersion = 2)
        { return 8 + MarchPathLength(path, routeVersion) * 4; }
        private ArmyOrder CreateMovement(int target, ArmyAction action, List<int> path)
        {
            double duration = MarchDurationSeconds(path);
            return new ArmyOrder { target=target, action=(int)action, duration=duration, remaining=duration, routeVersion=2, path=path, routeStart=0, routeEnd=1 };
        }
        public string TerritoryOrderProblem(int target, bool farm)
        {
            if (!IsYouzhou) return "此指令在幽州 S1 开放。";
            if (target < 0 || target >= lands.Count) return "目标不存在。";
            if (order != null) return "当前部队正在执行指令，请先撤回。";
            var t=lands[target];
            if (!t.owned || !t.Walkable) return "只能派往已占领的可通行土地。";
            if (t.home) return "部队已在主城。";
            if (troops < 50 || ActiveGenerals.Count == 0) return "请先配置武将并征兵。";
            if (farm && (t.capital || t.bridge || IsCityDistrict(t.id))) return "请在城区以外的普通资源地屯田。";
            if (farm && AvailableCommands < FarmCommandCost) return "屯田需要 2 政令。";
            if (FindMarchPath(homeId,target) == null) return "没有可通行的陆路，请寻找桥梁。";
            return "";
        }
        public Supplies FarmYield(int target)
        {
            var yield = new Supplies();
            if(target<0 || target>=lands.Count) return yield;
            var centre=lands[target];
            if(!centre.owned || !centre.Walkable || centre.home || centre.capital || centre.bridge || IsCityDistrict(centre.id)) return yield;
            foreach(var t in lands)
            {
                if(!t.owned || !t.Walkable || t.home || t.capital || t.bridge || IsCityDistrict(t.id) || Math.Abs(t.x-centre.x)>1 || Math.Abs(t.z-centre.z)>1) continue;
                double amount=t.level*(t.id==target?600:150);
                switch(t.kind){case LandKind.Grain:yield.grain+=amount;break;case LandKind.Pasture:yield.grass+=amount;break;case LandKind.Stone:yield.stone+=amount;break;case LandKind.Iron:yield.iron+=amount;break;}
            }
            return yield;
        }
        public string Garrison(int target) { return IssueTerritoryOrder(target,false); }
        public string Farm(int target) { return IssueTerritoryOrder(target,true); }
        private bool IsTrainingLand(int target)
        {
            if (target < 0 || target >= lands.Count) return false;
            var tile = lands[target];
            return tile.owned && tile.Walkable && !tile.home && !tile.capital && !tile.bridge && !IsCityDistrict(target);
        }
        public int TrainingExperience(int target)
        { return IsTrainingLand(target) ? lands[target].level * 100 : 0; }
        public string TrainingProblem(int target)
        {
            if (!IsYouzhou) return "地块练兵在幽州 S1 开放。";
            if (order != null) return "当前部队正在执行指令，请等待返回主城。";
            if (!IsTrainingLand(target)) return "请选择已占领且位于城区以外的普通资源地。";
            if (troops < 50 || ActiveGenerals.Count == 0) return "请先配置武将并征兵，至少需要 50 兵力。";
            if (ActiveGenerals.All(c => c.level >= c.LevelCap)) return "上阵武将均已达到等级上限。";
            if (AvailableCommands < TrainingCommandCost) return "练兵需要 2 政令。";
            if (FindMarchPath(homeId, target) == null) return "没有可通行的陆路，请寻找桥梁。";
            return "";
        }
        public string StartTraining(int target)
        {
            string problem = TrainingProblem(target); if (problem.Length > 0) return problem;
            EnsureSeasonState(); commands -= TrainingCommandCost;
            order = CreateMovement(target, ArmyAction.Training, FindMarchPath(homeId, target));
            order.trainingVersion = 1;
            order.trainingParticipants = ActiveGenerals.Select(c => c.uid).ToList();
            order.trainingExperience = TrainingExperience(target);
            const string result = "练兵部队已出发，消耗 2 政令；抵达后开始 60 秒练兵。";
            Note(result); return result;
        }
        private void SettleTraining(bool cancelled)
        {
            if (order == null || order.action != (int)ArmyAction.Training || order.trainingSettled) return;
            var receipt = new TrainingReceipt { target = order.target, experiencePerGeneral = order.trainingExperience,
                completedAt = elapsed, cancelled = cancelled, participantIds = new List<int>(order.trainingParticipants) };
            // Mark before transitioning to return. Replay, loading and further ticks cannot award this order again.
            order.trainingSettled = true; order.trainingCancelled = cancelled;
            foreach (int uid in receipt.participantIds)
            {
                var card = cards.Find(c => c.uid == uid);
                int earned = 0;
                if (!cancelled && card != null && card.level < card.LevelCap)
                {
                    int capacity = -card.experience;
                    for (int level = card.level; level < card.LevelCap; level++) capacity += level * 80;
                    earned = Math.Max(0, Math.Min(order.trainingExperience, capacity));
                    card.GainExperience(earned);
                }
                receipt.awardedExperience.Add(earned);
            }
            if (trainingHistory == null) trainingHistory = new List<TrainingReceipt>();
            trainingHistory.Add(receipt);
            if (trainingHistory.Count > 10) trainingHistory.RemoveAt(0);
            if (!cancelled) RecordCityAction("train");
        }
        private string IssueTerritoryOrder(int target, bool farm)
        {
            string problem=TerritoryOrderProblem(target,farm); if(problem.Length>0)return problem;
            EnsureSeasonState(); if(farm)commands-=FarmCommandCost;
            order=CreateMovement(target,farm?ArmyAction.Farm:ArmyAction.Garrison,FindMarchPath(homeId,target));
            RecordCityAction(farm ? "farm" : "garrison");
            string message=farm?"屯田部队已出发，消耗 2 政令。":"部队已出发，抵达后持续驻守。"; Note(message); return message;
        }
        public double ArmyRouteProgress
        {
            get
            {
                if(order==null)return 0;
                if(order.phase!=(int)ArmyPhase.Marching)return 1;
                double fraction=order.duration<=0?1:Math.Max(0,Math.Min(1,1-order.remaining/order.duration));
                return order.routeVersion>=1 ? order.routeStart+(order.routeEnd-order.routeStart)*fraction : order.returning?1-fraction:fraction;
            }
        }
        public void ArmyPosition(out double x,out double z)
        {
            var home=lands[homeId]; x=home.x;z=home.z;if(order==null)return;
            double progress=ArmyRouteProgress;
            if(order.routeVersion>=1 && order.path!=null && order.path.Count>1)
            {
                double remaining = Math.Max(0, Math.Min(1, progress)) * MarchPathLength(order.path, order.routeVersion);
                for (int i = 1; i < order.path.Count; i++)
                {
                    var a = lands[order.path[i - 1]]; var b = lands[order.path[i]];
                    double length = order.routeVersion == 1 ? 1 : Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z));
                    if (remaining <= length || i == order.path.Count - 1)
                    { double f = length == 0 ? 0 : Math.Max(0, Math.Min(1, remaining / length)); x = a.x + (b.x-a.x)*f; z = a.z + (b.z-a.z)*f; break; }
                    remaining -= length;
                }
            }
            else {var t=lands[order.target];x=home.x+(t.x-home.x)*progress;z=home.z+(t.z-home.z)*progress;}
        }
        public string RecallArmy()
        {
            if(order==null)return "部队已在主城。";
            if(order.returning)return "部队已在返城途中。";
            double progress=ArmyRouteProgress;
            SettleTraining(true);
            BeginReturn(progress); Note("部队已撤回，未完成的屯田或练兵不发放收益，已消耗政令不退还。"); return "已下达撤回指令。";
        }
        private void BeginReturn(double progress)
        {
            if(order.routeVersion>=1)
            {
                order.routeStart=progress;order.routeEnd=0;
                order.duration=Math.Max(.001,MarchDurationSeconds(order.path,order.routeVersion)*progress);
            }
            else order.duration=Math.Max(.001,order.duration*progress);
            order.returning=true;order.phase=0;order.remaining=order.duration;
        }
        private void CompleteArmyPhase()
        {
            if(order.returning){order=null;Note("部队已返回主城，可以补兵、调整编队。");return;}
            var t=lands[order.target];
            if (order.action == (int)ArmyAction.Training)
            {
                if (!IsTrainingLand(t.id))
                { SettleTraining(true); Note("练兵目标已不可用，未发放经验，部队返回主城。"); BeginReturn(1); return; }
                if (order.phase == (int)ArmyPhase.Working)
                { SettleTraining(false); Note("练兵完成，经验已结算，部队返回主城。"); BeginReturn(1); }
                else
                { order.phase = (int)ArmyPhase.Working; order.duration = TrainingSeconds; order.remaining = TrainingSeconds; Note("抵达目标土地，开始练兵。"); }
                return;
            }
            if(order.action!=0&&!t.owned){Note("目标已失去控制，部队返回主城。");BeginReturn(1);return;}
            if (order.action == (int)ArmyAction.Farm && IsCityDistrict(t.id))
            { Note("此地已划入主城城区，屯田结束，部队返回主城。"); BeginReturn(1); return; }
            if(order.action==(int)ArmyAction.Garrison)
            {order.phase=(int)ArmyPhase.Stationed;order.remaining=0;Note("已驻守"+t.name+"，覆盖本格及相邻八格领地。");return;}
            if(order.action==(int)ArmyAction.Farm)
            {
                if(order.phase==(int)ArmyPhase.Working)
                {
                    var gain=FarmYield(t.id);resources.grain+=gain.grain;resources.grass+=gain.grass;resources.stone+=gain.stone;resources.iron+=gain.iron;
                    RecordCityAction("farm_complete");
                    Note("屯田完成：粮 "+gain.grain+" / 草 "+gain.grass+" / 石 "+gain.stone+" / 铁 "+gain.iron+"，部队返回。"); BeginReturn(1);
                }
                else {order.phase=(int)ArmyPhase.Working;order.duration=FarmSeconds;order.remaining=FarmSeconds;Note("抵达资源地，开始屯田。");}
                return;
            }
            // A pre-city save may still be marching to one of its former adjacent
            // resource tiles. City migration must never make the army fight itself.
            if (IsYouzhou && t.owned && IsCityDistrict(t.id))
            { Note("目标已归入主城城区，部队直接返城。"); BeginReturn(1); return; }
            ResolveBattle(t);BeginReturn(1);
        }
        public bool IsGuarded(int target)
        {
            if(order==null || order.returning || order.action!=(int)ArmyAction.Garrison || order.phase!=(int)ArmyPhase.Stationed || target<0 || target>=lands.Count)return false;
            var a=lands[order.target];var b=lands[target];return b.owned && b.Walkable && Math.Abs(a.x-b.x)<=1 && Math.Abs(a.z-b.z)<=1;
        }
    }
}
