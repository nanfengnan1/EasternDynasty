// Standalone numerical rendering of the production raster. Not a Unity player screenshot.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using UnityEngine;

namespace EasternDynasty
{
    public sealed partial class EasternDynastyGame
    {
        private static UnityEngine.Color C(string hex)
        {
            int value = Convert.ToInt32(hex, 16);
            return new UnityEngine.Color(((value >> 16) & 255) / 255f, ((value >> 8) & 255) / 255f, (value & 255) / 255f, 1);
        }
        private static Color32[] Pixels(CitySceneRaster raster)
        { return (Color32[])typeof(CitySceneRaster).GetField("pixels", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(raster); }

        [DataContract]
        private sealed class CityArtEvidence
        {
            [DataMember] public string evidenceKind = "production-cpu-raster";
            [DataMember] public string limitation = "Standalone production raster; no Unity UI, labels, pointer input, safe area or Android device acceptance. Scaled previews are art legibility references, not phone screenshots.";
            [DataMember] public string generatedAtUtc = DateTime.UtcNow.ToString("o");
            [DataMember] public string sourcePath, sourceSha256;
            [DataMember] public string fixture = "Isolated World.CreateYouzhou(0); live StartCityConstruction and Tick; no save files.";
            [DataMember] public int newToWorkingChangedPixels, workingToBuiltChangedPixels, builtToDevelopedChangedPixels;
            [DataMember] public List<CityArtImageEvidence> images = new List<CityArtImageEvidence>();
        }

        [DataContract]
        private sealed class CityArtImageEvidence
        {
            [DataMember] public string file, state, fileSha256, sourcePixelsSha256, worldSha256;
            [DataMember] public int width, height, occupiedPlots, constructionPlot;
        }

        private static string Hash(byte[] bytes)
        {
            using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static byte[] WorldState(World world)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world);
                return stream.ToArray();
            }
        }

        private static byte[] PixelBytes(Color32[] pixels)
        {
            var bytes = new byte[pixels.Length * 4];
            for (int i = 0; i < pixels.Length; i++)
            {
                bytes[i * 4] = pixels[i].r; bytes[i * 4 + 1] = pixels[i].g;
                bytes[i * 4 + 2] = pixels[i].b; bytes[i * 4 + 3] = pixels[i].a;
            }
            return bytes;
        }

        private static Bitmap BitmapFromRaster(CitySceneRaster raster, int width, int height)
        {
            var pixels = Pixels(raster);
            if (pixels.Length != width * height) throw new Exception("Production raster dimensions changed without updating the art evidence.");
            var bitmap = new Bitmap(width, height);
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                var pixel = pixels[(height - 1 - y) * width + x];
                bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixel.a, pixel.r, pixel.g, pixel.b));
            }
            return bitmap;
        }

        private static void SaveOverviewImage(Bitmap bitmap, string directory, string filename, string state,
            string pixelHash, string worldHash, World world, CityArtEvidence evidence)
        {
            string path = Path.Combine(directory, filename);
            bitmap.Save(path, ImageFormat.Png);
            string fileHash = Hash(File.ReadAllBytes(path));
            evidence.images.Add(new CityArtImageEvidence
            {
                file = filename, state = state, width = bitmap.Width, height = bitmap.Height, fileSha256 = fileHash,
                sourcePixelsSha256 = pixelHash, worldSha256 = worldHash,
                occupiedPlots = world.cityPlots.Count(p => p.building != CityBuilding.None),
                constructionPlot = world.CityJob == null ? -1 : world.CityJob.plotIndex
            });
            Console.WriteLine("CITY_ART_IMAGE: " + filename + " " + bitmap.Width + "x" + bitmap.Height + " sha256=" + fileHash);
        }

        private static Color32[] CheckOverview(World world, string state, string directory, CityArtEvidence evidence)
        {
            string validProblem;
            if (!world.IsValid(out validProblem)) throw new Exception("Invalid isolated overview fixture " + state + ": " + validProblem);
            byte[] before = WorldState(world);
            var raster = CitySceneRaster.Overview(world);
            var pixels = Pixels(raster);
            var repeatedPixels = Pixels(CitySceneRaster.Overview(world));
            if (!before.SequenceEqual(WorldState(world))) throw new Exception("Rendering city overview changed World: " + state);
            if (!PixelBytes(pixels).SequenceEqual(PixelBytes(repeatedPixels))) throw new Exception("Rendering identical city state is nondeterministic: " + state);
            if (pixels.Length != CitySceneRaster.OverviewWidth * CitySceneRaster.OverviewHeight || pixels.Any(p => p.a != 255)) throw new Exception("Overview has invalid size or transparent gaps: " + state);
            string pixelHash = Hash(PixelBytes(pixels)), worldHash = Hash(before);
            using (var bitmap = BitmapFromRaster(raster, CitySceneRaster.OverviewWidth, CitySceneRaster.OverviewHeight))
            {
                SaveOverviewImage(bitmap, directory, "overview-" + state + ".png", state, pixelHash, worldHash, world, evidence);
                using (var town = bitmap.Clone(new Rectangle(CitySceneRaster.OverviewOffsetX, CitySceneRaster.OverviewOffsetY, 1000, 440), PixelFormat.Format32bppArgb))
                    SaveOverviewImage(town, directory, "overview-" + state + "-town.png", state, pixelHash, worldHash, world, evidence);
                // Uniform image scaling approximates compact art size only; Unity handles actual UI and safe areas.
                foreach (int width in new[] { 640, 400 })
                {
                    int height = (int)Math.Round(width * (double)CitySceneRaster.OverviewHeight / CitySceneRaster.OverviewWidth);
                    using (var scaled = new Bitmap(width, height))
                    using (var graphics = System.Drawing.Graphics.FromImage(scaled))
                    {
                        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                        graphics.DrawImage(bitmap, new Rectangle(0, 0, width, height));
                        SaveOverviewImage(scaled, directory, "overview-" + state + "-scaled-" + width + ".png", state, pixelHash, worldHash, world, evidence);
                    }
                }
            }
            Console.WriteLine("CITY_OVERVIEW_STATE_OK: " + state + "; deterministic rendering; complete World unchanged; pixel-sha256=" + pixelHash);
            return pixels;
        }

        private static int RequireVisualChange(Color32[] before, Color32[] after, string transition)
        {
            int changed = 0;
            for (int i = 0; i < before.Length; i++)
                if (before[i].r != after[i].r || before[i].g != after[i].g || before[i].b != after[i].b || before[i].a != after[i].a) changed++;
            if (changed < 100) throw new Exception("Actual construction transition did not visibly change the production overview: " + transition + " (" + changed + " pixels)");
            Console.WriteLine("CITY_OVERVIEW_TRANSITION_OK: " + transition + " changed-pixels=" + changed);
            return changed;
        }

        private static void BuildOverviewFacility(World world, int index, CityBuilding kind)
        {
            string problem = world.CityBuildProblem(index, kind);
            if (problem != "") throw new Exception("Overview fixture cannot build " + kind + ": " + problem);
            world.StartCityConstruction(index, kind);
            if (world.CityJob == null || world.CityJob.plotIndex != index || world.CityJob.building != kind)
                throw new Exception("Overview fixture did not start the actual construction queue: " + kind);
            int targetLevel = world.CityJob.targetLevel;
            world.Tick(world.CityJob.remaining + .01);
            if (world.CityJob != null || world.CityLevel(kind) != targetLevel)
                throw new Exception("Overview fixture did not complete through World.Tick: " + kind);
        }

        private static void CheckOverviews(string directory, string sourcePath)
        {
            var evidence = new CityArtEvidence { sourcePath = Path.GetFullPath(sourcePath), sourceSha256 = Hash(File.ReadAllBytes(sourcePath)) };
            var world = World.CreateYouzhou(0, "城景栅格验收");
            var fresh = CheckOverview(world, "new", directory, evidence);
            world.resources = new Supplies { grain = 50000, grass = 50000, stone = 50000, iron = 50000, gold = 50000 };
            int millPlot = world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index;
            string problem = world.CityBuildProblem(millPlot, CityBuilding.Mill);
            if (problem != "") throw new Exception("Overview fixture cannot start the actual mill: " + problem);
            world.StartCityConstruction(millPlot, CityBuilding.Mill);
            if (world.CityJob == null || world.CityJob.plotIndex != millPlot || world.CityLevel(CityBuilding.Mill) != 0)
                throw new Exception("Overview construction state is not an actual incomplete mill.");
            var working = CheckOverview(world, "working", directory, evidence);
            evidence.newToWorkingChangedPixels = RequireVisualChange(fresh, working, "new -> live construction");
            world.Tick(world.CityJob.remaining + .01);
            if (world.CityJob != null || world.CityLevel(CityBuilding.Mill) != 1 || world.CityHourlyIncome().grain != 160)
                throw new Exception("Overview completed mill is not backed by the actual finished facility and income.");
            var built = CheckOverview(world, "built", directory, evidence);
            evidence.workingToBuiltChangedPixels = RequireVisualChange(working, built, "live construction -> completed mill");
            BuildOverviewFacility(world, 4, CityBuilding.TownHall);
            BuildOverviewFacility(world, 4, CityBuilding.TownHall);
            foreach (var definition in CityCatalog.Buildings.Where(d => d.kind != CityBuilding.TownHall && d.kind != CityBuilding.Mill))
            {
                int plot = world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId)).index;
                BuildOverviewFacility(world, plot, definition.kind);
            }
            if (world.cityPlots.Count(p => p.building != CityBuilding.None) != 9) throw new Exception("Developed overview does not contain all nine actual facilities.");
            var developed = CheckOverview(world, "developed", directory, evidence);
            evidence.builtToDevelopedChangedPixels = RequireVisualChange(built, developed, "completed mill -> all facilities");
            using (var stream = File.Create(Path.Combine(directory, "manifest.json")))
                new DataContractJsonSerializer(typeof(CityArtEvidence)).WriteObject(stream, evidence);
            Console.WriteLine("CITY_OVERVIEW_RASTER_OK: four production states, actual construction and completion, immutable World, deterministic pixels; standalone raster evidence only.");
        }

        public static void Main(string[] args)
        {
            if (args.Length < 2) throw new ArgumentException("Expected evidence directory and production raster source path.");
            Directory.CreateDirectory(args[0]);
            using (var sheet = new Bitmap(780, 660))
            using (var graphics = System.Drawing.Graphics.FromImage(sheet))
            using (var font = new System.Drawing.Font("Microsoft YaHei", 13))
            {
                graphics.Clear(System.Drawing.Color.FromArgb(24, 39, 44));
                int index = 0;
                foreach (var definition in CityCatalog.Buildings)
                {
                    // Oversized comparison detects genuine clipping in the production 240x180 tile.
                    var large = new CitySceneRaster(480, 360); large.Building(new Vector2(240, 227), definition.kind);
                    var largePixels = Pixels(large); int minX = 480, maxX = 0, minY = 360, maxY = 0;
                    for (int y = 0; y < 360; y++) for (int x = 0; x < 480; x++)
                    {
                        if (largePixels[(359 - y) * 480 + x].a == 0) continue;
                        minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                    }
                    if (minX < 120 || maxX >= 360 || minY < 90 || maxY >= 270)
                        throw new Exception("Facility art clipped in actual detail tile: " + definition.kind);
                    var raster = new CitySceneRaster(240, 180); raster.Building(new Vector2(120, 137), definition.kind);
                    var pixels = Pixels(raster);
                    using (var bitmap = new Bitmap(240, 180))
                    {
                        for (int y = 0; y < 180; y++) for (int x = 0; x < 240; x++)
                        {
                            var pixel = pixels[(179 - y) * 240 + x];
                            bitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(pixel.a, pixel.r, pixel.g, pixel.b));
                        }
                        bitmap.Save(Path.Combine(args[0], definition.kind + ".png"), ImageFormat.Png);
                        graphics.DrawImageUnscaled(bitmap, (index % 3) * 260 + 10, (index / 3) * 220);
                        graphics.DrawString(definition.name + " / " + definition.kind, font, Brushes.White, (index % 3) * 260 + 14, (index / 3) * 220 + 185);
                    }
                    Console.WriteLine("RASTER_BOUNDS_OK: " + definition.kind + " [" + (minX - 120) + "," + (minY - 90) + "]..[" + (maxX - 120) + "," + (maxY - 90) + "]");
                    index++;
                }
                sheet.Save(Path.Combine(args[0], "facility-art-preview.png"), ImageFormat.Png);
            }
            CheckOverviews(args[0], args[1]);
        }
    }
}
