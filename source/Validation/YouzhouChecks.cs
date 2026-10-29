using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;

public static class YouzhouChecks
{
    private static int checks;
    private static readonly int[] Dx = { -1, 1, 0, 0 }, Dz = { 0, 0, -1, 1 };
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static World Fresh(int start = 0) { return World.CreateYouzhou(start, "Youzhou test"); }
    private static void Valid(World world, string context)
    { string problem; Check(world.IsValid(out problem), context + ": " + problem); }
    private static void Invalid(World world, string context)
    { string problem; Check(!world.IsValid(out problem) && !string.IsNullOrEmpty(problem), context); }
    private static IEnumerable<Land> Neighbors(World w, Land t)
    {
        for (int i = 0; i < 4; i++) { var next = w.TryGetLand(t.x + Dx[i], t.z + Dz[i]); if (next != null) yield return next; }
    }
    private static HashSet<int> Reachable(World w, bool allowBridges)
    {
        var reached = new HashSet<int> { w.homeId }; var pending = new Queue<int>(); pending.Enqueue(w.homeId);
        while (pending.Count > 0)
            foreach (var next in Neighbors(w, w.lands[pending.Dequeue()]))
                if (next.Walkable && (allowBridges || !next.bridge) && reached.Add(next.id)) pending.Enqueue(next.id);
        return reached;
    }
    private static World RoundTrip(World original, string label)
    {
        var serializer = new DataContractJsonSerializer(typeof(World));
        World restored;
        using (var buffer = new MemoryStream())
        {
            serializer.WriteObject(buffer, original); buffer.Position = 0;
            restored = (World)serializer.ReadObject(buffer);
        }
        Valid(restored, label + " roundtrip validation");
        Check(restored.IsYouzhou && restored.mapId == original.mapId && restored.mapRevision == original.mapRevision
            && restored.MapWidth == original.MapWidth && restored.MapHeight == original.MapHeight, label + " map identity changed");
        Check(restored.homeId == original.homeId && restored.birthRegion == original.birthRegion
            && restored.OwnedCount == original.OwnedCount && restored.lands.Count(t => t.bridge) == 6, label + " territory changed");
        Check(Math.Abs(restored.resources.grain - original.resources.grain) < .000001
            && restored.cards.Select(c => c.uid).SequenceEqual(original.cards.Select(c => c.uid))
            && restored.formation.SequenceEqual(original.formation), label + " economy/cards changed");
        Check((restored.order == null) == (original.order == null), label + " idle/order distinction changed");
        if (original.order != null)
            Check(restored.order.target == original.order.target && restored.order.returning == original.order.returning
                && Math.Abs(restored.order.remaining - original.order.remaining) < .000001, label + " march changed");
        return restored;
    }

    private static void CheckLegacyJson(Stream stream, string state, string source)
    {
        var legacy = (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream);
        Valid(legacy, source + " Unity v1 JSON " + state);
        Check(!legacy.IsYouzhou && legacy.MapWidth == 36 && legacy.lands.Count == 972, "Existing v1 JSON was reinterpreted");
        bool marching = state == "outbound" || state == "returning";
        Check((legacy.order != null) == marching, source + " " + state + " lost the idle/order distinction");
        if (marching)
            Check(legacy.order.returning == (state == "returning") && legacy.order.remaining > 0,
                source + " " + state + " changed march phase");
        Check(legacy.reports.Count == (state == "returning" || state == "returned" ? 1 : 0),
            source + " " + state + " changed the settlement count");
    }

    public static int Main(string[] args)
    {
        try
        {
            for (int start = 0; start < YouzhouMapDefinition.StartNames.Length; start++)
            {
                var w = Fresh(start); Valid(w, "start " + start);
                Check(w.saveVersion == 2 && w.mapId == "youzhou-40-v1" && w.mapRevision == 1, "Wrong metadata");
                Check(w.IsYouzhou && w.MapWidth == 40 && w.MapHeight == 40 && w.MapName == "幽州", "Wrong map identity");
                Check(w.lands.Count == 1600 && w.lands.Select(t => t.id).Distinct().Count() == 1600, "Missing/duplicate lands");
                Check(w.lands.All(t => t.id == t.z * 40 + t.x && object.ReferenceEquals(t, w.TryGetLand(t.x, t.z))), "ID/coordinate roundtrip mismatch");
                Check(w.lands.All(t => t.homeland == Homeland.Han) && w.origin == Homeland.Han, "False foreign culture in Youzhou");
                Check(w.lands.Count(t => t.capital) == 6 && w.lands.Where(t => t.capital).Select(t => t.name).OrderBy(n => n)
                    .SequenceEqual(YouzhouMapDefinition.CapitalNames.OrderBy(n => n)), "Wrong capitals");
                Check(w.lands.Count(t => t.home) == 1 && w.lands[w.homeId].owned && w.lands[w.homeId].region == w.birthRegion, "Invalid home");
                Check(w.lands[w.homeId].x == YouzhouMapDefinition.StartX(start) && w.lands[w.homeId].z == YouzhouMapDefinition.StartZ(start), "Wrong spawn anchor");
                Check(w.RegionName(w.birthRegion) == YouzhouMapDefinition.RegionNames[w.birthRegion], "Legacy region name leaked");
                Check(w.cards.Count == 3 && w.cards.All(c => c.Definition.homeland == Homeland.Han) && w.formation.Count == 3, "Starter roster mismatch");
                Check(!w.allPoolsUnlocked && w.lands.Any(t => w.AttackProblem(t.id).Length == 0), "No valid opening outside the nine city plots");
                Check(w.lands.Count(t => t.kind == LandKind.Water) == 74 && w.lands.Count(t => t.bridge) == 6, "River/crossing topology changed");
                Check(w.lands.Where(t => t.bridge).All(t => t.Walkable && t.kind == LandKind.Grain && !t.capital && !t.home && t.name.Contains("桥")), "Bridge contract broken");
                Check(w.lands.Where(t => t.kind == LandKind.Water || t.kind == LandKind.Mountain).All(t => !t.Walkable), "Obstacle became walkable");
                Check(w.lands.Count(t => t.kind == LandKind.Mountain) > 50 && w.lands.Where(t => t.kind == LandKind.Mountain).All(t => t.z >= 22 && t.x <= 21), "North/west ridge missing");
                Check(w.lands.Where(t => !t.capital && !t.home && !t.bridge).All(t => t.name == Catalog.LandName(t.kind)), "Resource name mismatch");
                var reachable = Reachable(w, true);
                Check(w.lands.Where(t => t.capital || t.bridge).All(t => reachable.Contains(t.id)), "A city or crossing is unreachable");
                Check(Enumerable.Range(0, 3).All(i => reachable.Contains(w.TryGetLand(YouzhouMapDefinition.StartX(i), YouzhouMapDefinition.StartZ(i)).id)), "Other start unreachable");
                var noBridges = Reachable(w, false);
                Check(w.lands.Any(t => t.capital && !noBridges.Contains(t.id)), "River can be crossed without its designated bridges");
                Check(w.TryGetLand(-1, 0) == null && w.TryGetLand(0, -1) == null && w.TryGetLand(40, 0) == null
                    && w.TryGetLand(0, 40) == null && w.TryGetLand(int.MaxValue, int.MaxValue) == null, "Out-of-range lookup accepted");
                Check(w.TryGetLand(0, 0).id == 0 && w.TryGetLand(39, 39).id == 1599, "Corner coordinates wrong");
                Check(Neighbors(w, w.TryGetLand(0, 0)).Count() == 2 && Neighbors(w, w.TryGetLand(39, 39)).Count() == 2, "Corner degree wrong");
                Check(!Neighbors(w, w.lands[39]).Any(t => t.id == 40), "Row wrap became adjacency");
            }
            {
                var w = Fresh(); var same = Fresh(1);
                Check(w.lands.All(t => t.home || same.lands[t.id].home || (t.kind == same.lands[t.id].kind && t.bridge == same.lands[t.id].bridge && t.region == same.lands[t.id].region)), "Birth choice regenerated geography");
                var before = w.HourlyIncome(); var bridge = w.lands.First(t => t.bridge); bridge.owned = true;
                Check(Math.Abs(w.HourlyIncome().grain - before.grain) < .000001 && w.OwnedCount == 1, "Bridge generated grain or failed territory count");
                var farm = w.lands.First(t => t.kind == LandKind.Grain && !t.home && !t.capital && !t.bridge && !w.IsCityDistrict(t.id));
                farm.owned = true; Check(Math.Abs(w.HourlyIncome().grain - before.grain - farm.level * 180) < .000001, "Direct owned mutation left stale income");
                farm.level = 5; Check(Math.Abs(w.HourlyIncome().grain - before.grain - 900) < .000001, "Direct level mutation left stale income");
                farm.owned = false; Check(Math.Abs(w.HourlyIncome().grain - before.grain) < .000001, "Ownership loss left stale income");
                var city = w.lands.First(t => t.capital); city.owned = true;
                Check(Math.Abs(w.HourlyIncome().grain - before.grain * 1.15) < .000001, "Capital bonus mismatch");
            }
            {
                var w = Fresh(); foreach (var t in w.lands) t.owned = false;
                w.lands[39].owned = true; var acrossRow = w.lands[40]; acrossRow.kind = LandKind.Grain;
                Check(w.AttackProblem(40).Contains("相邻"), "Attack allowed wrapped row neighbor");
                w.lands[41].owned = true;
                Check(w.AttackProblem(40).Length == 0, "Real horizontal neighbor rejected");
                Check(w.AttackProblem(-1).Length > 0 && w.AttackProblem(1600).Length > 0, "Out-of-range attack accepted");
            }
            {
                var w = Fresh(); w = RoundTrip(w, "idle");
                int target = w.lands.First(t => w.AttackProblem(t.id).Length == 0).id;
                w.March(target); w.Tick(w.order.duration * .5); w = RoundTrip(w, "outbound");
                w.Tick(w.order.remaining + .01);
                Check(w.order != null && w.order.returning && w.lands[target].owned, "Opening attack failed");
                w = RoundTrip(w, "returning"); w.Tick(w.order.remaining + .01); w = RoundTrip(w, "returned");
                Check(w.order == null && w.reports.Count == 1 && w.reports[0].text.Contains(w.RegionName(w.lands[target].region)), "Local region report/return mismatch");
            }
            {
                var a = Fresh(); var b = Fresh(); int target = a.lands.First(t => a.AttackProblem(t.id).Length == 0).id;
                a.March(target); b.March(target); a.Tick(1000); for (int i = 0; i < 1000; i++) b.Tick(1);
                Check(Math.Abs(a.resources.grain - b.resources.grain) < .00001 && a.troops == b.troops && a.OwnedCount == b.OwnedCount, "Offline/incremental Youzhou settlement differs");
            }
            {
                var w = Fresh(); foreach (var card in w.cards) card.GainExperience(1000000); w.troops = w.TroopCap;
                var city = w.lands.First(t => t.capital); Neighbors(w, city).First(t => t.Walkable).owned = true;
                w.March(city.id); w.Tick(1000);
                Check(city.owned && !w.allPoolsUnlocked, "A Han Youzhou city unlocked foreign recruitment");
            }
            {
                var w = Fresh(); w.mapId = "unknown-map"; Invalid(w, "Unknown map accepted");
                w = Fresh(); w.mapWidth = 128; Invalid(w, "Wrong width accepted");
                w = Fresh(); w.mapHeight = 0; Invalid(w, "Missing height accepted");
                w = Fresh(); w.mapRevision = 2; Invalid(w, "Unsupported map revision accepted");
                w = Fresh(); w.saveVersion = 1; Invalid(w, "Youzhou payload accepted as v1");
                w = Fresh(); w.mapId = World.LegacyMapId; Invalid(w, "Legacy map accepted in v2 envelope");
                w = Fresh(); w.saveVersion = 3; Invalid(w, "Unknown save version accepted");
                w = Fresh(); w.lands.RemoveAt(1599); Invalid(w, "Truncated map accepted");
                w = Fresh(); w.lands[40].id = 39; Invalid(w, "Duplicate ID accepted");
                w = Fresh(); w.lands[40].x = 1; Invalid(w, "Wrong tile coordinate accepted");
                w = Fresh(); w.lands[20].region = 12; Invalid(w, "Legacy region in Youzhou accepted");
                w = Fresh(); w.lands[20].homeland = Homeland.Steppe; Invalid(w, "False foreign culture accepted");
                w = Fresh(); w.lands.First(t => t.bridge).kind = LandKind.Water; Invalid(w, "Impassable bridge accepted");
                w = Fresh(); w.lands.First(t => t.bridge).capital = true; Invalid(w, "Bridge city accepted");
                w = Fresh(); w.lands[0].home = true; Invalid(w, "Two homes accepted");
                w = Fresh(); w.birthRegion = 12; Invalid(w, "Invalid birth region accepted");
                w = Fresh(); w.order = new ArmyOrder { target = 1600, duration = 1, remaining = 1 }; Invalid(w, "Out-of-map order accepted");
                w = Fresh(); w.order = new ArmyOrder { target = w.lands.First(t => t.kind == LandKind.Water).id, duration = 1, remaining = 1 }; Invalid(w, "Order into water accepted");
                w = World.Create(Homeland.Han, 4, "Legacy"); Valid(w, "legacy null metadata");
                Check(!w.IsYouzhou && w.MapWidth == 36 && w.MapHeight == 27 && w.RegionName(4) == "豫州", "Legacy context changed");
                w.mapId = YouzhouMapDefinition.Id; Invalid(w, "Youzhou ID accepted by v1");
                w = World.Create(Homeland.Han, 4, "Legacy"); w.mapWidth = 40; w.mapHeight = 40; Invalid(w, "New dimensions accepted by v1");
                bool rejected = false; try { World.CreateYouzhou(3, "Bad"); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "Unknown start accepted");
                rejected = false; try { World.CreateYouzhou(-1, "Bad"); } catch (ArgumentOutOfRangeException) { rejected = true; }
                Check(rejected, "Negative start accepted"); Valid(World.CreateYouzhou(), "default start");
            }
            // Always exercise frozen historical Unity output; generating today's World
            // here would miss compatibility regressions in old saves. These are test
            // inputs, independent of the current Editor's generated JSON artifacts.
            string[] legacyStates = { "idle", "outbound", "returning", "returned" };
            foreach (string state in legacyStates)
                using (var stream = LegacySerializationFixtures.Open(state))
                    CheckLegacyJson(stream, state, "frozen 2026-10-25");

            if (args.Length > 0)
            {
                string directory = Path.Combine(args[0], "Validation");
                bool hasGeneratedJson = legacyStates.Any(state => File.Exists(Path.Combine(directory, "serialization-" + state + ".json")));
                if (hasGeneratedJson)
                {
                    // If an Editor run wrote any output, require all four states. A
                    // partial run must not silently become a successful validation.
                    foreach (string state in legacyStates)
                    {
                        string path = Path.Combine(directory, "serialization-" + state + ".json");
                        using (var stream = File.OpenRead(path))
                            CheckLegacyJson(stream, state, "existing");
                    }
                }
                else Console.WriteLine("INFO: No generated Unity JSON artifacts; all four frozen v1 fixtures were validated. Publisher must still run Unity JsonUtility checks.");
            }
            Console.WriteLine("PASS: " + checks + " Youzhou checks; 3 starts, 1600 tiles, 6 Han cities, 74 water tiles, 6 bridge tiles, topology, neighbors, economy, metadata rejection and CLR JSON roundtrips. Unity JsonUtility execution remains a separate Editor check.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL: " + error); return 1; }
    }
}
