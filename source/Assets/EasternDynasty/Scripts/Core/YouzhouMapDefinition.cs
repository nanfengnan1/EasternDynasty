using System;
using System.Collections.Generic;

namespace EasternDynasty
{
    // Gameplay planning map, not a reconstruction of historical Youzhou geography.
    public static class YouzhouMapDefinition
    {
        public const string Id = "youzhou-40-v1";
        public const int Width = 40, Height = 40, Revision = 1;
        public static readonly string[] StartNames = { "蓟城近郊", "涿郡平原", "渔阳河谷" };
        public static readonly string[] RegionNames = { "蓟城与上谷", "涿郡平原", "渔阳与东部" };
        public static readonly string[] CapitalNames = { "蓟城", "涿城", "渔阳城", "上谷城", "右北平城", "辽西城" };
        private static readonly int[,] Starts = { { 9, 16 }, { 7, 6 }, { 30, 18 } };
        private static readonly int[,] Capitals = { { 11, 18 }, { 9, 7 }, { 28, 20 }, { 8, 30 }, { 33, 30 }, { 32, 9 } };
        private static readonly int[] BridgeRows = { 9, 19, 30 };

        public static int StartX(int index) { CheckStart(index); return Starts[index, 0]; }
        public static int StartZ(int index) { CheckStart(index); return Starts[index, 1]; }
        private static void CheckStart(int index)
        { if (index < 0 || index >= StartNames.Length) throw new ArgumentOutOfRangeException("index"); }
        public static int RegionAt(int x, int z) { return x >= 23 ? 2 : z < 13 ? 1 : 0; }

        internal static List<Land> CreateLands()
        {
            var lands = new List<Land>(Width * Height);
            var random = new Random(4026184);
            for (int z = 0; z < Height; z++) for (int x = 0; x < Width; x++)
            {
                int roll = random.Next(100);
                double pasture = Math.Sin(x * .31 + z * .13) + Math.Cos(z * .27 - x * .09);
                LandKind kind = roll < 42 ? LandKind.Grain : roll < 72 ? LandKind.Pasture : roll < 88 ? LandKind.Stone : LandKind.Iron;
                if (pasture > 1.1 && roll < 75) kind = LandKind.Pasture;
                int ridgeX = 5 + (int)Math.Round(Math.Sin(z * .23) * 2);
                int ridgeZ = 34 + (int)Math.Round(Math.Sin(x * .25) * 1.5);
                if ((z >= 22 && x >= 2 && Math.Abs(x - ridgeX) <= 1)
                    || (x >= 6 && x <= 21 && Math.Abs(z - ridgeZ) <= 1)) kind = LandKind.Mountain;
                int riverX = RiverX(z);
                bool river = x == riverX || x == riverX + 1;
                if (river) kind = LandKind.Water;
                bool bridge = river && Array.IndexOf(BridgeRows, z) >= 0;
                if (bridge) kind = LandKind.Grain;
                int nearestStart = int.MaxValue;
                for (int i = 0; i < StartNames.Length; i++)
                    nearestStart = Math.Min(nearestStart, Math.Abs(x - Starts[i, 0]) + Math.Abs(z - Starts[i, 1]));
                int level = nearestStart <= 3 ? 1 : Math.Min(5, 1 + nearestStart / 7 + random.Next(2));
                lands.Add(new Land
                {
                    id = z * Width + x, x = x, z = z, region = RegionAt(x, z), homeland = Homeland.Han,
                    kind = kind, bridge = bridge, level = bridge ? 1 : level,
                    name = bridge ? BridgeName(z) : Catalog.LandName(kind)
                });
            }
            for (int i = 0; i < CapitalNames.Length; i++)
            {
                var city = lands[Capitals[i, 1] * Width + Capitals[i, 0]];
                if (!city.Walkable || city.bridge) throw new InvalidOperationException("幽州城池落在不可通行地理上：" + CapitalNames[i]);
                city.capital = true; city.kind = LandKind.Grain; city.level = i == 0 ? 5 : 3 + i % 2; city.name = CapitalNames[i];
            }
            // Each possible start receives the same safe local opening, without erasing mountains or rivers.
            for (int i = 0; i < StartNames.Length; i++)
            {
                int sx = Starts[i, 0], sz = Starts[i, 1];
                for (int dz = -2; dz <= 2; dz++) for (int dx = -2; dx <= 2; dx++)
                {
                    if (Math.Abs(dx) + Math.Abs(dz) > 2) continue;
                    var tile = lands[(sz + dz) * Width + sx + dx];
                    if (tile.capital || tile.bridge || !tile.Walkable) continue;
                    tile.kind = LandKind.Grain; tile.level = 1; tile.name = Catalog.LandName(tile.kind);
                }
            }
            return lands;
        }

        private static int RiverX(int z) { return 25 + (int)Math.Round(Math.Sin(z * .18) * 2); }
        private static string BridgeName(int z) { return z == 9 ? "南渡桥" : z == 19 ? "蓟东桥" : "北渡桥"; }
    }
}
