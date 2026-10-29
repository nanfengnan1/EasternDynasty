using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;

internal static class CityProgressionChecks
{
    private static int checks;
    private static void Check(bool condition,string name) { if(!condition)throw new Exception("FAIL "+name);checks++;Console.WriteLine("PASS "+name); }
    private static void Near(double actual,double expected,string name) { Check(Math.Abs(actual-expected)<.00001,name); }
    private static World Fresh() { return World.CreateYouzhou(); }
    private static void Rich(World world) { world.resources=new Supplies{grain=1000000,grass=1000000,stone=1000000,iron=1000000,gold=1000000,jade=300}; }
    private static void Complete(World world,int index,CityBuilding building)
    { Check(world.CityBuildProblem(index,building)=="","build allowed "+building);world.StartCityConstruction(index,building);Check(world.CityJob!=null,"queued "+building);world.Tick(world.CityJob.remaining);Check(world.CityJob==null,"complete "+building); }
    private static void Valid(World world,string label) { string problem;Check(world.IsValid(out problem),label+" "+problem); }
    private static World Copy(World world)
    { var serializer=new DataContractJsonSerializer(typeof(World));using(var stream=new MemoryStream()){serializer.WriteObject(stream,world);stream.Position=0;return (World)serializer.ReadObject(stream);} }
    private static void SameSupplies(Supplies actual,Supplies expected,string name)
    {
        Check(Math.Abs(actual.grain-expected.grain)<.00001&&Math.Abs(actual.grass-expected.grass)<.00001&&
            Math.Abs(actual.stone-expected.stone)<.00001&&Math.Abs(actual.iron-expected.iron)<.00001&&
            Math.Abs(actual.gold-expected.gold)<.00001&&Math.Abs(actual.jade-expected.jade)<.00001,name);
    }
    private static Supplies WithRefund(Supplies balance,Supplies paid)
    { return new Supplies{grain=balance.grain+paid.grain,grass=balance.grass+paid.grass,stone=balance.stone+paid.stone,
        iron=balance.iron+paid.iron,gold=balance.gold+paid.gold,jade=balance.jade}; }
    private static void ConstructionRefundChecks()
    {
        var world=Fresh();Rich(world);
        world.resources.grain+=.125;world.resources.grass+=.25;world.resources.stone+=.375;
        world.resources.iron+=.5;world.resources.gold+=.625;
        SameSupplies(world.CityConstructionRefund(),new Supplies(),"idle construction preview has no refund");
        var before=WithRefund(world.resources,new Supplies());
        world.CancelCityConstruction();SameSupplies(world.resources,before,"idle cancel changes no resources");
        foreach(bool upgrade in new[]{false,true})
        {
            if(upgrade)Complete(world,0,CityBuilding.Mill);
            int previousLevel=world.CityLevel(CityBuilding.Mill);
            world.StartCityConstruction(0,CityBuilding.Mill);
            Check(world.CityJob!=null,"refund fixture started "+upgrade);
            world.Tick(world.CityJob.remaining*.4);
            var restored=Copy(world);Valid(restored,"old paid shape round trip "+upgrade);
            var receipt=restored.CityJob.paid;var preview=restored.CityConstructionRefund();
            SameSupplies(preview,receipt,"refund preview matches saved five-resource receipt "+upgrade);
            Check(!ReferenceEquals(preview,receipt),"refund preview cannot edit saved paid receipt "+upgrade);
            preview.stone=0;preview.grain=0;
            SameSupplies(restored.CityConstructionRefund(),receipt,"mutating preview leaves refund unchanged "+upgrade);
            var badReceipt=Copy(restored);badReceipt.CityJob.paid.stone+=.125;string problem;
            Check(!badReceipt.IsValid(out problem),"fractional paid tampering still rejected "+upgrade);
            badReceipt=Copy(restored);badReceipt.CityJob.paid.jade=1;
            Check(!badReceipt.IsValid(out problem),"paid jade inflation still rejected "+upgrade);
            var expected=WithRefund(restored.resources,receipt);
            restored.CancelCityConstruction();
            SameSupplies(restored.resources,expected,"cancel refunds all five resources and preserves jade "+upgrade);
            Check(restored.CityJob==null&&restored.CityLevel(CityBuilding.Mill)==previousLevel,
                "cancel preserves completed level and clears queue "+upgrade);
            Check(upgrade||!restored.CityQuestReady(2),"cancel new facility does not complete construction quest");
            restored=Copy(restored);restored.CancelCityConstruction();
            SameSupplies(restored.resources,expected,"cancel after reload cannot refund twice "+upgrade);
            SameSupplies(restored.CityConstructionRefund(),new Supplies(),"cancelled queue preview is empty "+upgrade);
            Valid(restored,"cancelled construction save valid "+upgrade);
            // Keep the original world idle for the next new-build/upgrade case.
            world.CancelCityConstruction();
        }
        world.StartCityConstruction(0,CityBuilding.Mill);world.Tick(world.CityJob.remaining);
        before=WithRefund(world.resources,new Supplies());world.CancelCityConstruction();
        SameSupplies(world.resources,before,"completed construction cannot be refunded");
        Check(world.CityLevel(CityBuilding.Mill)==2,"late cancellation preserves completed upgrade");
    }
    public static int Main()
    {
        try
        {
            ConstructionRefundChecks();
            for(int start=0;start<3;start++)
            {
                var world=World.CreateYouzhou(start);var original=YouzhouMapDefinition.CreateLands();
                Check(world.cityVersion==1&&world.cityPlots.Count==9,"nine district slots start "+start);
                Check(world.lands.Count(t=>t.owned)==9&&world.OwnedCount==0,"district ownership excluded from wild count "+start);
                Check(world.cityPlots.All(p=>world.IsCityDistrict(p.landId)),"nine available district plots "+start);
                Check(world.cityPlots[4].landId==world.homeId&&world.CityLevel(CityBuilding.TownHall)==1,"central town hall "+start);
                Check(world.cityPlots.Where(p=>p.index!=4).All(p=>world.lands[p.landId].kind==original[p.landId].kind&&world.lands[p.landId].level==original[p.landId].level),"district terrain preserved "+start);
                Near(world.HourlyIncome().grain,360,"no district resource double income "+start);Valid(world,"fresh city valid "+start);
            }
            var w=Fresh();var before=w.resources.grain;var price=w.CityBuildCost(CityBuilding.Mill,1);
            w.StartCityConstruction(0,CityBuilding.Mill);Near(w.resources.grain,before-price.grain,"construction pays once");
            Check(w.CityLevel(CityBuilding.Mill)==0,"facility does not produce before completion");
            var afterPaid=w.resources.grain;w.StartCityConstruction(1,CityBuilding.Quarry);Near(w.resources.grain,afterPaid,"busy queue does not charge");
            Check(w.CityJob.plotIndex==0,"single queue preserved");
            double duration=w.CityJob.duration;w.Tick(duration+10);
            Check(w.CityLevel(CityBuilding.Mill)==1&&w.CityJob==null,"building completes");
            Near(w.resources.grain,afterPaid+360*duration/3600+520d*10/3600,"income split at construction completion");
            Check(w.CityBuildProblem(1,CityBuilding.Mill)!="","duplicate facility rejected");
            Check(w.CityBuildProblem(0,CityBuilding.Quarry)!="","occupied plot replacement rejected");
            Check(w.CityBuildProblem(4,CityBuilding.Mill)!="","central plot restricted");
            Check(w.CityBuildProblem(1,CityBuilding.TownHall)!="","town hall restricted to centre");
            Check(w.CityBuildProblem(-1,CityBuilding.Mill)!=""&&w.CityBuildProblem(9,CityBuilding.Mill)!="","invalid plot rejected");
            Check(w.CityBuildProblem(1,(CityBuilding)99)!="","invalid facility rejected");
            Check(w.CityBuildProblem(1,CityBuilding.Barracks)!="","facility locked by hall");
            w.resources=new Supplies();w.StartCityConstruction(1,CityBuilding.Quarry);Check(w.CityJob==null,"insufficient resources no queue");
            Rich(w);Complete(w,4,CityBuilding.TownHall);int cap=w.TroopCap;Complete(w,1,CityBuilding.Barracks);
            Check(w.CityTroopBonus==250&&w.TroopCap==cap+250,"barracks changes actual troop cap");
            Complete(w,2,CityBuilding.DrillGround);Check(w.CityTroopBonus==350,"drill ground real troop bonus");
            Complete(w,4,CityBuilding.TownHall);double normal=w.CityBuildSeconds(CityBuilding.Quarry,1);Complete(w,3,CityBuilding.Workshop);
            Near(w.CityBuildSeconds(CityBuilding.Quarry,1),normal*.95,"workshop affects subsequent construction time");
            w.StartCityConstruction(5,CityBuilding.Quarry);var paid=w.CityJob.paid;double value=w.resources.stone;
            w.CancelCityConstruction();Near(w.resources.stone,value+paid.stone,"cancel refunds full original stone receipt");
            value=w.resources.stone;w.CancelCityConstruction();Near(w.resources.stone,value,"cancel cannot repeat refund");
            w.StartCityConstruction(5,CityBuilding.Quarry);w.Tick(3);var copy=Copy(w);
            Near(copy.CityJob.remaining,w.CityJob.remaining,"construction progress serializes");Valid(copy,"construction JSON round trip");
            copy.Tick(copy.CityJob.remaining+600);Check(copy.CityLevel(CityBuilding.Quarry)==1&&copy.CityJob==null,"loaded construction completes once");
            value=copy.resources.stone;copy.Tick(0);Near(copy.resources.stone,value,"zero tick does not duplicate rewards");
            var bad=Copy(w);bad.CityJob.paid.stone+=1;string problem;Check(!bad.IsValid(out problem),"tampered refund rejected");
            bad=Copy(w);bad.CityJob.remaining=double.NaN;Check(!bad.IsValid(out problem),"nonfinite construction rejected");
            bad=Copy(w);bad.cityConstruction.Add(new CityConstruction());Check(!bad.IsValid(out problem),"multiple queue entries rejected");
            bad=Copy(w);bad.cityConstruction[0]=null;Check(!bad.IsValid(out problem),"null queue entry rejected");
            bad=Copy(w);bad.cityPlots[0].landId=bad.homeId;Check(!bad.IsValid(out problem),"invalid district position rejected");
            bad=Copy(w);bad.cityPlots[0].building=CityBuilding.Barracks;Check(!bad.IsValid(out problem),"duplicate saved facilities rejected");
            bad=Copy(w);bad.cityClaimedQuests.Add(2);Check(!bad.IsValid(out problem),"nonsequential saved claims rejected");
            bad=Copy(w);bad.cityRecruitActions=-1;Check(!bad.IsValid(out problem),"negative task count rejected");

            var old=Fresh();foreach(var tile in old.lands)if(tile.id!=old.homeId)tile.owned=false;
            old.cityVersion=0;old.cityPlots=null;old.cityConstruction=null;old.cityClaimedQuests=null;
            int blocked=old.homeId-1;old.lands[blocked].kind=LandKind.Mountain;
            var existing=old.lands[old.homeId+1];existing.owned=true;existing.kind=LandKind.Iron;existing.level=4;existing.name="旧领地";
            old.EnsureCityState();Check(old.lands[blocked].kind==LandKind.Mountain&&!old.lands[blocked].owned,"migration preserves natural obstacle");
            Check(existing.kind==LandKind.Iron&&existing.level==4&&existing.name=="旧领地"&&existing.owned,"migration preserves existing terrain data");
            var blockedPlot=old.CityPlotAt(blocked);Check(blockedPlot!=null&&old.CityBuildProblem(blockedPlot.index,CityBuilding.Mill)!="","blocked old district cannot build");
            old.RecordCityAction("visit");old.ClaimCityQuest(1);old.EnsureCityState();Check(old.cityClaimedQuests.SequenceEqual(new[]{1}),"migration idempotent preserves claims");Valid(old,"migrated city valid");
            var legacy=World.Create(Homeland.Han,4,"旧州");legacy.EnsureCityState();Check(legacy.cityVersion==0&&!legacy.IsCityDistrict(legacy.homeId)&&legacy.CityTroopBonus==0,"legacy world isolated");

            var q=Fresh();Rich(q);Check(q.CurrentCityQuest.id==1&&!q.CityQuestReady(1),"first task requires actual city visit");
            value=q.resources.gold;q.ClaimCityQuest(1);Near(q.resources.gold,value,"unfinished task cannot claim");
            q.RecordCityAction("visit");q.ClaimCityQuest(1);Check(q.CurrentCityQuest.id==2,"first task advances");
            value=q.resources.gold;q.ClaimCityQuest(1);Near(q.resources.gold,value,"reward cannot be claimed twice");
            q.ClaimCityQuest(4);Check(q.CurrentCityQuest.id==2,"future ready task cannot bypass sequence");
            Complete(q,0,CityBuilding.Mill);Check(q.CityQuestReady(2),"production task complete");q.ClaimCityQuest(2);
            Complete(q,4,CityBuilding.TownHall);q.ClaimCityQuest(3);q.ClaimCityQuest(4);Check(q.CurrentCityQuest.id==5,"build and formation tasks progress");
            q.Recruit(10);Check(q.CityQuestReady(5),"successful recruiting records task");q.ClaimCityQuest(5);
            var land=q.lands.First(t=>!t.owned&&t.Walkable&&!t.bridge&&!t.capital&&q.AttackProblem(t.id)=="");q.March(land.id);q.Tick(q.order.remaining);
            Check(land.owned&&q.CityQuestReady(6),"wild capture completes task");q.Tick(100);q.ClaimCityQuest(6);
            Complete(q,1,CityBuilding.Barracks);q.ClaimCityQuest(7);Check(q.CurrentCityQuest.id==8,"farm task reached");
            q.Farm(land.id);Check(!q.CityQuestReady(8),"farm departure alone does not complete task");q.RecallArmy();q.Tick(500);
            Check(!q.CityQuestReady(8),"cancelled farm gives no task completion");q.Farm(land.id);q.Tick(500);Check(q.CityQuestReady(8),"farm completion records task");q.ClaimCityQuest(8);
            Complete(q,4,CityBuilding.TownHall);q.ClaimCityQuest(9);
            foreach(var tile in q.lands.Where(t=>!t.owned&&t.Walkable&&!t.bridge&&!t.capital&&!q.IsCityDistrict(t.id)).Take(2))tile.owned=true;
            Check(q.CityQuestReady(10),"three external resource lands complete chapter");q.ClaimCityQuest(10);Check(q.CurrentCityQuest==null,"all tasks finish");
            var restored=Copy(q);Check(restored.CurrentCityQuest==null&&restored.cityClaimedQuests.Count==10,"completed guide persists");value=restored.resources.grain;
            restored.ClaimCityQuest(10);Near(restored.resources.grain,value,"saved reward cannot be replayed");Valid(restored,"completed campaign valid");
            // Compare the values used by the facility preview with actual completed rules.
            // No preview is permitted to grant income/capacity before the construction ends.
            var preview=Fresh();Rich(preview);
            Complete(preview,4,CityBuilding.TownHall);Complete(preview,4,CityBuilding.TownHall);
            foreach(var definition in CityCatalog.Buildings.Where(d=>d.kind!=CityBuilding.TownHall))
            {
                var plot=preview.cityPlots.First(p=>p.building==CityBuilding.None);
                double incomeBefore=preview.HourlyIncome().grain+preview.HourlyIncome().grass+preview.HourlyIncome().stone+preview.HourlyIncome().iron+preview.HourlyIncome().gold;
                int capBefore=preview.TroopCap;double durationBefore=preview.CityBuildSeconds(CityBuilding.TownHall,4);
                int projected=CityCatalog.EffectAtLevel(definition.kind,1);
                preview.StartCityConstruction(plot.index,definition.kind);
                Check(preview.CityLevel(definition.kind)==0,"preview effect not granted at start "+definition.kind);
                Near(preview.TroopCap,capBefore,"preview does not raise cap while building "+definition.kind);
                preview.Tick(preview.CityJob.remaining);
                var actualIncome=preview.HourlyIncome();
                if(definition.kind>=CityBuilding.Mill&&definition.kind<=CityBuilding.Residence)
                    Near(actualIncome.grain+actualIncome.grass+actualIncome.stone+actualIncome.iron+actualIncome.gold-incomeBefore,projected,"preview equals completed income "+definition.kind);
                else if(definition.kind==CityBuilding.Workshop)
                    Near(preview.CityBuildSeconds(CityBuilding.TownHall,4),durationBefore*(1-projected/100d),"preview equals subsequent construction reduction");
                else Near(preview.TroopCap-capBefore,projected,"preview equals completed troop cap "+definition.kind);
            }
            var capped=preview.cityPlots.First(p=>p.building==CityBuilding.Workshop);capped.level=10;
            Near(CityCatalog.EffectAtLevel(CityBuilding.Workshop,10),40,"workshop preview shows actual forty percent cap");
            Near(preview.CityBuildSeconds(CityBuilding.TownHall,4),72,"capped workshop actual construction duration");
            Check(CityCatalog.RequiredHallLevel(CityBuilding.Barracks,1)==2&&CityCatalog.RequiredHallLevel(CityBuilding.Workshop,1)==3,"new facility prerequisite matches unlock levels");
            Check(CityCatalog.RequiredHallLevel(CityBuilding.Mill,6)==4,"upgrade prerequisite includes hall plus two limit");
            var previewMill=preview.cityPlots.First(p=>p.building==CityBuilding.Mill);previewMill.level=5;
            Check(preview.CityBuildProblem(previewMill.index,CityBuilding.Mill)!="","preview prerequisite blocks actual upgrade below hall four");
            Complete(preview,4,CityBuilding.TownHall);
            Check(preview.CityBuildProblem(previewMill.index,CityBuilding.Mill)=="","preview prerequisite unlocks actual upgrade at hall four");
            Check(CityCatalog.EffectAtLevel(CityBuilding.Mill,11)==1600&&CityCatalog.EffectAtLevel(CityBuilding.Mill,-1)==0,"preview does not advertise beyond supported levels");
            var edge=Fresh();var prior=edge.lands[edge.homeId];prior.home=false;var edgeHome=edge.TryGetLand(0,0);edgeHome.home=edgeHome.owned=true;edge.homeId=edgeHome.id;edge.birthRegion=edgeHome.region;
            edge.cityVersion=0;edge.EnsureCityState();Check(edge.cityPlots.Count==9&&edge.cityPlots.Count(p=>p.landId<0)==5,"edge city slots never wrap map");Valid(edge,"edge migrated city valid");
            Console.WriteLine("CITY_PROGRESSION_OK: "+checks+" assertions.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
