// Standalone numerical checks and previews of the production raster. No Unity or player process.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using ViewportPoint = EasternDynasty.MinimapViewportGeometry.Point;

namespace EasternDynasty
{
    public static class MinimapAtlasChecks
    {
        private static int checks;
        [DataContract] private sealed class Evidence
        {
            [DataMember] public string evidenceKind = "production-cpu-raster-and-viewport-geometry";
            [DataMember] public string limitation = "Production map raster and polygon geometry only; no Unity camera, UI, click targets, desktop or Android acceptance. PNGs are not game screenshots.";
            [DataMember] public string generatedAtUtc = DateTime.UtcNow.ToString("o");
            [DataMember] public string sourcePath, sourceSha256;
            [DataMember] public string viewportSourcePath, viewportSourceSha256;
            [DataMember] public int viewportCases;
            [DataMember] public int checks, mapWidth = 40, mapHeight = 40, checkedLandCoordinates;
            [DataMember] public List<string> states = new List<string>();
            [DataMember] public List<ImageEvidence> images = new List<ImageEvidence>();
        }
        [DataContract] private sealed class ImageEvidence
        {
            [DataMember] public string file, sha256, pixelSha256, worldSha256;
            [DataMember] public int width, height, startIndex;
        }
        private static void Require(bool condition, string message)
        { checks++; if (!condition) throw new Exception(message); }
        private static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        private static byte[] State(World world)
        {
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return stream.ToArray(); }
        }
        private static byte[] PixelBytes(int[] pixels)
        { var bytes = new byte[pixels.Length * 4]; Buffer.BlockCopy(pixels, 0, bytes, 0, bytes.Length); return bytes; }
        private static int Red(int p) { return (p >> 16) & 255; }
        private static int Green(int p) { return (p >> 8) & 255; }
        private static int Blue(int p) { return p & 255; }
        private static bool WaterColor(int p) { return Green(p) > Red(p) + 15 && Math.Abs(Green(p) - Blue(p)) < 30; }
        private static bool BridgeColor(int p) { return Red(p) > Green(p) + 12 && Green(p) > Blue(p) + 25; }
        private static int Center(int[] pixels, int x, int z)
        { return pixels[(z * 8 + 4) * MinimapAtlasRaster.Size + x * 8 + 4]; }
        private static void CheckRealCoordinates(World world, int[] pixels, Evidence evidence)
        {
            Require(world.MapWidth == 40 && world.MapHeight == 40 && world.lands.Count == 1600, "S1 fixture is not 40 x 40.");
            var seen = new HashSet<int>();
            int water = 0, bridges = 0, mountains = 0, landCount = 0;
            foreach (var land in world.lands)
            {
                Require(seen.Add(land.z * 40 + land.x), "Duplicate real land coordinate.");
                int center = Center(pixels, land.x, land.z);
                if (land.bridge) { bridges++; Require(BridgeColor(center), "Bridge missing at real coordinate " + land.id); }
                else if (land.kind == LandKind.Water) { water++; Require(WaterColor(center), "Water moved or lost at real coordinate " + land.id); }
                else if (land.kind == LandKind.Mountain)
                {
                    mountains++;
                    double average = 0;
                    for (int dy = 0; dy < 8; dy++) for (int dx = 0; dx < 8; dx++)
                        average += Red(pixels[(land.z * 8 + dy) * 320 + land.x * 8 + dx]);
                    Require(average / 64 < 155, "Mountain relief missing from real coordinate " + land.id);
                }
                else
                {
                    landCount++;
                    Require(!WaterColor(center) && Red(center) > 150 && Green(center) > 145,
                        "Traversable land turned into false water/mountain at " + land.id);
                }
            }
            evidence.checkedLandCoordinates += seen.Count;
            Require(water > 50 && bridges == 6 && mountains > 50 && landCount > 1300, "Actual S1 geography fixture changed unexpectedly.");
            // Real bridges run east/west. Their water beneath the deck must remain visible.
            foreach (var bridge in world.lands.Where(l => l.bridge))
            {
                int cx = bridge.x * 8 + 4, cz = bridge.z * 8 + 4;
                Require(BridgeColor(pixels[cz * 320 + cx - 3]) && BridgeColor(pixels[cz * 320 + cx + 3]), "Real east/west bridge lost its span.");
                Require(WaterColor(pixels[(cz - 4) * 320 + cx]) && WaterColor(pixels[(cz + 3) * 320 + cx]), "Real river does not continue under bridge.");
            }
        }
        private static void SaveImages(int[] pixels, World world, int start, string directory, Evidence evidence)
        {
            string pixelHash = Hash(PixelBytes(pixels)), worldHash = Hash(State(world));
            using (var bitmap = new Bitmap(320, 320))
            {
                for (int y = 0; y < 320; y++) for (int x = 0; x < 320; x++)
                    bitmap.SetPixel(x, 319 - y, Color.FromArgb(pixels[y * 320 + x]));
                foreach (int size in new[] { 320, 180 })
                {
                    string file = "atlas-start-" + start + "-" + size + ".png";
                    using (var scaled = new Bitmap(size, size))
                    using (var graphics = Graphics.FromImage(scaled))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.DrawImage(bitmap, 0, 0, size, size);
                        scaled.Save(Path.Combine(directory, file), ImageFormat.Png);
                    }
                    evidence.images.Add(new ImageEvidence { file = file, sha256 = Hash(File.ReadAllBytes(Path.Combine(directory, file))),
                        pixelSha256 = pixelHash, worldSha256 = worldHash, width = size, height = size, startIndex = start });
                }
            }
        }
        private static void CheckIndependentProjection()
        {
            var world = World.CreateYouzhou(0);
            int[] baseline = MinimapAtlasRaster.Render(world);
            world.lands.Reverse();
            Require(baseline.SequenceEqual(MinimapAtlasRaster.Render(world)), "Atlas uses list order rather than real x/z coordinates.");
            foreach (var land in world.lands) { land.owned = !land.owned; land.capital = !land.capital; land.home = !land.home; land.level += 3; land.name = "overlay-only"; }
            world.ruler = "overlay-independent"; world.troops += 999; world.elapsed += 30; world.resources.grain += 10;
            Require(baseline.SequenceEqual(MinimapAtlasRaster.Render(world)), "Dynamic gameplay or city state leaked into terrain raster.");

            // Deliberately rotate a crossing in an isolated fixture, so a hard-coded horizontal
            // bridge renderer fails. The fixture never writes a campaign save.
            world = World.CreateYouzhou(0);
            foreach (var land in world.lands) { land.kind = LandKind.Grain; land.bridge = false; }
            for (int z = 18; z <= 22; z++) for (int x = 17; x <= 23; x++) world.TryGetLand(x, z).kind = LandKind.Water;
            for (int z = 18; z <= 22; z++) { var land = world.TryGetLand(20, z); land.bridge = true; land.kind = LandKind.Grain; }
            byte[] before = State(world);
            int[] vertical = MinimapAtlasRaster.Render(world);
            Require(before.SequenceEqual(State(world)), "Synthetic bridge rendering changed World.");
            int centerX = 20 * 8 + 4;
            for (int z = 18; z <= 22; z++)
            {
                int cy = z * 8 + 4;
                Require(BridgeColor(vertical[(cy - 3) * 320 + centerX]) && BridgeColor(vertical[(cy + 3) * 320 + centerX]), "Vertical bridge span is not north/south.");
                Require(WaterColor(vertical[cy * 320 + centerX - 4]) && WaterColor(vertical[cy * 320 + centerX + 3]), "Vertical bridge erased side water.");
            }
            // Four deliberately distinct corner sentinels verify bottom-left indexing and scale.
            world.TryGetLand(0, 0).kind = LandKind.Water;
            world.TryGetLand(39, 39).kind = LandKind.Water;
            int[] corners = MinimapAtlasRaster.Render(world);
            Require(WaterColor(Center(corners, 0, 0)) && WaterColor(Center(corners, 39, 39)), "Corner coordinates were flipped or cropped.");
            Require(!WaterColor(Center(corners, 0, 39)) && !WaterColor(Center(corners, 39, 0)), "Opposite corners were incorrectly coloured as water.");
        }

        private static double PolygonArea(IList<ViewportPoint> polygon)
        {
            double area = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
                area += (double)a.x * b.y - (double)b.x * a.y;
            }
            return Math.Abs(area) / 2;
        }

        private static bool InsideConvex(IList<ViewportPoint> polygon, ViewportPoint point)
        {
            bool positive = false, negative = false;
            for (int i = 0; i < polygon.Count; i++)
            {
                var a = polygon[i]; var b = polygon[(i + 1) % polygon.Count];
                double cross = ((double)b.x - a.x) * (point.y - a.y) - ((double)b.y - a.y) * (point.x - a.x);
                positive |= cross > 0.0000002; negative |= cross < -0.0000002;
            }
            return !(positive && negative);
        }

        private static List<ViewportPoint> CheckClip(ViewportPoint[] input, string name, Evidence evidence)
        {
            var polygon = input.ToList(); MinimapViewportGeometry.ClipToMap(polygon); evidence.viewportCases++;
            foreach (var point in polygon)
            {
                Require(!float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsInfinity(point.x) && !float.IsInfinity(point.y)
                    && point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1, name + ": strict normalized bounds failed.");
                Require(InsideConvex(input, point), name + ": clipping added a vertex outside the actual camera footprint.");
            }
            Require(PolygonArea(polygon) <= PolygonArea(input) + 0.0000002 && PolygonArea(polygon) <= 1.0000002,
                name + ": clipping enlarged the visible area.");
            foreach (var point in input.Where(p => p.x >= 0 && p.x <= 1 && p.y >= 0 && p.y <= 1))
                Require(polygon.Contains(point), name + ": an untouched camera vertex moved.");
            // Clipping twice must not change even a boundary coordinate. This is an
            // exact check, like the publisher's JSON [0,1] assertion.
            var second = polygon.ToList(); MinimapViewportGeometry.ClipToMap(second);
            Require(polygon.SequenceEqual(second), name + ": clipping was not idempotent.");
            return polygon;
        }

        private static ViewportPoint[] Box(float x0, float y0, float x1, float y1)
        { return new[] { new ViewportPoint(x0, y0), new ViewportPoint(x1, y0), new ViewportPoint(x1, y1), new ViewportPoint(x0, y1) }; }

        private static void CheckViewportGeometry(Evidence evidence)
        {
            var inside = Box(.2f, .3f, .7f, .8f);
            Require(CheckClip(inside, "inside", evidence).SequenceEqual(inside), "Visible camera polygon changed without clipping.");
            var diamond = new[] { new ViewportPoint(-.25f, .5f), new ViewportPoint(.5f, 1.25f),
                new ViewportPoint(1.25f, .5f), new ViewportPoint(.5f, -.25f) };
            var clipped = CheckClip(diamond, "four map edges", evidence);
            Require(clipped.Count == 8 && Math.Abs(PolygonArea(clipped) - .875) < 1e-12,
                "Four-edge clipping lost the actual octagon/area; corner clamping is not polygon clipping.");
            Require(CheckClip(Box(-2, .2f, -1, .8f), "outside west", evidence).Count == 0, "Off-map camera fabricated a footprint.");
            Require(Math.Abs(PolygonArea(CheckClip(Box(-1, -1, 2, 2), "whole map", evidence)) - 1) < 1e-12,
                "Large camera must cover the whole map exactly.");
            Require(Math.Abs(PolygonArea(CheckClip(Box(-.2f, .2f, .3f, .8f), "west crossing", evidence)) - .18) < 1e-7,
                "Single-edge intersection changed the visible rectangle area.");

            // Reconstruct the missing west camera corner from the three unchanged
            // camera vertices in publication 02m-atlas-22.json. That capture had
            // x=-4.9622528308646E-09 and x=-1.2372847191244318E-09 after old Lerp.
            var b = new ViewportPoint(.2142687737941742f, .6141958236694336f);
            var c = new ViewportPoint(.5112536549568176f, .31721097230911257f);
            var d = new ViewportPoint(.22690649330615998f, .0328638069331646f);
            var a = new ViewportPoint(b.x + d.x - c.x, b.y + d.y - c.y);
            var regression = CheckClip(new[] { a, b, c, d }, "published navigation regression", evidence);
            Require(regression.Count == 5 && regression.Count(p => p.x == 0) == 2,
                "Published west-edge regression did not retain two exact boundary intersections.");
            var westY = regression.Where(p => p.x == 0).Select(p => p.y).OrderBy(y => y).ToArray();
            Require(Math.Abs(westY[0] - .25977033376693728) < 1e-6 && Math.Abs(westY[1] - .3999270498752594) < 1e-6,
                "Boundary repair changed the observed navigation footprint shape.");

            // Independent analytic orthographic footprints: the production camera
            // uses yaw=45 and pitch=30. No Unity camera is started in this check.
            double diagonal = Math.Sqrt(.5);
            foreach (double aspect in new[] { 1440.0 / 752, 1280.0 / 572, 1600.0 / 572 })
                foreach (double zoom in new[] { 2.7, 4.2, 9.0, 18.0 })
                    foreach (double focusX in new[] { 0.0, 8.323529243469239, 19.5, 39.0 })
                        foreach (double focusZ in new[] { 0.0, 19.5, 26.558822631835939, 39.0 })
                        {
                            var corners = new List<ViewportPoint>();
                            foreach (var sign in new[] { new[] { -1, -1 }, new[] { 1, -1 }, new[] { 1, 1 }, new[] { -1, 1 } })
                            {
                                double right = sign[0] * zoom * aspect, up = sign[1] * zoom * 2;
                                corners.Add(new ViewportPoint((float)((focusX + diagonal * (right + up) + .5) / 40),
                                    (float)((39.5 - focusZ - diagonal * (-right + up)) / 40)));
                            }
                            var result = CheckClip(corners.ToArray(), "camera-shaped sweep", evidence);
                            Require(result.Count >= 3 && PolygonArea(result) > .0001, "In-map camera focus lost its footprint.");
                        }
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                bool rejected = false;
                try { MinimapViewportGeometry.ClipToMap(new List<ViewportPoint> { new ViewportPoint(invalid, 0) }); }
                catch (ArgumentException) { rejected = true; }
                Require(rejected, "Nonfinite camera input must not become a visible polygon.");
            }
            evidence.states.Add("viewport: exact map boundaries; retained camera vertices; octagon area; off-map rejection; published west-edge regression; 192 camera-shaped fixtures; idempotence");
        }
        public static int Main(string[] args)
        {
            try
            {
                string directory = args[0], source = args[1]; Directory.CreateDirectory(directory);
                var evidence = new Evidence { sourcePath = source, sourceSha256 = Hash(File.ReadAllBytes(source)),
                    viewportSourcePath = args[2], viewportSourceSha256 = Hash(File.ReadAllBytes(args[2])) };
                int[] first = null;
                for (int start = 0; start < 3; start++)
                {
                    World world = World.CreateYouzhou(start); string error;
                    Require(world.IsValid(out error), "Invalid real start fixture: " + error);
                    byte[] before = State(world);
                    int[] pixels = MinimapAtlasRaster.Render(world);
                    Require(pixels.Length == 320 * 320 && pixels.All(p => (uint)p >> 24 == 255), "Invalid raster dimensions/opacity.");
                    Require(pixels.SequenceEqual(MinimapAtlasRaster.Render(world)), "Raster is nondeterministic.");
                    Require(before.SequenceEqual(State(world)), "Raster changed complete serialized World.");
                    Require(first == null || first.SequenceEqual(pixels), "Birth location changed terrain that should remain shared.");
                    first = pixels;
                    CheckRealCoordinates(world, pixels, evidence);
                    SaveImages(pixels, world, start, directory, evidence);
                    evidence.states.Add("start-" + start + ": 1600 actual coordinates; geography; deterministic; World unchanged");
                }
                CheckIndependentProjection();
                CheckViewportGeometry(evidence);
                evidence.checks = checks;
                using (var stream = File.Create(Path.Combine(directory, "result.json")))
                    new DataContractJsonSerializer(typeof(Evidence)).WriteObject(stream, evidence);
                Console.WriteLine("MINIMAP_ATLAS_CHECKS_OK: " + checks + " assertions; 4800 real coordinates; three starts; real and rotated bridges; " + evidence.viewportCases + " viewport geometry cases; strict [0,1] boundaries; read-only deterministic projection.");
                Console.WriteLine("PREVIEW_LIMITATION: CPU raster previews only, not game screenshots or desktop/Android acceptance.");
                return 0;
            }
            catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
        }
    }
}
