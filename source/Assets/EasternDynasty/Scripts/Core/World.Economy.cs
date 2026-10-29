using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public partial class World
    {
        public int OwnedCount { get { return lands.Count(t => t.owned && !t.home && !IsCityDistrict(t.id)); } }
        public int OwnedCapitals { get { return lands.Count(t => t.owned && t.capital); } }
        public Supplies HourlyIncome()
        {
            var income = new Supplies { grain = 360, grass = 240, stone = 180, iron = 90, gold = 120 };
            // Public Land fields are intentionally still editable: calculate one pass, with no stale cache.
            int capitals = 0;
            foreach (var tile in lands)
            {
                if (!tile.owned) continue;
                if (tile.capital) { capitals++; continue; }
                if (tile.home || tile.bridge || IsCityDistrict(tile.id)) continue;
                int amount = tile.level * 180;
                switch (tile.kind) { case LandKind.Grain: income.grain += amount; break; case LandKind.Pasture: income.grass += amount; break; case LandKind.Stone: income.stone += amount; break; case LandKind.Iron: income.iron += amount; break; }
            }
            var facilities = CityHourlyIncome();
            income.grain += facilities.grain; income.grass += facilities.grass; income.stone += facilities.stone;
            income.iron += facilities.iron; income.gold += facilities.gold;
            double bonus = 1 + capitals * .15;
            income.grain *= bonus; income.grass *= bonus; income.stone *= bonus; income.iron *= bonus; income.gold *= bonus;
            return income;
        }
        public void Tick(double seconds)
        {
            if (seconds <= 0 || double.IsNaN(seconds) || double.IsInfinity(seconds)) return;
            EnsureSeasonState();
            EnsureCityState();
            while (seconds > .000001)
            {
                bool timed = order != null && order.phase != (int)ArmyPhase.Stationed;
                double step = !timed ? seconds : Math.Min(seconds, Math.Max(0, order.remaining));
                if (CityJob != null) step = Math.Min(step, Math.Max(0, CityJob.remaining));
                var income = HourlyIncome();
                resources.grain += income.grain * step / 3600; resources.grass += income.grass * step / 3600;
                resources.stone += income.stone * step / 3600; resources.iron += income.iron * step / 3600; resources.gold += income.gold * step / 3600;
                elapsed += step; seconds -= step;
                AdvanceCityConstruction(step);
                if(IsYouzhou)
                {
                    if(commands>=CommandCap)commandRecovery=0;
                    else {commandRecovery+=step;int gained=(int)(commandRecovery/CommandRecoverySeconds);commands=Math.Min(CommandCap,commands+gained);commandRecovery=commands==CommandCap?0:commandRecovery%CommandRecoverySeconds;}
                }
                if (timed)
                {
                    order.remaining = Math.Max(0, order.remaining - step);
                    if (order.remaining <= .000001)
                    {
                        CompleteArmyPhase();
                    }
                }
            }
        }
    }
}
