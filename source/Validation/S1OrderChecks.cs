using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EasternDynasty;
public static class S1OrderChecks
{
    private static int checks;
    private static void Check(bool result,string message){if(!result)throw new Exception(message);checks++;}
    private static void Near(double a,double b,string message){Check(Math.Abs(a-b)<.00001,message+" "+a+" / "+b);}
    private static World Fresh(){return World.CreateYouzhou();}
    private static int OwnNext(World w){var t=w.lands.First(x=>w.AttackProblem(x.id)=="");t.owned=true;return t.id;}
    private static void Valid(World w){string p;Check(w.IsValid(out p),p);}
    private static World Copy(World w)
    {var s=new DataContractJsonSerializer(typeof(World));using(var b=new MemoryStream()){s.WriteObject(b,w);b.Position=0;return (World)s.ReadObject(b);}}
    public static int Main()
    {
        try
        {
            for(int start=0;start<3;start++)
            {
                var w=World.CreateYouzhou(start);var far=w.lands.First(t=>t.capital&&t.x>25);
                var path=w.FindMarchPath(w.homeId,far.id);Check(path!=null&&path.Count>2,"capital path missing");
                Check(path[0]==w.homeId&&path.Last()==far.id,"path endpoints");
                Check(path.All(i=>w.lands[i].Walkable),"path crossed river/mountain");
                for(int i=1;i<path.Count;i++)Check(w.CanMarchStep(path[i-1],path[i],2),"path jumped or cut a blocked corner");
            }
            {
                var w=Fresh();int t=OwnNext(w);w.Garrison(t);Valid(w);double duration=w.order.duration;
                w.Tick(duration);Check(w.order.phase==1&&!w.order.returning,"garrison did not station");
                Check(w.IsGuarded(t)&&w.lands.Any(n=>n.id!=t&&n.owned&&w.AreAdjacent(t,n.id)&&w.IsGuarded(n.id)),"guard coverage missing around stationed tile");
                var load=Copy(w);Valid(load);load.Tick(7*86400);Check(load.order.phase==1,"stationed offline did not remain");
                int before=load.troops;load.Recruit(20);Check(load.troops==before,"stationed troop replenishment allowed");
                Check(load.order.path.SequenceEqual(w.order.path),"station path roundtrip");
                load.RecallArmy();Check(load.order.returning&&!load.IsGuarded(t),"recall did not clear guard");load.Tick(1000);Check(load.order==null,"garrison return incomplete");Valid(load);
            }
            {
                var w=Fresh();int t=OwnNext(w);var expected=w.FarmYield(t);double start=w.resources.grain;var rate=w.HourlyIncome();
                w.Farm(t);Check(w.AvailableCommands==18,"farm did not spend two orders");double travel=w.order.duration;
                w.Tick(travel);Check(w.order.phase==2&&!w.order.returning,"farm work did not start");
                var load=Copy(w);Valid(load);load.Tick(World.FarmSeconds+travel);
                Check(load.order==null,"farm did not return");
                Near(load.resources.grain,start+rate.grain*(2*travel+World.FarmSeconds)/3600+expected.grain,"farm yield or income duplicated");
                Near(load.resources.grass,w.resources.grass+rate.grass*(World.FarmSeconds+travel)/3600+expected.grass,"farm forage yield wrong");
                var baseline=load.resources.grain;load.Tick(100);Near(load.resources.grain,baseline+rate.grain*100/3600,"farm rewarded twice");Valid(load);
            }
            {
                var w=Fresh();int t=OwnNext(w);w.Garrison(t);w.Tick(w.order.duration*.45);double x,z;w.ArmyPosition(out x,out z);
                w.RecallArmy();double rx,rz;w.ArmyPosition(out rx,out rz);Near(x,rx,"mid-march recall x jumps");Near(z,rz,"mid-march recall z jumps");
                var load=Copy(w);Valid(load);load.ArmyPosition(out rx,out rz);Near(x,rx,"return x after load");load.Tick(100);Check(load.order==null,"midroute return incomplete");
            }
            {
                var w=Fresh();int t=OwnNext(w);w.Farm(t);w.Tick(w.order.remaining+40);var rate=w.HourlyIncome();double before=w.resources.grain;
                w.RecallArmy();w.Tick(1000);Near(w.resources.grain,before+rate.grain*1000/3600,"cancelled farm rewarded");Check(w.AvailableCommands==18,"cancelled farm refunded orders");
                w.Tick(2600);Check(w.AvailableCommands==19,"command recovery incorrect");w.Tick(72000);Check(w.AvailableCommands==20&&w.commandRecovery==0,"command cap broken");
            }
            {
                var w=Fresh();int t=OwnNext(w);w.EnsureSeasonState();w.commands=1;int count=w.AvailableCommands;
                w.Farm(t);Check(w.order==null&&w.AvailableCommands==count,"failed farm mutated state");
                w.Garrison(-1);Check(w.order==null,"invalid garrison accepted");
                var city=w.lands.First(x=>x.capital);city.owned=true;w.Farm(city.id);Check(w.order==null,"farmed city");
                var water=w.lands.First(x=>!x.Walkable);water.owned=true;w.Garrison(water.id);Check(w.order==null,"garrison water");
                w.commands=20;w.Farm(t);w.Tick(w.order.remaining+1);w.lands[t].owned=false;var rate=w.HourlyIncome();double before=w.resources.grain;
                w.Tick(1000);Near(w.resources.grain,before+rate.grain*1000/3600,"lost farm target rewarded");Check(w.order==null,"lost target did not return");
            }
            {
                var w=Fresh();int t=OwnNext(w);w.Garrison(t);w.order.path[1]=w.lands.First(x=>!x.Walkable).id;string p;Check(!w.IsValid(out p),"corrupt route accepted");
                w=Fresh();t=OwnNext(w);w.Garrison(t);w.order.routeStart=double.NaN;Check(!w.IsValid(out p),"NaN route accepted");
                w=Fresh();w.commands=21;Check(!w.IsValid(out p),"invalid command count accepted");
            }
            {
                var w=Fresh();int t=w.lands.First(x=>w.AttackProblem(x.id)=="").id;
                w.order=new ArmyOrder{target=t,duration=12,remaining=6};Valid(w);w.Tick(30);Check(w.order==null&&w.reports.Count==1,"v2 pre-S1 order incompatible");
                Check(w.AvailableCommands==20,"old campaign commands not initialized");
                w=World.Create(Homeland.Han,4,"Legacy");w.Garrison(w.homeId);Check(w.order==null&&w.seasonMechanicsVersion==0,"legacy was changed");
            }
            for(int routeVersion=0;routeVersion<=1;routeVersion++)
            {
                var w=Fresh();var home=w.lands[w.homeId];var t=w.TryGetLand(home.x+1,home.z);
                w.cityVersion=0;w.cityPlots=null;w.cityConstruction=null;w.cityClaimedQuests=null;t.owned=false;
                w.order=new ArmyOrder{target=t.id,duration=12,remaining=6,routeVersion=routeVersion,
                    path=routeVersion==0?null:new System.Collections.Generic.List<int>{home.id,t.id},routeStart=0,routeEnd=1};
                Valid(w);int troops=w.troops,reports=w.reports.Count;var experience=w.cards.Sum(c=>c.experience);
                w.Tick(6);Check(t.owned&&w.order.returning,"old attack did not become a peaceful city return");
                Check(w.troops==troops&&w.reports.Count==reports&&w.cards.Sum(c=>c.experience)==experience,"city migration caused friendly battle or battle rewards");
                var restored=Copy(w);Valid(restored);restored.Tick(100);Check(restored.order==null&&restored.troops==troops,"old city return lost troops or failed");
            }
            {
                var w=Fresh();var home=w.lands[w.homeId];var t=w.TryGetLand(home.x+1,home.z);
                w.cityVersion=0;w.cityPlots=null;w.cityConstruction=null;w.cityClaimedQuests=null;t.owned=true;
                w.order=new ArmyOrder{target=t.id,action=(int)ArmyAction.Farm,phase=(int)ArmyPhase.Working,duration=90,remaining=45,
                    routeVersion=1,path=new System.Collections.Generic.List<int>{home.id,t.id},routeStart=0,routeEnd=1};
                Valid(w);w.Tick(45);Check(w.order.returning&&w.cityFarmCompletions==0,"migrated city farmland counted as a completed farm");
                w.Tick(100);Check(w.order==null,"migrated city farm did not return");Valid(w);
            }
            Console.WriteLine("S1_ORDER_CHECKS_OK: "+checks);return 0;
        }catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
