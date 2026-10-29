using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public partial class World
    {
        public bool IsValid(out string problem)
        {
            problem = "";
            bool legacyMetadata = saveVersion == 1 && (string.IsNullOrEmpty(mapId) || mapId == LegacyMapId)
                && ((mapWidth == 0 && mapHeight == 0) || (mapWidth == Width && mapHeight == Height))
                && (mapRevision == 0 || mapRevision == 1);
            bool youzhouMetadata = IsYouzhou && mapWidth == YouzhouMapDefinition.Width
                && mapHeight == YouzhouMapDefinition.Height && mapRevision == YouzhouMapDefinition.Revision;
            int regionCount = IsYouzhou ? YouzhouMapDefinition.RegionNames.Length : Catalog.Regions.Length;
            if (!legacyMetadata && !youzhouMetadata) problem = "地图标识、尺寸或存档版本不兼容。";
            else if (lands == null || lands.Count != MapWidth * MapHeight || resources == null || cards == null || formation == null || reports == null || messages == null || learnedSkills == null || orders == null || orders.Count > 1 || orders.Any(o => o == null)) problem = "存档格式不兼容。";
            else if (lands.Where((t, i) => t == null || t.id != i || t.x != i % MapWidth || t.z != i / MapWidth
                || t.region < 0 || t.region >= regionCount || (int)t.kind < 0 || (int)t.kind > (int)LandKind.Mountain
                || (int)t.homeland < 0 || (int)t.homeland > (int)Homeland.Northeast || t.level < 1 || t.level > 5
                || string.IsNullOrEmpty(t.name) || (t.home && t.capital)
                || (IsYouzhou && t.homeland != Homeland.Han)
                || (t.bridge && (!IsYouzhou || t.kind != LandKind.Grain || t.home || t.capital))).Any()) problem = "地图记录损坏。";
            else if (homeId < 0 || homeId >= lands.Count || !lands[homeId].home || !lands[homeId].owned
                || !lands[homeId].Walkable || lands.Count(t => t.home) != 1) problem = "主城记录损坏。";
            else if (birthRegion < 0 || birthRegion >= regionCount || lands[homeId].region != birthRegion
                || lands[homeId].homeland != origin || (IsYouzhou ? origin != Homeland.Han : RegionOrigin(birthRegion) != origin)) problem = "出生地区记录损坏。";
            else if (IsYouzhou && lands.Count(t => t.capital) != YouzhouMapDefinition.CapitalNames.Length) problem = "幽州城池记录损坏。";
            else if (cards.Count == 0 || cards.Any(c => c == null || c.Definition == null || c.uid < 1 || c.experience < 0 || c.level < 1 || c.level > c.LevelCap || c.rank < 0 || c.rank > c.Definition.stars) || cards.Select(c => c.uid).Distinct().Count() != cards.Count) problem = "武将记录损坏。";
            else if (formation.Count < 1 || formation.Count > 3 || formation.Distinct().Count() != formation.Count || formation.Any(id => !cards.Exists(c => c.uid == id))) problem = "编队记录损坏。";
            else if (order != null && (order.target < 0 || order.target >= lands.Count || !lands[order.target].Walkable || !Finite(order.remaining) || !Finite(order.duration) || order.remaining < 0 || order.duration <= 0 || order.remaining > order.duration)) problem = "行军记录损坏。";
            else if (seasonMechanicsVersion < 0 || seasonMechanicsVersion > 1 || commands < 0 || commands > CommandCap || !Finite(commandRecovery) || commandRecovery<0 || commandRecovery>=CommandRecoverySeconds) problem = "政令记录损坏。";
            else if (order != null && !ValidOrderRoute()) problem = "部队状态或路线损坏。";
            else if (!ValidTrainingState()) problem = "练兵订单或结算记录损坏。";
            else if (!ValidCityState()) problem = "城区、建设队列或主要事宜记录损坏。";
            else if (reports.Any(r => !BattleReplay.IsValid(r) || r.HasReplay && (r.targetId >= lands.Count
                || r.targetX != lands[r.targetId].x || r.targetZ != lands[r.targetId].z || r.time > elapsed))) problem = "战报事件或冻结阵容损坏。";
            else if (new[] { resources.grain, resources.grass, resources.stone, resources.iron, resources.gold, elapsed }.Any(n => !Finite(n) || n < 0) || resources.jade < 0 || troops < 0 || nextCardId <= cards.Max(c => c.uid)) problem = "资源或进度记录损坏。";
            return problem.Length == 0;
        }
        private static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }
        private bool ValidOrderRoute()
        {
            var o=order;
            if(o.action<0||o.action>3||o.phase<0||o.phase>2||o.routeVersion<0||o.routeVersion>2)return false;
            if(o.routeVersion==0)return o.action==0&&o.phase==0;
            if(!IsYouzhou || o.path==null || o.path.Count<1 || o.path.Count>lands.Count || o.path[0]!=homeId || o.path[o.path.Count-1]!=o.target)return false;
            if(!Finite(o.routeStart)||!Finite(o.routeEnd)||o.routeStart<0||o.routeStart>1||o.routeEnd<0||o.routeEnd>1)return false;
            if(o.returning ? (o.phase!=0||o.routeEnd!=0) : (o.routeStart!=0||o.routeEnd!=1))return false;
            if(o.phase==1 && (o.action!=1||o.remaining!=0))return false;
            if(o.phase==2 && o.action!=2 && o.action!=3)return false;
            for(int i=0;i<o.path.Count;i++)
            {int id=o.path[i];if(id<0||id>=lands.Count||!lands[id].Walkable)return false;if(i>0&&!CanMarchStep(o.path[i-1],id,o.routeVersion))return false;}
            return true;
        }

        private bool ValidTrainingState()
        {
            if (cityTrainingCompletions < 0 || cityTrainingCompletions > 100000) return false;
            if (trainingHistory != null)
            {
                if (trainingHistory.Count > 10 || !IsYouzhou && trainingHistory.Count > 0) return false;
                foreach (var receipt in trainingHistory)
                {
                    if (receipt == null || receipt.target < 0 || receipt.target >= lands.Count
                        || receipt.experiencePerGeneral < 1 || receipt.experiencePerGeneral > 500
                        || !Finite(receipt.completedAt) || receipt.completedAt < 0 || receipt.completedAt > elapsed
                        || receipt.participantIds == null || receipt.participantIds.Count < 1 || receipt.participantIds.Count > 3
                        || receipt.participantIds.Any(id => id < 1) || receipt.participantIds.Distinct().Count() != receipt.participantIds.Count
                        || receipt.awardedExperience == null || receipt.awardedExperience.Count != receipt.participantIds.Count
                        || receipt.awardedExperience.Any(x => x < 0 || x > receipt.experiencePerGeneral || receipt.cancelled && x != 0)) return false;
                }
            }
            var active = order;
            if (active == null || active.action != (int)ArmyAction.Training) return true;
            return active.trainingVersion == 1 && active.trainingExperience > 0 && active.trainingExperience <= 500
                && active.trainingParticipants != null && active.trainingParticipants.Count >= 1 && active.trainingParticipants.Count <= 3
                && active.trainingParticipants.Distinct().Count() == active.trainingParticipants.Count
                && active.trainingParticipants.All(id => cards.Exists(c => c.uid == id))
                && active.trainingSettled == active.returning && (!active.trainingCancelled || active.trainingSettled);
        }

    }
}
