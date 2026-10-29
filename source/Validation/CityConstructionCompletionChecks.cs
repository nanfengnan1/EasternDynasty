using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using EasternDynasty;

internal static class CityConstructionCompletionChecks
{
    private static int checks;
    private static readonly DateTime Now = new DateTime(2026, 10, 26, 0, 0, 0, DateTimeKind.Utc);
    private static void Check(bool value, string name)
    { if (!value) throw new Exception("FAIL " + name); checks++; Console.WriteLine("PASS " + name); }
    private static void Throws<T>(Action action, string name) where T : Exception
    { try { action(); } catch (T) { Check(true, name); return; } throw new Exception("FAIL " + name); }
    private static byte[] Encode(World world)
    { using (var stream = new MemoryStream()) { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return stream.ToArray(); } }
    private static World Decode(byte[] bytes)
    { using (var stream = new MemoryStream(bytes)) return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream); }
    private static World Copy(World world) { return Decode(Encode(world)); }
    private static string Hash(World world)
    { using (var algorithm = SHA256.Create()) return BitConverter.ToString(algorithm.ComputeHash(Encode(world))); }
    private static void Same(World expected, World actual, string name)
    { Check(Hash(expected) == Hash(actual), name + " (full World JSON SHA256)"); }

    private sealed class MemoryRepository : ICampaignRepository
    {
        private byte[] stored;
        public int saves;
        public bool failLoad, backup;
        public World overrideLoad;
        public void Seed(World world) { stored = Encode(world); }
        public World Snapshot() { return Decode(stored); }
        public bool Exists(bool youzhou) { return stored != null && Snapshot().IsYouzhou == youzhou; }
        public World Load(bool youzhou, out bool restoredBackup)
        {
            restoredBackup = backup;
            if (failLoad) throw new IOException("Injected memory repository read failure");
            return overrideLoad == null ? Snapshot() : Copy(overrideLoad);
        }
        public void Save(World world) { Seed(world); saves++; }
    }

    private static CampaignSession Fresh(MemoryRepository repository = null, bool readOnly = false)
    {
        var session = new CampaignSession(repository ?? new MemoryRepository(), () => Now, readOnly);
        var world = session.StartYouzhou(0, "Completion");
        world.resources = new Supplies { grain = 1000000, grass = 1000000, stone = 1000000, iron = 1000000, gold = 1000000 };
        return session;
    }
    private static int Start(World world, CityBuilding building)
    {
        var plot = world.cityPlots.FirstOrDefault(p => p.building == building);
        if (plot == null) plot = world.cityPlots.First(p => p.building == CityBuilding.None && world.IsCityDistrict(p.landId));
        Check(world.CityBuildProblem(plot.index, building) == "", "actual command prerequisites " + building);
        world.StartCityConstruction(plot.index, building);
        Check(world.CityJob != null && world.CityJob.plotIndex == plot.index && world.CityJob.building == building, "actual command starts " + building);
        return plot.index;
    }
    private static void Receipt(CityConstructionCompletion value, int index, CityBuilding building, int previous, int target, bool away, string name)
    {
        Check(value != null && value.plotIndex == index && value.building == building
            && value.previousLevel == previous && value.targetLevel == target && value.completedWhileAway == away, name + " identity and levels");
        Check(value.previousEffect == CityCatalog.EffectAtLevel(building, previous)
            && value.completedEffect == CityCatalog.EffectAtLevel(building, target), name + " frozen Core effects");
    }
    private static void AdvanceCompared(CampaignSession session, double seconds, string name)
    {
        var control = Copy(session.World); control.Tick(seconds); session.Tick(seconds);
        Same(control, session.World, name);
    }

    public static int Main()
    {
        try
        {
            Check(typeof(CityConstructionCompletion).GetFields(BindingFlags.Instance | BindingFlags.Public).All(f => f.IsInitOnly), "all receipt fields are immutable");
            Check(!typeof(CityConstructionCompletion).IsSerializable, "receipt has no persisted model contract");
            var empty = new CampaignSession(new MemoryRepository(), () => Now);
            empty.Tick(60); empty.ClearCityCompletion(null);
            Check(empty.LastCityCompletion == null && empty.World == null, "empty session does not fabricate completion");

            var session = Fresh(); var world = session.World;
            int index = Start(world, CityBuilding.Mill);
            foreach (double seconds in new[] { 0d, -1d, double.NaN, double.PositiveInfinity })
            {
                string before = Hash(world); session.Tick(seconds);
                Check(Hash(world) == before && session.LastCityCompletion == null, "paused or invalid Tick cannot finish " + seconds);
            }
            AdvanceCompared(session, world.CityJob.remaining - .25, "before completion matches Core exactly");
            Check(session.LastCityCompletion == null && world.CityJob != null, "partially built facility has no receipt");
            AdvanceCompared(session, .25, "completion adds no World mutation beyond Core");
            var first = session.LastCityCompletion;
            Receipt(first, index, CityBuilding.Mill, 0, 1, false, "new construction");
            AdvanceCompared(session, 60, "later Tick only applies Core progression");
            Check(ReferenceEquals(first, session.LastCityCompletion), "later Tick does not reissue completion");

            Start(world, CityBuilding.Mill);
            Check(ReferenceEquals(first, session.LastCityCompletion), "starting upgrade does not overwrite previous completion");
            AdvanceCompared(session, world.CityJob.remaining, "upgrade has exactly one Core settlement");
            var second = session.LastCityCompletion;
            Receipt(second, index, CityBuilding.Mill, 1, 2, false, "upgrade");
            Receipt(first, index, CityBuilding.Mill, 0, 1, false, "earlier receipt remains frozen after upgrade");
            string completedHash = Hash(world);
            session.ClearCityCompletion(first);
            Check(ReferenceEquals(second, session.LastCityCompletion) && Hash(world) == completedHash, "stale page cannot clear newer receipt or change World");
            session.ClearCityCompletion(null);
            Check(ReferenceEquals(second, session.LastCityCompletion) && Hash(world) == completedHash, "null dismissal cannot clear pending completion");
            session.ClearCityCompletion(second); session.ClearCityCompletion(second);
            Check(session.LastCityCompletion == null && Hash(world) == completedHash, "acknowledgement is idempotent and never mutates World");

            var small = Fresh(); var large = Fresh();
            int smallIndex = Start(small.World, CityBuilding.TownHall); Start(large.World, CityBuilding.TownHall);
            double duration = small.World.CityJob.remaining;
            for (int i = 0; i < 120; i++) small.Tick(duration / 120);
            large.Tick(duration);
            Receipt(small.LastCityCompletion, smallIndex, CityBuilding.TownHall, 1, 2, false, "small-step upgrade");
            Receipt(large.LastCityCompletion, smallIndex, CityBuilding.TownHall, 1, 2, false, "large-step upgrade");
            Check(Math.Abs(small.World.elapsed - large.World.elapsed) < .000001
                && Math.Abs(small.World.resources.grain - large.World.resources.grain) < .000001, "frame sizes agree on time and resource progression");

            session = Fresh(); world = session.World; index = Start(world, CityBuilding.Mill);
            session.Tick(world.CityJob.remaining / 2); world.CancelCityConstruction();
            AdvanceCompared(session, 100, "cancelled job remains cancelled during Tick");
            Check(session.LastCityCompletion == null && world.CityLevel(CityBuilding.Mill) == 0, "cancellation is never a completion");
            Start(world, CityBuilding.Mill); session.Tick(world.CityJob.remaining);
            Receipt(session.LastCityCompletion, index, CityBuilding.Mill, 0, 1, false, "rebuild after cancellation");

            var repository = new MemoryRepository(); session = Fresh(repository); world = session.World;
            index = Start(world, CityBuilding.Mill); world.savedUtcTicks = Now.AddSeconds(-world.CityJob.remaining).Ticks;
            repository.Seed(world); var stored = repository.Snapshot(); var expected = Copy(stored); expected.Tick(expected.CityJob.remaining);
            bool backup; world = session.Load(true, out backup);
            Receipt(session.LastCityCompletion, index, CityBuilding.Mill, 0, 1, true, "offline completion");
            Same(expected, world, "offline receipt does not alter settled snapshot");
            Same(stored, repository.Snapshot(), "offline Load leaves persisted snapshot untouched");
            Check(!backup && repository.saves == 0, "offline completion never writes implicitly");
            session.Save(); Check(repository.saves == 1, "explicit Save remains the only write");
            var savedComplete = repository.Snapshot();
            session.Load(true, out backup);
            Check(session.LastCityCompletion == null, "completed save does not invent historical completion");
            Same(savedComplete, session.World, "completed save remains unchanged when reloaded at same clock");
            Check(!System.Text.Encoding.UTF8.GetString(Encode(savedComplete)).Contains("CityCompletion"), "saved World excludes transient receipt");

            session = Fresh(repository); world = session.World; index = Start(world, CityBuilding.Mill);
            world.savedUtcTicks = Now.AddSeconds(-5).Ticks; repository.Seed(world);
            session.Load(true, out backup);
            Check(session.LastCityCompletion == null && session.World.CityJob != null && session.World.CityJob.remaining == 15, "offline partial construction remains pending");
            session.Tick(15); Receipt(session.LastCityCompletion, index, CityBuilding.Mill, 0, 1, false, "remaining construction completes while playing");
            foreach (long timestamp in new[] { 0L, Now.AddDays(1).Ticks })
            {
                var pending = Copy(world); pending.savedUtcTicks = timestamp; repository.Seed(pending);
                session.Load(true, out backup);
                Check(session.LastCityCompletion == null && session.World.CityJob != null && session.World.CityJob.remaining == 20, "missing or future timestamp cannot invent offline completion " + timestamp);
            }

            world.savedUtcTicks = Now.AddDays(-20).Ticks; repository.Seed(world); repository.backup = true;
            expected = Copy(world); expected.Tick(CampaignSession.MaximumOfflineSeconds);
            session.Load(true, out backup);
            Check(backup && session.World.elapsed == CampaignSession.MaximumOfflineSeconds, "offline construction respects seven-day cap and backup indicator");
            Receipt(session.LastCityCompletion, index, CityBuilding.Mill, 0, 1, true, "capped offline completion");
            Same(expected, session.World, "capped progression matches a single Core Tick");

            var kept = session.World; var keptReceipt = session.LastCityCompletion; string keptHash = Hash(kept);
            repository.failLoad = true;
            Throws<IOException>(() => session.Load(true, out backup), "repository read failure propagates");
            Check(ReferenceEquals(kept, session.World) && ReferenceEquals(keptReceipt, session.LastCityCompletion)
                && Hash(session.World) == keptHash, "read failure preserves old session and receipt");
            repository.failLoad = false; repository.overrideLoad = World.Create(Homeland.Han, 4, "Wrong slot");
            Throws<InvalidDataException>(() => session.Load(true, out backup), "wrong map slot rejects Load");
            Check(ReferenceEquals(kept, session.World) && ReferenceEquals(keptReceipt, session.LastCityCompletion)
                && Hash(session.World) == keptHash, "wrong-slot failure preserves old session and receipt");
            repository.overrideLoad = Copy(world); repository.overrideLoad.mapRevision = 999;
            Throws<InvalidDataException>(() => session.Load(true, out backup), "invalid snapshot rejects Load");
            Check(ReferenceEquals(kept, session.World) && ReferenceEquals(keptReceipt, session.LastCityCompletion)
                && Hash(session.World) == keptHash, "invalid snapshot preserves old session and receipt");
            repository.overrideLoad = null;
            Throws<ArgumentOutOfRangeException>(() => session.StartYouzhou(99, "Invalid"), "invalid new start rejects");
            Check(ReferenceEquals(kept, session.World) && ReferenceEquals(keptReceipt, session.LastCityCompletion), "failed start preserves old completion");
            session.StartYouzhou(0, "New"); Check(session.LastCityCompletion == null, "new Youzhou start clears earlier completion");
            Start(session.World, CityBuilding.Mill); session.Tick(session.World.CityJob.remaining);
            session.StartLegacy(Homeland.Han, 4, "Legacy"); session.Tick(100);
            Check(session.LastCityCompletion == null && !session.World.IsYouzhou, "legacy start clears completion and legacy Tick cannot create one");

            repository = new MemoryRepository(); repository.Seed(world); var originalStored = repository.Snapshot();
            var readOnly = new CampaignSession(repository, () => Now, true);
            readOnly.Load(true, out backup); var readOnlyReceipt = readOnly.LastCityCompletion; string readOnlyHash = Hash(readOnly.World);
            Receipt(readOnlyReceipt, index, CityBuilding.Mill, 0, 1, true, "read-only review offline completion");
            readOnly.Save(); readOnly.ClearCityCompletion(readOnlyReceipt); readOnly.Save();
            Check(repository.saves == 0 && Hash(readOnly.World) == readOnlyHash, "read-only Save and dismissal preserve full World");
            Same(originalStored, repository.Snapshot(), "review never changes memory repository save");
            string problem; Check(readOnly.World.IsValid(out problem), "settled campaign remains valid " + problem);
            Console.WriteLine("PASS: " + checks + " city construction completion checks; isolated memory repositories only.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
