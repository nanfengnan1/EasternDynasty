using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    public enum Homeland { Han, Steppe, Northeast }
    public enum LandKind { Grain, Pasture, Stone, Iron, Water, Mountain }

    [Serializable]
    public class Supplies
    {
        public double grass, grain, stone, iron, gold;
        public int jade;
        public bool Spend(double food, double forage, double rock, double metal, double coins)
        {
            if (food < 0 || forage < 0 || rock < 0 || metal < 0 || coins < 0) return false;
            if (grain + 0.00001 < food || grass + 0.00001 < forage || stone + 0.00001 < rock || iron + 0.00001 < metal || gold + 0.00001 < coins) return false;
            grain -= food; grass -= forage; stone -= rock; iron -= metal; gold -= coins;
            return true;
        }
    }

    [Serializable]
    public class Land
    {
        public int id, x, z, region, level;
        public Homeland homeland;
        public LandKind kind;
        public bool owned, capital, home;
        [System.Runtime.Serialization.OptionalField] public bool bridge;
        public string name;
        public bool Walkable { get { return kind != LandKind.Water && kind != LandKind.Mountain; } }
    }

    [Serializable]
    public class ArmyOrder
    {
        public int target;
        public double remaining, duration;
        public bool returning;
        [System.Runtime.Serialization.OptionalField] public int action, phase, routeVersion;
        [System.Runtime.Serialization.OptionalField] public List<int> path = new List<int>();
        [System.Runtime.Serialization.OptionalField] public double routeStart, routeEnd;
        // Identity is allocated at departure, before any combat is resolved.
        // Older saves have no watch intent and receive an ID during normalization.
        [System.Runtime.Serialization.OptionalField] public string battleId;
        [System.Runtime.Serialization.OptionalField] public bool watchOnArrival;
        // Training snapshots are absent (version 0) on all earlier save files.
        [System.Runtime.Serialization.OptionalField] public int trainingVersion, trainingExperience;
        [System.Runtime.Serialization.OptionalField] public List<int> trainingParticipants = new List<int>();
        [System.Runtime.Serialization.OptionalField] public bool trainingSettled, trainingCancelled;
    }

    [Serializable]
    public class TrainingReceipt
    {
        public int target, experiencePerGeneral;
        public List<int> participantIds = new List<int>();
        public List<int> awardedExperience = new List<int>();
        public double completedAt;
        public bool cancelled;
    }

    [Serializable]
    public class BattleRecord
    {
        public string title, text;
        public bool won;
        public double time;
        // Version zero is a historical text-only report. Never reconstruct events from its text.
        [System.Runtime.Serialization.OptionalField] public int replayVersion;
        [System.Runtime.Serialization.OptionalField] public string battleId, ruleVersion;
        [System.Runtime.Serialization.OptionalField] public int targetId, targetX, targetZ;
        [System.Runtime.Serialization.OptionalField] public string targetName;
        [System.Runtime.Serialization.OptionalField] public BattleArmySnapshot attacker, defender;
        [System.Runtime.Serialization.OptionalField] public List<BattleReplayEvent> events;
        [System.Runtime.Serialization.OptionalField] public int attackerFinal, defenderFinal, goldReward, experienceReward;
        [System.Runtime.Serialization.OptionalField] public bool captured;
        public bool HasReplay { get { return replayVersion == 1 && attacker != null && defender != null && events != null && events.Count >= 2; } }
    }

}
