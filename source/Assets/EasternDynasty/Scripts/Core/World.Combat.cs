using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public partial class World
    {
        // Existing project recruitment tuning; shared by previews and the actual payment.
        public static Supplies RecruitmentCost(int amount)
        {
            amount = Math.Max(0, amount);
            return new Supplies { grain = amount, grass = amount * .4, iron = amount * .1 };
        }
        public string Recruit(int amount)
        {
            if (order != null) return "部队行军期间不能补兵。";
            amount = Math.Min(Math.Max(0, amount), Math.Max(0, TroopCap - troops));
            if (amount == 0) return "兵力已达上限，培养武将可提高带兵量。";
            var cost = RecruitmentCost(amount);
            if (!resources.Spend(cost.grain, cost.grass, cost.stone, cost.iron, cost.gold)) return "征兵资源不足。";
            troops += amount; RecordCityAction("recruit"); return "已征募 " + amount + " 名士卒。";
        }
        public string AttackProblem(int tileId)
        {
            if (tileId < 0 || tileId >= lands.Count) return "目标不存在。";
            var tile = lands[tileId];
            if (order != null) return "部队正在行军或返城。";
            if (!tile.Walkable) return "山河不可直接占领，请寻找通路。";
            if (tile.owned) return "此地已属于你。";
            if (troops < 50 || ActiveGenerals.Count == 0) return "请先配置武将并征兵。";
            if (!HasAdjacentOwnedLand(tileId)) return "需要先占领与目标相邻的土地。";
            if (resources.grain < 30 || resources.grass < 20) return "出征需要 30 粮食、20 草料。";
            if (IsYouzhou && FindMarchPath(homeId,tileId)==null) return "没有可通行的陆路，请寻找桥梁。";
            return "";
        }
        private bool OwnedAt(int x, int z) { var tile = TryGetLand(x, z); return tile != null && tile.owned; }
        public string March(int tileId)
        { return March(tileId, false); }
        public string March(int tileId, bool watchOnArrival)
        {
            string problem = AttackProblem(tileId); if (problem.Length > 0) return problem;
            resources.Spend(30, 20, 0, 0, 0);
            var target = lands[tileId]; var home = lands[homeId];
            double duration = 8 + Math.Sqrt(Math.Pow(target.x - home.x, 2) + Math.Pow(target.z - home.z, 2)) * 4;
            order = new ArmyOrder { target = tileId, remaining = duration, duration = duration };
            if(IsYouzhou) order=CreateMovement(tileId,ArmyAction.Attack,FindMarchPath(homeId,tileId));
            order.battleId = Guid.NewGuid().ToString("N");
            order.watchOnArrival = watchOnArrival;
            return "部队出发，目标：" + target.name + "（" + target.x + "," + target.z + "）。";
        }
        public int Defenders(Land tile) { return (int)(new[] { 0, 130, 300, 650, 1100, 1800 }[Math.Max(1, Math.Min(5, tile.level))] * (tile.capital ? 2.2 : 1)); }
        private int SkillCount(string skill)
        { return ActiveGenerals.Sum(c => (c.Definition.skill == skill ? 1 : 0) + (c.learnedSkill == skill ? 1 : 0)); }
        private void ResolveBattle(Land tile)
        {
            int initial = troops, enemy = Defenders(tile), enemyInitial = enemy;
            var active = ActiveGenerals;
            var record = new BattleRecord
            {
                replayVersion = 1, battleId = order != null && !string.IsNullOrEmpty(order.battleId)
                    ? order.battleId : Guid.NewGuid().ToString("N"), ruleVersion = BattleReplay.RuleVersion,
                time = elapsed, targetId = tile.id, targetName = tile.name, targetX = tile.x, targetZ = tile.z,
                attacker = new BattleArmySnapshot { name = "我方部队", troops = initial,
                    generals = active.Select(c => new BattleParticipantSnapshot { uid = c.uid, definitionId = c.definition,
                        name = c.Definition.name, level = c.level, rank = c.rank, attack = c.Definition.attack,
                        skill = c.Definition.skill, learnedSkill = c.learnedSkill }).ToList() },
                defender = new BattleArmySnapshot { name = tile.name + "守军", troops = enemyInitial },
                events = new List<BattleReplayEvent>()
            };
            Action<BattleReplayEventKind, int, int, string> recordEvent = (kind, round, amount, message) =>
                record.events.Add(new BattleReplayEvent { sequence = record.events.Count, kind = kind, round = round,
                    amount = amount, attackerTroops = troops, defenderTroops = enemy, text = message });
            recordEvent(BattleReplayEventKind.Start, 0, 0, "交战开始 · 项目整队战斗规则；此处播放结算时记录的过程。");
            double force = active.Count == 0 ? 0 : active.Average(c => c.Definition.attack + c.level * 4 + c.rank * 9);
            var rounds = new List<string>();
            int completedRounds = 0;
            for (int round = 1; round <= 8 && troops > 0 && enemy > 0; round++)
            {
                int hit = Math.Max(1, (int)((troops * .18 + force) * (1 + SkillCount("突击") * .12)));
                int loss = Math.Max(1, (int)((enemy * .12 + tile.level * 12) * Math.Max(.5, 1 - SkillCount("坚守") * .08)));
                int inflicted = Math.Min(enemy, hit), suffered = Math.Min(troops, loss);
                enemy -= inflicted;
                recordEvent(BattleReplayEventKind.Attack, round, inflicted, "第" + round + "回合 · 我军整队造成 " + inflicted + " 伤害");
                troops -= suffered;
                recordEvent(BattleReplayEventKind.Loss, round, suffered, "第" + round + "回合 · 我军整队损失 " + suffered);
                int restored = Math.Min(initial - troops, SkillCount("抚军") * 9);
                if (troops > 0) troops += restored; else restored = 0;
                if (restored > 0) recordEvent(BattleReplayEventKind.Recovery, round, restored, "第" + round + "回合 · 抚军整队恢复 " + restored);
                rounds.Add("第" + round + "回合：击退 " + inflicted + "，损失 " + suffered + (restored > 0 ? "，恢复 " + restored : ""));
                completedRounds = round;
            }
            bool won = enemy <= 0 && troops > 0;
            if (won)
            {
                tile.owned = true;
                RecordCityAction("occupy");
                resources.gold += tile.level * 35;
                record.goldReward = tile.level * 35;
                if (tile.capital && tile.homeland != origin && !allPoolsUnlocked)
                { allPoolsUnlocked = true; Note("跨区立足：占领" + tile.name + "，已开放全部出生区域的招募卡池。"); }
            }
            record.experienceReward = tile.level * (won ? (tile.capital ? 240 : 100) : 25);
            for (int i = 0; i < active.Count; i++)
            {
                var card = active[i];
                int before = card.level * (card.level - 1) * 40 + card.experience;
                card.GainExperience(record.experienceReward);
                record.attacker.generals[i].awardedExperience = card.level * (card.level - 1) * 40 + card.experience - before;
            }
            string verdict = won ? "攻占成功" : (troops == 0 ? "兵败撤回" : "八回合未分胜负，撤回整备");
            record.won = won; record.captured = won; record.attackerFinal = troops; record.defenderFinal = enemy;
            record.title = tile.name + " · " + verdict;
            record.text = "目标：" + RegionName(tile.region) + "（" + tile.x + "," + tile.z + "）\n我军：" + initial + " → " + troops + "  守军：" + enemyInitial + " → " + Math.Max(0, enemy) + "\n\n" + string.Join("\n", rounds.ToArray()) + "\n\n" + (won ? (tile.bridge ? "已控制桥梁通路并获得武将经验；桥梁不产出资源。" : "已获得领地收益与武将经验。") : "部队正在返城，可补兵、培养武将后再次出征。");
            recordEvent(BattleReplayEventKind.Result, completedRounds, 0, verdict + " · 铜钱 +" + record.goldReward
                + " · 武将实得经验合计 +" + record.attacker.generals.Sum(g => g.awardedExperience));
            reports.Insert(0, record); if (reports.Count > 30) reports.RemoveAt(reports.Count - 1);
            Note(record.title);
        }
    }
}
