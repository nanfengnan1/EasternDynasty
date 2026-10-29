using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public class GeneralDefinition
    {
        public string id, name, skill, description;
        public Homeland homeland;
        public int stars, attack;
        public string faction = "汉", troop = "步", sourceUrl = "", evidence = "", nativeSkill = "", nativeSkillText = "";
        public bool s1, recruitable;
        public double defense, strategy, speed, cost, attackGrowth;
        // Display-only official source fields; combat and save identifiers keep their existing meanings.
        public double defenseGrowth, strategyGrowth, speedGrowth, siege, siegeGrowth;
        public int attackDistance;
        public string biography = "", advancedTroops = "", combination = "", policyName = "", policyText = "";
        public GeneralDefinition(string key, string title, Homeland origin, int rarity, int force, string tactic, string text)
        { id = key; name = title; homeland = origin; stars = rarity; attack = force; skill = tactic; description = text; }
    }

    [Serializable]
    public class GeneralCard
    {
        public int uid, level = 1, experience, rank;
        public string definition, learnedSkill = "";
        public GeneralDefinition Definition { get { return Catalog.Generals.Find(g => g.id == definition); } }
        public int LevelCap { get { return Definition.stars * 10; } }
        public void GainExperience(int value)
        {
            experience += Math.Max(0, value);
            while (level < LevelCap && experience >= level * 80) { experience -= level * 80; level++; }
            if (level >= LevelCap) experience = 0;
        }
    }

}
