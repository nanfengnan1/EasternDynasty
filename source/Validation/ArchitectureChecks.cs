using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using EasternDynasty;

public static class ArchitectureChecks
{
    private static int checks;
    private static readonly DateTime Now = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
    private static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); } catch (T) { checks++; return; }
        throw new Exception(message);
    }
    private static World Fresh(bool youzhou = true)
    { return youzhou ? World.CreateYouzhou(0, "Architecture") : World.Create(Homeland.Han, 4, "Architecture"); }
    private static byte[] Encode(World world)
    {
        using (var stream = new MemoryStream())
        { new DataContractJsonSerializer(typeof(World)).WriteObject(stream, world); return stream.ToArray(); }
    }
    private static World Decode(byte[] data)
    {
        using (var stream = new MemoryStream(data))
            return (World)new DataContractJsonSerializer(typeof(World)).ReadObject(stream);
    }
    private static void Equivalent(World actual, World expected, string context)
    {
        Check(Math.Abs(actual.resources.grain - expected.resources.grain) < .000001
            && Math.Abs(actual.resources.grass - expected.resources.grass) < .000001
            && Math.Abs(actual.resources.gold - expected.resources.gold) < .000001, context + " resource settlement mismatch");
        Check(Math.Abs(actual.elapsed - expected.elapsed) < .000001 && actual.troops == expected.troops
            && actual.OwnedCount == expected.OwnedCount && actual.reports.Count == expected.reports.Count, context + " progression mismatch");
        Check((actual.order == null) == (expected.order == null), context + " order state mismatch");
        if (expected.order != null)
            Check(actual.order.target == expected.order.target && actual.order.returning == expected.order.returning
                && Math.Abs(actual.order.remaining - expected.order.remaining) < .000001, context + " order timing mismatch");
    }

    private sealed class MemoryRepository : ICampaignRepository
    {
        private readonly Dictionary<bool, byte[]> slots = new Dictionary<bool, byte[]>();
        public int Saves, Loads, ExistenceQueries;
        public bool FailSave, FailLoad, Backup;
        public World OverrideLoad;
        public void Seed(World world) { slots[world.IsYouzhou] = Encode(world); }
        public World Snapshot(bool youzhou) { return Decode(slots[youzhou]); }
        public bool Exists(bool youzhou) { ExistenceQueries++; return slots.ContainsKey(youzhou); }
        public World Load(bool youzhou, out bool restoredBackup)
        {
            Loads++; restoredBackup = false;
            if (FailLoad) throw new IOException("Injected read failure");
            restoredBackup = Backup;
            if (OverrideLoad != null) return OverrideLoad;
            if (!slots.ContainsKey(youzhou)) throw new FileNotFoundException("No memory save");
            return Snapshot(youzhou);
        }
        public void Save(World world)
        {
            if (FailSave) throw new IOException("Injected write failure");
            slots[world.IsYouzhou] = Encode(world); Saves++;
        }
    }

    private static void CheckSerializedFields()
    {
        string[] worldFields = { "saveVersion", "mapId", "mapWidth", "mapHeight", "mapRevision", "ruler", "origin", "birthRegion", "homeId", "troops", "nextCardId", "allPoolsUnlocked", "elapsed", "savedUtcTicks", "resources", "lands", "cards", "formation", "learnedSkills", "reports", "messages", "orders", "seasonMechanicsVersion", "commands", "commandRecovery" };
        string[] cityFields = { "cityVersion", "cityPlots", "cityConstruction", "cityClaimedQuests", "cityRecruitActions", "cityFarmCompletions", "cityGarrisonActions", "cityOccupyActions", "cityVisited", "cityTrainingCompletions", "trainingHistory" };
        worldFields=worldFields.Concat(cityFields).ToArray();
        Check(typeof(World).GetFields(BindingFlags.Instance | BindingFlags.Public).Select(f => f.Name).OrderBy(n => n)
            .SequenceEqual(worldFields.OrderBy(n => n)), "World serialized field surface changed");
        foreach(string field in new[]{"seasonMechanicsVersion","commands","commandRecovery"}.Concat(cityFields))
            Check(typeof(World).GetField(field).IsDefined(typeof(System.Runtime.Serialization.OptionalFieldAttribute),false),"S1 field must remain optional for prior saves: "+field);
        Check(typeof(World).IsSerializable && typeof(Land).IsSerializable && typeof(Supplies).IsSerializable
            && typeof(ArmyOrder).IsSerializable && typeof(BattleRecord).IsSerializable && typeof(GeneralCard).IsSerializable, "Serializable model attributes changed");
        Check(typeof(World).FullName == "EasternDynasty.World" && typeof(Land).FullName == "EasternDynasty.Land", "Model namespace changed");
        Check(typeof(World).GetField("orders").FieldType == typeof(List<ArmyOrder>)
            && typeof(World).GetField("savedUtcTicks").FieldType == typeof(long)
            && typeof(World).GetField("lands").FieldType == typeof(List<Land>)
            && typeof(Land).GetField("bridge").FieldType == typeof(bool), "Persisted field types changed");
    }

    public static int Main()
    {
        try
        {
            CheckSerializedFields();
            Throws<ArgumentNullException>(() => new CampaignSession(null, () => Now), "Null repository accepted");
            Throws<ArgumentNullException>(() => new CampaignSession(new MemoryRepository(), null), "Null clock accepted");
            {
                var repository = new MemoryRepository(); int clockReads = 0;
                var session = new CampaignSession(repository, () => { clockReads++; return Now; });
                Check(session.World == null, "Session created a world implicitly");
                session.Tick(10); session.Save();
                Check(repository.Saves == 0 && clockReads == 0, "Empty session save had side effects");
                Check(!session.HasSave(true) && !session.HasSave(false) && repository.ExistenceQueries == 2, "Save existence routing failed");
                World youzhou = session.StartYouzhou(2, "幽州");
                Check(object.ReferenceEquals(session.World, youzhou) && youzhou.IsYouzhou && youzhou.birthRegion == 2, "Youzhou start contract failed");
                Check(repository.Saves == 0 && youzhou.savedUtcTicks == 0, "Start auto-saved");
                session.Save();
                Check(repository.Saves == 1 && youzhou.savedUtcTicks == Now.Ticks && repository.Snapshot(true).savedUtcTicks == Now.Ticks, "Save timestamp mismatch");
                Check(session.HasSave(true) && !session.HasSave(false), "Slot isolation failed");
                World legacy = session.StartLegacy(Homeland.Steppe, 9, "Legacy");
                Check(!legacy.IsYouzhou && legacy.origin == Homeland.Steppe && object.ReferenceEquals(session.World, legacy), "Legacy start contract failed");
                session.Save();
                Check(session.HasSave(false) && session.HasSave(true) && repository.Snapshot(true).ruler == "幽州", "Saving legacy overwrote Youzhou");
                Throws<ArgumentOutOfRangeException>(() => session.StartYouzhou(99, "Bad"), "Invalid start accepted");
                Check(object.ReferenceEquals(session.World, legacy), "Failed start replaced current World");
                Throws<ArgumentException>(() => session.StartLegacy(Homeland.Han, 12, "Bad"), "Invalid legacy start accepted");
                Check(object.ReferenceEquals(session.World, legacy), "Failed legacy start replaced current World");
            }
            foreach (bool youzhou in new[] { false, true })
            {
                var original = Fresh(youzhou); original.savedUtcTicks = Now.AddHours(-2).Ticks;
                var expected = Decode(Encode(original)); expected.Tick(7200);
                var repository = new MemoryRepository(); repository.Seed(original);
                var session = new CampaignSession(repository, () => Now); bool restored;
                World loaded = session.Load(youzhou, out restored);
                Check(!restored && object.ReferenceEquals(loaded, session.World) && loaded.IsYouzhou == youzhou, "Load result/slot mismatch");
                Equivalent(loaded, expected, "Two hour load");
                Check(repository.Saves == 0 && repository.Snapshot(youzhou).elapsed == original.elapsed, "Load wrote or mutated stored snapshot");
                World repeated = session.Load(youzhou, out restored);
                Equivalent(repeated, expected, "Repeated load");
                Check(!object.ReferenceEquals(loaded, repeated), "Repository did not return an independent snapshot");
            }
            {
                var old = Fresh(); old.savedUtcTicks = Now.AddDays(-10).Ticks;
                var expected = Decode(Encode(old)); expected.Tick(7 * 86400);
                var repository = new MemoryRepository(); repository.Seed(old);
                var session = new CampaignSession(repository, () => Now); bool restored;
                Equivalent(session.Load(true, out restored), expected, "Offline seven-day cap");
                foreach (long timestamp in new[] { 0L, -1L, Now.AddDays(2).Ticks, long.MaxValue })
                {
                    var noOffline = Fresh(); noOffline.savedUtcTicks = timestamp; repository.Seed(noOffline);
                    Check(session.Load(true, out restored).elapsed == 0, "Missing/future timestamp advanced time");
                }
                var precise = Fresh(); precise.savedUtcTicks = Now.AddTicks(-1234567).Ticks; repository.Seed(precise);
                Check(Math.Abs(session.Load(true, out restored).elapsed - .1234567) < .00000001, "Subsecond offline settlement lost precision");
            }
            {
                var marching = Fresh(); int target = marching.lands.First(t => marching.AttackProblem(t.id).Length == 0).id;
                marching.March(target); marching.Tick(3); marching.savedUtcTicks = Now.AddMinutes(-5).Ticks;
                var expected = Decode(Encode(marching)); expected.Tick(300);
                var repository = new MemoryRepository(); repository.Seed(marching); repository.Backup = true;
                var session = new CampaignSession(repository, () => Now); bool restored;
                World loaded = session.Load(true, out restored);
                Check(restored, "Backup indicator was lost");
                Equivalent(loaded, expected, "Offline capture and return");
                Check(loaded.order == null && loaded.lands[target].owned && loaded.reports.Count == 1, "Offline battle not resolved exactly once");
                var tickExpected = Decode(Encode(loaded)); tickExpected.Tick(123.5); session.Tick(123.5);
                Equivalent(session.World, tickExpected, "Session Tick");
                session.Tick(double.NaN); session.Tick(-1);
                Check(session.World.elapsed == tickExpected.elapsed, "Invalid Tick changed campaign");
            }
            {
                var repository = new MemoryRepository(); int clockReads = 0;
                var readOnly = new CampaignSession(repository, () => { clockReads++; return Now; }, true);
                var world = readOnly.StartYouzhou(0, "Review"); world.savedUtcTicks = 123;
                readOnly.Save();
                Check(repository.Saves == 0 && clockReads == 0 && world.savedUtcTicks == 123, "Read-only Save was not a no-op");
                readOnly.Tick(10);
                Check(world.elapsed == 10, "Read-only preview could not simulate");
                var saved = Fresh(); saved.savedUtcTicks = Now.AddHours(-1).Ticks; repository.Seed(saved);
                bool backup; readOnly.Load(true, out backup); readOnly.Save();
                Check(repository.Saves == 0 && readOnly.World.elapsed == 3600, "Read-only load/save contract failed");
            }
            {
                var repository = new MemoryRepository(); var session = new CampaignSession(repository, () => Now);
                var active = session.StartYouzhou(0, "Keep"); active.savedUtcTicks = 42; repository.FailSave = true;
                Throws<IOException>(() => session.Save(), "Write failure was swallowed");
                Check(active.savedUtcTicks == 42 && repository.Saves == 0, "Failed write retained a false saved timestamp");
                repository.FailSave = false; active.mapWidth = 128;
                Throws<InvalidDataException>(() => session.Save(), "Invalid World was saved");
                Check(active.savedUtcTicks == 42 && repository.Saves == 0, "Invalid save changed timestamp");
                active.mapWidth = 40;
                repository.OverrideLoad = Fresh(false); bool restored;
                Throws<InvalidDataException>(() => session.Load(true, out restored), "Wrong map slot accepted from repository");
                Check(object.ReferenceEquals(session.World, active), "Wrong-slot load replaced active world");
                repository.OverrideLoad = Fresh(); repository.OverrideLoad.mapRevision = 999;
                Throws<InvalidDataException>(() => session.Load(true, out restored), "Invalid repository snapshot accepted");
                Check(object.ReferenceEquals(session.World, active), "Invalid load replaced active world");
                repository.OverrideLoad = null; repository.FailLoad = true;
                Throws<IOException>(() => session.Load(true, out restored), "Repository read failure was swallowed");
                Check(object.ReferenceEquals(session.World, active), "Failed read replaced active world");
            }
            Console.WriteLine("PASS: " + checks + " architecture checks; serialized surface, session lifecycle, independent slots, fixed-clock offline cap, battle/return settlement, read-only behavior, validation and failure rollback. No filesystem repository or Unity process was used.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("FAIL: " + error); return 1; }
    }
}
