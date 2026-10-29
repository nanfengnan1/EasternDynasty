using System;
using System.Collections.Generic;
using System.Linq;

namespace EasternDynasty
{
    [Serializable]
    public partial class World
    {
        public const int Width = 36, Height = 27;
        public const string LegacyMapId = "legacy-campaign-v1";
        public int saveVersion = 1;
        // Missing fields in v1 JSON remain zero/null and are explicitly accepted only as legacy metadata.
        [System.Runtime.Serialization.OptionalField] public string mapId;
        [System.Runtime.Serialization.OptionalField] public int mapWidth, mapHeight, mapRevision;
        public string ruler = "新任太守";
        public Homeland origin;
        public int birthRegion, homeId, troops = 450, nextCardId = 1;
        public bool allPoolsUnlocked;
        public double elapsed;
        public long savedUtcTicks;
        public Supplies resources = new Supplies();
        public List<Land> lands = new List<Land>();
        public List<GeneralCard> cards = new List<GeneralCard>();
        public List<int> formation = new List<int>();
        public List<string> learnedSkills = new List<string>();
        public List<BattleRecord> reports = new List<BattleRecord>();
        public List<string> messages = new List<string>();
        // Unity serializes null inline classes as default instances. An empty list
        // preserves the difference between an idle army and an active order.
        public List<ArmyOrder> orders = new List<ArmyOrder>();
        [System.Runtime.Serialization.OptionalField] public int seasonMechanicsVersion, commands;
        [System.Runtime.Serialization.OptionalField] public double commandRecovery;
        [System.Runtime.Serialization.OptionalField] public List<TrainingReceipt> trainingHistory = new List<TrainingReceipt>();
        public TrainingReceipt LastTrainingReceipt
        { get { return trainingHistory == null || trainingHistory.Count == 0 ? null : trainingHistory[trainingHistory.Count - 1]; } }
        public bool IsYouzhou { get { return saveVersion == 2 && mapId == YouzhouMapDefinition.Id; } }
        public int MapWidth { get { return IsYouzhou ? YouzhouMapDefinition.Width : Width; } }
        public int MapHeight { get { return IsYouzhou ? YouzhouMapDefinition.Height : Height; } }
        public string MapName { get { return IsYouzhou ? "幽州" : "天下"; } }
        public string RegionName(int region)
        {
            var names = IsYouzhou ? YouzhouMapDefinition.RegionNames : Catalog.Regions;
            return region >= 0 && region < names.Length ? names[region] : "未知地区";
        }
        public Land TryGetLand(int x, int z)
        {
            if (x < 0 || z < 0 || x >= MapWidth || z >= MapHeight || lands == null) return null;
            int id = z * MapWidth + x;
            return id < lands.Count ? lands[id] : null;
        }
        public ArmyOrder order
        {
            get { return orders == null || orders.Count == 0 ? null : orders[0]; }
            set
            {
                if (orders == null) orders = new List<ArmyOrder>();
                orders.Clear();
                if (value != null) orders.Add(value);
            }
        }

        public void Note(string message) { messages.Insert(0, message); if (messages.Count > 20) messages.RemoveAt(messages.Count - 1); }
    }
}
