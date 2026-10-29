using System;
using System.Linq;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        // Cached CPU rasterisation keeps scene drawing to one texture per frame. The cache only
        // changes with construction / facility state; the live countdown is drawn by the UI.
        private sealed class CitySceneRaster
        {
            internal const int OverviewWidth = 1800, OverviewHeight = 880;
            internal const int OverviewOffsetX = 400, OverviewOffsetY = 220;
            private readonly int width, height;
            private int originX, originY;
            private readonly Color32[] pixels;
            public CitySceneRaster(int width, int height) { this.width = width; this.height = height; pixels = new Color32[width * height]; }
            public static Vector2 PlotPoint(int index)
            { return new Vector2(500 + (index % 3 - index / 3) * 137, 235 + (index % 3 + index / 3 - 2) * 58); }

            // One production composition also used by the offline raster check. Scenic houses
            // are not CityPlots, add no income, and never receive facility interaction targets.
            public static CitySceneRaster Overview(World world)
            {
                // Keep the playable town at its established 1000x440 coordinates, but extend
                // the surrounding landscape beyond it. The viewport can now fill every aspect
                // ratio without stretching the town or moving any facility's hit geometry.
                var r = new CitySceneRaster(OverviewWidth, OverviewHeight)
                    { originX = OverviewOffsetX, originY = OverviewOffsetY };
                r.CityMeadow(); r.Outskirts();
                var top = new Vector2(500, 40); var right = new Vector2(934, 220);
                var bottom = new Vector2(500, 407); var left = new Vector2(66, 220);
                r.Polygon(C("293B30"), left + new Vector2(7, 20), top + new Vector2(7, 20), right + new Vector2(7, 20), bottom + new Vector2(7, 20));
                r.Polygon(C("87876A"), left, top, right, bottom);
                r.Polygon(C("999278"), new Vector2(100, 220), new Vector2(500, 57), new Vector2(900, 220), new Vector2(500, 389));
                r.CityGroundRelief();
                r.CityStreets();
                r.Wall(left, top, false); r.Wall(top, right, false);
                // A low rear watchtower stays wholly inside the source image (the old tall
                // corner tower was cropped at its roof). Foreground towers frame the streets.
                r.RearTower(top);
                r.Neighbourhoods();
                foreach (var plot in world.cityPlots.OrderBy(p => p.index / 3 + p.index % 3))
                {
                    var point = PlotPoint(plot.index);
                    bool job = world.CityJob != null && world.CityJob.plotIndex == plot.index;
                    if (plot.building == CityBuilding.None)
                    {
                        if (job) r.Construction(point);
                        else r.GardenPlot(point, plot.index);
                    }
                    else
                    {
                        r.Building(point, plot.building);
                        if (job) r.Scaffold(point, plot.building == CityBuilding.TownHall ? 100 : 71);
                    }
                }
                r.StreetLife();
                // Leave an actual opening through the southern wall, aligned with the paved
                // approach and gatehouse rather than painting an unbroken wall over the road.
                r.Wall(left, bottom + new Vector2(-32, -14), true);
                r.Wall(bottom + new Vector2(32, -14), right, true);
                r.Gate(bottom + new Vector2(0, -5));
                r.Tower(left); r.Tower(right);
                return r;
            }

            private void Outskirts()
            {
                // Riverbank, towpath and cultivated strips make a continuous landscape around
                // the walls. These are original scenery, never playable land or resource nodes.
                RiverLandscape();
                for (int i = 0; i < 4; i++)
                {
                    var p = new Vector2(824 + i * 28, 354 - i * 15);
                    Shape(p, i % 2 == 0 ? "737E53" : "8B8C5D", -28, 0, 27, -24, 73, -5, 17, 20);
                    for (int j = 0; j < 5; j++) Stroke(p, -19 + j * 9, 2 + j * 3, 26 + j * 9, -18 + j * 3, "A0A070", 1);
                }
                Line(new Vector2(500, 398), new Vector2(572, 440), C("61634E"), 42);
                Line(new Vector2(500, 398), new Vector2(572, 440), C("B2A181"), 31);
                Grove(new Vector2(50, 99), 9, 24);
                Grove(new Vector2(890, 72), 11, 23);
                Grove(new Vector2(61, 366), 7, 19);
                Grove(new Vector2(755, 407), 9, 18);
                Grove(new Vector2(966, 279), 5, 15);
                Grove(new Vector2(186, -58), 16, 22);
                Grove(new Vector2(790, -62), 18, 23);
                Grove(new Vector2(-132, 166), 20, 27);
                Grove(new Vector2(1122, 202), 19, 25);
                Grove(new Vector2(966, 472), 15, 24);
                Grove(new Vector2(137, 499), 13, 23);
                // Foreground groundcover and the approach continue beyond the town crop.
                Line(new Vector2(572, 440), new Vector2(812, 660), C("727757"), 42);
                Line(new Vector2(572, 440), new Vector2(812, 660), C("AE9E78"), 29);
            }

            private void Grove(Vector2 center, int count, float size)
            {
                // An irregular oval copse, with a shared understorey and varied crown sizes.
                // Stable polar placement avoids both straight rows and repeated rectangular
                // tree stamps; depth ordering still puts nearer trunks in front of far ones.
                float spreadX = size * (1.2f + (float)Math.Sqrt(count) * .36f), spreadY = spreadX * .54f;
                for (int y = (int)-spreadY; y <= spreadY; y++) for (int x = (int)-spreadX; x <= spreadX; x++)
                {
                    float d = x * x / (spreadX * spreadX) + y * y / (spreadY * spreadY);
                    float patch = MeadowNoise((int)center.x + x + 2048, (int)center.y + y + 2048, 12);
                    if (d < 1) BlendPixel((int)center.x + x, (int)center.y + y, Mix(C("596847"), C("7A8050"), patch),
                        (1 - d) * (.3f + patch * .22f));
                }
                var trees = new Vector2[count];
                for (int i = 0; i < count; i++)
                {
                    double angle = i * 2.39996323 + center.x * .017;
                    float radius = (float)Math.Sqrt((i + .4f) / count);
                    trees[i] = center + new Vector2((float)Math.Cos(angle) * radius * spreadX, (float)Math.Sin(angle) * radius * spreadY);
                }
                foreach (var tree in trees.OrderBy(t => t.y))
                    Tree(tree, size * (.62f + Math.Abs((int)(tree.x * 7 + tree.y * 3)) % 6 * .075f));
            }

            // Original scenic river, not a world tile, a bridge or an official city facility.
            // The cross section follows a smooth centre line with irregular wet banks; its
            // pixels use the same cached scene coordinates as the existing town and gardens.
            private static float RiverCentre(float x)
            { return 304 + x * .36f + (float)Math.Sin(x * .009f) * 9 + Math.Max(0, x - 270) * .24f; }

            private void RiverLandscape()
            {
                Color32 deep = C("31594F"), shallow = C("789787"), sand = C("B9AD84"), mud = C("737E5D");
                for (int x = -originX; x < width - originX; x++)
                {
                    float centre = RiverCentre(x), slope = (RiverCentre(x + 1) - RiverCentre(x - 1)) * .5f;
                    float normal = (float)Math.Sqrt(1 + slope * slope);
                    float channel = 16 + (float)Math.Sin(x * .014f) * 3;
                    int reach = (int)(43 * normal);
                    for (int y = (int)centre - reach; y <= (int)centre + reach; y++)
                    {
                        float signed = (y - centre) / normal;
                        float edge = Math.Abs(signed) + (float)Math.Sin(x * .081f + y * .063f) * 1.6f;
                        if (edge > channel + 22) continue;
                        if (edge > channel)
                        {
                            float bank = edge - channel;
                            float variation = MeadowNoise(x + 2048, y + 2048, 9);
                            Color32 earth = Mix(mud, sand, .35f + variation * .5f);
                            // Exposed gravel on the near shore, damp roots against the far bank.
                            if (signed < 0) earth = Mix(earth, mud, .48f);
                            BlendPixel(x, y, earth, Math.Min(1, (22 - bank) / 11f));
                            continue;
                        }
                        float depth = Math.Max(0, Math.Min(1, (channel - edge) / 12));
                        Color32 water = Mix(shallow, deep, depth);
                        float ripple = (float)Math.Sin(x * .12f - y * .28f) * (float)Math.Sin(x * .019f + y * .047f);
                        water = Mix(water, shallow, Math.Max(0, ripple) * .25f);
                        if (channel - edge < 1.4f) water = Mix(water, sand, .34f);
                        Pixel(x, y, water);
                    }
                }
                // Rock clusters and staggered reeds break the long bank silhouette; never
                // register click targets or add resources to the real nine city districts.
                for (int i = 0; i < 43; i++)
                {
                    float x = -365 + i * 26, centre = RiverCentre(x);
                    float bank = (i % 3 == 0 ? -1 : 1) * (22 + (float)Math.Sin(x * .014f) * 3);
                    var p = new Vector2(x, centre + bank);
                    if (i % 3 == 0 || i % 5 == 0)
                    {
                        Shape(p, "596B55", -8, 3, -5, -2, 3, -5, 9, 1, 4, 5);
                        Shape(p, "B7B397", -6, 0, -3, -5, 3, -6, 7, -1, 2, 2);
                        Stroke(p, -3, -5, 3, -6, "D0C7A6", 1);
                    }
                    else
                        for (int stem = 0; stem < 5; stem++)
                        {
                            float sx = -6 + stem * 3, tall = 7 + (i + stem * 3) % 9;
                            Stroke(p, sx, 2, sx - 3, -tall, "60784E", 1);
                            Stroke(p, sx - 3, -tall, sx - 5, -tall + 4, "ADB07A", 1);
                            Stroke(p, sx, 0, sx + 4, -tall * .6f, "88965F", 1);
                        }
                }
            }

            private static Color32 Mix(Color32 a, Color32 b, float t)
            {
                t = Math.Max(0, Math.Min(1, t));
                return new Color32((byte)(a.r + (b.r - a.r) * t), (byte)(a.g + (b.g - a.g) * t),
                    (byte)(a.b + (b.b - a.b) * t), 255);
            }

            private void BlendPixel(int x, int y, Color32 color, float opacity)
            {
                int px = x + originX, py = y + originY;
                if (px < 0 || px >= width || py < 0 || py >= height) return;
                int index = (height - 1 - py) * width + px;
                pixels[index] = Mix(pixels[index], color, opacity);
            }

            private void CityGroundRelief()
            {
                // Irregular stone/earth patches under streets tie the neighbourhoods together.
                // This layer is scenery only: all nine plot identities still come from World.
                for (int row = 0; row < 20; row++) for (int col = 0; col < 34; col++)
                {
                    float x = 90 + col * 25 + row % 2 * 11, y = 63 + row * 17;
                    if (Math.Abs(x - 500) / 393f + Math.Abs(y - 222) / 159f > .95f) continue;
                    int seed = (row * 17 + col * 31) % 9;
                    var p = new Vector2(x, y);
                    if (seed < 3) Shape(p, seed == 0 ? "A29D7E" : "919176", -12, 0, -2, -5, 13, -1, 6, 4, -9, 3);
                    if (seed == 4) Stroke(p, -6, 0, 3, -4, "B1A98B", 1);
                }
            }

            private void PavedStreet(Vector2 a, Vector2 b, float thickness, bool main)
            {
                var delta = b - a;
                if (delta.magnitude < 1) return;
                var across = new Vector2(-delta.y, delta.x) / delta.magnitude;
                // Individual staggered flags sit in a recessed bed, with curbs and worn
                // central traffic stones. All lanes share their actual projected direction.
                Line(a, b, C("656B58"), thickness + 5);
                Line(a, b, C("8C8D76"), thickness);
                int lanes = main ? 3 : 2;
                int count = Math.Max(1, (int)(delta.magnitude / (main ? 10 : 12)));
                for (int lane = 0; lane < lanes; lane++) for (int i = -1; i < count; i++)
                {
                    float t0 = Math.Max(0, (i + (lane % 2) * .5f) / count);
                    float t1 = Math.Min(1, (i + 1 + (lane % 2) * .5f) / count);
                    if (t1 <= t0) continue;
                    var edge = across * (-thickness * .46f + lane * thickness * .92f / lanes);
                    var p0 = Vector2.Lerp(a, b, t0) + edge;
                    var p1 = Vector2.Lerp(a, b, t1) + edge;
                    var span = across * (thickness * .92f / lanes - .65f);
                    float variation = ((i + 7) * 13 + lane * 7) % 9 / 8f;
                    Color32 stone = Mix(C(main ? "9D9B81" : "91917A"), C("C1B99B"), variation * .65f);
                    Material(stone, p0, p1, p1 + span, p0 + span);
                    Line(p0, p1, C("D0C5A5"), .65f);
                    Line(p0, p0 + span, C("747A66"), .7f);
                }
                Line(a - across * (thickness * .53f), b - across * (thickness * .53f), C("C3B89A"), 1.5f);
                Line(a + across * (thickness * .53f), b + across * (thickness * .53f), C("6B7562"), 1.5f);
            }

            private void CityStreets()
            {
                // Broad diagonal gate avenues and narrow neighbourhood lanes; no empty nine
                // box floor plan. The nine districts remain a management concept, not the art.
                PavedStreet(new Vector2(105, 220), new Vector2(500, 389), 23, true);
                PavedStreet(new Vector2(500, 389), new Vector2(895, 220), 23, true);
                PavedStreet(new Vector2(105, 220), new Vector2(500, 57), 12, false);
                PavedStreet(new Vector2(500, 57), new Vector2(895, 220), 12, false);
                PavedStreet(new Vector2(500, 375), new Vector2(500, 257), 25, true);
                PavedStreet(new Vector2(122, 220), new Vector2(878, 220), 17, true);
                PavedStreet(new Vector2(229, 268), new Vector2(632, 100), 10, false);
                PavedStreet(new Vector2(368, 100), new Vector2(771, 268), 10, false);
                PavedStreet(new Vector2(256, 165), new Vector2(638, 327), 8, false);
                PavedStreet(new Vector2(362, 327), new Vector2(744, 165), 8, false);
                Shape(new Vector2(500, 259), "C4B697", -54, 0, 0, -23, 54, 0, 0, 23);
                Shape(new Vector2(500, 259), "A29D83", -32, 0, 0, -14, 32, 0, 0, 14);
                Shape(new Vector2(500, 259), "D2C3A3", -25, 0, 0, -10, 25, 0, 0, 10);
            }

            private void Neighbourhoods()
            {
                // Low rooflines form a service/living ring around the larger named facilities.
                // They are deliberately small and desaturated, with no independent UI targets.
                var homes = new[] {
                    new Vector2(415,101),new Vector2(366,120),new Vector2(317,141),new Vector2(268,161),new Vector2(219,182),new Vector2(169,203),
                    new Vector2(585,101),new Vector2(634,120),new Vector2(683,141),new Vector2(732,161),new Vector2(781,182),new Vector2(831,203),
                    new Vector2(181,247),new Vector2(236,270),new Vector2(291,293),new Vector2(346,316),new Vector2(401,340),new Vector2(455,363),
                    new Vector2(819,247),new Vector2(764,270),new Vector2(709,293),new Vector2(654,316),new Vector2(599,340),new Vector2(545,363)
                };
                for (int i = 0; i < homes.Length; i++)
                {
                    var p = homes[i];
                    ScenicCourt(p, i);
                }
            }

            private void ScenicCourt(Vector2 p, int variant)
            {
                // Paired roofs, side walls and small courtyards replace the repeated single
                // house ring. Lower rooflines keep the actual facility silhouettes dominant.
                Shape(p, "5D6751", -32, 3, -5, -14, 35, 0, 12, 20, -19, 15);
                Shape(p, "A99D7C", -28, 0, -2, -14, 29, -1, 3, 14);
                string roof = variant % 4 == 0 ? "73664E" : variant % 3 == 0 ? "5B6B60" : "4D655C";
                House(p + new Vector2(-8, -7), 16 + variant % 3, 7, 17 + variant % 2 * 5, "C1AE88", roof);
                if (variant % 3 == 0)
                    OpenShed(p + new Vector2(19, 1), 13, 6, 12, "8C7954");
                else
                    House(p + new Vector2(17, 2), 12, 6, 12 + variant % 2 * 2, "A89D7D", "697B68");
                // An interrupted wall leaves a readable gate and a lit patch of courtyard.
                Stroke(p, -29, 4, -7, 14, "6B7057", 5);
                Stroke(p, -29, 1, -7, 11, "D0B995", 2);
                Stroke(p, 7, 14, 32, 3, "74785D", 5);
                Stroke(p, 7, 11, 32, 0, "B8AE8B", 2);
                if (variant % 4 == 0)
                {
                    Tree(p + new Vector2(-25, 8), 8);
                    Ellipse(p + new Vector2(1, 5), 4, 2, "D4C6A2");
                    Ellipse(p + new Vector2(1, 4), 2, 1, "455B4C");
                }
                else if (variant % 4 == 1)
                {
                    Stroke(p, -13, 1, 4, 8, "876B48", 3);
                    Box(p.x - 7, p.y + 1, 4, 3, C("C2A270"));
                }
                else if (variant % 4 == 2) MarketStall(p + new Vector2(-5, 12), "966649");
            }

            private void GardenPlot(Vector2 p, int index)
            {
                // A continuous grove, pond or stone garden occupies an unbuilt district.
                // No fake facility or level is added; the planning overlay selects this same plot.
                Shape(p, "858C69", -64, -8, -39, -25, -5, -29, 32, -20, 62, -7, 47, 8, 11, 21, -25, 15);
                Shape(p, "9B9C76", -45, -7, -16, -21, 19, -18, 42, -5, 24, 10, -9, 15, -38, 5);
                Stroke(p, -51, 5, -16, 21, "C0B58B", 4);
                Stroke(p, 15, 20, 48, 5, "B3AA82", 4);
                Tree(p + new Vector2(-45, -8), 13 + index % 3);
                Tree(p + new Vector2(-23, -19), 10);
                if (index % 3 == 0)
                {
                    Shape(p, "B0AB88", -17, -5, -6, -14, 16, -16, 36, -8, 30, 3, 7, 10, -10, 5);
                    Shape(p, "547B6B", -13, -5, -4, -11, 16, -13, 31, -7, 25, 1, 6, 6, -7, 3);
                    Stroke(p, -7, -5, 9, -9, "95B09A", 1);
                    Stroke(p, 9, 1, 21, -2, "87A690", 1);
                    // Small stepping stones are visual scenery, not traversable world tiles.
                    for (int i = 0; i < 4; i++) Ellipse(p + new Vector2(-12 + i * 8, -1 + i % 2 * 2), 3, 2, "C6B897");
                    Tree(p + new Vector2(42, -3), 12);
                }
                else if (index % 3 == 1)
                {
                    for (int i = 0; i < 5; i++) Tree(p + new Vector2(2 + i * 10, -12 + i % 2 * 12), 7 + i % 2 * 2);
                    Stroke(p, -20, 6, 1, 14, "C5B88F", 4);
                }
                else
                {
                    Shape(p, "B2AD8C", -20, -3, 4, -15, 30, -4, 6, 8);
                    for (int i = 0; i < 3; i++)
                    {
                        var rock = p + new Vector2(-7 + i * 15, i % 2 * 6);
                        Shape(rock, "6E7E6D", -7, 0, -4, -10 - i * 2, 4, -7, 8, 1, 1, 4);
                        Stroke(rock, -4, -9 - i * 2, 1, -3, "B2B497", 2);
                    }
                    Tree(p + new Vector2(38, -12), 11);
                }
            }

            private void RearTower(Vector2 p)
            {
                BuildingBody(p, 21, 10, 14, C("9C9C82"));
                Roof(p + new Vector2(0, -14), 29, 11, 8, C("526D65"));
            }

            private void StreetLife()
            {
                // Stalls and handcarts stay on the edge streets, below the facility artwork.
                MarketStall(new Vector2(137, 228), "A56B4E");
                MarketStall(new Vector2(870, 229), "8B8D60");
                Cart(new Vector2(432, 365)); Cart(new Vector2(568, 365));
                foreach (var p in new[] { new Vector2(300, 305),new Vector2(700, 305),new Vector2(492, 287),new Vector2(513, 299),new Vector2(153, 240),new Vector2(847, 240) })
                {
                    Ellipse(p + new Vector2(2, 2), 4, 2, "7A7C66");
                    Shape(p, "695D49", -2, -7, 2, -7, 3, 1, -3, 1);
                    Box(p.x - 1, p.y - 11, 3, 4, C("D1BC8F"));
                }
            }

            private void MarketStall(Vector2 p, string awning)
            {
                Shape(p, "5A604D", -17, 0, 0, 9, 22, 0, 3, -9);
                Stroke(p, -14, 0, -14, -18, "BDA87D", 2); Stroke(p, 15, 0, 15, -18, "BDA87D", 2);
                Shape(p, awning, -20, -18, 0, -28, 22, -18, 1, -8);
                Stroke(p, -20, -18, 1, -8, "D4BB8E", 2);
                Shape(p, "B59B70", -14, -3, 0, -9, 16, -3, 1, 4);
                Ellipse(p + new Vector2(-5, -5), 4, 2, "D2BD80"); Ellipse(p + new Vector2(5, -3), 4, 2, "9D7750");
            }

            private void Cart(Vector2 p)
            {
                Shape(p, "8D7753", -13, -9, 2, -16, 15, -10, -1, -3);
                Shape(p, "B39B6F", -13, -9, -1, -3, -1, 4, -13, -2);
                Ellipse(p + new Vector2(-8, 3), 4, 5, "4D5746"); Ellipse(p + new Vector2(10, -4), 4, 5, "4D5746");
                Stroke(p, 10, -10, 28, -18, "C5AE7B", 2);
            }
            private void Pixel(int x, int y, Color32 color)
            {
                x += originX; y += originY;
                if (x >= 0 && x < width && y >= 0 && y < height) pixels[(height - 1 - y) * width + x] = color;
            }
            private void CityMeadow()
            {
                // Broad soil/grass variation reads as land at phone scale; tiny uniform noise
                // made the former courtyard model appear to float on a green felt board.
                for (int y = -originY; y < height - originY; y++) for (int x = -originX; x < width - originX; x++)
                {
                    float broad = MeadowNoise(x + 1024, y + 1024, 91), fine = MeadowNoise(x + 1067, y + 1041, 27);
                    float light = 1 - Math.Min(1, Math.Abs(x - 460) / 1100f + Math.Abs(y) / 1500f);
                    int grain = Math.Abs(x * 13 + y * 17 + x * y % 19) % 3;
                    Pixel(x, y, new Color32((byte)(64 + broad * 28 + fine * 9 + light * 15 + grain),
                        (byte)(80 + broad * 26 + fine * 9 + light * 17 + grain),
                        (byte)(53 + broad * 18 + fine * 6 + light * 9 + grain), 255));
                }
                for (int i = 0; i < 520; i++)
                {
                    var p = new Vector2(-originX + 8 + i * 97 % (width - 16), -originY + 8 + i * 53 % (height - 16));
                    if (Math.Abs(p.x - 500) / 460f + Math.Abs(p.y - 220) / 200f < 1.12f) continue;
                    Stroke(p, -2, 1, 0, -3, "929B6A", 1);
                    Stroke(p, 0, 1, 3, -2, "6B7D52", 1);
                }
            }

            private static float MeadowNoise(int x, int y, int cell)
            {
                int ix = x / cell, iy = y / cell;
                float u = x % cell / (float)cell, v = y % cell / (float)cell;
                u = u * u * (3 - 2 * u); v = v * v * (3 - 2 * v);
                float a = MeadowSample(ix, iy), b = MeadowSample(ix + 1, iy);
                float c = MeadowSample(ix, iy + 1), d = MeadowSample(ix + 1, iy + 1);
                return (a + (b - a) * u) * (1 - v) + (c + (d - c) * u) * v;
            }

            private static float MeadowSample(int x, int y)
            { return ((x * 71 + y * 113 + x * y * 29) % 101) / 100f; }

            public void Ground()
            {
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    int noise = (x * 13 + y * 17 + (x * y) % 19) % 7;
                    float edge = Math.Min(1, Math.Abs(x - 500) / 550f + Math.Abs(y - 210) / 550f);
                    Pixel(x, y, new Color32((byte)(57 + noise - edge * 10), (byte)(76 + noise - edge * 13), (byte)(56 + noise - edge * 8), 255));
                }
                for (int i = 0; i < 18; i++)
                {
                    float x = 18 + (i * 197) % 960, y = 40 + (i * 97) % 365;
                    if (Math.Abs(x - 500) / 440f + Math.Abs(y - 220) / 190f > 1.16f) Tree(new Vector2(x, y), 12 + i % 8);
                }
            }
            public void Polygon(Color color, params Vector2[] points)
            { PaintPolygon(color, 0, points); }
            private void Material(Color color, params Vector2[] points)
            { PaintPolygon(color, 3, points); }
            private void PaintPolygon(Color color, int grain, params Vector2[] points)
            {
                Color32 fill = color;
                int minY = height - originY - 1, maxY = -originY;
                foreach (var p in points) { minY = Math.Min(minY, (int)Math.Floor(p.y)); maxY = Math.Max(maxY, (int)Math.Ceiling(p.y)); }
                minY = Math.Max(-originY, minY); maxY = Math.Min(height - originY - 1, maxY);
                var crossings = new float[points.Length];
                for (int y = minY; y <= maxY; y++)
                {
                    int count = 0; float scan = y + .5f;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var a = points[i]; var b = points[(i + 1) % points.Length];
                        if ((a.y <= scan && b.y > scan) || (b.y <= scan && a.y > scan)) crossings[count++] = a.x + (scan - a.y) * (b.x - a.x) / (b.y - a.y);
                    }
                    Array.Sort(crossings, 0, count);
                    for (int i = 0; i + 1 < count; i += 2)
                        for (int x = Math.Max(-originX, (int)Math.Ceiling(crossings[i])); x <= Math.Min(width - originX - 1, (int)crossings[i + 1]); x++)
                        {
                            if (grain == 0) { Pixel(x, y, fill); continue; }
                            // Stable mineral/plaster grain belongs to the material, never to
                            // World state or a frame clock. Keep the relief readable at 1:1.
                            int speck = ((x * 31 + y * 17 + x * y % 23) % 7 - 3) * grain;
                            Pixel(x, y, new Color32((byte)Math.Max(0, Math.Min(255, fill.r + speck)),
                                (byte)Math.Max(0, Math.Min(255, fill.g + speck)),
                                (byte)Math.Max(0, Math.Min(255, fill.b + speck)), fill.a));
                        }
                }
            }
            public void Line(Vector2 a, Vector2 b, Color color, float thickness)
            {
                var delta = b - a; float length = delta.magnitude; if (length < .01f) return;
                var perpendicular = new Vector2(-delta.y, delta.x) * (thickness / length / 2);
                Polygon(color, a + perpendicular, b + perpendicular, b - perpendicular, a - perpendicular);
            }
            private void Box(float x, float y, float w, float h, Color color)
            { Polygon(color, new Vector2(x, y), new Vector2(x + w, y), new Vector2(x + w, y + h), new Vector2(x, y + h)); }
            public void Tree(Vector2 p, float size)
            {
                int seed = Math.Abs((int)(p.x * 17 + p.y * 31));
                // Broken, feathered cast shadow anchors each grove in the existing ground.
                for (int y = -3; y <= size * .65f; y++) for (int x = (int)-size; x <= size * 1.6f; x++)
                {
                    float dx = (x - size * .3f) / (size * 1.2f), dy = (y - size * .24f) / (size * .43f);
                    float d = dx * dx + dy * dy;
                    if (d < 1) BlendPixel((int)p.x + x, (int)p.y + y, C("203D30"), (1 - d) * .48f);
                }
                Stroke(p, 1, 4, -1, -size * 1.55f, "4C5036", Math.Max(2, size * .14f));
                Stroke(p, -1, 2, -3, -size * 1.45f, "A39868", Math.Max(1, size * .055f));
                Color32 shade = C(seed % 4 == 0 ? "3D5239" : "294C36");
                Color32 light = C(seed % 4 == 0 ? "A4A06B" : "8D9F62");
                bool slender = seed % 5 == 0;
                // Irregular broadleaf crowns and taller tiered trees share the same light
                // direction. Lobes have coarse leaf masses rather than repeated flat discs.
                for (int tier = 0; tier < (slender ? 5 : 3); tier++)
                {
                    float taper = slender ? 1 - tier * .15f : 1 - tier * .16f;
                    float level = -.65f - tier * (slender ? .32f : .38f);
                    for (int side = -1; side <= 1; side++)
                    {
                        float sx = side * size * (slender ? .28f : .43f) * taper;
                        float sy = level * size + (side == 0 ? -size * .14f : side * size * .05f);
                        var branch = p + new Vector2(sx, sy);
                        Line(p + new Vector2(0, sy + size * .4f), branch, C("786C47"), Math.Max(1, size * .06f));
                        FoliageLobe(branch, size * .61f * taper, size * (slender ? .39f : .51f) * taper,
                            shade, light, seed + tier * 11 + side * 7);
                    }
                }
            }

            private void FoliageLobe(Vector2 p, float rx, float ry, Color32 shade, Color32 light, int seed)
            {
                int reachX = (int)Math.Ceiling(rx * 1.18f), reachY = (int)Math.Ceiling(ry * 1.18f);
                for (int y = -reachY; y <= reachY; y++) for (int x = -reachX; x <= reachX; x++)
                {
                    float dx = x / rx, dy = y / ry;
                    double angle = Math.Atan2(dy, dx);
                    float edge = 1 + (float)Math.Sin(angle * 7 + seed) * .095f + (float)Math.Sin(angle * 11 - seed * .3) * .06f;
                    float radius = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (radius > edge) continue;
                    int px = (int)p.x + x, py = (int)p.y + y;
                    float mass = MeadowNoise(px + 4096 + seed % 23, py + 4096, Math.Max(3, (int)(rx * .31f)));
                    float illumination = .53f - dx * .19f - dy * .28f + (mass - .5f) * .56f;
                    if (radius > edge - .13f) illumination -= .12f;
                    // Sparse paired leaf strokes follow the lit clusters; keep dark openings.
                    if (mass > .52f && (px + py * 3 + seed) % 7 < 2) illumination += .1f;
                    Pixel(px, py, Mix(shade, light, illumination));
                }
            }
            public void Wall(Vector2 a, Vector2 b, bool front)
            {
                var direction = b - a;
                if (direction.magnitude < 1) return;
                var along = direction / direction.magnitude;
                var inward = new Vector2(-along.y, along.x);
                var toTown = new Vector2(500, 220) - (a + b) * .5f;
                if (inward.x * toTown.x + inward.y * toTown.y < 0) inward = -inward;
                inward *= 10;
                var lift = new Vector2(0, -23);
                // An actual wall section: projecting footing, stone curtain, walkable crown
                // and two parapet edges. Its ground endpoints retain the established city ring.
                Polygon(C("435449"), a - inward * .7f, b - inward * .7f, b + new Vector2(9, 9), a + new Vector2(9, 9));
                StoneFace(a - inward * .3f, b - inward * .3f, 5, front ? "6F7A6D" : "A0A58C", false);
                StoneFace(a, b, 23, front ? "768276" : "969E88", !front);
                Polygon(C("747E6C"), a + lift, b + lift, b + lift + inward, a + lift + inward);
                int count = Math.Max(1, (int)(direction.magnitude / 21));
                for (int i = 0; i < count; i++)
                {
                    var p = Vector2.Lerp(a, b, (i + .35f) / count) + lift;
                    Line(p, p + inward, C("A8AC93"), 1);
                }
                // The inner lip stays low enough to show the dark walkway between the walls.
                Line(a + lift + inward, b + lift + inward, C("B6B99D"), 2);
                for (int i = 0; i < count; i++)
                {
                    var p = Vector2.Lerp(a, b, (i + .5f) / count);
                    if (front && i % 4 == 1)
                    {
                        var outer = p - inward * .65f;
                        StoneFace(outer - along * 6, outer + along * 6, 22, "778376", false);
                        Polygon(C("A7AD92"), p - along * 6 + lift, p + along * 6 + lift,
                            outer + along * 6 + lift, outer - along * 6 + lift);
                        Polygon(C("52675B"), outer + along * 6, p + along * 6,
                            p + along * 6 + lift, outer + along * 6 + lift);
                    }
                    var c = p + lift;
                    var l = c - along * 6; var r = c + along * 6;
                    StoneFace(l, r, 7, front ? "A6AE96" : "B8BBA0", false);
                    Polygon(C("697C6C"), r, r + inward * .4f, r + inward * .4f + new Vector2(0, -7), r + new Vector2(0, -7));
                    Polygon(C("D0CCAD"), l + new Vector2(0, -7), r + new Vector2(0, -7),
                        r + inward * .4f + new Vector2(0, -7), l + inward * .4f + new Vector2(0, -7));
                }
            }

            private void StoneFace(Vector2 a, Vector2 b, float tall, string color, bool shade)
            {
                // Each stone is projected on its facade plane. Offset courses, bevel light,
                // chipped corners and damp footings avoid a flat repeated line grid.
                var up = new Vector2(0, -tall);
                Color32 stone = C(color);
                Polygon(Mix(stone, C("344E43"), .5f), a, b, b + up, a + up);
                int rows = Math.Max(1, (int)Math.Ceiling(tall / 6));
                int columns = Math.Max(1, (int)((b - a).magnitude / 14));
                for (int row = 0; row < rows; row++) for (int col = -1; col < columns; col++)
                {
                    float t0 = Math.Max(0, (col + row % 2 * .5f) / columns);
                    float t1 = Math.Min(1, (col + 1 + row % 2 * .5f) / columns);
                    if (t1 <= t0) continue;
                    var lower = up * (row / (float)rows);
                    var upper = up * ((row + 1) / (float)rows) + new Vector2(0, .7f);
                    var l = Vector2.Lerp(a, b, t0); var r = Vector2.Lerp(a, b, t1);
                    int seed = Math.Abs((int)(a.x * 7 + a.y * 3) + row * 13 + col * 37);
                    var face = Mix(stone, C("C0BDA0"), (seed % 11) * .022f);
                    face = Mix(face, C("344F44"), (shade ? .12f : 0) + (row == 0 ? .18f : 0));
                    Material(face, l + lower, r + lower, r + upper, l + upper);
                    Line(l + upper, r + upper, Mix(face, C("D4CEAF"), .4f), .8f);
                    Line(l + lower, l + upper, C("52695B"), .8f);
                    if (seed % 4 == 0)
                    {
                        var chip = Vector2.Lerp(l, r, .72f) + upper;
                        Line(chip, chip + new Vector2(-2, 2), Mix(face, C("334E42"), .35f), 1);
                    }
                }
            }

            public void Tower(Vector2 p)
            {
                // Stone bastion and a smaller timber lookout replace the former ground-level
                // house silhouette; the roof/flag envelope stays within the old corner bounds.
                StoneFace(p + new Vector2(-34, 0), p + new Vector2(0, 18), 23, "A4AA91", false);
                StoneFace(p + new Vector2(0, 18), p + new Vector2(34, 0), 23, "7D8B7B", true);
                Shape(p, "C7C2A3", -37, -24, 0, -43, 37, -24, 0, -4);
                BuildingBody(p + new Vector2(0, -25), 27, 14, 20, C("AE9D78"));
                Roof(p + new Vector2(0, -45), 39, 21, 12, C("435D5C"));
                Line(p + new Vector2(0, -70), p + new Vector2(0, -55), C("B4A37A"), 2);
                Polygon(C("B76145"), p + new Vector2(0, -70), p + new Vector2(18, -67), p + new Vector2(0, -62));
            }
            public void Gate(Vector2 p)
            {
                Polygon(C("4D5E4B"), p + new Vector2(-45, 3), p + new Vector2(3, 27), p + new Vector2(61, 3), p + new Vector2(24, -14));
                StoneFace(p + new Vector2(-43, 0), p + new Vector2(0, 21), 34, "ACAD91", false);
                StoneFace(p + new Vector2(0, 21), p + new Vector2(43, 0), 34, "84917D", true);
                // Arch on the front-facing masonry plane: voussoirs, recessed soffit and
                // open timber leaves expose a lit threshold. This is scenery, never a portal.
                var door = p + new Vector2(20, 11);
                Func<float, float, Vector2> arch = (x, y) => door + new Vector2(x, y - x * .49f);
                Polygon(C("C6BCA0"), arch(-15, 0), arch(-15, -17), arch(-10, -27), arch(0, -31), arch(10, -27), arch(15, -17), arch(15, 0));
                Polygon(C("263C32"), arch(-11, 0), arch(-11, -16), arch(-7, -23), arch(0, -26), arch(7, -23), arch(11, -16), arch(11, 0));
                Polygon(C("67735A"), arch(-11, 0), arch(-11, -16), arch(-7, -23), arch(0, -26), arch(-3, -20), arch(-5, -13), arch(-5, 0));
                Polygon(C("BCAB83"), arch(-11, 0), arch(-5, -6), arch(7, -6), arch(11, 0));
                for (int i = 0; i < 7; i++)
                {
                    double angle = Math.PI * (i + .5f) / 7;
                    Line(arch((float)Math.Cos(angle) * 12, -16 - (float)Math.Sin(angle) * 10),
                        arch((float)Math.Cos(angle) * 16, -16 - (float)Math.Sin(angle) * 14), C("6F7B65"), 1);
                }
                Polygon(C("735A3C"), arch(-10, -15), arch(-6, -12), arch(-6, -1), arch(-10, 0));
                Polygon(C("594E36"), arch(8, -16), arch(10, -16), arch(10, 0), arch(8, -3));
                Line(arch(-10, -5), arch(-6, -4), C("B19562"), 1);
                // A broad stone terrace separates the gate's heavy base from the open gallery.
                Shape(p, "C9BFA0", -46, -35, 0, -57, 46, -35, 0, -12);
                BuildingBody(p + new Vector2(0, -34), 31, 14, 17, C("B9A17B"));
                Roof(p + new Vector2(0, -51), 49, 22, 12, C("49665F"));
                Stroke(p, -29, -37, 0, -22, "C4AF84", 2);
                Stroke(p, 0, -22, 29, -37, "8F8562", 2);
            }
            private void BuildingBody(Vector2 p, float w, float depth, float tall, Color wall)
            {
                Polygon(C("50604B"), p + new Vector2(-w, 0), p + new Vector2(0, depth), p + new Vector2(w + 22, 2), p + new Vector2(w + 17, -depth));
                Facade(p + new Vector2(-w, 0), p + new Vector2(0, depth), tall, wall, false);
                Facade(p + new Vector2(0, depth), p + new Vector2(w, 0), tall, Color.Lerp(wall, C("263F38"), .35f), true);
            }

            private void Facade(Vector2 a, Vector2 b, float tall, Color plaster, bool shade)
            {
                var up = new Vector2(0, -tall);
                Material(plaster, a, b, b + up, a + up);
                // Deep eaves, stone footing and timber bays give each wall a physical edge.
                Polygon(Color.Lerp(plaster, C("263C34"), .7f), a + up, b + up, b + up + new Vector2(0, 5), a + up + new Vector2(0, 5));
                Polygon(C(shade ? "737A65" : "A5A58A"), a, b, b + new Vector2(0, -4), a + new Vector2(0, -4));
                Line(a, b, C(shade ? "5C6957" : "D5C7A3"), 2);
                Line(a + new Vector2(0, -5), b + new Vector2(0, -5), C(shade ? "4B5040" : "867151"), 1);
                int bays = tall < 20 ? 3 : 4;
                for (int i = 0; i <= bays; i++)
                {
                    var post = Vector2.Lerp(a, b, i / (float)bays);
                    Line(post + new Vector2(0, -4), post + up + new Vector2(0, 4), C(shade ? "4F4C39" : "82654A"), 2);
                    if (i == bays || tall < 15) continue;
                    float t0 = (i + .23f) / bays, t1 = (i + .77f) / bays;
                    var wl = Vector2.Lerp(a, b, t0) + new Vector2(0, -Math.Max(7, tall * .27f));
                    var wr = Vector2.Lerp(a, b, t1) + new Vector2(0, -Math.Max(7, tall * .27f));
                    var windowUp = new Vector2(0, -Math.Max(4, tall * .35f));
                    if (shade && i == bays / 2)
                    {
                        wl = Vector2.Lerp(a, b, t0) + new Vector2(0, -3);
                        wr = Vector2.Lerp(a, b, t1) + new Vector2(0, -3);
                        windowUp = new Vector2(0, -Math.Max(7, tall * .61f));
                    }
                    Polygon(C(shade ? "233B34" : "344B3C"), wl, wr, wr + windowUp, wl + windowUp);
                    Line(wl, wr, C(shade ? "A28E65" : "D6BB88"), 1);
                    Line(wl + windowUp * .5f, wr + windowUp * .5f, C(shade ? "7B7758" : "9F966F"), 1);
                    var mullion = Vector2.Lerp(wl, wr, .5f);
                    Line(mullion, mullion + windowUp, C(shade ? "8F825F" : "B9A47A"), 1);
                }
            }
            private void Roof(Vector2 p, float w, float depth, float rise, Color roof)
            {
                var left = p + new Vector2(-w, -2); var near = p + new Vector2(0, depth);
                var right = p + new Vector2(w, -2); var back = p + new Vector2(0, -depth);
                var ridgeBack = p + new Vector2(-w * .35f, -depth * .55f - rise - 4);
                var ridgeNear = p + new Vector2(w * .35f, depth * .55f - rise - 4);
                // A genuine hipped roof: two long tiled slopes, two hip triangles, a raised
                // diagonal ridge and layered upturned eaves, instead of a flat diamond.
                Polygon(C("263D35"), left + new Vector2(0, 3), back + new Vector2(0, 3), right + new Vector2(0, 3), near + new Vector2(0, 4));
                Material(Color.Lerp(roof, C("223D35"), .2f), back, right, ridgeNear, ridgeBack);
                Material(Color.Lerp(roof, C("C1BD96"), .2f), left, ridgeBack, ridgeNear, near);
                Material(Color.Lerp(roof, C("DFCF9E"), .32f), left, back, ridgeBack);
                Material(Color.Lerp(roof, C("233D36"), .35f), near, ridgeNear, right);
                RoofTiles(ridgeBack, ridgeNear, left, near, roof, false);
                RoofTiles(ridgeBack, ridgeNear, back, right, roof, true);
                var eaveLight = Color.Lerp(roof, C("DDD0A3"), .66f);
                var eaveDark = Color.Lerp(roof, C("344C3D"), .45f);
                Line(left + new Vector2(0, 3), near + new Vector2(0, 3), C("5A4934"), 3);
                Line(near + new Vector2(0, 3), right + new Vector2(0, 3), C("3A4132"), 3);
                Line(left, near, eaveLight, 2); Line(near, right, eaveDark, 2);
                Line(left, ridgeBack, eaveLight, 1); Line(right, ridgeNear, eaveDark, 1);
                Line(ridgeBack, ridgeNear, Color.Lerp(roof, C("D9BE83"), .7f), 3);
                Line(ridgeBack + new Vector2(0, -1), ridgeNear + new Vector2(0, -1), C("DFCA99"), 1);
                foreach (var tip in new[] { left, right })
                {
                    float sign = tip.x < p.x ? -1 : 1;
                    Line(tip + new Vector2(-sign * 5, 1), tip + new Vector2(sign * 4, -5), eaveLight, 2);
                }
                Line(ridgeBack, ridgeBack + new Vector2(-2, -4), eaveLight, 2);
                Line(ridgeNear, ridgeNear + new Vector2(2, -4), eaveLight, 2);
            }

            private void RoofTiles(Vector2 ridgeA, Vector2 ridgeB, Vector2 eaveA, Vector2 eaveB, Color roof, bool shade)
            {
                int columns = Math.Max(4, (int)((eaveB - eaveA).magnitude / 4));
                var highlight = Color.Lerp(roof, C(shade ? "96A68C" : "CBC6A1"), shade ? .15f : .35f);
                var groove = Color.Lerp(roof, C("243F36"), .45f);
                for (int i = 1; i < columns; i++)
                {
                    float t = i / (float)columns;
                    var a = Vector2.Lerp(ridgeA, ridgeB, t); var b = Vector2.Lerp(eaveA, eaveB, t);
                    Line(a, b, groove, 1);
                    Line(a + new Vector2(-1, 0), b + new Vector2(-1, 0), highlight, 1);
                }
                for (int row = 1; row < 5; row++)
                {
                    float t = row / 5f;
                    Line(Vector2.Lerp(ridgeA, eaveA, t), Vector2.Lerp(ridgeB, eaveB, t), Color.Lerp(roof, C("284539"), .18f), 1);
                }
            }
            public void Building(Vector2 point, CityBuilding kind)
            {
                // Original schematic silhouettes, not reconstructions of official facility art.
                // Keep every facility inside x +/- 85, y -120..30 for both the panorama and
                // the transparent 240 x 180 detail texture (whose anchor is 120,137).
                switch (kind)
                {
                    case CityBuilding.TownHall: HallComplex(point); break;
                    case CityBuilding.Mill: GrainMill(point); break;
                    case CityBuilding.Lumberyard: TimberYard(point); break;
                    case CityBuilding.Quarry: StoneQuarry(point); break;
                    case CityBuilding.Ironworks: IronFurnace(point); break;
                    case CityBuilding.Residence: ResidentialCourt(point); break;
                    case CityBuilding.Barracks: MilitaryCamp(point); break;
                    case CityBuilding.DrillGround: PracticeField(point); break;
                    case CityBuilding.Workshop: CraftWorkshop(point); break;
                }
            }

            private void Shape(Vector2 p, string color, params float[] xy)
            {
                var points = new Vector2[xy.Length / 2];
                for (int i = 0; i < points.Length; i++) points[i] = p + new Vector2(xy[i * 2], xy[i * 2 + 1]);
                Polygon(C(color), points);
            }
            private void Stroke(Vector2 p, float x1, float y1, float x2, float y2, string color, float thickness = 2)
            { Line(p + new Vector2(x1, y1), p + new Vector2(x2, y2), C(color), thickness); }
            private void Ellipse(Vector2 p, float rx, float ry, string color)
            {
                var points = new Vector2[20];
                for (int i = 0; i < points.Length; i++)
                {
                    double angle = i * Math.PI * 2 / points.Length;
                    points[i] = p + new Vector2((float)Math.Cos(angle) * rx, (float)Math.Sin(angle) * ry);
                }
                Polygon(C(color), points);
            }
            private void Yard(Vector2 p, string surface)
            {
                Shape(p, "445344", -79, -4, -5, -41, 81, -6, 7, 29);
                Material(C(surface), p + new Vector2(-78, -9), p + new Vector2(-5, -45), p + new Vector2(78, -10), p + new Vector2(6, 25));
                Stroke(p, -78, -9, 6, 25, "A99E7C", 2);
                Stroke(p, 6, 25, 78, -10, "6F725A", 2);
            }
            private void House(Vector2 p, float w, float depth, float tall, string wall, string roof)
            {
                Polygon(C("4A5C45"), p + new Vector2(-w, 0), p + new Vector2(0, depth + 3), p + new Vector2(w + 10, 4), p + new Vector2(w + 8, -depth));
                Facade(p + new Vector2(-w, 0), p + new Vector2(0, depth), tall, C(wall), false);
                Facade(p + new Vector2(0, depth), p + new Vector2(w, 0), tall, Color.Lerp(C(wall), C("263B36"), .35f), true);
                Roof(p + new Vector2(0, -tall), w + 5, depth + 3, 9, C(roof));
            }
            private void Pennant(Vector2 p, float tall, string color)
            {
                Stroke(p, 0, 0, 0, -tall, "C7B383", 2);
                Shape(p, color, 1, -tall, 17, -tall + 4, 12, -tall + 9, 1, -tall + 12);
            }

            private void HallComplex(Vector2 p)
            {
                Yard(p, "ACA184");
                // Three successive halls and two low wings make the seat of government read
                // as a compound; its silhouette is deliberately different from one tall house.
                Shape(p, "857D60", -52, -20, 0, -45, 52, -21, 52, -12, 0, 12, -52, -12);
                Material(C("D2C5A4"), p + new Vector2(-52, -22), p + new Vector2(0, -47), p + new Vector2(52, -23), p + new Vector2(0, 2));
                Stroke(p, -51, -19, 0, 5, "F0DDB5", 2);
                Stroke(p, 0, 5, 51, -20, "A2A085", 2);
                House(p + new Vector2(0, -41), 43, 16, 33, "CDB68B", "967447");
                // The central seat has two roof tiers, vermilion columns and a stepped stone
                // terrace; its silhouette and materials distinguish it from the living ring.
                Roof(p + new Vector2(0, -84), 31, 12, 11, C("B38B4C"));
                for (int i = 0; i < 5; i++)
                {
                    float x = -37 + i * 8, y = -41 + (i + 1) * 3;
                    Stroke(p, x, y, x, y - 23, "994E35", 3);
                    Stroke(p, x - 1, y, x - 1, y - 22, "CA8357", 1);
                    Stroke(p, -x, y, -x, y - 23, "663F30", 3);
                    Stroke(p, x - 3, y - 21, x + 3, y - 18, "BFA576", 2);
                    Stroke(p, -x - 3, y - 18, -x + 3, y - 21, "9B855F", 2);
                }
                Shape(p, "D8C8A5", -33, -24, 0, -39, 33, -24, 0, -9);
                for (int i = 0; i < 5; i++)
                {
                    Stroke(p, -28 + i * 2, -23 + i * 3, 0, -11 + i * 3, "9E9B83", 2);
                    Stroke(p, 0, -11 + i * 3, 28 - i * 2, -23 + i * 3, "B1AA8D", 2);
                    Stroke(p, -27 + i * 2, -24 + i * 3, 0, -12 + i * 3, "E5D7B6", 1);
                }
                Stroke(p, -34, -25, -34, -35, "E0D0A9", 3);
                Stroke(p, 34, -25, 34, -35, "BEB797", 3);
                House(p + new Vector2(-49, -4), 21, 10, 22, "B9A582", "4C655C");
                House(p + new Vector2(48, -5), 21, 10, 22, "B9A582", "4C655C");
                Shape(p, "CFC2A2", -22, -3, 0, -13, 22, -3, 0, 8);
                for (int i = 0; i < 3; i++) Stroke(p, -18 + i * 2, 1 + i * 3, 2, 10 + i * 3, "8C8973", 2);
                House(p + new Vector2(0, 9), 26, 10, 19, "BFA67D", "3F5C56");
                Balustrade(p + new Vector2(-65, 1), p + new Vector2(-30, 17));
                Balustrade(p + new Vector2(30, 17), p + new Vector2(65, 1));
                foreach (float x in new[] { -34f, 34f })
                {
                    Stroke(p, x, 14, x, -8, "655D48", 2);
                    Box(p.x + x - 3, p.y - 9, 6, 7, C("CF9460"));
                }
                Pennant(p + new Vector2(-72, -2), 45, "A5513F");
                Pennant(p + new Vector2(64, 1), 43, "A5513F");
            }
            private void Balustrade(Vector2 a, Vector2 b)
            {
                Line(a, b, C("8E8D73"), 4);
                Line(a + new Vector2(0, -7), b + new Vector2(0, -7), C("DAD0AC"), 2);
                for (int i = 0; i <= 4; i++)
                {
                    var post = Vector2.Lerp(a, b, i / 4f);
                    Line(post, post + new Vector2(0, -10), C("DDD0AA"), 3);
                    Box(post.x - 2, post.y - 12, 4, 3, C("F0DCB5"));
                }
            }
            private void GrainMill(Vector2 p)
            {
                Yard(p, "A99A73");
                // Raised grain store, separate round silo, and a large vertical wooden wheel.
                House(p + new Vector2(14, -15), 34, 16, 38, "B9A47B", "788065");
                Stroke(p, -11, -1, -11, 11, "655941", 4);
                Stroke(p, 27, -4, 27, 9, "655941", 4);
                var silo = p + new Vector2(54, -7);
                Shape(silo, "A99164", -15, -37, 15, -37, 15, 0, 10, 5, -10, 5, -15, 0);
                for (int i = 0; i < 7; i++)
                {
                    float x = -13 + i * 4;
                    Stroke(silo, x, -34, x, 1, i < 3 ? "C5AE7A" : "887449", 1);
                }
                Ellipse(silo + new Vector2(0, -35), 15, 6, "C0AC7C");
                Shape(silo, "C4B780", -19, -36, 0, -55, 19, -36, 0, -29);
                for (int i = 0; i < 7; i++) Stroke(silo, 0, -54, -16 + i * 5, -35 + (i < 3 ? i : 6 - i), "A29360", 1);
                Stroke(silo, -14, -20, 0, -15, "756C4E", 2);
                Stroke(silo, 0, -15, 14, -20, "756C4E", 2);
                Stroke(silo, -14, -7, 0, -2, "756C4E", 2);
                Stroke(silo, 0, -2, 14, -7, "756C4E", 2);
                var wheel = p + new Vector2(-35, -8);
                Ellipse(wheel + new Vector2(4, 1), 23, 25, "554B36");
                Ellipse(wheel, 23, 25, "B39865");
                Ellipse(wheel, 17, 19, "4D5744");
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    Line(wheel, wheel + new Vector2((float)Math.Cos(angle) * 20, (float)Math.Sin(angle) * 22), C("CCB47A"), 3);
                }
                Ellipse(wheel, 5, 6, "756044");
                Ellipse(p + new Vector2(13, 15), 9, 6, "C8B887");
                Ellipse(p + new Vector2(29, 10), 8, 6, "B9A575");
            }
            private void OpenShed(Vector2 p, float w, float depth, float tall, string roof)
            {
                // Open bays and visible beams distinguish production sheds from closed houses.
                Polygon(C("59604B"), p + new Vector2(-w, 0), p + new Vector2(0, depth), p + new Vector2(w, 0), p + new Vector2(0, -depth));
                foreach (var foot in new[] { new Vector2(-w + 4, 0), new Vector2(0, depth - 2), new Vector2(w - 4, 0), new Vector2(0, -depth + 3) })
                    Line(p + foot, p + foot + new Vector2(0, -tall), C("BCA273"), 4);
                Stroke(p, -w + 4, -8, 0, depth - 12, "8C734F", 3);
                Roof(p + new Vector2(0, -tall), w + 5, depth + 3, 7, C(roof));
            }
            private void Log(Vector2 p, float length)
            {
                var end = p + new Vector2(length, -length * .42f);
                Line(p, end, C("684F35"), 10);
                Line(p + new Vector2(0, -2), end + new Vector2(0, -2), C("B08C59"), 5);
                Ellipse(p, 5, 5, "CFB383");
                Ellipse(p, 2, 3, "947449");
            }
            private void TimberYard(Vector2 p)
            {
                Yard(p, "8C8B61");
                OpenShed(p + new Vector2(-26, -15), 35, 15, 31, "8E875E");
                for (int i = 0; i < 4; i++) Log(p + new Vector2(-7 + i * 8, 16 - i * 2), 40);
                for (int i = 0; i < 3; i++) Log(p + new Vector2(-3 + i * 8, 6 - i * 2), 40);
                Log(p + new Vector2(5, -4), 40);
                // A long saw rack and cut stump keep the work area open.
                Stroke(p, 30, -29, 30, -59, "BEA574", 4);
                Stroke(p, 64, -14, 64, -43, "A48B61", 4);
                Stroke(p, 24, -57, 70, -39, "C4AE7F", 4);
                Stroke(p, 33, -48, 61, -35, "A3AC9A", 2);
                Shape(p, "776144", -57, 0, -43, 4, -43, 14, -57, 10);
                Ellipse(p + new Vector2(-50, 0), 11, 6, "C6A673");
                Stroke(p, -50, -1, -44, -18, "645B45", 3);
                Shape(p, "A8B4A3", -45, -21, -33, -18, -35, -12, -46, -15);
            }
            private void StoneQuarry(Vector2 p)
            {
                Yard(p, "93937C");
                Shape(p, "677267", -67, -15, -58, -48, -36, -69, -5, -55, 13, -29, -12, 3);
                Shape(p, "B0B29E", -67, -15, -58, -48, -36, -69, -29, -40, -47, -13);
                Shape(p, "919A89", -36, -69, -5, -55, 13, -29, -29, -40);
                Shape(p, "C2C0A9", -61, -21, -29, -40, -5, -31, -38, -12);
                Shape(p, "818C7E", -61, -21, -38, -12, -38, 1, -60, -6);
                Shape(p, "AFB29D", -38, -12, -5, -31, 8, -20, -26, 1, -38, 1);
                // Chisel seams and layered rock faces keep the quarry distinct from plaster
                // buildings; each crack follows a visible surface rather than screen noise.
                Stroke(p, -54, -43, -44, -53, "D0CAB0", 2);
                Stroke(p, -55, -35, -47, -39, "909A85", 1);
                Stroke(p, -32, -61, -22, -51, "B6B9A0", 2);
                Stroke(p, -28, -40, -13, -36, "D0CDB2", 1);
                Stroke(p, -57, -23, -38, -33, "919C87", 1);
                Stroke(p, -40, -20, -28, -26, "919C87", 1);
                Stroke(p, -29, -15, -22, -16, "6B7D70", 1);
                Stroke(p, -22, -16, -18, -10, "6B7D70", 1);
                Stroke(p, -54, -7, -48, -5, "B8BBA1", 1);
                Stroke(p, -45, -45, -40, -32, "727E70", 2);
                Stroke(p, -22, -52, -12, -43, "65746A", 2);
                // Timber lifting frame with a visible suspended stone block.
                Stroke(p, 31, 0, 31, -82, "AA8D5C", 6);
                Stroke(p, 11, 7, 31, -55, "C1A16C", 4);
                Stroke(p, 56, -1, 31, -55, "816D4E", 4);
                Stroke(p, 1, -70, 66, -89, "C2A675", 6);
                Stroke(p, 31, -82, 54, -90, "DBC28A", 2);
                Stroke(p, 60, -86, 60, -35, "D6C393", 2);
                Shape(p, "AEB5A4", 49, -35, 60, -41, 72, -35, 72, -24, 60, -18, 49, -24);
                Stroke(p, 60, -40, 60, -19, "778572", 2);
                for (int i = 0; i < 3; i++)
                {
                    var rock = p + new Vector2(-22 + i * 21, 12 - i * 3);
                    Shape(rock, "B4B6A2", -9, -5, 0, -11, 10, -5, 8, 4, -7, 5);
                    Stroke(rock, 0, -10, 1, 3, "899481", 2);
                }
            }
            private void IronFurnace(Vector2 p)
            {
                Yard(p, "777C69");
                // The stepped brick stack is the highest industrial silhouette. No animated
                // smoke or sparks are needed, so rebuilding the cached texture stays cheap.
                Shape(p, "75776A", 20, -91, 40, -97, 43, -22, 24, -15);
                Shape(p, "999780", 13, -95, 20, -91, 24, -15, 10, -22);
                Shape(p, "BEC0A2", 10, -102, 31, -108, 44, -101, 23, -94);
                Shape(p, "3F4C45", 17, -101, 31, -105, 37, -102, 23, -98);
                for (int i = 0; i < 10; i++)
                {
                    float y = -87 + i * 7;
                    Stroke(p, 21, y, 40, y - 6, "525F52", 1);
                    Stroke(p, 14, y - 5, 21, y, "C0B99A", 1);
                    float joint = i % 2 == 0 ? 28 : 35;
                    Stroke(p, joint, y - (joint - 21) * .3f, joint, y - (joint - 21) * .3f + 6, "525F52", 1);
                }
                Shape(p, "A28B68", -41, 4, -43, -27, -34, -49, -10, -58, 14, -47, 23, -25, 19, 1, -9, 14);
                Shape(p, "7E735C", -10, -58, 14, -47, 23, -25, 19, 1, -9, 14, -9, -20);
                Shape(p, "B29C74", -43, -27, -34, -49, -10, -58, -9, -20);
                Shape(p, "413D2D", -33, 3, -32, -16, -24, -25, -16, -19, -15, 9);
                Shape(p, "D78F4B", -30, 3, -29, -10, -24, -17, -19, -10, -18, 7);
                Shape(p, "E9BB64", -27, 3, -24, -8, -21, 5);
                Stroke(p, -40, -13, -11, -3, "6D624B", 2);
                Stroke(p, -39, -28, -10, -18, "786A50", 2);
                Stroke(p, -33, -45, -10, -52, "C1AA7D", 1);
                Stroke(p, -37, -35, -10, -43, "887A59", 1);
                Stroke(p, -31, -34, -31, -26, "786A50", 1);
                Stroke(p, -20, -46, -20, -38, "786A50", 1);
                Stroke(p, -9, -34, 18, -30, "5C5C49", 1);
                Stroke(p, -8, -21, 21, -18, "5C5C49", 1);
                Stroke(p, -8, -8, 20, -6, "5C5C49", 1);
                Stroke(p, 7, -32, 7, -21, "5C5C49", 1);
                Stroke(p, 14, -18, 14, -7, "5C5C49", 1);
                Shape(p, "37443C", 31, 9, 42, -8, 55, -13, 68, 4, 55, 15);
                Shape(p, "5F6958", 31, 9, 42, -8, 51, -1, 55, 15);
                Stroke(p, 8, 18, 28, 8, "C1A36B", 4);
                Stroke(p, 16, 21, 36, 11, "A78A57", 4);
            }
            private void ResidentialCourt(Vector2 p)
            {
                Yard(p, "A39A7C");
                House(p + new Vector2(0, -29), 31, 13, 25, "C0AC85", "727C68");
                House(p + new Vector2(-44, -2), 24, 12, 22, "B7A789", "5D7567");
                House(p + new Vector2(43, -4), 24, 12, 22, "B9AA89", "777C66");
                // Low enclosing wall, open entrance and shared well leave an obvious courtyard.
                Stroke(p, -66, 3, -17, 24, "AFA88B", 5);
                Stroke(p, 16, 24, 65, 3, "9B9C81", 5);
                Ellipse(p + new Vector2(0, 4), 11, 6, "B4B29A");
                Ellipse(p + new Vector2(0, 2), 7, 3, "4C6258");
                Stroke(p, -9, 3, -9, -15, "8D7751", 2);
                Stroke(p, 9, 3, 9, -15, "8D7751", 2);
                Stroke(p, -12, -15, 12, -15, "C3A575", 3);
                Stroke(p, 0, -15, 0, -1, "DAC898", 1);
            }
            private void Tent(Vector2 p, float w, float tall, string canvas)
            {
                Shape(p, "5C604A", -w - 5, 2, 0, 13, w + 8, 2, 4, -9);
                Shape(p, canvas, -w, 0, -7, -tall, 12, -tall + 6, w, 6, 0, 13);
                Shape(p, "8F795B", -7, -tall, 12, -tall + 6, w, 6, 0, 13);
                Shape(p, "403F31", -6, -tall + 11, -11, 7, 1, 12);
                Stroke(p, -7, -tall, -7, -tall - 7, "B7A06F", 2);
                Stroke(p, -w, -1, -w - 5, 7, "CAB888", 1);
                Stroke(p, w, 5, w + 5, 10, "CAB888", 1);
                Stroke(p, -7, -tall, 12, -tall + 6, "D3BE8C", 2);
                Stroke(p, -7, -tall + 3, -w + 7, -1, "D4C396", 1);
                Stroke(p, 3, -tall + 5, 1, 10, "B8A274", 1);
                Stroke(p, 11, -tall + 8, w - 6, 5, "735F43", 1);
                Stroke(p, -w, 0, -2, 12, "DFCC9A", 2);
                Stroke(p, 0, 13, w, 6, "6A5C43", 2);
                for (int i = 0; i < 4; i++)
                {
                    float x = -w + 4 + i * w / 4;
                    float y = 2 + i * 2.5f;
                    Stroke(p, x, y - 2, x + 2, y + 2, "8C7654", 1);
                }
            }
            private void MilitaryCamp(Vector2 p)
            {
                Yard(p, "8F8C68");
                Tent(p + new Vector2(0, -31), 27, 33, "C4AF80");
                Tent(p + new Vector2(-41, -3), 26, 31, "C0A577");
                Tent(p + new Vector2(38, -3), 26, 31, "BCA479");
                Pennant(p + new Vector2(64, -19), 63, "A84E3B");
                // Stacked shields and a spear rack belong to the camp foreground.
                Stroke(p, -8, 18, -8, -14, "BAA878", 2);
                Stroke(p, 10, 19, 10, -13, "BAA878", 2);
                Stroke(p, -15, 0, 17, 0, "8A714F", 3);
                for (int i = 0; i < 3; i++)
                {
                    var shield = p + new Vector2(-10 + i * 10, 13 - i);
                    Shape(shield, "9B5F45", -5, -9, 5, -9, 5, 0, 0, 6, -5, 0);
                    Stroke(shield, 0, -8, 0, 3, "D4BA83", 1);
                }
            }
            private void PracticeField(Vector2 p)
            {
                Yard(p, "A89971");
                Shape(p, "C0AF80", -65, -10, 0, -40, 66, -11, 2, 19);
                // An unroofed training court with target stands and weapon racks, not a house.
                for (int i = 0; i < 4; i++)
                    Stroke(p, -43 + i * 17, -15 + i * 7, -14 + i * 17, -28 + i * 7, "D4C18D", 1);
                Stroke(p, -70, -17, -3, -48, "BDA373", 3);
                for (int i = 0; i < 5; i++)
                {
                    var post = p + new Vector2(-70 + i * 16, -17 - i * 7.5f);
                    Stroke(post, 0, 6, 0, -10, "8D7651", 3);
                }
                for (int i = 0; i < 3; i++)
                {
                    var target = p + new Vector2(10 + i * 22, -29 + i * 9);
                    Stroke(target, -7, 6, 0, -21, "75674B", 2);
                    Stroke(target, 8, 4, 0, -21, "75674B", 2);
                    Ellipse(target + new Vector2(0, -24), 10, 12, "CEBF90");
                    Ellipse(target + new Vector2(0, -24), 6, 7, "997F57");
                    Ellipse(target + new Vector2(0, -24), 3, 3, "B46043");
                }
                Pennant(p + new Vector2(-62, 3), 65, "9C5842");
                Stroke(p, -27, 10, -8, 19, "6E6146", 4);
                for (int i = 0; i < 4; i++)
                {
                    float x = -29 + i * 8, y = 9 + i * 3;
                    Stroke(p, x, y, x + 4, y - 31, "7D7050", 2);
                    Shape(p + new Vector2(x + 4, y - 31), "BDC0A0", -2, 2, 0, -6, 3, 2);
                }
            }
            private void CraftWorkshop(Vector2 p)
            {
                Yard(p, "939078");
                // A long open-sided work hall, low side store and exposed workbench.
                House(p + new Vector2(-45, -14), 22, 12, 22, "AA9675", "657365");
                OpenShed(p + new Vector2(12, -16), 42, 17, 31, "65796D");
                Stroke(p, -5, 1, -5, -12, "BEA579", 4);
                Stroke(p, 25, -9, 25, -21, "99805A", 4);
                Shape(p, "C2A372", -17, -14, 15, -28, 35, -21, 3, -7);
                Shape(p, "576255", 2, -22, 13, -25, 24, -21, 17, -16, 9, -17, 6, -13, 0, -16);
                Stroke(p, -9, -17, -14, -28, "7F6442", 3);
                Shape(p, "B2B49B", -19, -30, -8, -34, -4, -28, -15, -24);
                // Separate timber and finished stone stock identify an active fabrication yard.
                for (int i = 0; i < 3; i++) Log(p + new Vector2(-54 + i * 8, 12 - i * 3), 24);
                for (int i = 0; i < 2; i++)
                {
                    var crate = p + new Vector2(42 + i * 13, 6 - i * 5);
                    Shape(crate, "B3A17C", -8, -9, 3, -14, 13, -9, 13, 3, 2, 8, -8, 3);
                    Stroke(crate, 2, -13, 2, 7, "756B50", 2);
                    Stroke(crate, -7, -5, 2, -1, "D1BD8E", 2);
                }
                Ellipse(p + new Vector2(18, 16), 11, 4, "5E6E60");
                Ellipse(p + new Vector2(18, 14), 7, 3, "9B9E85");
            }
            public void Construction(Vector2 p)
            {
                Polygon(C("8C7B5B"), p + new Vector2(-61, 0), p + new Vector2(0, -26), p + new Vector2(61, 0), p + new Vector2(0, 26));
                for (int i = 0; i < 4; i++) Line(p + new Vector2(-40, -4 + i * 7), p + new Vector2(28, -25 + i * 7), C("BAA478"), 5);
                Scaffold(p, 66);
            }
            public void Scaffold(Vector2 p, float w)
            {
                var tint = C("D3B77D");
                for (int i = -1; i <= 1; i++) Line(p + new Vector2(i * w, 8), p + new Vector2(i * w, -65), tint, 2);
                Line(p + new Vector2(-w, -44), p + new Vector2(w, -44), tint, 3);
                Line(p + new Vector2(-w, -15), p + new Vector2(w, -15), tint, 2);
                Line(p + new Vector2(-w, 5), p + new Vector2(0, -44), tint, 2);
                Line(p + new Vector2(w, 5), p + new Vector2(0, -44), tint, 2);
            }
            public Texture2D Texture()
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { name = "CityOverviewProcedural", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                texture.SetPixels32(pixels); texture.Apply(false, true); return texture;
            }
        }
    }
}
