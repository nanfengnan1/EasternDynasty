using System;

namespace EasternDynasty
{
    /// <summary>
    /// Original CPU cartography of the actual map. No labels, ownership, cities or camera state.
    /// Pixels are opaque ARGB (0xFFRRGGBB), indexed x + y * Size, with a bottom-left origin.
    /// This presentation-only projection never changes World or invents traversable terrain.
    /// </summary>
    public static class MinimapAtlasRaster
    {
        public const int Size = 320;

        public static int[] Render(World world)
        {
            if (world == null) throw new ArgumentNullException("world");
            return new Atlas(world).Draw();
        }

        private sealed class Atlas
        {
            private readonly int width, height;
            private readonly Land[] lands;
            private readonly int[] pixels = new int[Size * Size];
            private readonly bool[] water = new bool[Size * Size];
            private readonly bool[] mountain = new bool[Size * Size];

            public Atlas(World world)
            {
                width = world.MapWidth; height = world.MapHeight;
                lands = new Land[width * height];
                if (world.lands != null) foreach (Land land in world.lands)
                    if (land != null && land.x >= 0 && land.x < width && land.z >= 0 && land.z < height)
                        lands[land.z * width + land.x] = land;
            }

            public int[] Draw()
            {
                // All resource kinds share one quiet ground material. Resource categories are
                // shown by the main map; random per-tile colours would conceal strategic geography.
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    Land land = At(x * width / Size, y * height / Size);
                    int index = y * Size + x;
                    // Interpolate the cell-centre mask, not the land data. This rounds the
                    // atlas coast at sub-cell scale while every real cell centre keeps its kind.
                    water[index] = TerrainCoverage(x, y, false) >= .5;
                    mountain[index] = TerrainCoverage(x, y, true) >= .5;
                    int shade = (int)Math.Round(3 * Math.Sin(x * .025 + y * .012)
                        + 2 * Math.Cos(y * .041 - x * .009));
                    // Subtle paper fibres are spatial, deterministic and independent of land value.
                    if ((x * 7 + y * 11) % 43 == 0) shade--;
                    pixels[index] = Rgb(169 + shade, 168 + shade, 132 + shade);
                }

                DrawFields();
                DrawWaterAndBanks();
                DrawMountains();
                DrawBridges();
                return pixels;
            }

            private void DrawFields()
            {
                // Sparse fine hatching only in actual grain land. It remains below the visual
                // weight of river, mountain and the dynamic ownership overlays drawn by the UI.
                for (int z = 0; z < height; z++) for (int x = 0; x < width; x++)
                {
                    Land land = At(x, z);
                    if (land == null || land.bridge || land.kind != LandKind.Grain || (x + z * 3) % 7 != 0) continue;
                    int left = x * Size / width, right = (x + 1) * Size / width;
                    int bottom = z * Size / height, top = (z + 1) * Size / height;
                    for (int y = bottom + 2; y < top - 1; y += 3)
                        for (int px = left + 1; px < right - 1; px++)
                            pixels[y * Size + px] = Tint(pixels[y * Size + px], -5);
                }
            }

            private void DrawWaterAndBanks()
            {
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                {
                    int index = y * Size + x;
                    bool wet = water[index];
                    int distance = BoundaryDistance(x, y, wet);
                    if (wet)
                    {
                        // Bridges belong to the river bed here and are added after its banks.
                        // This keeps the true north/south river continuous beneath each crossing.
                        if (distance == 1) pixels[index] = Rgb(77, 104, 91);
                        else if (distance == 2) pixels[index] = Rgb(104, 133, 119);
                        else
                        {
                            int light = ((y + x / 9) % 13 == 0 && x % 11 < 6) ? 6 : 0;
                            pixels[index] = Rgb(83 + light, 123 + light, 119 + light);
                        }
                    }
                    else if (distance == 1 && !mountain[index]) pixels[index] = Rgb(190, 183, 144);
                    else if (distance == 2 && !mountain[index]) pixels[index] = Tint(pixels[index], 5);
                }
            }

            private int BoundaryDistance(int x, int y, bool wet)
            {
                for (int radius = 1; radius <= 2; radius++)
                    for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius) continue;
                        int px = x + dx, py = y + dy;
                        // The image border is a crop, not an invented coast.
                        if (px >= 0 && py >= 0 && px < Size && py < Size && water[py * Size + px] != wet) return radius;
                    }
                return 3;
            }

            private void DrawMountains()
            {
                var relief = new float[Size * Size];
                int radius = Math.Max(8, Size * 3 / width / 2);
                // Overlapping hills form a continuous mountain range, rather than one square
                // mountain icon in each cell. All shaded pixels remain within its terrain mask.
                for (int z = 0; z < height; z++) for (int x = 0; x < width; x++)
                {
                    Land land = At(x, z);
                    if (land == null || land.kind != LandKind.Mountain) continue;
                    int cx = (2 * x + 1) * Size / (2 * width), cy = (2 * z + 1) * Size / (2 * height);
                    float strength = 6 + (x * 3 + z * 7) % 5;
                    for (int dy = -radius; dy <= radius; dy++) for (int dx = -radius; dx <= radius; dx++)
                    {
                        int px = cx + dx, py = cy + dy;
                        if (px < 0 || py < 0 || px >= Size || py >= Size) continue;
                        float falloff = 1 - (dx * dx + dy * dy) / (float)(radius * radius);
                        if (falloff > 0) relief[py * Size + px] += falloff * falloff * strength;
                    }
                }
                for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++)
                    if (mountain[y * Size + x])
                    {
                        float dx = relief[y * Size + Math.Min(Size - 1, x + 1)] - relief[y * Size + Math.Max(0, x - 1)];
                        float dy = relief[Math.Min(Size - 1, y + 1) * Size + x] - relief[Math.Max(0, y - 1) * Size + x];
                        int shade = (int)Math.Max(-29, Math.Min(29, (dx - dy) * 9));
                        pixels[y * Size + x] = Rgb(125 + shade, 137 + shade, 106 + shade);
                    }

                // North-facing peaks have pale west slopes and an ink-shadowed east face.
                // Clipping against the real mountain mask keeps every pass and resource tile open.
                for (int z = 0; z < height; z++) for (int x = 0; x < width; x++)
                {
                    Land land = At(x, z);
                    if (land == null || land.kind != LandKind.Mountain || (x + z * 2) % 3 != 0) continue;
                    int left = x * Size / width, right = (x + 1) * Size / width;
                    int bottom = z * Size / height, top = (z + 1) * Size / height;
                    int peakX = (left + right) / 2 + ((x + z) % 3 - 1);
                    int peakY = top;
                    for (int py = bottom; py <= peakY; py++)
                    {
                        int half = 1 + (peakY - py) * 3 / 4;
                        for (int px = peakX - half; px <= peakX + half; px++)
                        {
                            int color = px < peakX ? Rgb(155, 163, 123) : Rgb(87, 106, 84);
                            if (px == peakX) color = Rgb(182, 182, 141);
                            MountainPixel(px, py, color);
                        }
                    }
                    // A second low ridge makes the range read as relief at compact HUD sizes.
                    MountainPixel(peakX - 2, bottom + 2, Rgb(100, 118, 91));
                    MountainPixel(peakX - 1, bottom + 1, Rgb(100, 118, 91));
                }
            }

            private void DrawBridges()
            {
                for (int z = 0; z < height; z++) for (int x = 0; x < width; x++)
                {
                    Land land = At(x, z);
                    if (land == null || !land.bridge) continue;
                    // A crossing follows neighbouring bridge cells when present. For a lone
                    // bridge, dry banks determine the span direction (not a hard-coded S1 row).
                    int horizontal = BridgeAt(x - 1, z) + BridgeAt(x + 1, z);
                    int vertical = BridgeAt(x, z - 1) + BridgeAt(x, z + 1);
                    bool eastWest = horizontal != vertical ? horizontal > vertical
                        : WetAt(x, z - 1) + WetAt(x, z + 1) >= WetAt(x - 1, z) + WetAt(x + 1, z);
                    int left = x * Size / width, right = (x + 1) * Size / width;
                    int bottom = z * Size / height, top = (z + 1) * Size / height;
                    int center = eastWest ? (bottom + top) / 2 : (left + right) / 2;
                    int from = eastWest ? left : bottom, to = eastWest ? right : top;
                    for (int p = from; p < to; p++) for (int offset = -2; offset <= 2; offset++)
                    {
                        int color = Math.Abs(offset) == 2 ? Rgb(107, 90, 61)
                            : Math.Abs(offset) == 1 ? Rgb(218, 198, 147)
                            : p % 3 == 0 ? Rgb(169, 142, 99) : Rgb(193, 169, 117);
                        int px = eastWest ? p : center + offset, py = eastWest ? center + offset : p;
                        if (px >= 0 && py >= 0 && px < Size && py < Size) pixels[py * Size + px] = color;
                    }
                }
            }

            private void MountainPixel(int x, int y, int color)
            {
                if (x >= 0 && y >= 0 && x < Size && y < Size && mountain[y * Size + x]) pixels[y * Size + x] = color;
            }
            private Land At(int x, int z)
            { return x < 0 || z < 0 || x >= width || z >= height ? null : lands[z * width + x]; }
            private double TerrainCoverage(int x, int y, bool mountains)
            {
                double gx = (x + .5) * width / Size - .5, gy = (y + .5) * height / Size - .5;
                int ix = (int)Math.Floor(gx), iy = (int)Math.Floor(gy);
                double fx = gx - ix, fy = gy - iy;
                // Smooth interpolation turns stair steps into contour bends; no new river or
                // mountain can appear without neighbouring real cells of that terrain kind.
                fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
                double a = TerrainAt(ix, iy, mountains), b = TerrainAt(ix + 1, iy, mountains);
                double c = TerrainAt(ix, iy + 1, mountains), d = TerrainAt(ix + 1, iy + 1, mountains);
                return (a + (b - a) * fx) * (1 - fy) + (c + (d - c) * fx) * fy;
            }
            private int TerrainAt(int x, int z, bool mountains)
            {
                Land land = At(Math.Max(0, Math.Min(width - 1, x)), Math.Max(0, Math.Min(height - 1, z)));
                return mountains ? (land != null && land.kind == LandKind.Mountain ? 1 : 0) : (IsWater(land) ? 1 : 0);
            }
            private static bool IsWater(Land land) { return land != null && (land.kind == LandKind.Water || land.bridge); }
            private int WetAt(int x, int z) { return IsWater(At(x, z)) ? 1 : 0; }
            private int BridgeAt(int x, int z) { Land land = At(x, z); return land != null && land.bridge ? 1 : 0; }
        }

        private static int Rgb(int r, int g, int b) { return unchecked((int)0xff000000) | (r << 16) | (g << 8) | b; }
        private static int Tint(int color, int amount)
        { return Rgb(((color >> 16) & 255) + amount, ((color >> 8) & 255) + amount, (color & 255) + amount); }
    }
}
