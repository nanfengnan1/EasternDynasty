using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace EasternDynasty
{
    public static class RiverLayoutChecks
    {
        private static int checks;
        private static void Require(bool condition, string message)
        { checks++; if (!condition) throw new Exception(message); }
        private static void Near(double actual, double expected, double tolerance, string message)
        { Require(Math.Abs(actual - expected) <= tolerance, message + ": " + actual + " / " + expected); }
        private static byte[] State(World world)
        { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return stream.ToArray(); } }
        private static World Reload(byte[] bytes)
        { using (var stream = new MemoryStream(bytes)) return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream); }

        // Independent global oracle: clamp the sample onto every dry rectangle,
        // with no radius restriction, chunk boundary or production helper calls.
        private static double ReferenceDistance(IList<Land> dry, double x, double z)
        {
            double best = 2;
            foreach (var land in dry)
            {
                double px = Math.Max(land.x - .5, Math.Min(land.x + .5, x));
                double pz = Math.Max(land.z - .5, Math.Min(land.z + .5, z));
                double squared = (px - x) * (px - x) + (pz - z) * (pz - z);
                if (squared < best * best) best = Math.Sqrt(squared);
            }
            return best;
        }

        private static byte[] CheckWorld(World world)
        {
            byte[] before = State(world);
            var distances = YouzhouRiverLayout.BuildBankDistances(world);
            int samples = YouzhouRiverLayout.SamplesPerCell, width = world.MapWidth * samples;
            Require(width == 640 && distances.Length == 640 * 640, "S1 whole-map field dimensions changed.");
            var dry = world.lands.Where(t => t.kind != LandKind.Water && !t.bridge).ToList();
            int wetCount = 0, bendCount = 0, bridgeCount = 0, boundaryCount = 0;
            foreach (var land in world.lands)
            {
                bool wet = land.kind == LandKind.Water || land.bridge;
                Require(YouzhouRiverLayout.IsWater(land) == wet, "Water/bridge identity mismatch.");
                if (wet) wetCount++;
                if (land.bridge) bridgeCount++;
                bool pixelsValid = true;
                for (int j = 0; j < samples; j++)
                    for (int i = 0; i < samples; i++)
                    {
                        int px = land.x * samples + i, pz = land.z * samples + j;
                        byte value = distances[pz * width + px];
                        if (!wet) { if (value != 0) pixelsValid = false; continue; }
                        double x = (px + .5) / samples - .5, z = (pz + .5) / samples - .5;
                        // Every water pixel is checked against the global oracle;
                        // the byte conversion may err by at most half one step.
                        double exact = ReferenceDistance(dry, x, z);
                        if (value == 0 || Math.Abs(value / 255.0 * 2 - exact) > 1.0 / 255 + .000001)
                            pixelsValid = false;
                    }
                Require(pixelsValid, "Dry coverage, pixel-centre mapping or bank quantization failed at " + land.id);
                if (!wet) continue;
                foreach (float dx in new[] { -.499f, -.27f, 0, .31f, .499f })
                    Near(YouzhouRiverLayout.BankDistance(world, land.x + dx, land.z + .12f),
                        ReferenceDistance(dry, land.x + dx, land.z + .12f), .00001, "Global nearest bank differs");
                // Water at the top/bottom exits must keep the inland bank distance.
                if (land.z == 0 || land.z == world.MapHeight - 1)
                {
                    boundaryCount++;
                    float edge = land.z == 0 ? -.5f : world.MapHeight - .5f;
                    Near(YouzhouRiverLayout.BankDistance(world, land.x, edge), ReferenceDistance(dry, land.x, edge), .00001,
                        "An imaginary bank closes a river map exit");
                    Require(YouzhouRiverLayout.BankDistance(world, land.x, edge) > .45f, "River exit collapsed.");
                }
                var above = world.TryGetLand(land.x, land.z + 1);
                if (above != null && above.kind != LandKind.Water && !above.bridge) bendCount++;
            }
            Require(wetCount == 80 && bridgeCount == 6 && bendCount > 0 && boundaryCount == 4,
                "Natural river/bridge/bend/exit fixture coverage changed.");

            // Mesh chunks are eight cells. Test the distance on both sides of
            // every actual shared boundary, including dry/water intersections.
            int wetSeams = 0;
            for (int seam = 8; seam < 40; seam += 8)
                for (int p = 0; p < 640; p++)
                {
                    float at = (p + .5f) / samples - .5f;
                    float edge = seam - .5f, epsilon = .0001f;
                    float left = YouzhouRiverLayout.BankDistance(world, edge - epsilon, at);
                    float right = YouzhouRiverLayout.BankDistance(world, edge + epsilon, at);
                    float top = YouzhouRiverLayout.BankDistance(world, at, edge - epsilon);
                    float bottom = YouzhouRiverLayout.BankDistance(world, at, edge + epsilon);
                    Require(Math.Abs(left - right) < .00021f && Math.Abs(top - bottom) < .00021f,
                        "River bank distance has a chunk seam.");
                    if (left > .01f && right > .01f || top > .01f && bottom > .01f) wetSeams++;
                }
            Require(wetSeams > 50, "Chunk seam fixture missed the actual river.");
            Require(distances.SequenceEqual(YouzhouRiverLayout.BuildBankDistances(world)), "Repeated rendering differs.");
            Require(distances.SequenceEqual(YouzhouRiverLayout.BuildBankDistances(Reload(before))), "Reload changes river layout.");

            var allWaterBridges = Reload(before);
            foreach (var bridge in allWaterBridges.lands.Where(t => t.bridge))
            { bridge.kind = LandKind.Water; bridge.bridge = false; }
            Require(distances.SequenceEqual(YouzhouRiverLayout.BuildBankDistances(allWaterBridges)),
                "A bridge interrupts the river beneath it.");
            foreach (int row in new[] { 9, 19, 30 })
            {
                var bridge = world.lands.First(t => t.bridge && t.z == row);
                var from = world.TryGetLand(bridge.x - 1, row);
                var to = world.TryGetLand(bridge.x + 2, row);
                var path = world.FindMarchPath(from.id, to.id);
                Require(path != null && path.Contains(bridge.id), "River presentation blocked the real bridge route.");
                for (int i = 1; i < path.Count; i++) Require(world.CanMarchStep(path[i - 1], path[i], 2), "Bridge route includes an illegal step.");
                var blocked = world.lands.First(t => t.kind == LandKind.Water && !t.bridge);
                Require(world.FindMarchPath(from.id, blocked.id) == null, "Presentation made ordinary water traversable.");
            }
            Require(before.SequenceEqual(State(world)), "River geometry mutated the complete World or stored routes.");
            return distances;
        }

        private static void CheckAnalyticGeometry()
        {
            var world = World.CreateYouzhou();
            foreach (var land in world.lands) { land.kind = LandKind.Water; land.bridge = false; }
            var openWater = YouzhouRiverLayout.BuildBankDistances(world);
            Require(openWater.All(value => value == 255), "Map border created a bank in open water.");
            Near(YouzhouRiverLayout.BankDistance(world, -.5f, -.5f), 2, .000001, "Lower map corner is not valid water");
            Near(YouzhouRiverLayout.BankDistance(world, 39.5f, 39.5f), 2, .000001, "Upper map corner is not valid water");
            var island = world.TryGetLand(15, 15); island.kind = LandKind.Mountain;
            Near(YouzhouRiverLayout.BankDistance(world, 16, 16), Math.Sqrt(.5), .000001, "Diagonal shore is not Euclidean");
            Near(YouzhouRiverLayout.BankDistance(world, 17, 15), 1.5, .000001, "Axial bank distance differs");
            Near(YouzhouRiverLayout.BankDistance(world, 18, 15), 2, .000001, "Distant bank is not capped");
            Near(YouzhouRiverLayout.BankDistance(world, 15, 15), 0, .000001, "Dry mountain has river distance");
            Near(YouzhouRiverLayout.BankDistance(world, 15.5f, 15), 0, .000001, "Bank boundary is not zero");
            world.TryGetLand(16, 16).kind = LandKind.Grain; world.TryGetLand(16, 16).bridge = true;
            Near(YouzhouRiverLayout.BankDistance(world, 16, 16), Math.Sqrt(.5), .000001, "Traversable bridge becomes a dry bank");
            Require(YouzhouRiverLayout.BankDistance(world, -.501f, 0) == 0
                && YouzhouRiverLayout.BankDistance(world, 39.501f, 0) == 0
                && YouzhouRiverLayout.BankDistance(world, 0, -.501f) == 0
                && YouzhouRiverLayout.BankDistance(world, 0, 39.501f) == 0,
                "Outside-map input is treated as a selectable cell.");
        }

        private static void CheckRejectedInputs()
        {
            Require(!YouzhouRiverLayout.IsWater(null), "Null land is water.");
            Require(YouzhouRiverLayout.BuildBankDistances(null).Length == 0
                && YouzhouRiverLayout.BankDistance(null, 0, 0) == 0, "Null world produced river data.");
            var legacy = World.Create(Homeland.Han, 4, "Legacy");
            Require(YouzhouRiverLayout.BuildBankDistances(legacy).Length == 0
                && YouzhouRiverLayout.BankDistance(legacy, 10, 10) == 0, "S1 river was applied to a legacy map.");
            var world = World.CreateYouzhou();
            Require(YouzhouRiverLayout.BankDistance(world, float.NaN, 10) == 0
                && YouzhouRiverLayout.BankDistance(world, 10, float.NaN) == 0
                && YouzhouRiverLayout.BankDistance(world, float.PositiveInfinity, 10) == 0
                && YouzhouRiverLayout.BankDistance(world, 10, float.NegativeInfinity) == 0, "Invalid coordinates accepted.");
            world.lands[0] = null;
            Require(YouzhouRiverLayout.BuildBankDistances(world).Length == 0, "Incomplete world produced a bank field.");
            world.lands.RemoveAt(0);
            Require(YouzhouRiverLayout.BuildBankDistances(world).Length == 0, "Wrong-sized world produced a bank field.");
            world = World.CreateYouzhou(); world.lands[0].x = 1;
            Require(YouzhouRiverLayout.BuildBankDistances(world).Length == 0, "Reordered world produced a bank field.");
        }

        private static void ProjectedCorner(World world, float x, float y, float z, int bridgeRow, string part)
        {
            // ViewRotation is pitch 30 / yaw 45 degrees. A ray hitting this
            // vertex reaches the y=0 picking plane with this displacement.
            double pitch = 30 * Math.PI / 180, yaw = 45 * Math.PI / 180;
            double dx = Math.Abs(y) / Math.Tan(pitch) * Math.Sin(yaw);
            double dz = Math.Abs(y) / Math.Tan(pitch) * Math.Cos(yaw);
            Near(dx, Math.Abs(y) * Math.Sqrt(1.5), .0000001, "Camera projection x bound differs");
            Near(dz, Math.Abs(y) * Math.Sqrt(1.5), .0000001, "Camera projection z bound differs");
            // Check both signs so the bound also survives viewing the opposite
            // side of the arch. x may move onto the same span or its dry bank.
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                {
                    int px = (int)Math.Floor(x + sx * dx + .5);
                    int pz = (int)Math.Floor(z + sz * dz + .5);
                    var picked = world.TryGetLand(px, pz);
                    Require(pz == bridgeRow && picked != null
                        && (picked.bridge || picked.kind != LandKind.Water),
                        part + " projects into adjacent ordinary water: " + px + "," + pz);
                }
        }

        private static void ProjectedStrip(World world, float a, float b, float z0, float z1,
            float ha, float hb, float rise, int bridgeRow, string part)
        {
            foreach (float z in new[] { z0, z1 })
            {
                ProjectedCorner(world, a, ha, z, bridgeRow, part);
                ProjectedCorner(world, b, hb, z, bridgeRow, part);
                ProjectedCorner(world, a, ha + rise, z, bridgeRow, part);
                ProjectedCorner(world, b, hb + rise, z, bridgeRow, part);
            }
        }

        private static void CheckBridgeProfile()
        {
            var world = World.CreateYouzhou(); byte[] before = State(world);
            int spans = 0, cells = 0, crossChunk = 0;
            foreach (var land in world.lands.Where(t => t.bridge))
            {
                float a, b;
                Require(YouzhouRiverLayout.TryBridgeSpan(world, land, out a, out b), "Real crossing has no shared bridge span.");
                var row = world.lands.Where(t => t.bridge && t.z == land.z).ToList();
                Near(a, row.Min(t => t.x) - .5f, 0, "Shared span omits its first crossing cell");
                Near(b, row.Max(t => t.x) + .5f, 0, "Shared span omits its last crossing cell");
            }
            foreach (var first in world.lands.Where(t => t.bridge))
            {
                if (world.TryGetLand(first.x - 1, first.z).bridge) continue;
                int last = first.x;
                while (world.TryGetLand(last + 1, first.z).bridge) last++;
                float start, end;
                Require(YouzhouRiverLayout.TryBridgeSpan(world, first, out start, out end), "Bridge span unavailable.");
                spans++;
                Near(YouzhouRiverLayout.BridgeDeckHeight(start, start, end), .04, .000001, "Bridge does not meet its first bank");
                Near(YouzhouRiverLayout.BridgeDeckHeight(end, start, end), .04, .000001, "Bridge does not meet its last bank");
                Near(YouzhouRiverLayout.BridgeDeckHeight((start + end) / 2, start, end), .11, .000001, "Bridge arch peak changed");
                for (int x = first.x; x <= last; x++)
                {
                    cells++;
                    for (int i = 0; i < 8; i++)
                    {
                        float a = x - .5f + i * .125f, b = a + .125f;
                        float ha = YouzhouRiverLayout.BridgeDeckHeight(a, start, end);
                        float hb = YouzhouRiverLayout.BridgeDeckHeight(b, start, end);
                        Require(ha >= .039999f && ha <= .110001f && hb >= .039999f && hb <= .110001f,
                            "Raised bridge exceeds the safe height envelope.");
                        ProjectedStrip(world, a, b, first.z - YouzhouRiverLayout.BridgeDeckHalfWidth,
                            first.z + YouzhouRiverLayout.BridgeDeckHalfWidth, ha - .026f, hb - .026f, .026f, first.z, "deck base");
                        ProjectedStrip(world, a + .006f, b - .006f, first.z - YouzhouRiverLayout.BridgeDeckHalfWidth,
                            first.z + YouzhouRiverLayout.BridgeDeckHalfWidth, ha, hb, .009f, first.z, "deck planks");
                        for (int side = -1; side <= 1; side += 2)
                        {
                            float rz = first.z + side * YouzhouRiverLayout.BridgeRailOffset;
                            ProjectedStrip(world, a, b, rz - YouzhouRiverLayout.BridgeRailHalfWidth,
                                rz + YouzhouRiverLayout.BridgeRailHalfWidth,
                                ha + YouzhouRiverLayout.BridgeRailRise, hb + YouzhouRiverLayout.BridgeRailRise,
                                YouzhouRiverLayout.BridgeRailThickness, first.z, "rail");
                            if (i % 2 == 0)
                                ProjectedStrip(world, a - YouzhouRiverLayout.BridgePostHalfWidth,
                                    a + YouzhouRiverLayout.BridgePostHalfWidth,
                                    rz - YouzhouRiverLayout.BridgePostHalfWidth, rz + YouzhouRiverLayout.BridgePostHalfWidth,
                                    ha, ha, YouzhouRiverLayout.BridgePostHeight, first.z, "post");
                        }
                    }
                    if (x < last)
                    {
                        float leftEdge = x + .5f, rightEdge = (x + 1) - .5f;
                        Near(YouzhouRiverLayout.BridgeDeckHeight(leftEdge, start, end),
                            YouzhouRiverLayout.BridgeDeckHeight(rightEdge, start, end), 0, "Adjoining bridge decks disconnect");
                        if (x / 8 != (x + 1) / 8)
                        {
                            crossChunk++;
                            Require(x == 23 && x + 1 == 24, "Expected actual bridge at the x=23/24 chunk boundary.");
                        }
                        ProjectedStrip(world, x + .44f, x + .56f, first.z - .25f, first.z + .25f,
                            .015f, .015f, YouzhouRiverLayout.BridgeDeckHeight(x + .5f, start, end) - .04f, first.z, "central pier");
                    }
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    float rz = first.z + side * YouzhouRiverLayout.BridgeRailOffset;
                    float height = YouzhouRiverLayout.BridgeDeckHeight(end, start, end);
                    ProjectedStrip(world, end - YouzhouRiverLayout.BridgePostHalfWidth, end + YouzhouRiverLayout.BridgePostHalfWidth,
                        rz - YouzhouRiverLayout.BridgePostHalfWidth, rz + YouzhouRiverLayout.BridgePostHalfWidth,
                        height, height, YouzhouRiverLayout.BridgePostHeight, first.z, "last post");
                }
                ProjectedStrip(world, start - .07f, start + .08f, first.z - .28f, first.z + .28f,
                    .015f, .015f, .030f, first.z, "first abutment");
                ProjectedStrip(world, end - .08f, end + .07f, first.z - .28f, first.z + .28f,
                    .015f, .015f, .030f, first.z, "last abutment");
            }
            Require(spans == 3 && cells == 6 && crossChunk == 1, "Actual bridge/span/chunk coverage changed.");
            double maximumPickOffset = YouzhouRiverLayout.BridgeRailOffset + YouzhouRiverLayout.BridgePostHalfWidth
                + (.11 + YouzhouRiverLayout.BridgePostHeight) * Math.Sqrt(1.5);
            Require(maximumPickOffset < .46, "Tallest bridge post leaves less than .04 cell of pick-plane margin.");
            Near(YouzhouRiverLayout.BridgeDeckHeight(-100, 1, 3), .04, .000001, "Profile fails to clamp before span");
            Near(YouzhouRiverLayout.BridgeDeckHeight(100, 1, 3), .04, .000001, "Profile fails to clamp after span");
            Near(YouzhouRiverLayout.BridgeDeckHeight(1, 1, 1), .04, .000001, "Empty span produces invalid height");
            Near(YouzhouRiverLayout.BridgeDeckHeight(float.NaN, 1, 3), .04, .000001, "Invalid input produces invalid height");
            float rejectedStart, rejectedEnd;
            var realBridge = world.lands.First(t => t.bridge);
            Require(!YouzhouRiverLayout.TryBridgeSpan(null, realBridge, out rejectedStart, out rejectedEnd)
                && !YouzhouRiverLayout.TryBridgeSpan(world, null, out rejectedStart, out rejectedEnd)
                && !YouzhouRiverLayout.TryBridgeSpan(world, world.lands[world.homeId], out rejectedStart, out rejectedEnd)
                && !YouzhouRiverLayout.TryBridgeSpan(World.Create(Homeland.Han, 4, "Legacy"), realBridge, out rejectedStart, out rejectedEnd)
                && !YouzhouRiverLayout.TryBridgeSpan(world, Reload(before).lands[realBridge.id], out rejectedStart, out rejectedEnd)
                && rejectedStart == 0 && rejectedEnd == 0, "Invalid/foreign/legacy bridge creates a span.");
            Require(before.SequenceEqual(State(world)), "Bridge presentation changes persisted World state.");
        }

        public static int Main()
        {
            try
            {
                byte[] baseline = null;
                for (int start = 0; start < 3; start++)
                {
                    var current = CheckWorld(World.CreateYouzhou(start));
                    if (baseline == null) baseline = current;
                    else Require(baseline.SequenceEqual(current), "Choosing a homeland changes actual river geometry.");
                }
                CheckAnalyticGeometry(); CheckRejectedInputs(); CheckBridgeProfile();
                Console.WriteLine("RIVER_LAYOUT_CHECKS_OK: " + checks
                    + " assertions; all three starts, every water texel against global dry-rectangle oracle, quantization, bridges/routes, bends/exits, chunk continuity, bridge profile/corner pick-plane bounds at pitch30/yaw45, reload and complete World immutability. No Unity/shader/visual acceptance.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
