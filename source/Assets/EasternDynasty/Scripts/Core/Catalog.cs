using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public static partial class Catalog
    {
        // This is a small, editable prototype roster, not the final 160-character catalogue.
        public static readonly List<GeneralDefinition> Generals = new List<GeneralDefinition>
        {
            new GeneralDefinition("han_scout", "汉军斥候", Homeland.Han, 1, 24, "突击", "侦知敌情，先声夺人。"),
            new GeneralDefinition("han_officer", "汉军校尉", Homeland.Han, 2, 42, "坚守", "整肃军阵，护持中军。"),
            new GeneralDefinition("huangfu_song", "皇甫嵩", Homeland.Han, 3, 70, "坚守", "临危受命，平定黄巾。"),
            new GeneralDefinition("zhu_jun", "朱儁", Homeland.Han, 3, 72, "突击", "临阵决断，奋兵进击。"),
            new GeneralDefinition("liu_bei", "刘备", Homeland.Han, 4, 74, "抚军", "安抚士卒，集众成军。"),
            new GeneralDefinition("sun_jian", "孙坚", Homeland.Han, 4, 86, "突击", "江东猛虎，锐意进取。"),
            new GeneralDefinition("cao_cao", "曹操", Homeland.Han, 5, 94, "坚守", "统御诸军，审势而动。"),
            new GeneralDefinition("guan_yu", "关羽", Homeland.Han, 5, 98, "突击", "威震诸军，长驱破阵。"),
            new GeneralDefinition("zhou_yu", "周瑜", Homeland.Han, 5, 94, "抚军", "运筹帷幄，协同诸军。"),
            new GeneralDefinition("steppe_scout", "草原游骑", Homeland.Steppe, 1, 28, "突击", "逐水草而居，乘长风而战。"),
            new GeneralDefinition("steppe_officer", "鲜卑百夫长", Homeland.Steppe, 2, 46, "坚守", "率领部众，守护营地。"),
            new GeneralDefinition("qiuliju", "丘力居", Homeland.Steppe, 3, 73, "抚军", "聚拢诸部，驰骋塞外。"),
            new GeneralDefinition("budugen", "步度根", Homeland.Steppe, 4, 85, "坚守", "控弦之众，列阵草原。"),
            new GeneralDefinition("kebineng", "轲比能", Homeland.Steppe, 5, 96, "突击", "统合部众，纵骑千里。"),
            new GeneralDefinition("forest_scout", "山林猎手", Homeland.Northeast, 1, 26, "突击", "循踪山林，熟谙地形。"),
            new GeneralDefinition("liaodong_officer", "辽东军候", Homeland.Northeast, 2, 44, "坚守", "驻守边城，护卫商路。"),
            new GeneralDefinition("gongsun_gong", "公孙恭", Homeland.Northeast, 3, 68, "抚军", "镇守辽东，安辑部众。"),
            new GeneralDefinition("gongsun_kang", "公孙康", Homeland.Northeast, 4, 84, "突击", "经营边郡，进取四邻。"),
            new GeneralDefinition("gongsun_du", "公孙度", Homeland.Northeast, 5, 93, "坚守", "据守辽东，开拓疆土。")
        };
        static Catalog() { Generals.AddRange(S1Roster()); }
        public static readonly string[] S1Factions = { "全部", "汉", "魏", "蜀", "吴", "群", "晋" };
        public static readonly string[] Regions = { "梁州", "荆州", "扬州", "雍州", "豫州", "徐州", "冀州", "兖州", "青州", "鲜卑诸部", "南部草原", "辽东汉地", "山林诸部" };
        public static readonly string[] Capitals = { "汉中", "襄阳", "秣陵", "长安", "许县", "下邳", "邺城", "陈留", "临淄", "北庭部落", "云中营地", "襄平", "山林大寨" };
        public static string OriginName(Homeland value) { return value == Homeland.Han ? "汉地 · 九州" : value == Homeland.Steppe ? "草原 · 诸部" : "东北 · 边地"; }
        public static string LandName(LandKind value)
        { return new[] { "粮田", "草场", "石场", "铁场", "河流", "山地" }[(int)value]; }
    }

}
