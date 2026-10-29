using System;
using System.IO;

namespace EasternDynasty
{
    /// <summary>Owns the active campaign and application lifecycle, without Unity or filesystem dependencies.</summary>
    public sealed class CampaignSession
    {
        public const double MaximumOfflineSeconds = 7 * 86400;
        private readonly ICampaignRepository repository;
        private readonly Func<DateTime> utcNow;
        private readonly bool readOnly;

        public World World { get; private set; }
        public CityConstructionCompletion LastCityCompletion { get; private set; }

        public CampaignSession(ICampaignRepository repository, Func<DateTime> utcNow, bool readOnly = false)
        {
            if (repository == null) throw new ArgumentNullException("repository");
            if (utcNow == null) throw new ArgumentNullException("utcNow");
            this.repository = repository; this.utcNow = utcNow; this.readOnly = readOnly;
        }

        public World StartYouzhou(int startIndex, string ruler)
        {
            var created = EasternDynasty.World.CreateYouzhou(startIndex, ruler);
            World = created;
            LastCityCompletion = null;
            return created;
        }

        public World StartLegacy(Homeland origin, int region, string ruler)
        {
            var created = EasternDynasty.World.Create(origin, region, ruler);
            World = created;
            LastCityCompletion = null;
            return created;
        }

        public World Load(bool youzhou, out bool restoredBackup)
        {
            restoredBackup = false;
            bool usedBackup;
            var loaded = repository.Load(youzhou, out usedBackup);
            RequireValid(loaded, youzhou);
            long nowTicks = UtcNow().Ticks;
            CityConstructionCompletion completion = null;
            if (loaded.savedUtcTicks > 0)
            {
                // Both operands are nonnegative; subtract ticks before converting to retain subsecond precision.
                double seconds = (nowTicks - loaded.savedUtcTicks) / (double)TimeSpan.TicksPerSecond;
                completion = Advance(loaded, Math.Max(0, Math.Min(MaximumOfflineSeconds, seconds)), true);
            }
            World = loaded;
            LastCityCompletion = completion;
            restoredBackup = usedBackup;
            return loaded;
        }

        public void Save()
        {
            if (World == null || readOnly) return;
            RequireValid(World, World.IsYouzhou);
            long previous = World.savedUtcTicks;
            World.savedUtcTicks = UtcNow().Ticks;
            try { repository.Save(World); }
            catch { World.savedUtcTicks = previous; throw; }
        }

        public void Tick(double seconds)
        {
            if (World == null) return;
            var completion = Advance(World, seconds, false);
            if (completion != null) LastCityCompletion = completion;
        }

        // Dismissing an older page must never erase a newer construction receipt.
        public void ClearCityCompletion(CityConstructionCompletion expected)
        {
            if (object.ReferenceEquals(LastCityCompletion, expected)) LastCityCompletion = null;
        }

        private static CityConstructionCompletion Advance(World world, double seconds, bool completedWhileAway)
        {
            var job = world.CityJob;
            var plot = job == null || world.cityPlots == null || job.plotIndex < 0 || job.plotIndex >= world.cityPlots.Count
                ? null : world.cityPlots[job.plotIndex];
            int previousLevel = plot == null ? 0 : plot.level;
            world.Tick(seconds);
            // Core owns completion, payment and the facility effect. Observe only a job
            // that actually finished in this Tick; neither cancellation nor loaded levels qualify.
            if (job == null || plot == null || world.cityConstruction.Contains(job) || job.remaining > .000001
                || plot.building != job.building || plot.level != job.targetLevel || plot.level != previousLevel + 1) return null;
            return new CityConstructionCompletion(job.plotIndex, job.building, previousLevel, job.targetLevel, completedWhileAway);
        }
        public bool HasSave(bool youzhou) { return repository.Exists(youzhou); }

        private DateTime UtcNow()
        {
            DateTime now = utcNow();
            return now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
        }

        private static void RequireValid(World world, bool youzhou)
        {
            if (world == null) throw new InvalidDataException("存档为空。");
            string problem;
            if (!world.IsValid(out problem)) throw new InvalidDataException(problem);
            if (world.IsYouzhou != youzhou) throw new InvalidDataException("存档地图与选择的征程不一致。");
        }
    }
}
