using System;
using System.Linq;

namespace EasternDynasty
{
    // Transient confirmation only. Neither the plan nor its receipt belongs to a save.
    public sealed class ReinforcementPlan
    {
        public readonly int requested, amount, beforeTroops, capacity, affordable;
        public readonly Supplies cost;
        public readonly string problem;
        private readonly World campaign;
        private readonly int[] formation, participants;
        private readonly bool wasBusy;
        private bool executed, expired;
        public bool CanExecute { get { return !executed && !expired && problem.Length == 0; } }

        private ReinforcementPlan(World world, int requested)
        {
            campaign = world; this.requested = requested;
            if (world == null)
            {
                cost = World.RecruitmentCost(0); formation = participants = new int[0];
                problem = "请先进入征程。"; return;
            }
            beforeTroops = world.troops; capacity = world.TroopCap;
            formation = world.formation.ToArray(); participants = world.ActiveGenerals.Select(c => c.uid).ToArray();
            wasBusy = world.order != null;
            int missing = Math.Max(0, capacity - beforeTroops);
            amount = Math.Min(Math.Max(0, requested), missing);
            cost = World.RecruitmentCost(amount);
            affordable = Affordable(world.resources, missing);
            problem = wasBusy ? "部队执行军令期间不能征兵，请等待返回主城。"
                : missing == 0 ? "兵力已达上限，培养武将或升级兵营可提高带兵量。"
                : requested <= 0 ? "请选择大于零的征兵人数。"
                : !CanPay(world.resources, amount) ? "征兵资源不足，可减少人数或等待资源恢复。" : "";
        }

        public static ReinforcementPlan Create(World world, int requested)
        { return new ReinforcementPlan(world, requested); }

        public bool TryExecute(World world, out ReinforcementReceipt receipt, out string message)
        {
            receipt = null;
            if (executed) { message = "本次征兵已完成，请重新选择人数。"; return false; }
            if (expired) { message = "征兵方案已过期，请重新确认。"; return false; }
            if (world == null || !ReferenceEquals(world, campaign))
            { expired = true; message = "征程已变化，请重新确认征兵。"; return false; }
            if (world.troops != beforeTroops || world.TroopCap != capacity
                || (world.order != null) != wasBusy || !world.formation.SequenceEqual(formation)
                || !world.ActiveGenerals.Select(c => c.uid).SequenceEqual(participants))
            { expired = true; message = "部队状态已变化，请重新确认征兵人数与费用。"; return false; }
            if (requested <= 0 || amount <= 0 || wasBusy)
            { message = problem; return false; }
            if (!CanPay(world.resources, amount))
            { message = "征兵资源不足，可减少人数或等待资源恢复。"; return false; }

            var before = Copy(world.resources);
            message = world.Recruit(amount);
            if (world.troops - beforeTroops != amount) return false;
            executed = true;
            receipt = new ReinforcementReceipt(beforeTroops, world.troops, world.TroopCap,
                new Supplies { grain = before.grain - world.resources.grain,
                    grass = before.grass - world.resources.grass, stone = before.stone - world.resources.stone,
                    iron = before.iron - world.resources.iron, gold = before.gold - world.resources.gold,
                    jade = before.jade - world.resources.jade });
            return true;
        }

        private static int Affordable(Supplies balance, int limit)
        {
            // Spend owns the established floating-point tolerance. Probe a copy so
            // preview and command agree even immediately below a fractional cost.
            int low = 0, high = limit;
            while (low < high)
            {
                int middle = low + (high - low) / 2 + 1;
                if (CanPay(balance, middle)) low = middle; else high = middle - 1;
            }
            return low;
        }

        private static bool CanPay(Supplies balance, int count)
        {
            if (balance == null) return false;
            var quote = World.RecruitmentCost(count);
            return Copy(balance).Spend(quote.grain, quote.grass, quote.stone, quote.iron, quote.gold);
        }

        private static Supplies Copy(Supplies value)
        { return new Supplies { grain = value.grain, grass = value.grass, stone = value.stone,
            iron = value.iron, gold = value.gold, jade = value.jade }; }
    }

    public sealed class ReinforcementReceipt
    {
        public readonly int beforeTroops, afterTroops, amount, capacity;
        public readonly Supplies cost;
        internal ReinforcementReceipt(int before, int after, int limit, Supplies paid)
        { beforeTroops = before; afterTroops = after; amount = after - before; capacity = limit; cost = paid; }
    }
}
