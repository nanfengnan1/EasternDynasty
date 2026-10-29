using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public partial class World
    {
        public GeneralCard AddCard(string definition)
        {
            if (!Catalog.Generals.Exists(g => g.id == definition)) throw new ArgumentException("未知武将。");
            var card = new GeneralCard { uid = nextCardId++, definition = definition };
            cards.Add(card); return card;
        }
        public List<GeneralCard> ActiveGenerals { get { return formation.Select(id => cards.Find(c => c.uid == id)).Where(c => c != null).ToList(); } }
        private bool IsDeployedGeneral(int uid)
        { return order != null && (formation.Contains(uid) || order.trainingParticipants != null && order.trainingParticipants.Contains(uid)); }
        public int TroopCap { get { return 400 + ActiveGenerals.Sum(c => c.level * 70 + c.rank * 120) + CityTroopBonus; } }
        public string Draw(bool premium, Random rng)
        {
            int price = premium ? 100 : 200;
            if (premium ? resources.jade < price : resources.gold < price) return premium ? "灵玉不足。" : "金币不足。";
            int roll = rng.Next(100);
            int rarity = premium ? (roll < 60 ? 3 : roll < 90 ? 4 : 5) : (roll < 55 ? 1 : roll < 90 ? 2 : 3);
            var pool = Catalog.Generals.Where(g => !g.s1 && (allPoolsUnlocked || g.homeland == origin) && g.stars == rarity).ToList();
            if (pool.Count == 0) return "该星级卡池尚未配置。";
            if (premium) resources.jade -= price; else resources.gold -= price;
            var selected = pool[rng.Next(pool.Count)]; AddCard(selected.id);
            return "招募成功：" + selected.name + " · " + rarity + "星";
        }
        public string Assign(int uid, int slot)
        {
            if (order != null) return "请等待部队返回主城。";
            if (slot < 0 || slot >= 3) return "编队位置无效。";
            var card = cards.Find(c => c.uid == uid);
            if (card == null) return "武将不存在。";
            if (formation.Contains(uid)) return "该武将已经上阵。";
            if (ActiveGenerals.Where((c, i) => i != slot).Any(c => c.definition == card.definition || (card.Definition.s1 && c.Definition.s1 && c.Definition.name == card.Definition.name))) return "同名武将不能重复上阵。";
            if (slot < formation.Count) formation[slot] = uid; else if (slot == formation.Count) formation.Add(uid); else return "请先填满前一位置。";
            troops = Math.Min(troops, TroopCap); return card.Definition.name + "已上阵。";
        }
        public string Train(int uid)
        {
            var card = cards.Find(c => c.uid == uid);
            if (card == null) return "武将不存在。";
            if (IsDeployedGeneral(uid)) return "出征中的武将不能演武。";
            if (card.level >= card.LevelCap) return "已达到星级对应的等级上限。";
            if (!resources.Spend(200, 0, 0, 0, 60)) return "演武需要 200 粮食、60 金币。";
            card.GainExperience(240); return card.Definition.name + "获得 240 经验。";
        }
        public string Promote(int uid)
        {
            var card = cards.Find(c => c.uid == uid);
            if (card == null) return "武将不存在。";
            if (IsDeployedGeneral(uid)) return "请等待部队返回主城。";
            if (card.rank >= card.Definition.stars) return "已达到进阶上限。";
            var material = cards.Where(c => c.uid != uid && c.definition == card.definition && c.rank == 0 && !formation.Contains(c.uid) && !IsDeployedGeneral(c.uid)).OrderBy(c => c.level).FirstOrDefault();
            if (material == null) return "需要一张未上阵、未进阶的同名同星武将。";
            cards.Remove(material); card.rank++; return card.Definition.name + "进阶至 " + card.rank + " 阶。";
        }
        public string Dismantle(int uid)
        {
            var card = cards.Find(c => c.uid == uid);
            if (card == null) return "武将不存在。";
            if (card.Definition.s1) return "此武将的拆解战法尚未完成规则实现，暂不消耗卡片。";
            if (IsDeployedGeneral(uid)) return "请等待部队返回主城。";
            if (formation.Contains(uid)) return "上阵武将不能拆解，请先换下。";
            if (learnedSkills.Contains(card.Definition.skill)) return "此技能已经解锁，无需再次拆解。";
            learnedSkills.Add(card.Definition.skill); cards.Remove(card);
            return "已消耗" + card.Definition.name + "，解锁技能「" + card.Definition.skill + "」。";
        }
        public string Teach(int uid, string skill)
        {
            var card = cards.Find(c => c.uid == uid);
            if (card == null || !learnedSkills.Contains(skill)) return "尚未解锁此技能。";
            if (IsDeployedGeneral(uid)) return "请等待部队返回主城。";
            if (card.learnedSkill == skill || card.Definition.skill == skill) return "该武将已掌握此技能。";
            if (!resources.Spend(0, 0, 0, 0, 100)) return "学习技能需要 100 金币。";
            card.learnedSkill = skill; return card.Definition.name + "习得「" + skill + "」。";
        }
        public List<GeneralDefinition> S1Pool(string faction, bool includeArchives=false)
        { return Catalog.Generals.Where(g=>g.s1 && (includeArchives||g.recruitable) && (faction=="全部"||g.faction==faction)).OrderByDescending(g=>g.stars).ThenBy(g=>g.name).ToList(); }
        public string DrawS1(bool premium, string faction, Random random)
        {
            if(!IsYouzhou)return "S1 招募在幽州开放。";
            if(random==null)throw new ArgumentNullException("random");
            if(!Catalog.S1Factions.Contains(faction))return "阵营无效。";
            int price=premium?100:200;
            if(premium?resources.jade<price:resources.gold<price)return premium?"灵玉不足。":"金币不足。";
            var pool=S1Pool(faction).Where(g=>premium||g.stars==4).ToList();
            if(pool.Count==0)return "此阵营暂无符合条件的已取证招募武将。";
            // Equal per-card prototype sampling. Never claim the original game's commercial probabilities.
            var selected=pool[random.Next(pool.Count)];
            if(premium)resources.jade-=price;else resources.gold-=price;AddCard(selected.id);
            return "招募成功："+selected.name+" · "+selected.faction+" · "+selected.stars+"星";
        }
    }
}
