using System;
using System.Collections.Generic;
using System.Linq;
using EasternDynasty;

public static class S1RosterChecks
{
    private static int checks;
    private static void Check(bool value,string message)
    { if(!value)throw new Exception("S1 roster: "+message); checks++; }
    private static GeneralDefinition Card(string id)
    { return Catalog.Generals.Single(g=>g.id=="s1_"+id); }
    private sealed class ChooseIndex : Random
    {
        private readonly int index;
        public ChooseIndex(int value){index=value;}
        public override int Next(int maximum)
        { if(index<0||index>=maximum)throw new Exception("Draw requested an unexpected candidate range.");return index; }
    }
    public static int Main()
    {
        try
        {
            var roster=Catalog.Generals.Where(g=>g.s1).ToList();
            Check(roster.Count==245,"pinned archive must retain every identity, including excluded entries");
            Check(Catalog.Generals.Select(g=>g.id).Distinct().Count()==Catalog.Generals.Count,"duplicate stable IDs");
            Check(roster.All(g=>g.id.StartsWith("s1_")&&g.stars>=3&&g.stars<=5),"invalid namespace/rarity");
            Check(roster.Count(g=>g.stars==3)==29&&roster.Count(g=>g.stars==4)==111&&roster.Count(g=>g.stars==5)==105,"archive rarities changed");
            Check(roster.All(g=>!string.IsNullOrWhiteSpace(g.sourceUrl)&&!string.IsNullOrWhiteSpace(g.evidence)),"missing source/evidence");
            Check(roster.All(g=>g.attack>0&&g.cost>0&&g.nativeSkill==g.skill),"official attributes/skill identity missing");
            var allFactionCounts=new Dictionary<string,int>{{"汉",39},{"魏",41},{"蜀",39},{"吴",42},{"群",64},{"晋",20}};
            var eligibleFactionCounts=new Dictionary<string,int>{{"汉",9},{"魏",7},{"蜀",6},{"吴",7},{"群",7},{"晋",5}};
            var w=World.CreateYouzhou();
            foreach(var pair in allFactionCounts)
            {
                var all=w.S1Pool(pair.Key,true);var playable=w.S1Pool(pair.Key);
                Check(all.Count==pair.Value&&all.All(g=>g.faction==pair.Key),pair.Key+" archive faction filter");
                Check(playable.Count==eligibleFactionCounts[pair.Key]&&playable.All(g=>g.faction==pair.Key&&g.recruitable),pair.Key+" eligible faction filter");
            }
            Check(w.S1Pool("全部",true).Count==245&&w.S1Pool("全部").Count==41,"archive/recruitment distinction");
            string[] expectedRecruitment={"100805","100840","100769","100443","100655","100804","100450","100790","100671","100619","100771","100616","100803","100449","100631","100707","100574","100811","100630","100702","100706","100615","100585","100670","100620","100451","100704","100705","100524","100007","100486","100570","100772","100310","100074","100522","100696","100697","100698","100699","100700"};
            Check(w.S1Pool("全部").Select(g=>g.id).OrderBy(id=>id).SequenceEqual(expectedRecruitment.Select(id=>"s1_"+id).OrderBy(id=>id)),"evidence-filtered identity fixture changed");
            Check(w.S1Pool("全部").Count(g=>g.stars==4)==7&&w.S1Pool("全部").Count(g=>g.stars==5)==34,"verified recruitment rarities");
            Check(w.S1Pool("不存在",true).Count==0,"unknown faction unexpectedly returned cards");
            Check(new[]{"100805","100043","100033","100088"}.Select(Card).Select(g=>g.id).Distinct().Count()==4,"same-name Sun Jian identities merged");
            Check(Card("100805").faction=="汉"&&Card("100805").troop=="弓"&&Card("100805").stars==5,"Han bow Sun Jian identity");
            Check(Card("100033").faction=="吴"&&Card("100033").troop=="步"&&Card("100033").stars==5,"Wu infantry Sun Jian identity");
            Check(Card("100840").attack==97&&Card("100840").nativeSkill=="蛮王御众","new Xu Chu incorrectly mapped to an old variant");
            Check(Card("100700").attack==75&&Card("100700").stars==4&&Card("100700").faction=="吴","quality 3-R must map to four stars");
            foreach(string id in new[]{"100710","100009","100024","100031","100017","100812","100337","102002","100586"})
                Check(!Card(id).recruitable,id+" reward-only/expired/unassigned/SP/removed archive leaked into sampling");
            Check(Card("100619").recruitable,"a separate valid EX route should survive an expired event route");
            Check(Card("100805").recruitable&&Card("100803").recruitable,"an independent EX route should survive reward-only exclusion");

            // Enumerate each selectable index: every eligible identity must be reachable without archived entries.
            foreach(bool premium in new[]{false,true})
            {
                var candidates=w.S1Pool("全部").Where(g=>premium||g.stars==4).ToList();
                for(int index=0;index<candidates.Count;index++)
                {
                    w.resources.gold=1000;w.resources.jade=1000;int before=w.cards.Count;
                    w.DrawS1(premium,"全部",new ChooseIndex(index));
                    Check(w.cards.Count==before+1&&w.cards.Last().definition==candidates[index].id,"candidate reachability/order "+index);
                    Check(w.resources.gold==(premium?1000:800)&&w.resources.jade==(premium?900:1000),"draw charged wrong currency");
                }
            }
            w.resources.gold=500;w.resources.jade=500;int count=w.cards.Count;
            w.DrawS1(false,"晋",new ChooseIndex(0));
            Check(w.cards.Count==count&&w.resources.gold==500&&w.resources.jade==500,"empty four-star Jin pool consumed funds");
            w.DrawS1(true,"不明阵营",new ChooseIndex(0));
            Check(w.cards.Count==count&&w.resources.gold==500&&w.resources.jade==500,"invalid faction consumed funds");
            w.resources.gold=0;w.resources.jade=0;
            w.DrawS1(false,"汉",new ChooseIndex(0));w.DrawS1(true,"汉",new ChooseIndex(0));
            Check(w.cards.Count==count&&w.resources.gold==0&&w.resources.jade==0,"unaffordable draw mutated state");
            var legacy=World.Create(Homeland.Han,4,"Legacy");int oldCount=legacy.cards.Count;double oldGold=legacy.resources.gold;int oldJade=legacy.resources.jade;
            legacy.DrawS1(true,"全部",new ChooseIndex(0));
            Check(legacy.cards.Count==oldCount&&legacy.resources.gold==oldGold&&legacy.resources.jade==oldJade,"S1 entry leaked into legacy campaign");
            legacy.resources.gold=10000;legacy.resources.jade=10000;
            for(int i=0;i<20;i++)legacy.Draw(i%2==0,new Random(i));
            Check(legacy.cards.All(c=>!c.Definition.s1),"legacy recruitment accidentally uses S1 catalogue");
            var material=w.AddCard("s1_100840");int held=w.cards.Count;var skills=w.learnedSkills.ToArray();
            w.Dismantle(material.uid);
            Check(w.cards.Count==held&&w.cards.Contains(material)&&w.learnedSkills.SequenceEqual(skills),"unimplemented S1 dismantle consumed a card");
            var firstSun=w.AddCard("s1_100805");var secondSun=w.AddCard("s1_100033");
            w.Assign(firstSun.uid,0);w.Assign(secondSun.uid,1);
            Check(w.formation.Contains(firstSun.uid)&&!w.formation.Contains(secondSun.uid),"same named S1 identities can both occupy formation");
            w.Assign(secondSun.uid,0);
            Check(w.formation[0]==secondSun.uid&&!w.formation.Contains(firstSun.uid),"legitimate same-slot variant replacement rejected");
            string error;Check(w.IsValid(out error),"drawn archive cards invalidate save: "+error);
            Console.WriteLine("S1_ROSTER_CHECKS_OK: "+checks+" assertions; 245 archived identities / 41 evidence-filtered prototype recruits.");return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
