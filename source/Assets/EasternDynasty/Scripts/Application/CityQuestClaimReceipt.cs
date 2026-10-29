using System.Linq;

namespace EasternDynasty
{
    // A transient receipt of one successful Core command. Never serialized into a campaign.
    public sealed class CityQuestClaimReceipt
    {
        public readonly int questId, chapterIndex, nextQuestId;
        public readonly string title;
        public readonly bool chapterComplete, allComplete;
        public readonly Supplies credited, balance;

        private CityQuestClaimReceipt(World world, CityQuestDefinition quest, Supplies before)
        {
            questId=quest.id;title=quest.title;chapterIndex=ChapterIndex(quest.id);
            balance=Copy(world.resources);
            credited=new Supplies { grain=balance.grain-before.grain, grass=balance.grass-before.grass,
                stone=balance.stone-before.stone, iron=balance.iron-before.iron, gold=balance.gold-before.gold };
            chapterComplete=CityCatalog.Quests.Where(q=>ChapterIndex(q.id)==chapterIndex).All(q=>world.cityClaimedQuests.Contains(q.id));
            var next=world.CurrentCityQuest;nextQuestId=next==null?0:next.id;allComplete=next==null;
        }

        public static int ChapterIndex(int id) { return id<=3?0:id<=7?1:2; }

        public static CityQuestClaimReceipt Claim(World world,int id,out string message)
        {
            if(world==null){message="请先进入征程。";return null;}
            var quest=world.CurrentCityQuest;
            bool alreadyClaimed=world.cityClaimedQuests!=null&&world.cityClaimedQuests.Contains(id);
            var before=Copy(world.resources);
            message=world.ClaimCityQuest(id);
            if(alreadyClaimed||quest==null||quest.id!=id||!world.cityClaimedQuests.Contains(id))return null;
            return new CityQuestClaimReceipt(world,quest,before);
        }

        private static Supplies Copy(Supplies value)
        { return new Supplies { grain=value.grain,grass=value.grass,stone=value.stone,iron=value.iron,gold=value.gold,jade=value.jade }; }
    }
}
