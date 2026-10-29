using System;

namespace EasternDynasty
{
    // Presentation geometry only. Resource identity, picking and march routes still
    // use the complete logical cell; a patch never substitutes a different land.
    internal static class YouzhouTerrainLayout
    {
        public const string Style = "youzhou-terrain-patches-v1";

        internal struct Patch
        {
            public int image;
            public float xMin, zMin, xMax, zMax;
        }

        public static float MeadowX(Patch patch, float cellX)
        {
            // Pick the wider exposed side, leaving enough distance from the cell
            // edge for a rounded screen-pixel click at normal tactical zoom.
            return patch.xMin - (cellX - .5f) > cellX + .5f - patch.xMax
                ? (patch.xMin + cellX - .5f) / 2 : (patch.xMax + cellX + .5f) / 2;
        }

        public static bool TryPatch(World world, Land land, out Patch patch)
        {
            patch = new Patch();
            if (world == null || land == null || !world.IsYouzhou || land.home || land.capital
                || land.bridge || world.IsCityDistrict(land.id)) return false;
            int image = land.kind == LandKind.Grain ? 0 : land.kind == LandKind.Pasture ? 1
                : land.kind == LandKind.Stone ? 2 : land.kind == LandKind.Iron ? 3 : -1;
            if (image < 0) return false;
            // Stable per-coordinate arrangement, independent of ownership, time,
            // camera, level or World RNG. Do not invent visual resource-level data.
            uint seed = unchecked((uint)(land.x * 73856093) ^ (uint)(land.z * 19349663));
            seed ^= seed >> 13; seed = unchecked(seed * 1274126177u); seed ^= seed >> 16;
            float width = .70f + (seed & 255) / 255f * .14f;
            float depth = .70f + ((seed >> 8) & 255) / 255f * .14f;
            float dx = (((seed >> 16) & 255) / 255f - .5f) * .10f;
            float dz = (((seed >> 24) & 255) / 255f - .5f) * .10f;
            patch = new Patch { image = image, xMin = land.x + dx - width / 2,
                xMax = land.x + dx + width / 2, zMin = land.z + dz - depth / 2,
                zMax = land.z + dz + depth / 2 };
            return true;
        }
    }
}
