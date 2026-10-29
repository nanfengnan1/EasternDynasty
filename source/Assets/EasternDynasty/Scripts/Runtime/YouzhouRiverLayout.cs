using System;

namespace EasternDynasty
{
    // Read-only presentation data. A bridge remains a traversable Core cell while
    // its water continues underneath. These distances never change map picking,
    // passability, resource identity, march paths or persisted terrain.
    internal static class YouzhouRiverLayout
    {
        public const string Style = "youzhou-river-banks-v1";
        public const int SamplesPerCell = 16;
        public const float MaxBankDistance = 2f;
        public const float BridgeDeckHalfWidth = .24f;
        public const float BridgeRailOffset = .20f;
        public const float BridgeRailHalfWidth = .014f;
        public const float BridgePostHalfWidth = .020f;
        public const float BridgePostHeight = .08f;
        public const float BridgeRailRise = .055f;
        public const float BridgeRailThickness = .02f;
        private const int SearchRadius = 3;

        public static bool TryBridgeSpan(World world, Land land, out float start, out float end)
        {
            start = end = 0;
            if (!HasMap(world) || land == null || !land.bridge
                || !ReferenceEquals(world.TryGetLand(land.x, land.z), land)
                || land.id != land.z * world.MapWidth + land.x) return false;
            int left = land.x, right = land.x;
            Land neighbour;
            while ((neighbour = world.TryGetLand(left - 1, land.z)) != null && neighbour.bridge) left--;
            while ((neighbour = world.TryGetLand(right + 1, land.z)) != null && neighbour.bridge) right++;
            start = left - .5f; end = right + .5f;
            return true;
        }

        // Shared by adjoining meshes and diagnostics. Keep the raised silhouette
        // inside the selectable crossing after projection onto the y=0 pick plane.
        public static float BridgeDeckHeight(float x, float start, float end)
        {
            if (float.IsNaN(x) || float.IsNaN(start) || float.IsNaN(end)
                || float.IsInfinity(x) || float.IsInfinity(start) || float.IsInfinity(end)
                || end <= start) return .04f;
            double t = Math.Max(0, Math.Min(1, ((double)x - start) / ((double)end - start)));
            return .04f + .07f * (float)Math.Sin(t * Math.PI);
        }

        public static bool IsWater(Land land)
        { return land != null && (land.kind == LandKind.Water || land.bridge); }

        public static byte[] BuildBankDistances(World world)
        {
            if (!HasMap(world)) return new byte[0];
            // Reject incomplete/reordered inputs instead of drawing invented banks.
            for (int i = 0; i < world.lands.Count; i++)
            {
                var land = world.lands[i];
                if (land == null || land.id != i || land.x != i % world.MapWidth
                    || land.z != i / world.MapWidth) return new byte[0];
            }
            int width = world.MapWidth * SamplesPerCell;
            var values = new byte[width * world.MapHeight * SamplesPerCell];
            foreach (var land in world.lands)
            {
                if (!IsWater(land)) continue;
                for (int j = 0; j < SamplesPerCell; j++)
                    for (int i = 0; i < SamplesPerCell; i++)
                    {
                        int px = land.x * SamplesPerCell + i;
                        int pz = land.z * SamplesPerCell + j;
                        float x = (px + .5f) / SamplesPerCell - .5f;
                        float z = (pz + .5f) / SamplesPerCell - .5f;
                        float distance = Distance(world, x, z, land.x, land.z);
                        values[pz * width + px] = (byte)Math.Round(distance / MaxBankDistance * 255);
                    }
            }
            return values;
        }

        // Distance in world-cell units, capped at MaxBankDistance. Both physical
        // map edges are included; outside the map is not an imaginary dry cell.
        public static float BankDistance(World world, float x, float z)
        {
            if (!HasMap(world) || float.IsNaN(x) || float.IsNaN(z)
                || x < -.5f || z < -.5f || x > world.MapWidth - .5f || z > world.MapHeight - .5f)
                return 0;
            int cx = Math.Min(world.MapWidth - 1, (int)Math.Floor(x + .5f));
            int cz = Math.Min(world.MapHeight - 1, (int)Math.Floor(z + .5f));
            if (!IsWater(world.TryGetLand(cx, cz))) return 0;
            return Distance(world, x, z, cx, cz);
        }

        private static bool HasMap(World world)
        { return world != null && world.IsYouzhou && world.lands != null
            && world.lands.Count == world.MapWidth * world.MapHeight; }

        private static float Distance(World world, float x, float z, int cx, int cz)
        {
            float nearestSquared = MaxBankDistance * MaxBankDistance;
            // Any rectangle farther than three cell indices cannot be within
            // two world units. Search world coordinates, never a mesh chunk.
            for (int nz = Math.Max(0, cz - SearchRadius); nz <= Math.Min(world.MapHeight - 1, cz + SearchRadius); nz++)
                for (int nx = Math.Max(0, cx - SearchRadius); nx <= Math.Min(world.MapWidth - 1, cx + SearchRadius); nx++)
                {
                    var neighbour = world.TryGetLand(nx, nz);
                    if (neighbour == null || IsWater(neighbour)) continue;
                    float dx = Math.Max(0, Math.Abs(x - nx) - .5f);
                    float dz = Math.Max(0, Math.Abs(z - nz) - .5f);
                    nearestSquared = Math.Min(nearestSquared, dx * dx + dz * dz);
                }
            return (float)Math.Sqrt(nearestSquared);
        }
    }
}
