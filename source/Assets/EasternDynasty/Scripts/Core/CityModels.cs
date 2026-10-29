using System;
using System.Collections.Generic;

namespace EasternDynasty
{
    public enum CityBuilding { None, TownHall, Mill, Lumberyard, Quarry, Ironworks, Residence, Barracks, DrillGround, Workshop }

    [Serializable]
    public sealed class CityPlot
    {
        public int index, landId, level;
        public CityBuilding building;
    }

    [Serializable]
    public sealed class CityConstruction
    {
        public int plotIndex, targetLevel;
        public CityBuilding building;
        public double remaining, duration;
        // Original resource receipt. Preserve this save field when refund policy changes.
        public Supplies paid = new Supplies();
    }

    public sealed class CityBuildingDefinition
    {
        public CityBuilding kind;
        public string name, description, color, symbol;
        public int unlockLevel, maximumLevel;
        public CityBuildingDefinition(CityBuilding kind, string name, string description, string color, string symbol, int unlockLevel, int maximumLevel = 10)
        { this.kind=kind;this.name=name;this.description=description;this.color=color;this.symbol=symbol;this.unlockLevel=unlockLevel;this.maximumLevel=maximumLevel; }
    }

    public sealed class CityQuestDefinition
    {
        public int id;
        public string title, objective, action;
        public CityBuilding building;
        public Supplies reward;
        public CityQuestDefinition(int id,string title,string objective,string action,CityBuilding building,double grain,double grass,double stone,double iron,double gold)
        { this.id=id;this.title=title;this.objective=objective;this.action=action;this.building=building;reward=new Supplies{grain=grain,grass=grass,stone=stone,iron=iron,gold=gold}; }
    }

    public static class CityCatalog
    {
        // Facility roles follow the local S1 guide. All costs, levels, rewards and times are prototype tuning.
        public static readonly CityBuildingDefinition[] Buildings = {
            new CityBuildingDefinition(CityBuilding.TownHall,"城主府","提升城主府等级，解锁设施与更高建设等级。","d9b16d","府",1,8),
            new CityBuildingDefinition(CityBuilding.Mill,"磨坊","每级增加 160 粮食/小时。","d7bf67","粮",1),
            new CityBuildingDefinition(CityBuilding.Lumberyard,"伐木场","每级增加 140 木材/小时。","70b590","木",1),
            new CityBuildingDefinition(CityBuilding.Quarry,"采石场","每级增加 120 石料/小时。","a7b3c6","石",1),
            new CityBuildingDefinition(CityBuilding.Ironworks,"炼铁场","每级增加 80 铁矿/小时。","8bb3c8","铁",1),
            new CityBuildingDefinition(CityBuilding.Residence,"民居","每级增加 100 金币/小时。","e1b174","民",1),
            new CityBuildingDefinition(CityBuilding.Barracks,"兵营","每级提高部队带兵上限 250。","c66f60","兵",2),
            new CityBuildingDefinition(CityBuilding.DrillGround,"校场","每级提高部队带兵上限 100。","8d9fcf","武",2),
            new CityBuildingDefinition(CityBuilding.Workshop,"工坊","每级缩短新建设工期 5%，最多 40%。","a18abd","工",3)
        };
        public static readonly CityQuestDefinition[] Quests = {
            new CityQuestDefinition(1,"立足幽州","查看主城与九格城区","city",CityBuilding.TownHall,300,250,250,100,100),
            new CityQuestDefinition(2,"兴修农事","建成一座 1 级磨坊","city",CityBuilding.Mill,500,400,400,150,100),
            new CityQuestDefinition(3,"扩建府署","将城主府升至 2 级","city",CityBuilding.TownHall,650,500,550,220,150),
            new CityQuestDefinition(4,"点将出阵","配置一支三将部队","formation",CityBuilding.None,350,250,150,100,150),
            new CityQuestDefinition(5,"整军备战","完成一次征兵","recruit",CityBuilding.Barracks,450,350,150,120,100),
            new CityQuestDefinition(6,"开疆拓土","占领一块城区外资源地","territory",CityBuilding.None,650,500,550,220,180),
            new CityQuestDefinition(7,"扩充兵营","建成一座 1 级兵营","city",CityBuilding.Barracks,750,600,650,300,200),
            new CityQuestDefinition(8,"以战养战","在己方资源地完成一次屯田","farm",CityBuilding.None,1000,800,800,400,250),
            new CityQuestDefinition(9,"安定郡县","将城主府升至 3 级","city",CityBuilding.TownHall,1000,800,1000,500,300),
            new CityQuestDefinition(10,"沃野千里","拥有三块城区外资源地","territory",CityBuilding.None,1500,1200,1200,650,400)
        };
        public static CityBuildingDefinition Get(CityBuilding kind) { foreach(var d in Buildings)if(d.kind==kind)return d;return null; }

        // Shared by the actual rules and the upgrade preview. These are project tuning,
        // not imported values from historical facility-tree screenshots.
        public static int EffectAtLevel(CityBuilding kind,int level)
        {
            var definition=Get(kind);if(definition==null)return 0;
            level=Math.Max(0,Math.Min(definition.maximumLevel,level));
            switch(kind)
            {
                case CityBuilding.TownHall:return level==0?0:level+2;
                case CityBuilding.Mill:return level*160;
                case CityBuilding.Lumberyard:return level*140;
                case CityBuilding.Quarry:return level*120;
                case CityBuilding.Ironworks:return level*80;
                case CityBuilding.Residence:return level*100;
                case CityBuilding.Barracks:return level*250;
                case CityBuilding.DrillGround:return level*100;
                case CityBuilding.Workshop:return Math.Min(40,level*5);
                default:return 0;
            }
        }
        public static int RequiredHallLevel(CityBuilding kind,int targetLevel)
        {
            var definition=Get(kind);if(definition==null||kind==CityBuilding.TownHall)return 0;
            return Math.Max(definition.unlockLevel,targetLevel-2);
        }
    }
}
