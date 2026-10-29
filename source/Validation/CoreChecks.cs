using System;
using System.Linq;
using System.Collections.Generic;
using EasternDynasty;

public static class CoreChecks
{
    private static int checks;
    private static void Check(bool ok, string message)
    { if (!ok) throw new Exception(message); checks++; }
    private static World Fresh() { return World.Create(Homeland.Han,4,"Test"); }
    private static int NextTarget(World w) { return w.lands.First(t=>w.AttackProblem(t.id).Length==0).id; }
    public static int Main()
    {
        try
        {
            for (int region=0;region<Catalog.Regions.Length;region++)
            {
                World w=World.Create(World.RegionOrigin(region),region,"Test"); string problem;
                Check(w.IsValid(out problem),"Invalid start in region "+region+": "+problem);
                Check(w.lands[w.homeId].region==region,"Spawn belongs to wrong region.");
                Check(w.lands.Count(t=>t.capital)==13,"Missing regional capitals.");
                Check(w.lands.Where(t=>t.capital).All(t=>t.region==Array.IndexOf(Catalog.Capitals,t.name)),"Capital region mismatch.");
                Check(w.cards.All(c=>c.Definition.homeland==w.origin),"Foreign starter.");
                Check(w.lands.Any(t=>w.AttackProblem(t.id).Length==0),"No opening move.");
                Check(w.lands.Count(t=>t.kind==LandKind.Water)==15,"Starter fields erased the river.");
                var reachable=new HashSet<int>{w.homeId};var frontier=new Queue<int>();frontier.Enqueue(w.homeId);
                while(frontier.Count>0)
                {
                    var here=w.lands[frontier.Dequeue()];
                    foreach(var next in w.lands.Where(t=>t.Walkable&&Math.Abs(t.x-here.x)+Math.Abs(t.z-here.z)==1))
                        if(reachable.Add(next.id))frontier.Enqueue(next.id);
                }
                Check(w.lands.Where(t=>t.capital).All(t=>reachable.Contains(t.id)),"Preserved geography made a capital unreachable from region "+region);
                Check(w.lands.Where(t=>!t.capital&&!t.home).All(t=>t.name==Catalog.LandName(t.kind)),"Resource label disagrees with land kind.");
            }
            {
                var w=Fresh();int count=w.cards.Count;w.resources.gold=0;w.resources.jade=0;
                w.Draw(false,new Random(1));w.Draw(true,new Random(1));
                Check(w.cards.Count==count&&w.resources.gold==0&&w.resources.jade==0,"Failed draw mutated economy.");
                w.resources.gold=200;w.Draw(false,new Random(1));
                Check(w.cards.Count==count+1&&w.resources.gold==0,"Gold draw cost mismatch.");
                w.resources.jade=100;w.Draw(true,new Random(1));
                Check(w.resources.jade==0&&w.cards.Last().Definition.stars>=3,"Premium draw mismatch.");
                w.resources.gold=1000000;w.resources.jade=1000000;var rng=new Random(3);
                for(int i=0;i<200;i++)w.Draw(i%2==0,rng);
                Check(w.cards.All(c=>c.Definition.homeland==w.origin),"Starting pool leaked foreign cards.");
                w.allPoolsUnlocked=true;
                for(int i=0;i<200;i++)w.Draw(i%2==0,rng);
                Check(w.cards.Any(c=>c.Definition.homeland==Homeland.Steppe)&&w.cards.Any(c=>c.Definition.homeland==Homeland.Northeast),"Unlocked pool omitted a culture.");
            }
            {
                var w=Fresh();var commander=w.cards[0];int before=w.cards.Count;
                w.Dismantle(commander.uid);Check(w.cards.Count==before,"An assigned card was dismantled.");
                w.Promote(commander.uid);Check(commander.rank==0,"Promotion succeeded without duplicate.");
                var material=w.AddCard(commander.definition);w.Promote(commander.uid);
                Check(commander.rank==1&&w.cards.All(c=>c.uid!=material.uid)&&w.formation.Contains(commander.uid),"Promotion consumed wrong card.");
                for(int i=0;i<8;i++){w.AddCard(commander.definition);w.Promote(commander.uid);}
                Check(commander.rank==commander.Definition.stars,"Rank exceeded star cap.");
                commander.GainExperience(1000000);Check(commander.level==commander.LevelCap&&commander.experience==0,"Level exceeded star cap.");
                var copy=w.AddCard("zhu_jun");w.Dismantle(copy.uid);
                Check(w.learnedSkills.Contains("突击")&&!w.cards.Contains(copy),"Skill dismantling failed.");
                double coins=w.resources.gold;w.Teach(commander.uid,"突击");
                Check(commander.learnedSkill=="突击"&&w.resources.gold==coins-100,"Learned skill or cost missing.");
            }
            {
                var w=Fresh();int target=NextTarget(w);w.resources.grass=0;double grain=w.resources.grain;
                w.March(target);Check(w.order==null&&w.resources.grain==grain,"Partial resource deduction on failed march.");
                w.resources.grass=3000;w.March(target);var old=w.formation[0];
                var replacement=w.AddCard("cao_cao");w.Assign(replacement.uid,0);
                Check(w.formation[0]==old,"Formation changed during march.");
                w.Tick(1000);
                Check(w.order==null&&w.lands[target].owned&&w.reports.Count==1&&w.reports[0].won,"Opening march/capture/return failed.");
                Check(w.cards[0].experience>0||w.cards[0].level>1,"Battle did not grant experience.");
                Check(w.resources.grain>=0&&w.resources.grass>=0&&w.troops>=0,"Negative resources or troops.");
            }
            {
                var a=Fresh();var b=Fresh();int target=NextTarget(a);a.March(target);b.March(target);
                a.Tick(1000);for(int i=0;i<1000;i++)b.Tick(1);
                Check(Math.Abs(a.resources.grain-b.resources.grain)<.00001&&a.troops==b.troops&&a.OwnedCount==b.OwnedCount,"Large time step differs from incremental settlement.");
            }
            for(int i=0;i<3;i++)
            {
                var w=World.Create((Homeland)i,i==0?4:i==1?9:11,"Test");
                foreach(var c in w.cards)c.GainExperience(1000000);
                w.troops=w.TroopCap;
                var target=w.lands.First(t=>t.capital&&t.homeland!=w.origin);
                w.lands[target.id-1].owned=true;w.March(target.id);w.Tick(1000);
                Check(target.owned&&w.allPoolsUnlocked,"Cross-origin capital failed to unlock roster.");
            }
            {
                var w=Fresh();foreach(var c in w.cards)c.GainExperience(1000000);w.troops=w.TroopCap;
                var target=w.lands.First(t=>t.capital&&t.homeland==w.origin);
                w.lands[target.id-1].owned=true;w.March(target.id);w.Tick(1000);
                Check(target.owned&&!w.allPoolsUnlocked,"Domestic capital incorrectly unlocked foreign recruitment.");
            }
            {
                string error;var w=Fresh();w.cards[0].definition="missing";
                Check(!w.IsValid(out error),"Corrupt general accepted.");
                w=Fresh();w.formation.Add(99999);Check(!w.IsValid(out error),"Missing formation member accepted.");
                w=Fresh();w.lands[3]=null;Check(!w.IsValid(out error),"Corrupt land accepted.");
                w=Fresh();w.lands[w.homeId]=null;Check(!w.IsValid(out error),"Missing home accepted.");
                w=Fresh();w.cards.Clear();Check(!w.IsValid(out error),"Empty card collection accepted.");
                w=Fresh();w.resources.gold=double.NaN;Check(!w.IsValid(out error),"Non-finite resource amount accepted.");
                w=Fresh();w.resources.grain=-1;Check(!w.IsValid(out error),"Negative resource amount accepted.");
            }
            Console.WriteLine("PASS: "+checks+" checks across 13 starts, recruitment, progression, skill transfer, economy, marches, battles, offline settlement and unlock rules.");
            return 0;
        }
        catch(Exception error){Console.Error.WriteLine("FAIL: "+error);return 1;}
    }
}
