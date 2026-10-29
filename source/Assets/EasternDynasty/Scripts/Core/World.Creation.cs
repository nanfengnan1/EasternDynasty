using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public partial class World
    {
        public static World Create(Homeland origin, int region, string ruler)
        {
            if (region < 0 || region >= Catalog.Regions.Length || RegionOrigin(region) != origin) throw new ArgumentException("出生地与区域不一致。");
            var world = new World { origin = origin, birthRegion = region, ruler = string.IsNullOrWhiteSpace(ruler) ? "新任太守" : ruler.Trim().Substring(0, Math.Min(12, ruler.Trim().Length)) };
            world.resources = new Supplies { grain = 4000, grass = 3000, stone = 1800, iron = 900, gold = 1600, jade = 300 };
            world.CreateMap();
            var capital = world.lands.Find(t => t.capital && t.region == region);
            var home = world.lands.Where(t => t.region == region && !t.capital && t.x == capital.x - 2 && t.z == capital.z).First();
            home.home = true; home.owned = true; home.kind = LandKind.Grain; home.level = 1; home.name = origin == Homeland.Han ? "新城" : "新营地";
            world.homeId = home.id;
            foreach (var near in world.lands.Where(t => Math.Abs(t.x - home.x) + Math.Abs(t.z - home.z) == 1))
            { near.kind = LandKind.Grain; near.name = Catalog.LandName(near.kind); }
            foreach (int stars in new[] { 3, 2, 1 })
            {
                var definition = Catalog.Generals.Find(g => g.homeland == origin && g.stars == stars);
                world.formation.Add(world.AddCard(definition.id).uid);
            }
            world.Note("立足" + Catalog.Regions[region] + "。先占领周边一级资源地，再扩展疆域。");
            world.Note("试玩赠送 300 灵玉；本版本没有充值和真实支付。");
            return world;
        }

        public static Homeland RegionOrigin(int region) { return region < 9 ? Homeland.Han : region < 11 ? Homeland.Steppe : Homeland.Northeast; }
        public static World CreateYouzhou(int startIndex = 0, string ruler = "新任太守")
        {
            if (startIndex < 0 || startIndex >= YouzhouMapDefinition.StartNames.Length)
                throw new ArgumentOutOfRangeException("startIndex", "幽州出生点无效。");
            var world = new World
            {
                saveVersion = 2, mapId = YouzhouMapDefinition.Id,
                mapWidth = YouzhouMapDefinition.Width, mapHeight = YouzhouMapDefinition.Height,
                mapRevision = YouzhouMapDefinition.Revision, origin = Homeland.Han,
                ruler = string.IsNullOrWhiteSpace(ruler) ? "新任太守" : ruler.Trim().Substring(0, Math.Min(12, ruler.Trim().Length)),
                resources = new Supplies { grain = 4000, grass = 3000, stone = 1800, iron = 900, gold = 1600, jade = 300 },
                lands = YouzhouMapDefinition.CreateLands()
            };
            var home = world.TryGetLand(YouzhouMapDefinition.StartX(startIndex), YouzhouMapDefinition.StartZ(startIndex));
            if (home == null || !home.Walkable || home.bridge || home.capital)
                throw new InvalidOperationException("幽州出生点与地图定义不一致。");
            home.home = true; home.owned = true; home.kind = LandKind.Grain; home.level = 1; home.name = "新城";
            world.homeId = home.id; world.birthRegion = home.region;
            // Project-specific trial starters from the RAG archive; not a claim about official S1 gifts.
            foreach (string id in new[] { "s1_100039", "s1_100744", "s1_100746" })
                world.formation.Add(world.AddCard(id).uid);
            world.EnsureCityState();
            world.Note("立足幽州 · " + YouzhouMapDefinition.StartNames[startIndex] + "。先占领周边一级资源地。");
            world.Note("幽州地理为规划示意，并非历史地理复原；桥梁可通行但不产出粮食。");
            world.Note("试玩赠送 300 灵玉；本版本没有充值和真实支付。");
            world.Note("皇甫嵩、王子服、郑泰为本项目试玩赠将，并非官方新手奖励复刻。");
            return world;
        }
        public static int RegionAt(int x, int z)
        {
            if (x >= 26 && z >= 17) return z < 22 ? 11 : 12;
            if (z >= 21) return x < 13 ? 9 : 10;
            return Math.Min(2, z / 7) * 3 + Math.Min(2, x / 12);
        }

        private void CreateMap()
        {
            var rng = new Random(184);
            for (int z = 0; z < Height; z++) for (int x = 0; x < Width; x++)
            {
                int region = RegionAt(x, z);
                int roll = rng.Next(100);
                LandKind kind = roll < 39 ? LandKind.Grain : roll < 66 ? LandKind.Pasture : roll < 82 ? LandKind.Stone : LandKind.Iron;
                if (region == 9 || region == 10) kind = roll < 60 ? LandKind.Pasture : kind;
                if ((x == 18 + (z % 5 == 0 ? 1 : 0)) && z < 17 && z != 5 && z != 12) kind = LandKind.Water;
                if (roll > 94 && x > 2 && x < 32 && kind != LandKind.Water) kind = LandKind.Mountain;
                int level = 1 + rng.Next(5);
                lands.Add(new Land { id = z * Width + x, x = x, z = z, region = region, homeland = RegionOrigin(region), level = level, kind = kind, name = Catalog.LandName(kind) });
            }
            int[,] centers = { {6,3},{17,3},{29,3},{6,10},{17,10},{29,10},{6,17},{17,17},{29,15},{6,24},{19,24},{30,19},{30,24} };
            for (int i = 0; i < Catalog.Regions.Length; i++)
            {
                var city = lands[centers[i, 1] * Width + centers[i, 0]];
                city.capital = true; city.kind = LandKind.Grain; city.level = 3 + i % 2; city.name = Catalog.Capitals[i];
                // Starter farmland must not erase the river or mountain geography.
                foreach (var near in lands.Where(t => Math.Abs(t.x - city.x) + Math.Abs(t.z - city.z) <= 4 && !t.capital && t.Walkable))
                { near.kind = LandKind.Grain; near.name = Catalog.LandName(near.kind); near.level = 1 + ((near.x + near.z) % 2); }
            }
        }

    }
}
