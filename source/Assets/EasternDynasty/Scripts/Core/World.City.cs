using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EasternDynasty
{
    public partial class World
    {
        [OptionalField] public int cityVersion;
        [OptionalField] public List<CityPlot> cityPlots = new List<CityPlot>();
        [OptionalField] public List<CityConstruction> cityConstruction = new List<CityConstruction>();
        [OptionalField] public List<int> cityClaimedQuests = new List<int>();
        [OptionalField] public int cityRecruitActions, cityFarmCompletions, cityGarrisonActions, cityOccupyActions;
        [OptionalField] public int cityTrainingCompletions;
        [OptionalField] public bool cityVisited;

        public bool IsCityDistrict(int landId)
        {
            if(!IsYouzhou||lands==null||homeId<0||homeId>=lands.Count||landId<0||landId>=lands.Count)return false;
            var home=lands[homeId];var tile=lands[landId];
            return home!=null&&tile!=null&&tile.Walkable&&!tile.bridge&&!tile.capital&&Math.Abs(tile.x-home.x)<=1&&Math.Abs(tile.z-home.z)<=1;
        }
        public void EnsureCityState()
        {
            if(!IsYouzhou||cityVersion!=0||lands==null||homeId<0||homeId>=lands.Count)return;
            var home=lands[homeId];
            cityPlots=new List<CityPlot>();cityConstruction=new List<CityConstruction>();cityClaimedQuests=new List<int>();
            for(int index=0;index<9;index++)
            {
                var tile=TryGetLand(home.x+index%3-1,home.z+1-index/3);
                cityPlots.Add(new CityPlot{index=index,landId=tile==null?-1:tile.id,building=index==4?CityBuilding.TownHall:CityBuilding.None,level=index==4?1:0});
                // Preserve the original terrain, levels and names; never erase mountains, water, bridges or default cities.
                if(tile!=null&&IsCityDistrict(tile.id))tile.owned=true;
            }
            cityRecruitActions=cityFarmCompletions=cityGarrisonActions=cityOccupyActions=0;cityVisited=false;cityVersion=1;
        }
        public CityPlot CityPlotAt(int landId)
        { return cityPlots==null?null:cityPlots.Find(p=>p!=null&&p.landId==landId); }
        public int CityLevel(CityBuilding building)
        {
            if(!IsYouzhou)return 0;
            if(cityVersion==0)return building==CityBuilding.TownHall?1:0;
            var plot=cityPlots==null?null:cityPlots.Find(p=>p!=null&&p.building==building);
            return plot==null?0:plot.level;
        }
        public int CityTroopBonus { get { return CityCatalog.EffectAtLevel(CityBuilding.Barracks,CityLevel(CityBuilding.Barracks))+CityCatalog.EffectAtLevel(CityBuilding.DrillGround,CityLevel(CityBuilding.DrillGround)); } }
        public CityConstruction CityJob { get { return cityConstruction==null||cityConstruction.Count==0?null:cityConstruction[0]; } }
        public Supplies CityHourlyIncome()
        { return new Supplies{grain=CityCatalog.EffectAtLevel(CityBuilding.Mill,CityLevel(CityBuilding.Mill)),grass=CityCatalog.EffectAtLevel(CityBuilding.Lumberyard,CityLevel(CityBuilding.Lumberyard)),stone=CityCatalog.EffectAtLevel(CityBuilding.Quarry,CityLevel(CityBuilding.Quarry)),iron=CityCatalog.EffectAtLevel(CityBuilding.Ironworks,CityLevel(CityBuilding.Ironworks)),gold=CityCatalog.EffectAtLevel(CityBuilding.Residence,CityLevel(CityBuilding.Residence))}; }

        public Supplies CityBuildCost(CityBuilding kind,int level)
        {
            if(CityCatalog.Get(kind)==null||level<1||level>10)return new Supplies();
            double factor=Math.Pow(1.65,level-1)*(kind==CityBuilding.TownHall?1.8:kind==CityBuilding.Barracks?1.35:1);
            return new Supplies{grain=Math.Ceiling(100*factor),grass=Math.Ceiling(90*factor),stone=Math.Ceiling(150*factor),iron=Math.Ceiling(35*factor),gold=Math.Ceiling(40*factor)};
        }
        public double CityBuildSeconds(CityBuilding kind,int level)
        { return Math.Max(5,(kind==CityBuilding.TownHall?30:20)*Math.Max(1,level)*(1-CityCatalog.EffectAtLevel(CityBuilding.Workshop,CityLevel(CityBuilding.Workshop))/100d)); }
        public string CityBuildProblem(int index,CityBuilding kind)
        {
            if(!IsYouzhou)return "城建在幽州开放。";
            EnsureCityState();
            if(index<0||index>=cityPlots.Count)return "城区位置无效。";
            var plot=cityPlots[index];var definition=CityCatalog.Get(kind);
            if(!IsCityDistrict(plot.landId))return "此处地形不可建设。";
            if(definition==null)return "请选择建设设施。";
            if(CityJob!=null)return "工匠正在施工，请等待当前建设完成。";
            if((index==4)!=(kind==CityBuilding.TownHall))return "城主府位于主城中央。";
            if(plot.building!=CityBuilding.None&&plot.building!=kind)return "此城区已有其他设施。";
            if(cityPlots.Any(p=>p.index!=index&&p.building==kind))return "该设施已经建成，请选择原位置升级。";
            int target=plot.level+1;
            if(target>definition.maximumLevel)return "设施已达最高等级。";
            if(CityLevel(CityBuilding.TownHall)<definition.unlockLevel)return "需要城主府 "+definition.unlockLevel+" 级。";
            if(CityLevel(CityBuilding.TownHall)<CityCatalog.RequiredHallLevel(kind,target))return "请先提升城主府等级。";
            var price=CityBuildCost(kind,target);
            if(resources.grain<price.grain||resources.grass<price.grass||resources.stone<price.stone||resources.iron<price.iron||resources.gold<price.gold)return "建设资源不足。";
            return "";
        }
        public string StartCityConstruction(int index,CityBuilding kind)
        {
            string problem=CityBuildProblem(index,kind);if(problem.Length>0)return problem;
            var plot=cityPlots[index];int target=plot.level+1;var cost=CityBuildCost(kind,target);
            if(!resources.Spend(cost.grain,cost.grass,cost.stone,cost.iron,cost.gold))return "建设资源不足。";
            double duration=CityBuildSeconds(kind,target);
            cityConstruction.Add(new CityConstruction{plotIndex=index,building=kind,targetLevel=target,paid=cost,duration=duration,remaining=duration});
            string result=CityCatalog.Get(kind).name+"已开工，目标 "+target+" 级。";Note(result);return result;
        }
        public Supplies CityConstructionRefund()
        {
            var job=CityJob;
            if(job==null||job.paid==null)return new Supplies();
            // R08 (2026-04-15) describes full construction interruption refunds.
            // Use the original receipt, never a recalculated price, and keep previews detached.
            var paid=job.paid;
            return new Supplies{grain=paid.grain,grass=paid.grass,stone=paid.stone,iron=paid.iron,gold=paid.gold};
        }
        public string CancelCityConstruction()
        {
            var job=CityJob;if(job==null)return "当前没有建设队列。";
            var refund=CityConstructionRefund();cityConstruction.Clear();AddCityResources(refund,1);
            return "建设已取消，返还 100% 已投入资源。";
        }
        private void AdvanceCityConstruction(double seconds)
        {
            var job=CityJob;if(job==null)return;
            job.remaining=Math.Max(0,job.remaining-seconds);
            if(job.remaining>.000001)return;
            var plot=cityPlots[job.plotIndex];plot.building=job.building;plot.level=job.targetLevel;
            string result=CityCatalog.Get(job.building).name+"已升至 "+job.targetLevel+" 级。";cityConstruction.Clear();Note(result);
        }
        public void RecordCityAction(string action)
        {
            if(!IsYouzhou)return;EnsureCityState();
            if(action=="visit")cityVisited=true;
            if(action=="recruit")cityRecruitActions=Math.Min(100000,cityRecruitActions+1);
            if(action=="farm_complete")cityFarmCompletions=Math.Min(100000,cityFarmCompletions+1);
            if(action=="garrison")cityGarrisonActions=Math.Min(100000,cityGarrisonActions+1);
            if(action=="occupy")cityOccupyActions=Math.Min(100000,cityOccupyActions+1);
            if(action=="train")cityTrainingCompletions=Math.Min(100000,cityTrainingCompletions+1);
        }
        public CityQuestDefinition CurrentCityQuest
        { get { if(!IsYouzhou)return null;return CityCatalog.Quests.FirstOrDefault(q=>cityClaimedQuests==null||!cityClaimedQuests.Contains(q.id)); } }
        public int CityQuestProgress(int id)
        {
            switch(id)
            {
                case 1:return cityVisited?1:0;
                case 2:return CityLevel(CityBuilding.Mill);
                case 3:return CityLevel(CityBuilding.TownHall);
                case 4:return ActiveGenerals.Count;
                case 5:return cityRecruitActions;
                case 6:case 10:return lands.Count(t=>t.owned&&!t.home&&!t.capital&&!t.bridge&&!IsCityDistrict(t.id)&&t.Walkable);
                case 7:return CityLevel(CityBuilding.Barracks);
                case 8:return cityFarmCompletions;
                case 9:return CityLevel(CityBuilding.TownHall);
                default:return 0;
            }
        }
        public int CityQuestTarget(int id) { return id==3?2:id==4||id==9||id==10?3:1; }
        public bool CityQuestReady(int id)
        { return IsYouzhou&&CityCatalog.Quests.Any(q=>q.id==id)&&CityQuestProgress(id)>=CityQuestTarget(id); }
        public string ClaimCityQuest(int id)
        {
            if(!IsYouzhou)return "主要事宜在幽州开放。";EnsureCityState();
            if(cityClaimedQuests.Contains(id))return "这份任务奖励已经领取。";
            var quest=CurrentCityQuest;
            if(quest==null||quest.id!=id)return "请先完成当前主要事宜。";
            if(!CityQuestReady(id))return "任务目标尚未完成。";
            cityClaimedQuests.Add(id);AddCityResources(quest.reward,1);string result="「"+quest.title+"」已完成，资源奖励已入库。";Note(result);return result;
        }
        private void AddCityResources(Supplies supplies,double factor)
        { resources.grain+=supplies.grain*factor;resources.grass+=supplies.grass*factor;resources.stone+=supplies.stone*factor;resources.iron+=supplies.iron*factor;resources.gold+=supplies.gold*factor; }

        private bool ValidCityState()
        {
            if(cityVersion==0)return true;
            if(!IsYouzhou||cityVersion!=1||cityPlots==null||cityPlots.Count!=9||cityConstruction==null||cityConstruction.Count>1||cityConstruction.Any(j=>j==null)||cityClaimedQuests==null)return false;
            if(cityRecruitActions<0||cityFarmCompletions<0||cityGarrisonActions<0||cityOccupyActions<0)return false;
            if(cityClaimedQuests.Count>CityCatalog.Quests.Length||cityClaimedQuests.Distinct().Count()!=cityClaimedQuests.Count)return false;
            for(int i=0;i<cityClaimedQuests.Count;i++)if(cityClaimedQuests[i]!=i+1)return false;
            var home=lands[homeId];var used=new HashSet<CityBuilding>();
            for(int i=0;i<9;i++)
            {
                var p=cityPlots[i];var t=TryGetLand(home.x+i%3-1,home.z+1-i/3);
                if(p==null||p.index!=i||p.landId!=(t==null?-1:t.id))return false;
                if(p.building==CityBuilding.None){if(p.level!=0||i==4)return false;continue;}
                var def=CityCatalog.Get(p.building);
                if(def==null||p.level<1||p.level>def.maximumLevel||!IsCityDistrict(p.landId)||!used.Add(p.building)||(i==4)!=(p.building==CityBuilding.TownHall))return false;
            }
            if(CityJob!=null)
            {
                var j=CityJob;var def=CityCatalog.Get(j.building);
                if(def==null||j.plotIndex<0||j.plotIndex>=9||!Finite(j.duration)||!Finite(j.remaining)||j.duration<=0||j.remaining<0||j.remaining>j.duration||j.paid==null)return false;
                var p=cityPlots[j.plotIndex];
                if(!IsCityDistrict(p.landId)||j.targetLevel!=p.level+1||j.targetLevel>def.maximumLevel||(j.plotIndex==4)!=(j.building==CityBuilding.TownHall)||
                    (p.building!=CityBuilding.None&&p.building!=j.building)||cityPlots.Any(q=>q.index!=j.plotIndex&&q.building==j.building))return false;
                var paid=CityBuildCost(j.building,j.targetLevel);
                if(j.paid.grain!=paid.grain||j.paid.grass!=paid.grass||j.paid.stone!=paid.stone||j.paid.iron!=paid.iron||j.paid.gold!=paid.gold||j.paid.jade!=0)return false;
            }
            return true;
        }
    }
}
